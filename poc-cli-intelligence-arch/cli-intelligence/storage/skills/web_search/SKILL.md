---
name: web_search
description: Search the web using DuckDuckGo instant answers.
version: 1.0.0
metadata:
  openclaw:
    os: [win32, darwin, linux]
    emoji: 🔍
---

# Web Search Tool

Search the web using DuckDuckGo's Instant Answer API. No API key required.

## Parameters

- **query** (required): The search query
- **max_results**: Maximum number of results to return (1-10, default 5)

## Usage

```
[TOOL: web_search query="PowerShell get file hash"]
[TOOL: web_search query="C# record type" max_results=3]
```

## Limitations

- Uses DuckDuckGo Instant Answer API which provides summaries and related topics.
- For deep web crawling or full-page content, use `http_request` on a specific URL.
- Timeout: 10 seconds.
