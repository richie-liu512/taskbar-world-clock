# Taskbar World Clock

[English](README.md)

Taskbar World Clock 是一款轻量级 Windows 任务栏时钟叠加工具，用来显示一个不同于系统时间的时区。

这个工具最初是为开发者做的：有些时候需要让系统时间保持在一个地区，但又要随时查看另一个地区的工作时间。例如跨国协作、查看海外同事时间，或者使用某些依赖本地系统时间的工具时，都可以用它补充显示另一个时区。

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

## 后续方向

- 日历视图
- 秒表
- 计时器
- 更多任务栏集成选项

## 反馈

欢迎通过 GitHub Issues 提交建议和问题反馈。

## 许可证

MIT License。详见 [LICENSE](LICENSE)。
