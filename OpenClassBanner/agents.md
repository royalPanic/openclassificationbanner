# Open Class Banner Implementation Plan

## Purpose
Build a Windows desktop banner that remains visible at the top of each connected display and reserves that area so maximized applications do not overlap it.

## Decisions
- C# WPF targeting .NET 8.
- Started manually in an interactive user session.
- One top-edge banner window per display.
- Register each banner with the Windows AppBar API (`SHAppBarMessage`).
- Read `config.json` beside the executable, creating a default file if missing.
- Reload valid configuration changes automatically and offer manual reload through the tray icon.
- Provide a system-tray menu with Reload Config and Exit commands.
- Write basic rotating logs under `%LOCALAPPDATA%\OpenClassBanner\logs`.
- Publish as a self-contained single-file Windows x64 executable.

## Configuration
`config.json` supports banner text, background and foreground hex colors, banner height, font size, and font family. Invalid changes are logged and ignored; the last valid configuration remains active.

## Runtime behavior
- Enumerate connected monitors and create one banner per monitor.
- Reserve the top edge of each display using the AppBar API; log registration failures and retain a visible overlay fallback.
- Unregister AppBars and dispose the watcher and tray icon during graceful exit.

## Validation
Test startup and default config generation, multi-monitor placement and work-area reservation, live config reload (including malformed JSON), tray reload/exit, DPI scaling, graceful shutdown, and single-file publishing on Windows 10/11.
