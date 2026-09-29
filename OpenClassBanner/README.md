# Open Class Banner

A .NET 8 WPF application that displays a top-edge classification banner

## Run

Start `OpenClassBanner.exe` manually in the interactive Windows session.

## Configuration

```json
{
  "Name": "Example Configuration",
	"LeftText": "",
  "CenterText": "UNCLASSIFIED",
  "RightText": "",
  "BackgroundColor": "#007A33",
  "ForegroundColor": "#FFFFFF",
  "HeightPx": 36,
  "FontSize": 16,
	"VerticalTextOffsetPx": 6,
  "FontFamily": "Segoe UI"
}
```

`LeftText`, `CenterText`, and `RightText` control the left-, center-, and right-aligned banner elements independently. Existing configuration files containing only `BannerText` continue to use that value for the centered element.

`VerticalTextOffsetPx` controls the base upward optical-centering adjustment. It scales proportionally with `FontSize`; the default value of `6` preserves the standard appearance at font size `16`.

Create additional profiles by copying the example file and giving each copy a different filename, such as `unclassified.json` or `classified.json`. Change the `Name` property in each file to control the label shown in the tray Profiles submenu. Profiles are discovered and reloaded automatically when files are created, edited, renamed, or deleted.

Logs are written to `%LOCALAPPDATA%\OpenClassBanner\logs` with a 5 MB per-file limit and three retained files.

## Build and publish

Build the `OpenClassBanner.csproj` project in Visual Studio 2022 or newer

## Notes

The app requires an interactive desktop session and is intended to be launched manually. AppBar placement and interactions with other AppBars or auto-hide taskbars should be validated on the target Windows configuration.
