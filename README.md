# Taskbar World Clock

[简体中文](README.zh-CN.md)

Taskbar World Clock is a lightweight Windows taskbar clock overlay for showing another time zone without changing your system time.

It was originally built for developers who need to keep their system time in one region while checking another region's working hours, such as teammates in a different country or tools that depend on local system time.

## Features

- Show a configurable time zone on the Windows taskbar.
- Search Windows time zones by city, country, English name, localized name, or UTC offset.
- Switch between 24-hour and 12-hour time formats.
- Customize layout, date format, weekday display, fonts, text colors, and background color.
- Pick a color directly from the screen to match the taskbar or desktop.
- Set widget position, size, and coordinates.
- Auto-collapse on hover to avoid blocking content.
- Optional click-through mode.
- Optional start with Windows.
- Settings persist in `%LOCALAPPDATA%\TaskbarWorldClock\settings.xml`.

## Preview

<table>
  <tr>
    <td width="50%">
      <strong>Time Zone Search</strong><br>
      <img src="docs/media/timezone-search.gif" width="360" alt="Time zone search">
    </td>
    <td width="50%">
      <strong>Time Format And Position</strong><br>
      <img src="docs/media/time-format-position.gif" width="360" alt="Time format and position">
    </td>
  </tr>
  <tr>
    <td width="50%">
      <strong>Screen Color Picker</strong><br>
      <img src="docs/media/screen-color-picker.gif" width="360" alt="Screen color picker">
    </td>
    <td width="50%">
      <strong>Auto Collapse</strong><br>
      <img src="docs/media/auto-collapse.gif" width="360" alt="Auto collapse">
    </td>
  </tr>
</table>

<details>
<summary>More previews</summary>

### Click Through

<img src="docs/media/click-through.gif" width="520" alt="Click through">

### Multi Language

<img src="docs/media/multi-language.gif" width="520" alt="Multi language">

</details>

## Download

Download the latest executable from [GitHub Releases](https://github.com/richie-liu512/taskbar-world-clock/releases).

The release file is:

```text
TaskbarWorldClock.exe
```

## Build

Run in PowerShell:

```powershell
.\build.ps1
```

The executable is generated at:

```text
dist\TaskbarWorldClock.exe
```

## Usage

Open the settings panel from the tray icon, right-click the widget, double-click the widget, or run:

```powershell
.\dist\TaskbarWorldClock.exe --settings
```

## Roadmap

- Calendar view
- Stopwatch
- Timer
- More taskbar integration options

## Feedback

Suggestions and bug reports are welcome through GitHub Issues.

## License

MIT License. See [LICENSE](LICENSE).

## Other Languages

- Simplified Chinese: Taskbar World Clock is a Windows taskbar world clock tool for showing another time zone.
- Traditional Chinese: Taskbar World Clock is a Windows taskbar world clock tool for showing another time zone.
- Japanese: Taskbar World Clock shows another time zone on the Windows taskbar.
- Korean: Taskbar World Clock shows another time zone on the Windows taskbar.
- German: Taskbar World Clock shows another time zone on the Windows taskbar.
- French: Taskbar World Clock shows another time zone on the Windows taskbar.
- Spanish: Taskbar World Clock shows another time zone on the Windows taskbar.
- Portuguese: Taskbar World Clock shows another time zone on the Windows taskbar.
- Russian: Taskbar World Clock shows another time zone on the Windows taskbar.
