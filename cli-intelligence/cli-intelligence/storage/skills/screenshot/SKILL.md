---
name: screenshot
description: Capture a screenshot of the screen, active window, or active monitor. Saves the image, copies to clipboard, and provides image data for vision analysis.
os: win32
---

# Screenshot Tool

Use the `screenshot` tool when the user asks to capture, screenshot, snap, or photograph their screen.

## Capture Modes

- `full_screen` — captures the entire virtual screen (all monitors combined)
- `active_window` — captures only the currently focused/foreground window
- `active_monitor` — captures the full monitor that contains the active window

## Invocation

```
[TOOL: screenshot mode=full_screen]
[TOOL: screenshot mode=active_window]
[TOOL: screenshot mode=active_monitor]
```

If the user does not specify a mode, ask which they prefer or default to `full_screen`.

## Output

The tool saves a PNG to `data/screenshots/`, copies it to the clipboard, and returns the image data.
When image data is returned, you can describe what you see in the screenshot if the user asks.

## When to Use

- User says "take a screenshot", "capture my screen", "show me what's on screen"
- User wants to share or analyze what they currently see
- User asks you to look at something on their screen
