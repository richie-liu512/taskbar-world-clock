# Taskbar World Clock

[简体中文](README.zh-CN.md)

Taskbar World Clock is a lightweight Windows taskbar overlay for watching another time zone without changing your system clock.

It is built for people who need a second time reference that stays visible: cross-time-zone collaboration, overseas working hours, remote teams, or developer tools that depend on the local system time staying unchanged.

## Why It Exists

Changing the Windows system time is a blunt workaround. It can affect logs, scheduled jobs, authentication flows, and tools that assume local time is stable.

Taskbar World Clock keeps your system clock untouched and adds a small, configurable time-zone display on top of the taskbar. The design goal is to make another time zone glanceable without turning it into a full calendar app or a distracting desktop widget.

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
- Built-in UI localization for multiple languages.
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

## Language Support

Taskbar World Clock includes built-in UI localization for Simplified Chinese, Traditional Chinese, Japanese, Korean, German, French, Spanish, Portuguese, and Russian.

The project keeps English as the primary README language and provides a Simplified Chinese README. Additional README translations are not maintained for now because the app itself is already localized.

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
