# Copyright 2026 GildenEye
# SPDX-License-Identifier: Apache-2.0

import gzip
import json
import ssl
import zlib
import asyncio
from dataclasses import dataclass
from urllib.parse import urlparse
from typing import Any, AsyncGenerator
from .types import AIFuncConfig, ModelRequestParams, ModelResponse

_MAX_REDIRECTS = 5
_MAX_CHUNK_SIZE = 16 * 1024 * 1024
_MAX_BODY_SIZE = 64 * 1024 * 1024
_MAX_HEADER_LINE = 16 * 1024

_ENGINE_DEFAULT_TIMEOUT: float = 7.0
_ENGINE_DEFAULT_MAX_RETRIES: int = 1


@dataclass
class ProjectDefaults:
    """Subset of aifunc.json fields injected at build time by the CLI."""
    timeout: float | None = None
    max_retries: int | None = None


def _same_origin(url_a: str, url_b: str) -> bool:
    a = urlparse(url_a)
    b = urlparse(url_b)
    port_a = a.port or (443 if a.scheme == "https" else 80)
    port_b = b.port or (443 if b.scheme == "https" else 80)
    return (a.scheme == b.scheme and a.hostname == b.hostname and port_a == port_b)


def _resolve_url(base_url: str) -> str:
    base = base_url.rstrip("/")
    if base.endswith("/chat/completions"):
        return base
    return base + "/chat/completions"


async def send_request(
    config: AIFuncConfig,
    params: ModelRequestParams,
    project_defaults: ProjectDefaults | None = None,
) -> ModelResponse:
    if not config.base_url:
        raise ValueError("AIFuncConfig.base_url is required when mock mode is disabled")
    if not config.api_key:
        raise ValueError("AIFuncConfig.api_key is required when mock mode is disabled")

    url = _resolve_url(config.base_url)

    pd = project_defaults or ProjectDefaults()
    timeout_sec: float = config.timeout if config.timeout is not None else (pd.timeout if pd.timeout is not None else _ENGINE_DEFAULT_TIMEOUT)
    max_retries: int = config.max_retries if config.max_retries is not None else (pd.max_retries if pd.max_retries is not None else _ENGINE_DEFAULT_MAX_RETRIES)

    body: dict[str, Any] = {
        "model": params.model,
        "messages": params.messages,
    }
    if params.response_format:
        body["response_format"] = params.response_format
    if params.temperature is not None:
        body["temperature"] = params.temperature
    if params.top_p is not None:
        body["top_p"] = params.top_p
    if params.max_tokens is not None:
        body["max_tokens"] = params.max_tokens

    data = json.dumps(body).encode("utf-8")

    last_error: Exception = RuntimeError("Unknown error during model request")
    for attempt in range(max_retries + 1):
        try:
            status, response_body = await asyncio.wait_for(
                _request_with_redirects(url, data, config.api_key),
                timeout=timeout_sec,
            )
        except asyncio.TimeoutError:
            last_error = RuntimeError(f"Request timeout after {timeout_sec}s")
            if attempt < max_retries:
                continue
            raise last_error from None
        except Exception as e:
            last_error = e
            if attempt < max_retries:
                continue
            raise

        if status >= 400:
            last_error = RuntimeError(f"Model API returned {status}: {response_body[:500]}")
            if attempt < max_retries:
                continue
            raise last_error

        response_data = json.loads(response_body)
        return ModelResponse.from_dict(response_data)

    raise last_error


async def send_stream_request(
    config: AIFuncConfig,
    params: ModelRequestParams,
    project_defaults: ProjectDefaults | None = None,
    cancel_event: asyncio.Event | None = None,
) -> AsyncGenerator[str, None]:
    """Yields raw text tokens from a Server-Sent Events streaming response."""
    if not config.base_url:
        raise ValueError("AIFuncConfig.base_url is required when mock mode is disabled")
    if not config.api_key:
        raise ValueError("AIFuncConfig.api_key is required when mock mode is disabled")

    url = _resolve_url(config.base_url)
    pd = project_defaults or ProjectDefaults()
    timeout_sec: float = (
        config.timeout if config.timeout is not None
        else (pd.timeout if pd.timeout is not None else _ENGINE_DEFAULT_TIMEOUT)
    )

    body: dict[str, Any] = {
        "model": params.model,
        "messages": params.messages,
        "stream": True,
    }
    if params.temperature is not None:
        body["temperature"] = params.temperature
    if params.top_p is not None:
        body["top_p"] = params.top_p
    if params.max_tokens is not None:
        body["max_tokens"] = params.max_tokens

    data = json.dumps(body).encode("utf-8")

    parsed = urlparse(url)
    use_tls = parsed.scheme == "https"
    host = parsed.hostname or ""
    port = parsed.port or (443 if use_tls else 80)
    path = parsed.path or "/"
    if parsed.query:
        path += "?" + parsed.query

    host_header = host if port in (80, 443) else f"{host}:{port}"
    headers: dict[str, str] = {
        "Host": host_header,
        "Content-Type": "application/json",
        "Content-Length": str(len(data)),
        "Authorization": f"Bearer {config.api_key}",
        "Accept": "text/event-stream",
        "Cache-Control": "no-cache",
        "Connection": "close",
    }

    ssl_ctx: ssl.SSLContext | None = None
    if use_tls:
        ssl_ctx = ssl.create_default_context()

    try:
        reader, writer = await asyncio.wait_for(
            asyncio.open_connection(host, port, ssl=ssl_ctx),
            timeout=timeout_sec,
        )
    except asyncio.TimeoutError:
        raise RuntimeError(f"Stream connection timeout after {timeout_sec}s") from None

    try:
        request_line = f"POST {path} HTTP/1.1\r\n"
        header_lines = "".join(f"{k}: {v}\r\n" for k, v in headers.items())
        raw_request = (request_line + header_lines + "\r\n").encode("utf-8") + data
        writer.write(raw_request)
        await writer.drain()

        status, resp_headers = await asyncio.wait_for(
            _read_response_headers(reader),
            timeout=timeout_sec,
        )
        if status >= 400:
            body_bytes = await asyncio.wait_for(
                _read_response_body(reader, resp_headers),
                timeout=timeout_sec,
            )
            raise RuntimeError(
                f"Model API returned {status}: "
                f"{_decode_body(body_bytes, resp_headers)[:500]}"
            )

        return _read_sse_stream(reader, writer, cancel_event)
    except Exception:
        writer.close()
        raise


async def _read_sse_stream(
    reader: asyncio.StreamReader,
    writer: asyncio.StreamWriter,
    cancel_event: asyncio.Event | None,
) -> AsyncGenerator[str, None]:
    buffer = ""
    try:
        while True:
            if cancel_event is not None and cancel_event.is_set():
                return

            try:
                chunk = await asyncio.wait_for(reader.read(4096), timeout=60.0)
            except asyncio.TimeoutError:
                raise RuntimeError("Stream stalled: no data received for 60s")
            except Exception:
                return

            if not chunk:
                return

            buffer += chunk.decode("utf-8", errors="replace")
            lines = buffer.split("\n")
            buffer = lines.pop()

            for line in lines:
                if cancel_event is not None and cancel_event.is_set():
                    return
                stripped = line.rstrip("\r")
                if not stripped.startswith("data:"):
                    continue
                payload = stripped[5:].strip()
                if payload == "[DONE]":
                    return
                try:
                    obj = json.loads(payload)
                    content = (
                        obj.get("choices", [{}])[0]
                        .get("delta", {})
                        .get("content")
                    )
                    if content:
                        yield content
                except (json.JSONDecodeError, IndexError, KeyError):
                    pass
    finally:
        writer.close()


async def _request_with_redirects(
    url: str, body: bytes, api_key: str
) -> tuple[int, str]:
    original_url = url

    for _ in range(_MAX_REDIRECTS):
        parsed = urlparse(url)
        use_tls = parsed.scheme == "https"
        host = parsed.hostname or ""
        port = parsed.port or (443 if use_tls else 80)
        path = parsed.path or "/"
        if parsed.query:
            path += "?" + parsed.query

        host_header = host if port in (80, 443) else f"{host}:{port}"
        is_same_origin = _same_origin(original_url, url)

        headers: dict[str, str] = {
            "Host": host_header,
            "Content-Type": "application/json",
            "Content-Length": str(len(body)),
            "Accept-Encoding": "gzip, deflate",
            "Connection": "close",
        }
        if is_same_origin:
            headers["Authorization"] = f"Bearer {api_key}"

        status, resp_headers, raw_body = await _do_request(
            host, port, path, headers, body, use_tls
        )

        if 300 <= status < 400:
            location = resp_headers.get("location")
            if not location:
                raise RuntimeError(f"Redirect {status} without Location header")
            if location.startswith("/"):
                url = f"{parsed.scheme}://{host_header}{location}"
            else:
                url = location
            continue

        decoded = _decode_body(raw_body, resp_headers)
        return status, decoded

    raise RuntimeError(f"Too many redirects (max {_MAX_REDIRECTS})")


def _decode_body(raw: bytes, headers: dict[str, str]) -> str:
    encoding = headers.get("content-encoding", "").lower()
    if encoding == "gzip":
        raw = gzip.decompress(raw)
    elif encoding == "deflate":
        try:
            raw = zlib.decompress(raw)
        except zlib.error:
            raw = zlib.decompress(raw, -zlib.MAX_WBITS)
    return raw.decode("utf-8")


async def _do_request(
    host: str,
    port: int,
    path: str,
    headers: dict[str, str],
    body: bytes,
    use_tls: bool,
) -> tuple[int, dict[str, str], bytes]:
    ssl_ctx: ssl.SSLContext | None = None
    if use_tls:
        ssl_ctx = ssl.create_default_context()

    reader, writer = await asyncio.open_connection(host, port, ssl=ssl_ctx)

    try:
        request_line = f"POST {path} HTTP/1.1\r\n"
        header_lines = "".join(f"{k}: {v}\r\n" for k, v in headers.items())
        raw_request = (request_line + header_lines + "\r\n").encode("utf-8") + body

        writer.write(raw_request)
        await writer.drain()

        status, resp_headers = await _read_response_headers(reader)
        response_body = await _read_response_body(reader, resp_headers)
        return status, resp_headers, response_body
    finally:
        writer.close()
        await writer.wait_closed()


async def _read_line_limited(reader: asyncio.StreamReader) -> bytes:
    line = await reader.readline()
    if len(line) > _MAX_HEADER_LINE:
        raise RuntimeError(
            f"Header line exceeds maximum length ({_MAX_HEADER_LINE} bytes)"
        )
    return line


async def _read_response_headers(reader: asyncio.StreamReader) -> tuple[int, dict[str, str]]:
    status_line = await _read_line_limited(reader)
    if not status_line:
        raise RuntimeError("Empty response from server")

    parts = status_line.decode("utf-8").split(" ", 2)
    if len(parts) < 2:
        raise RuntimeError(f"Malformed status line: {status_line!r}")

    try:
        status = int(parts[1])
    except ValueError:
        raise RuntimeError(f"Malformed status code: {parts[1]!r}")

    headers: dict[str, str] = {}
    while True:
        line = await _read_line_limited(reader)
        if not line or line in (b"\r\n", b"\n"):
            break
        decoded = line.decode("utf-8").strip()
        if not decoded:
            break
        key, _, value = decoded.partition(":")
        headers[key.strip().lower()] = value.strip()

    return status, headers


async def _read_response_body(reader: asyncio.StreamReader, headers: dict[str, str]) -> bytes:
    transfer_encoding = headers.get("transfer-encoding", "")
    if "chunked" in transfer_encoding.lower():
        return await _read_chunked(reader)

    content_length = headers.get("content-length")
    if content_length is not None:
        length = int(content_length)
        if length > _MAX_BODY_SIZE:
            raise RuntimeError(f"Response body too large: {length} bytes")
        return await reader.readexactly(length)

    chunks = []
    total = 0
    while True:
        chunk = await reader.read(65536)
        if not chunk:
            break
        total += len(chunk)
        if total > _MAX_BODY_SIZE:
            raise RuntimeError(f"Response body exceeded {_MAX_BODY_SIZE} bytes")
        chunks.append(chunk)
    return b"".join(chunks)


async def _read_chunked(reader: asyncio.StreamReader) -> bytes:
    body = bytearray()
    while True:
        size_line = await _read_line_limited(reader)
        if not size_line:
            raise RuntimeError("Connection closed while reading chunk size")

        size_str = size_line.strip()
        if not size_str:
            raise RuntimeError("Empty chunk size line")

        if b";" in size_str:
            size_str = size_str.split(b";")[0]

        try:
            chunk_size = int(size_str, 16)
        except ValueError:
            raise RuntimeError(f"Invalid chunk size: {size_line!r}")

        if chunk_size == 0:
            await _read_line_limited(reader)
            break

        if chunk_size > _MAX_CHUNK_SIZE:
            raise RuntimeError(f"Chunk size {chunk_size} exceeds limit")
        if len(body) + chunk_size > _MAX_BODY_SIZE:
            raise RuntimeError(f"Chunked body exceeded {_MAX_BODY_SIZE} bytes")

        chunk = await reader.readexactly(chunk_size)
        body.extend(chunk)
        await _read_line_limited(reader)

    return bytes(body)
