# Taskbar World Clock

[English](README.md)

Taskbar World Clock 是一个 Windows 任务栏的小工具。它不会修改系统时间，只是在任务栏上额外显示一个你关心的时区。

典型场景包括：跨时区协作时确认对方的工作时间；使用 Claude 这类要求时区与网络环境保持一致的工具。

组件本身尽量像任务栏的一部分：可以调整位置和大小，放在不遮挡系统时间的位置；可以用屏幕取色匹配任务栏背景；可以自定义时间日期格式；需要空间时也可以快速收起。

## 功能

- 支持按城市、国家、本地化名称或 UTC 偏移搜索时区。
- 支持自定义时间/日期格式、星期显示、字体、颜色、位置和大小。
- 支持屏幕取色，让组件背景更容易匹配任务栏。
- 支持悬停自动收起，也可以使用点击穿透模式，减少对当前工作的遮挡。
- 支持开机自启动。

## 预览

<table>
  <tr>
    <td width="50%">
      <strong>时区搜索</strong><br>
      <img src="docs/media/timezone-search.gif" width="360" alt="时区搜索">
    </td>
    <td width="50%">
      <strong>时间格式和位置</strong><br>
      <img src="docs/media/time-format-position.gif" width="360" alt="时间格式和位置">
    </td>
  </tr>
  <tr>
    <td width="50%">
      <strong>屏幕取色</strong><br>
      <img src="docs/media/screen-color-picker.gif" width="360" alt="屏幕取色">
    </td>
    <td width="50%">
      <strong>自动收起</strong><br>
      <img src="docs/media/auto-collapse.gif" width="360" alt="自动收起">
    </td>
  </tr>
</table>

<details>
<summary>更多预览</summary>

### 点击穿透

<img src="docs/media/click-through.gif" width="520" alt="点击穿透">

### 多语言

<img src="docs/media/multi-language.gif" width="520" alt="多语言">

</details>

## 下载

从 [GitHub Releases](https://github.com/richie-liu512/taskbar-world-clock/releases) 下载最新版。

发布文件为：

```text
TaskbarWorldClock.exe
```

## 构建

在 PowerShell 中运行：

```powershell
.\build.ps1
```

生成文件位于：

```text
dist\TaskbarWorldClock.exe
```

## 使用

可以通过托盘图标、右键点击组件、双击组件打开设置面板，也可以运行：

```powershell
.\dist\TaskbarWorldClock.exe --settings
```

## 多语言支持

Taskbar World Clock 内置多语言界面，支持简体中文、繁体中文、日语、韩语、德语、法语、西班牙语、葡萄牙语和俄语。

## 设置和本地数据

设置会保存在本机：

```text
%LOCALAPPDATA%\TaskbarWorldClock\settings.xml
```

正常使用不需要账号，也不需要网络连接。

## 后续方向

- 日历视图
- 秒表
- 计时器
- 更多任务栏集成选项

## 反馈

欢迎通过 GitHub Issues 提交建议和问题反馈。

## 许可证

MIT License。详见 [LICENSE](LICENSE)。
