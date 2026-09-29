# Troubleshooting

## Banner does not reserve the top of the display

The application logs AppBar registration failures under `%LOCALAPPDATA%\OpenClassBanner\logs`. Another AppBar, an auto-hide taskbar, or an Explorer state issue can prevent registration. Close competing desktop-toolbar applications and restart Explorer, then restart OpenClassBanner.

## Configuration changes are not applied

Confirm that the edited file is `config.json` beside the running executable. The JSON must use valid `#RRGGBB` or `#AARRGGBB` colors, a height from 12 through 200, and a font size from 8 through 96. Invalid changes are logged and the last valid configuration remains active.

## The tray icon is missing

The tray icon is created only after the application starts in an interactive Windows desktop session. Check the notification-area overflow list and verify that the process is not being launched as a noninteractive service.

## Applications appear behind or overlap the banner

AppBar reservations are managed by Explorer. Restart Explorer and then restart the banner. If registration continues to fail, the application remains visible as a topmost overlay but cannot change the system working area.

## DPI or monitor placement is incorrect

Confirm that the application manifest is included in the published executable and that Windows display scaling is configured as expected. Restart the application after changing display topology or scaling.
