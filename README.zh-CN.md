# Taskbar World Clock

[English](README.md)

Taskbar World Clock 是一个轻量级 Windows 任务栏时区小工具。它不会修改系统时间，只是在任务栏上额外显示一个你关心的时区。

它适合需要长期查看另一个时间参考的人：跨时区协作、海外工作时间、远程团队，或者某些依赖本地系统时间保持不变的开发工具。

## 为什么做这个工具

直接修改 Windows 系统时间不是一个好办法。它可能影响日志、计划任务、认证流程，以及依赖本地时间稳定的工具。

Taskbar World Clock 的思路是：不碰系统时间，只在任务栏上补充一个小而可配置的时区显示。它不是完整日历应用，也不是醒目的桌面组件，而是一个可以长期挂着、随时瞥一眼的时间参考。

## 功能

- 在 Windows 任务栏显示可配置的另一个时区。
- 支持按城市、国家、英文名、本地化名称或 UTC 偏移搜索时区。
- 支持 24 小时制和 12 小时制。
- 支持自定义时间/日期排布、日期格式、星期显示、字体、字号、文字颜色和背景颜色。
- 支持从屏幕取色，用来匹配任务栏或桌面颜色。
- 支持设置组件位置、大小和坐标。
- 支持鼠标悬停自动收起，减少遮挡。
- 支持点击穿透模式。
- 支持开机自启动。
- 内置多语言界面。
- 设置会保存在 `%LOCALAPPDATA%\TaskbarWorldClock\settings.xml`。

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

项目文档目前保留英文主 README 和简体中文 README，不单独维护每一种语言的 README；多语言能力主要体现在软件界面本身。

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
