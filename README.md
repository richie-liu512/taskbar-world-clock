# Taskbar World Clock

[简体中文](README.zh-CN.md)

Taskbar World Clock is a lightweight Windows taskbar overlay for keeping another time zone visible without changing your system clock.

It is designed to feel like part of your taskbar: place it on the left side to avoid covering the system clock, adjust its size and position, match the background color to your taskbar, choose your preferred time and date format, and collapse or bring it back quickly when you need the space.

It is useful when your system time needs to stay in one region, but you still want a quick view of another time zone: remote collaboration, overseas working hours, travel planning, or developer workflows involving Claude and other AI tools.

## Why It Exists

Sometimes changing the Windows system time is inconvenient, and sometimes you simply need to watch two time zones at once.

Taskbar World Clock keeps the system clock as it is and adds a small, configurable time-zone display on the taskbar. It is meant to be glanceable, quiet, and easy to leave running while you work.

## Features

- Show another time zone directly on the Windows taskbar.
- Search Windows time zones by city, country, localized name, or UTC offset.
- Customize time/date format, weekday display, font, colors, position, and size.
- Match the widget background to the taskbar with the screen color picker.
- Collapse on hover or use click-through mode when you do not want it blocking content.
- Optional start with Windows.
- Built-in UI localization for multiple languages.

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

## Language Support

Taskbar World Clock includes built-in UI localization for Simplified Chinese, Traditional Chinese, Japanese, Korean, German, French, Spanish, Portuguese, and Russian.

## Settings And Local Data

Settings are stored locally at:

```text
%LOCALAPPDATA%\TaskbarWorldClock\settings.xml
```

The app does not require an account or a network connection for normal use.

## Roadmap

- Calendar view
- Stopwatch
- Timer
- More taskbar integration options

## Feedback

Suggestions and bug reports are welcome through GitHub Issues.

## License

MIT License. See [LICENSE](LICENSE).
