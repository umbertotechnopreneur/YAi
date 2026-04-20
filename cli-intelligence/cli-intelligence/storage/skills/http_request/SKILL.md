---
name: http_request
description: Make HTTP requests to public web URLs with SSRF protection.
version: 1.0.0
metadata:
  openclaw:
    os: [win32, darwin, linux]
    emoji: 🌐
---

# HTTP Request Tool

Make GET, POST, PUT, or HEAD requests to public URLs.

## Parameters

- **url** (required): The URL to request
- **method**: HTTP method — GET (default), POST, PUT, HEAD
- **body**: Request body (for POST/PUT)
- **content_type**: Media type (default: application/json)
- **header**: Custom headers as comma-separated key:value pairs

## Usage

```
[TOOL: http_request url="https://api.example.com/data"]
[TOOL: http_request url="https://api.example.com/items" method=POST body="{\"name\":\"test\"}" content_type="application/json"]
```

## Safety

- Requests to private/internal IP ranges (10.x, 172.16-31.x, 192.168.x, localhost) are **blocked** to prevent SSRF.
- Timeout: 15 seconds.
- Response bodies are truncated at 4 KB.
