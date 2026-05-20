using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using CheckBoxState = System.Windows.Forms.VisualStyles.CheckBoxState;

namespace TaskbarWorldClock
{
    internal static class Program
    {
        private const string MutexName = @"Local\TaskbarWorldClock.SingleInstance";
        private const int ShowSettingsMessage = 0x8001;

        [STAThread]
        private static void Main(string[] args)
        {
            bool createdNew;
            using (var mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    if (HasArg(args, "--settings"))
                    {
                        NativeMethods.PostMessage(NativeMethods.HwndBroadcast, ShowSettingsMessage, IntPtr.Zero, IntPtr.Zero);
                    }
                    return;
                }

                Application.EnableVisualStyles();
                try { NativeMethods.SetProcessDPIAware(); } catch { }
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new ClockForm(ShowSettingsMessage, HasArg(args, "--settings")));
            }
        }

        private static bool HasArg(string[] args, string value)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal sealed class ClockSettings
    {
        public string LanguageCode = DetectDefaultLanguage();
        public string TimeZoneId = "China Standard Time";
        public string Layout = "TimeAboveDate";
        public bool Use24Hour = true;
        public string AmPmMode = "Suffix";
        public string DateFormat = "MM/dd";
        public bool ShowWeekday = true;
        public string WeekdayFormat = "ddd";
        public string TimeFontName = "Segoe UI Variable Text";
        public float TimeFontSize = 10f;
        public string DateFontName = "Segoe UI";
        public float DateFontSize = 9.25f;
        public int ForeColorArgb = Color.Black.ToArgb();
        public int TimeForeColorArgb = Color.Black.ToArgb();
        public int DateForeColorArgb = Color.Black.ToArgb();
        public int BackColorArgb = -2500390;
        public string Position = "BottomLeft";
        public int OffsetX = 0;
        public int OffsetY = 0;
        public int Width = 100;
        public int Height = 59;
        public int PaddingLeft = 0;
        public int PaddingTop = 3;
        public int PaddingRight = 0;
        public int PaddingBottom = 3;
        public int TimeOffsetX = 10;
        public int DateOffsetX = 0;
        public int X = 0;
        public int Y = 1021;
        public bool ClickThrough = false;
        public bool AutoCollapse = true;
        public int CollapseDelayMs = 900;
        public int CollapsedWidth = 18;
        public int SettingsWindowWidth = 700;
        public int SettingsWindowHeight = 660;

        private static string LegacyConfigDir
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ChinaTaskbarClock");
            }
        }

        public static string ConfigDir
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TaskbarWorldClock");
            }
        }

        private static string DetectDefaultLanguage()
        {
            string name = CultureInfo.CurrentUICulture.Name.ToLowerInvariant();
            if (name.StartsWith("zh-hant") || name == "zh-tw" || name == "zh-hk" || name == "zh-mo")
            {
                return "zh-TW";
            }
            if (name.StartsWith("zh"))
            {
                return "zh-CN";
            }
            if (name.StartsWith("ja")) return "ja-JP";
            if (name.StartsWith("ko")) return "ko-KR";
            if (name.StartsWith("de")) return "de-DE";
            if (name.StartsWith("fr")) return "fr-FR";
            if (name.StartsWith("es")) return "es-ES";
            if (name.StartsWith("pt")) return "pt-BR";
            if (name.StartsWith("ru")) return "ru-RU";
            return "en-US";
        }

        private static string NormalizeLanguageCode(string languageCode, string fallback)
        {
            if (IsSupportedLanguage(languageCode))
            {
                return languageCode;
            }
            return IsSupportedLanguage(fallback) ? fallback : "en-US";
        }

        private static bool IsSupportedLanguage(string languageCode)
        {
            return languageCode == "zh-CN"
                || languageCode == "zh-TW"
                || languageCode == "en-US"
                || languageCode == "ja-JP"
                || languageCode == "ko-KR"
                || languageCode == "de-DE"
                || languageCode == "fr-FR"
                || languageCode == "es-ES"
                || languageCode == "pt-BR"
                || languageCode == "ru-RU";
        }

        public static string ConfigPath
        {
            get { return Path.Combine(ConfigDir, "settings.xml"); }
        }

        public static ClockSettings Load()
        {
            var settings = new ClockSettings();
            MigrateLegacyConfig();
            if (!File.Exists(ConfigPath))
            {
                // First run: create the config with the current Windows UI language.
                settings.Save();
                return settings;
            }

            var doc = new XmlDocument();
            doc.Load(ConfigPath);
            XmlElement root = doc.DocumentElement;
            if (root == null)
            {
                return settings;
            }

            // Existing config: keep the user's saved language instead of following later system changes.
            settings.LanguageCode = NormalizeLanguageCode(ReadString(root, "LanguageCode", settings.LanguageCode), settings.LanguageCode);
            settings.TimeZoneId = ReadString(root, "TimeZoneId", settings.TimeZoneId);
            settings.Layout = ReadString(root, "Layout", settings.Layout);
            settings.Use24Hour = ReadBool(root, "Use24Hour", settings.Use24Hour);
            settings.AmPmMode = ReadString(root, "AmPmMode", settings.AmPmMode);
            settings.DateFormat = ReadString(root, "DateFormat", settings.DateFormat);
            settings.ShowWeekday = ReadBool(root, "ShowWeekday", settings.ShowWeekday);
            settings.WeekdayFormat = ReadString(root, "WeekdayFormat", settings.WeekdayFormat);
            settings.TimeFontName = ReadString(root, "TimeFontName", settings.TimeFontName);
            settings.TimeFontSize = ReadFloat(root, "TimeFontSize", settings.TimeFontSize);
            settings.DateFontName = ReadString(root, "DateFontName", settings.DateFontName);
            settings.DateFontSize = ReadFloat(root, "DateFontSize", settings.DateFontSize);
            settings.ForeColorArgb = ReadInt(root, "ForeColorArgb", settings.ForeColorArgb);
            settings.TimeForeColorArgb = ReadInt(root, "TimeForeColorArgb", settings.ForeColorArgb);
            settings.DateForeColorArgb = ReadInt(root, "DateForeColorArgb", settings.ForeColorArgb);
            settings.BackColorArgb = ReadInt(root, "BackColorArgb", settings.BackColorArgb);
            settings.Position = ReadString(root, "Position", settings.Position);
            settings.OffsetX = ReadInt(root, "OffsetX", settings.OffsetX);
            settings.OffsetY = ReadInt(root, "OffsetY", settings.OffsetY);
            settings.Width = ReadInt(root, "Width", settings.Width);
            settings.Height = ReadInt(root, "Height", settings.Height);
            settings.PaddingLeft = ReadInt(root, "PaddingLeft", settings.PaddingLeft);
            settings.PaddingTop = ReadInt(root, "PaddingTop", settings.PaddingTop);
            settings.PaddingRight = ReadInt(root, "PaddingRight", settings.PaddingRight);
            settings.PaddingBottom = ReadInt(root, "PaddingBottom", settings.PaddingBottom);
            settings.TimeOffsetX = ReadInt(root, "TimeOffsetX", settings.TimeOffsetX);
            settings.DateOffsetX = ReadInt(root, "DateOffsetX", settings.DateOffsetX);
            settings.X = ReadInt(root, "X", settings.X);
            settings.Y = ReadInt(root, "Y", settings.Y);
            settings.ClickThrough = ReadBool(root, "ClickThrough", settings.ClickThrough);
            settings.AutoCollapse = ReadBool(root, "AutoCollapse", settings.AutoCollapse);
            settings.CollapseDelayMs = ReadInt(root, "CollapseDelayMs", settings.CollapseDelayMs);
            settings.CollapsedWidth = ReadInt(root, "CollapsedWidth", settings.CollapsedWidth);
            settings.SettingsWindowWidth = ReadInt(root, "SettingsWindowWidth", settings.SettingsWindowWidth);
            settings.SettingsWindowHeight = ReadInt(root, "SettingsWindowHeight", settings.SettingsWindowHeight);
            return settings;
        }

        private static void MigrateLegacyConfig()
        {
            string legacy = Path.Combine(LegacyConfigDir, "settings.xml");
            if (File.Exists(ConfigPath) || !File.Exists(legacy))
            {
                return;
            }

            Directory.CreateDirectory(ConfigDir);
            File.Copy(legacy, ConfigPath, false);
        }

        public void Save()
        {
            Directory.CreateDirectory(ConfigDir);
            var doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Settings");
            doc.AppendChild(root);

            Write(doc, root, "LanguageCode", LanguageCode);
            Write(doc, root, "TimeZoneId", TimeZoneId);
            Write(doc, root, "Layout", Layout);
            Write(doc, root, "Use24Hour", Use24Hour.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "AmPmMode", AmPmMode);
            Write(doc, root, "DateFormat", DateFormat);
            Write(doc, root, "ShowWeekday", ShowWeekday.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "WeekdayFormat", WeekdayFormat);
            Write(doc, root, "TimeFontName", TimeFontName);
            Write(doc, root, "TimeFontSize", TimeFontSize.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "DateFontName", DateFontName);
            Write(doc, root, "DateFontSize", DateFontSize.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "ForeColorArgb", ForeColorArgb.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "TimeForeColorArgb", TimeForeColorArgb.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "DateForeColorArgb", DateForeColorArgb.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "BackColorArgb", BackColorArgb.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "Position", Position);
            Write(doc, root, "OffsetX", OffsetX.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "OffsetY", OffsetY.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "Width", Width.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "Height", Height.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "PaddingLeft", PaddingLeft.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "PaddingTop", PaddingTop.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "PaddingRight", PaddingRight.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "PaddingBottom", PaddingBottom.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "TimeOffsetX", TimeOffsetX.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "DateOffsetX", DateOffsetX.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "X", X.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "Y", Y.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "ClickThrough", ClickThrough.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "AutoCollapse", AutoCollapse.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "CollapseDelayMs", CollapseDelayMs.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "CollapsedWidth", CollapsedWidth.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "SettingsWindowWidth", SettingsWindowWidth.ToString(CultureInfo.InvariantCulture));
            Write(doc, root, "SettingsWindowHeight", SettingsWindowHeight.ToString(CultureInfo.InvariantCulture));
            doc.Save(ConfigPath);
        }

        private static void Write(XmlDocument doc, XmlElement root, string name, string value)
        {
            XmlElement node = doc.CreateElement(name);
            node.InnerText = value ?? "";
            root.AppendChild(node);
        }

        private static string ReadString(XmlElement root, string name, string fallback)
        {
            XmlNode node = root.SelectSingleNode(name);
            return node == null ? fallback : node.InnerText;
        }

        private static bool ReadBool(XmlElement root, string name, bool fallback)
        {
            bool value;
            return bool.TryParse(ReadString(root, name, ""), out value) ? value : fallback;
        }

        private static int ReadInt(XmlElement root, string name, int fallback)
        {
            int value;
            return int.TryParse(ReadString(root, name, ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : fallback;
        }

        private static float ReadFloat(XmlElement root, string name, float fallback)
        {
            float value;
            return float.TryParse(ReadString(root, name, ""), NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : fallback;
        }
    }

    internal sealed class ClockForm : Form
    {
        public const string WindowTitle = "TaskbarWorldClock.MainWindow";

        private static readonly IntPtr HwndTopMost = new IntPtr(-1);
        private readonly int showSettingsMessage;
        private readonly Label timeLabel;
        private readonly Label dateLabel;
        private readonly Label collapseLabel;
        private readonly TableLayoutPanel panel;
        private readonly System.Windows.Forms.Timer timer;
        private readonly System.Windows.Forms.Timer hoverTimer;
        private readonly System.Windows.Forms.Timer animationTimer;
        private readonly NotifyIcon notifyIcon;
        private readonly ContextMenuStrip widgetMenu;
        private readonly ToolStripMenuItem settingsMenuItem;
        private readonly ToolStripMenuItem exitMenuItem;
        private readonly NativeMethods.LowLevelMouseProc menuMouseProc;
        private ClockSettings settings;
        private ModernSettingsForm settingsForm;
        private bool collapsed;
        private bool menuOpen;
        private IntPtr menuMouseHook = IntPtr.Zero;
        private int currentWidth;
        private int targetWidth;
        private DateTime hoverStartedUtc;
        private bool hoverTracking;

        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;

        public ClockForm(int showSettingsMessage, bool openSettings)
        {
            this.showSettingsMessage = showSettingsMessage;
            settings = ClockSettings.Load();
            currentWidth = settings.Width;
            targetWidth = settings.Width;

            Text = WindowTitle;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            ShowIcon = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Opacity = 1;

            panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Margin = new Padding(0)
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            timeLabel = CreateLabel(ContentAlignment.BottomLeft);
            dateLabel = CreateLabel(ContentAlignment.TopLeft);
            collapseLabel = CreateLabel(ContentAlignment.MiddleCenter);
            collapseLabel.Text = ">";
            collapseLabel.Font = CreateFont("Segoe UI Symbol", 13.0f);
            collapseLabel.Visible = false;
            collapseLabel.Paint += DrawCollapseEdge;
            panel.Controls.Add(timeLabel, 0, 0);
            panel.Controls.Add(dateLabel, 0, 1);
            Controls.Add(panel);
            Controls.Add(collapseLabel);
            collapseLabel.BringToFront();

            settingsMenuItem = new ToolStripMenuItem();
            settingsMenuItem.Click += delegate { ShowSettings(); };
            exitMenuItem = new ToolStripMenuItem();
            exitMenuItem.Click += delegate { Close(); };
            widgetMenu = CreateTrayMenu();
            menuMouseProc = MenuMouseHookCallback;
            widgetMenu.Opening += delegate { menuOpen = true; InstallMenuMouseHook(); };
            widgetMenu.Closed += delegate { menuOpen = false; hoverTracking = false; UninstallMenuMouseHook(); };
            ContextMenuStrip = widgetMenu;
            panel.ContextMenuStrip = widgetMenu;
            timeLabel.ContextMenuStrip = widgetMenu;
            dateLabel.ContextMenuStrip = widgetMenu;
            collapseLabel.ContextMenuStrip = widgetMenu;
            MouseClick += ClockMouseClick;
            panel.MouseClick += ClockMouseClick;
            timeLabel.MouseClick += ClockMouseClick;
            dateLabel.MouseClick += ClockMouseClick;
            collapseLabel.MouseClick += ClockMouseClick;
            MouseDoubleClick += ClockMouseDoubleClick;
            panel.MouseDoubleClick += ClockMouseDoubleClick;
            timeLabel.MouseDoubleClick += ClockMouseDoubleClick;
            dateLabel.MouseDoubleClick += ClockMouseDoubleClick;
            collapseLabel.MouseDoubleClick += ClockMouseDoubleClick;

            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "Taskbar World Clock",
                Visible = true,
                ContextMenuStrip = widgetMenu
            };
            notifyIcon.DoubleClick += delegate { ShowSettings(); };

            timer = new System.Windows.Forms.Timer { Interval = 1000 };
            timer.Tick += delegate
            {
                UpdateClockText();
                UpdateClockPosition();
            };

            hoverTimer = new System.Windows.Forms.Timer { Interval = 120 };
            hoverTimer.Tick += delegate { UpdateHoverCollapse(); };

            animationTimer = new System.Windows.Forms.Timer { Interval = 15 };
            animationTimer.Tick += delegate { UpdateCollapseAnimation(); };

            ApplySettings();

            if (openSettings)
            {
                Application.Idle += OpenSettingsOnIdle;
            }

            Shown += delegate
            {
                UpdateClockText();
                UpdateClockPosition();
                timer.Start();
                hoverTimer.Start();
            };
        }

        private void OpenSettingsOnIdle(object sender, EventArgs e)
        {
            Application.Idle -= OpenSettingsOnIdle;
            ShowSettings();
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int wsExNoActivate = 0x08000000;
                const int wsExToolWindow = 0x00000080;
                const int wsExLayered = 0x00080000;
                const int wsExTransparent = 0x00000020;

                var cp = base.CreateParams;
                cp.ExStyle |= wsExNoActivate | wsExToolWindow;
                if (settings != null && settings.ClickThrough)
                {
                    cp.ExStyle |= wsExLayered | wsExTransparent;
                }
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int wmNchittest = 0x0084;
            const int htTransparent = -1;

            if (m.Msg == showSettingsMessage)
            {
                ShowSettings();
                return;
            }

            if (settings.ClickThrough && m.Msg == wmNchittest)
            {
                m.Result = new IntPtr(htTransparent);
                return;
            }

            base.WndProc(ref m);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyClickThroughStyle();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (notifyIcon != null)
                {
                    notifyIcon.Visible = false;
                    notifyIcon.Dispose();
                }
                UninstallMenuMouseHook();
            }
            base.Dispose(disposing);
        }

        private Label CreateLabel(ContentAlignment alignment)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = alignment,
                Margin = new Padding(0),
                Padding = new Padding(0),
                UseCompatibleTextRendering = false,
                AutoSize = false
            };
        }

        private ContextMenuStrip CreateTrayMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add(settingsMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitMenuItem);
            return menu;
        }

        private void InstallMenuMouseHook()
        {
            if (menuMouseHook != IntPtr.Zero)
            {
                return;
            }

            menuMouseHook = NativeMethods.SetWindowsHookEx(WH_MOUSE_LL, menuMouseProc, IntPtr.Zero, 0);
        }

        private void UninstallMenuMouseHook()
        {
            if (menuMouseHook == IntPtr.Zero)
            {
                return;
            }

            NativeMethods.UnhookWindowsHookEx(menuMouseHook);
            menuMouseHook = IntPtr.Zero;
        }

        private IntPtr MenuMouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && menuOpen)
            {
                int message = wParam.ToInt32();
                if (message == WM_LBUTTONDOWN || message == WM_RBUTTONDOWN || message == WM_MBUTTONDOWN)
                {
                    Point cursor = Cursor.Position;
                    if (!widgetMenu.Bounds.Contains(cursor) && !Bounds.Contains(cursor))
                    {
                        BeginInvoke((MethodInvoker)delegate { widgetMenu.Close(ToolStripDropDownCloseReason.AppClicked); });
                    }
                }
            }
            return NativeMethods.CallNextHookEx(menuMouseHook, nCode, wParam, lParam);
        }

        private void ClockMouseClick(object sender, MouseEventArgs e)
        {
            if (settings.ClickThrough)
            {
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                ToggleCollapsed();
            }
        }

        private void ClockMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (settings.ClickThrough)
            {
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                if (collapsed)
                {
                    SetCollapsed(false);
                }
                ShowSettings();
            }
        }

        private void ToggleCollapsed()
        {
            SetCollapsed(!collapsed);
        }

        private void SetCollapsed(bool value)
        {
            if (collapsed == value)
            {
                return;
            }

            collapsed = value;
            targetWidth = collapsed ? Math.Max(2, settings.CollapsedWidth) : settings.Width;
            if (!collapsed)
            {
                timeLabel.Visible = true;
                dateLabel.Visible = true;
                panel.Visible = true;
                collapseLabel.Visible = false;
            }
            animationTimer.Start();
            UpdateClockPosition();
        }

        private void UpdateCollapseAnimation()
        {
            int delta = targetWidth - currentWidth;
            if (delta == 0)
            {
                animationTimer.Stop();
                timeLabel.Visible = !collapsed;
                dateLabel.Visible = !collapsed;
                panel.Visible = !collapsed;
                collapseLabel.Visible = collapsed;
                return;
            }

            int step = Math.Max(1, Math.Abs(delta) / 4);
            currentWidth += delta > 0 ? step : -step;
            if ((delta > 0 && currentWidth > targetWidth) || (delta < 0 && currentWidth < targetWidth))
            {
                currentWidth = targetWidth;
            }
            UpdateClockPosition();
        }

        private void UpdateHoverCollapse()
        {
            if (!settings.AutoCollapse || settings.ClickThrough || collapsed || menuOpen)
            {
                hoverTracking = false;
                return;
            }

            if (Bounds.Contains(Cursor.Position))
            {
                if (!hoverTracking)
                {
                    hoverTracking = true;
                    hoverStartedUtc = DateTime.UtcNow;
                }
                else if ((DateTime.UtcNow - hoverStartedUtc).TotalMilliseconds >= settings.CollapseDelayMs)
                {
                    SetCollapsed(true);
                }
            }
            else
            {
                hoverTracking = false;
            }
        }

        private void ShowSettings()
        {
            if (settingsForm == null || settingsForm.IsDisposed)
            {
                settingsForm = new ModernSettingsForm(settings);
                settingsForm.SettingsApplied += delegate(ClockSettings updated)
                {
                    settings = updated;
                    settings.Save();
                    ApplySettings();
                    UpdateClockText();
                    UpdateClockPosition();
                };
            }

            settingsForm.Show();
            settingsForm.WindowState = FormWindowState.Normal;
            settingsForm.Activate();
        }

        private void ApplySettings()
        {
            Color backColor = Color.FromArgb(settings.BackColorArgb);
            string lang = settings.LanguageCode;
            Text = UiText.AppName(lang);
            notifyIcon.Text = ShortNotifyText(UiText.AppName(lang));
            settingsMenuItem.Text = UiText.T(lang, "settingsMenu");
            exitMenuItem.Text = UiText.T(lang, "exitMenu");
            if (!collapsed)
            {
                currentWidth = settings.Width;
                targetWidth = settings.Width;
            }
            BackColor = backColor;
            panel.BackColor = backColor;
            collapseLabel.BackColor = backColor;
            collapseLabel.ForeColor = Color.FromArgb(settings.TimeForeColorArgb);
            panel.Padding = new Padding(settings.PaddingLeft, settings.PaddingTop, settings.PaddingRight, settings.PaddingBottom);

            timeLabel.BackColor = backColor;
            timeLabel.ForeColor = Color.FromArgb(settings.TimeForeColorArgb);
            timeLabel.Font = CreateFont(settings.TimeFontName, settings.TimeFontSize);
            timeLabel.Padding = new Padding(settings.TimeOffsetX, 0, 0, 0);

            dateLabel.BackColor = backColor;
            dateLabel.ForeColor = Color.FromArgb(settings.DateForeColorArgb);
            dateLabel.Font = CreateFont(settings.DateFontName, settings.DateFontSize);
            dateLabel.Padding = new Padding(settings.DateOffsetX, 0, 0, 0);

            panel.Controls.Clear();
            if (settings.Layout == "DateAboveTime")
            {
                dateLabel.TextAlign = ContentAlignment.BottomLeft;
                timeLabel.TextAlign = ContentAlignment.TopLeft;
                panel.Controls.Add(dateLabel, 0, 0);
                panel.Controls.Add(timeLabel, 0, 1);
            }
            else
            {
                timeLabel.TextAlign = ContentAlignment.BottomLeft;
                dateLabel.TextAlign = ContentAlignment.TopLeft;
                panel.Controls.Add(timeLabel, 0, 0);
                panel.Controls.Add(dateLabel, 0, 1);
            }
            timeLabel.Visible = !collapsed;
            dateLabel.Visible = !collapsed;
            ApplyClickThroughStyle();
        }

        private void ApplyClickThroughStyle()
        {
            if (!IsHandleCreated)
            {
                return;
            }

            const int gwlExStyle = -20;
            const int wsExLayered = 0x00080000;
            const int wsExTransparent = 0x00000020;
            const int lwaAlpha = 0x00000002;
            int exStyle = NativeMethods.GetWindowLong(Handle, gwlExStyle);
            int target = settings.ClickThrough
                ? (exStyle | wsExLayered | wsExTransparent)
                : (exStyle & ~wsExTransparent);
            if (target != exStyle)
            {
                NativeMethods.SetWindowLong(Handle, gwlExStyle, target);
            }
            if (settings.ClickThrough)
            {
                NativeMethods.SetLayeredWindowAttributes(Handle, 0, 255, lwaAlpha);
            }
        }

        private static Font CreateFont(string name, float size)
        {
            try
            {
                return new Font(name, size, FontStyle.Regular, GraphicsUnit.Point);
            }
            catch
            {
            return new Font("Segoe UI", size, FontStyle.Regular, GraphicsUnit.Point);
            }
        }

        private static string ShortNotifyText(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= 60)
            {
                return string.IsNullOrEmpty(text) ? "Taskbar World Clock" : text;
            }
            return text.Substring(0, 60);
        }

        private void UpdateClockText()
        {
            TimeZoneInfo zone;
            try
            {
                zone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
            }
            catch
            {
                zone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
            }

            DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
            timeLabel.Text = FormatTime(now);
            dateLabel.Text = FormatDate(now);
        }

        private string FormatTime(DateTime now)
        {
            if (settings.Use24Hour)
            {
                return now.ToString("HH:mm", CultureInfo.InvariantCulture);
            }

            string hourText = now.ToString("hh:mm", CultureInfo.InvariantCulture);
            string ampm = now.ToString("tt", CultureInfo.InvariantCulture);
            if (settings.AmPmMode == "Prefix")
            {
                return ampm + " " + hourText;
            }
            if (settings.AmPmMode == "Hidden")
            {
                return hourText;
            }
            return hourText + " " + ampm;
        }

        private string FormatDate(DateTime now)
        {
            string text = now.ToString(settings.DateFormat, CultureInfo.InvariantCulture);
            if (settings.ShowWeekday)
            {
                if (settings.WeekdayFormat == "Localized")
                {
                    text += " " + now.ToString("ddd", GetCulture(settings.LanguageCode));
                }
                else
                {
                    text += " " + now.ToString(settings.WeekdayFormat, CultureInfo.InvariantCulture);
                }
            }
            return text;
        }

        private static CultureInfo GetCulture(string languageCode)
        {
            try { return CultureInfo.GetCultureInfo(languageCode); }
            catch { return CultureInfo.InvariantCulture; }
        }

        private void UpdateClockPosition()
        {
            Rectangle rect = GetTaskbarRectangle();
            int x;
            int y;

            if (settings.Position == "BottomRight")
            {
                x = rect.Right - settings.Width + settings.OffsetX;
                y = rect.Bottom - settings.Height - settings.OffsetY;
                collapseLabel.Text = "<";
            }
            else if (settings.Position == "TopLeft")
            {
                Rectangle screen = Screen.PrimaryScreen.Bounds;
                x = screen.Left + settings.OffsetX;
                y = screen.Top - settings.OffsetY;
                collapseLabel.Text = ">";
            }
            else if (settings.Position == "TopRight")
            {
                Rectangle screen = Screen.PrimaryScreen.Bounds;
                x = screen.Right - settings.Width + settings.OffsetX;
                y = screen.Top - settings.OffsetY;
                collapseLabel.Text = "<";
            }
            else if (settings.Position == "Custom")
            {
                x = settings.X;
                y = settings.Y;
                collapseLabel.Text = ">";
            }
            else
            {
                x = rect.Left + settings.OffsetX;
                y = rect.Bottom - settings.Height - settings.OffsetY;
                collapseLabel.Text = ">";
            }

            int width = Math.Max(2, currentWidth <= 0 ? settings.Width : currentWidth);
            if (collapsed && (settings.Position == "BottomRight" || settings.Position == "TopRight"))
            {
                x = x + settings.Width - width;
            }

            Bounds = new Rectangle(x, y, width, settings.Height);
            NativeMethods.SetWindowPos(Handle, HwndTopMost, x, y, width, settings.Height, 0x0010);
            collapseLabel.Invalidate();
        }

        private void DrawCollapseEdge(object sender, PaintEventArgs e)
        {
            if (!collapsed)
            {
                return;
            }

            bool anchoredRight = settings.Position == "BottomRight" || settings.Position == "TopRight";
            int x = anchoredRight ? 0 : collapseLabel.Width - 1;
            using (var pen = new Pen(Color.FromArgb(90, 90, 90)))
            {
                e.Graphics.DrawLine(pen, x, 4, x, Math.Max(4, collapseLabel.Height - 5));
            }
        }

        private static Rectangle GetTaskbarRectangle()
        {
            IntPtr taskbar = NativeMethods.FindWindow("Shell_TrayWnd", null);
            NativeMethods.RECT nativeRect;
            if (taskbar != IntPtr.Zero && NativeMethods.GetWindowRect(taskbar, out nativeRect))
            {
                return Rectangle.FromLTRB(nativeRect.Left, nativeRect.Top, nativeRect.Right, nativeRect.Bottom);
            }
            Rectangle screen = Screen.PrimaryScreen.Bounds;
            return new Rectangle(screen.Left, screen.Bottom - 48, screen.Width, 48);
        }
    }

    internal sealed class ValueItem
    {
        public readonly string Value;
        private readonly string text;

        public ValueItem(string value, string text)
        {
            Value = value;
            this.text = text;
        }

        public override string ToString()
        {
            return UiText.T(UiText.CurrentLanguage, text);
        }
    }

    internal sealed class CenteredCheckBox : CheckBox
    {
        public CenteredCheckBox()
        {
            Text = "";
            AutoSize = false;
            Size = new Size(22, 22);
            TabStop = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var brush = new SolidBrush(Parent == null ? BackColor : Parent.BackColor))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }

            CheckBoxState state;
            if (!Enabled)
            {
                state = Checked ? CheckBoxState.CheckedDisabled : CheckBoxState.UncheckedDisabled;
            }
            else if (MouseButtons == MouseButtons.Left && ClientRectangle.Contains(PointToClient(Cursor.Position)))
            {
                state = Checked ? CheckBoxState.CheckedPressed : CheckBoxState.UncheckedPressed;
            }
            else if (ClientRectangle.Contains(PointToClient(Cursor.Position)))
            {
                state = Checked ? CheckBoxState.CheckedHot : CheckBoxState.UncheckedHot;
            }
            else
            {
                state = Checked ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal;
            }

            Size glyph = CheckBoxRenderer.GetGlyphSize(e.Graphics, state);
            Point location = new Point((ClientSize.Width - glyph.Width) / 2, (ClientSize.Height - glyph.Height) / 2);
            CheckBoxRenderer.DrawCheckBox(e.Graphics, location, state);
        }

        protected override void OnMouseEnter(EventArgs eventargs)
        {
            base.OnMouseEnter(eventargs);
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs eventargs)
        {
            base.OnMouseLeave(eventargs);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            Invalidate();
        }
    }

    internal static class UiText
    {
        public static string CurrentLanguage = "zh-CN";

        private static readonly Dictionary<string, Dictionary<string, string>> Texts = new Dictionary<string, Dictionary<string, string>>
        {
            {"zh-CN", new Dictionary<string, string>{{"settings","Taskbar World Clock 设置"},{"close","关闭"},{"apply","应用"},{"save","保存"},{"languageTimeZone","语言与时区"},{"display","显示格式"},{"text","文字"},{"background","背景"},{"position","位置"},{"behavior","行为"},{"language","界面语言"},{"timezone","时区"},{"timezoneHint","点击后可输入搜索，支持国家、城市、英文名称和 UTC 偏移。"},{"layout","上下排布"},{"timeAbove","时间在上，日期在下"},{"dateAbove","日期在上，时间在下"},{"timeFormat","时间格式"},{"ampm","AM/PM"},{"suffix","放在时间后面"},{"prefix","放在时间前面"},{"hidden","不显示"},{"dateFormat","日期格式"},{"showWeekday","显示星期"},{"weekdayFormat","星期格式"},{"localized","跟随界面语言"},{"enShort","英文三字母"},{"enLong","英文完整"},{"timeFont","时间字体"},{"timeSize","时间字号"},{"timeColor","时间颜色"},{"timeOffset","时间横向偏移"},{"dateFont","日期字体"},{"dateSize","日期字号"},{"dateColor","日期颜色"},{"dateOffset","日期横向偏移"},{"textPadding","文字左边距"},{"backColor","背景颜色"},{"width","宽度"},{"height","高度"},{"presetPosition","预设位置"},{"bottomLeft","左下"},{"bottomRight","右下"},{"topLeft","左上"},{"topRight","右上"},{"custom","自定义坐标"},{"x","坐标 X"},{"y","坐标 Y"},{"offsetX","横向偏移"},{"offsetY","纵向偏移"},{"autoStart","开机自启动"},{"clickThrough","允许点击穿透"},{"clickThroughNote","开启后，小组件不会拦截鼠标，但也不能响应右键、双击和点击收起。"},{"autoCollapse","悬停自动收起"},{"collapseDelay","悬停延迟毫秒"},{"collapsedWidth","收起宽度"},{"reset","恢复默认设置"},{"enabled","启用"},{"palette","取色板"},{"screenPick","屏幕取色"},{"pickHint","移动鼠标，点击取色"},{"moreFonts","更多字体..."}}},
            {"zh-TW", new Dictionary<string, string>{{"settings","Taskbar World Clock 設定"},{"close","關閉"},{"apply","套用"},{"save","儲存"},{"languageTimeZone","語言與時區"},{"display","顯示格式"},{"text","文字"},{"background","背景"},{"position","位置"},{"behavior","行為"},{"language","介面語言"},{"timezone","時區"},{"timezoneHint","點選後可輸入搜尋，支援國家、城市、英文名稱和 UTC 偏移。"},{"layout","上下排列"},{"timeAbove","時間在上，日期在下"},{"dateAbove","日期在上，時間在下"},{"timeFormat","時間格式"},{"ampm","AM/PM"},{"suffix","放在時間後面"},{"prefix","放在時間前面"},{"hidden","不顯示"},{"dateFormat","日期格式"},{"showWeekday","顯示星期"},{"weekdayFormat","星期格式"},{"localized","跟隨介面語言"},{"enShort","英文三字母"},{"enLong","英文完整"},{"timeFont","時間字型"},{"timeSize","時間字號"},{"timeColor","時間顏色"},{"timeOffset","時間橫向偏移"},{"dateFont","日期字型"},{"dateSize","日期字號"},{"dateColor","日期顏色"},{"dateOffset","日期橫向偏移"},{"textPadding","文字左邊距"},{"backColor","背景顏色"},{"width","寬度"},{"height","高度"},{"presetPosition","預設位置"},{"bottomLeft","左下"},{"bottomRight","右下"},{"topLeft","左上"},{"topRight","右上"},{"custom","自訂座標"},{"x","座標 X"},{"y","座標 Y"},{"offsetX","橫向偏移"},{"offsetY","縱向偏移"},{"autoStart","開機自動啟動"},{"clickThrough","允許點擊穿透"},{"clickThroughNote","開啟後，小元件不會攔截滑鼠，但也不能回應右鍵、雙擊和點擊收起。"},{"autoCollapse","懸停自動收起"},{"collapseDelay","懸停延遲毫秒"},{"collapsedWidth","收起寬度"},{"reset","恢復預設設定"},{"enabled","啟用"},{"palette","取色板"},{"screenPick","螢幕取色"},{"pickHint","移動滑鼠，點擊取色"},{"moreFonts","更多字型..."}}},
            {"en-US", new Dictionary<string, string>{{"settings","Taskbar World Clock Settings"},{"close","Close"},{"apply","Apply"},{"save","Save"},{"languageTimeZone","Language & Time Zone"},{"display","Display"},{"text","Text"},{"background","Background"},{"position","Position"},{"behavior","Behavior"},{"language","Language"},{"timezone","Time zone"},{"timezoneHint","Click to type and search by country, city, English name, or UTC offset."},{"layout","Layout"},{"timeAbove","Time above date"},{"dateAbove","Date above time"},{"timeFormat","Time format"},{"ampm","AM/PM"},{"suffix","After time"},{"prefix","Before time"},{"hidden","Hidden"},{"dateFormat","Date format"},{"showWeekday","Show weekday"},{"weekdayFormat","Weekday format"},{"localized","Use interface language"},{"enShort","English short"},{"enLong","English full"},{"timeFont","Time font"},{"timeSize","Time size"},{"timeColor","Time color"},{"timeOffset","Time X offset"},{"dateFont","Date font"},{"dateSize","Date size"},{"dateColor","Date color"},{"dateOffset","Date X offset"},{"textPadding","Text left padding"},{"backColor","Background color"},{"width","Width"},{"height","Height"},{"presetPosition","Preset position"},{"bottomLeft","Bottom left"},{"bottomRight","Bottom right"},{"topLeft","Top left"},{"topRight","Top right"},{"custom","Custom coordinates"},{"x","X"},{"y","Y"},{"offsetX","X offset"},{"offsetY","Y offset"},{"autoStart","Start with Windows"},{"clickThrough","Allow click-through"},{"clickThroughNote","When enabled, the widget will not intercept the mouse and cannot handle right-click, double-click, or click-to-collapse."},{"autoCollapse","Auto-collapse on hover"},{"collapseDelay","Hover delay ms"},{"collapsedWidth","Collapsed width"},{"reset","Restore defaults"},{"enabled","Enabled"},{"palette","Palette"},{"screenPick","Pick screen color"},{"pickHint","Move mouse, click to pick"},{"moreFonts","More fonts..."}}},
            {"ja-JP", new Dictionary<string, string>{{"settings","Taskbar World Clock 設定"},{"close","閉じる"},{"apply","適用"},{"save","保存"},{"languageTimeZone","言語とタイムゾーン"},{"display","表示形式"},{"text","テキスト"},{"background","背景"},{"position","位置"},{"behavior","動作"},{"language","言語"},{"timezone","タイムゾーン"},{"timezoneHint","クリックして入力し、国、都市、英語名、UTC オフセットで検索できます。"},{"layout","配置"},{"timeAbove","時刻を上、日付を下"},{"dateAbove","日付を上、時刻を下"},{"timeFormat","時刻形式"},{"ampm","AM/PM"},{"suffix","時刻の後"},{"prefix","時刻の前"},{"hidden","非表示"},{"dateFormat","日付形式"},{"showWeekday","曜日を表示"},{"weekdayFormat","曜日形式"},{"localized","表示言語に従う"},{"enShort","英語短縮"},{"enLong","英語完全"},{"timeFont","時刻フォント"},{"timeSize","時刻サイズ"},{"timeColor","時刻色"},{"timeOffset","時刻 X オフセット"},{"dateFont","日付フォント"},{"dateSize","日付サイズ"},{"dateColor","日付色"},{"dateOffset","日付 X オフセット"},{"textPadding","文字左余白"},{"backColor","背景色"},{"width","幅"},{"height","高さ"},{"presetPosition","プリセット位置"},{"bottomLeft","左下"},{"bottomRight","右下"},{"topLeft","左上"},{"topRight","右上"},{"custom","カスタム座標"},{"x","座標 X"},{"y","座標 Y"},{"offsetX","X オフセット"},{"offsetY","Y オフセット"},{"autoStart","Windows 起動時に開始"},{"clickThrough","クリック透過"},{"clickThroughNote","有効にするとマウスを遮らず、右クリック、ダブルクリック、クリックでの折りたたみに反応しません。"},{"autoCollapse","ホバーで自動折りたたみ"},{"collapseDelay","ホバー遅延 ms"},{"collapsedWidth","折りたたみ幅"},{"reset","既定に戻す"},{"enabled","有効"},{"palette","パレット"},{"screenPick","画面から色を取得"},{"pickHint","マウスを移動し、クリックで取得"},{"moreFonts","その他のフォント..."}}},
            {"ko-KR", new Dictionary<string, string>{{"settings","Taskbar World Clock 설정"},{"close","닫기"},{"apply","적용"},{"save","저장"},{"languageTimeZone","언어 및 시간대"},{"display","표시 형식"},{"text","텍스트"},{"background","배경"},{"position","위치"},{"behavior","동작"},{"language","언어"},{"timezone","시간대"},{"timezoneHint","클릭 후 국가, 도시, 영어 이름, UTC 오프셋으로 검색할 수 있습니다."},{"layout","배치"},{"timeAbove","시간 위, 날짜 아래"},{"dateAbove","날짜 위, 시간 아래"},{"timeFormat","시간 형식"},{"ampm","AM/PM"},{"suffix","시간 뒤"},{"prefix","시간 앞"},{"hidden","숨김"},{"dateFormat","날짜 형식"},{"showWeekday","요일 표시"},{"weekdayFormat","요일 형식"},{"localized","인터페이스 언어 사용"},{"enShort","영어 약어"},{"enLong","영어 전체"},{"timeFont","시간 글꼴"},{"timeSize","시간 크기"},{"timeColor","시간 색상"},{"timeOffset","시간 X 오프셋"},{"dateFont","날짜 글꼴"},{"dateSize","날짜 크기"},{"dateColor","날짜 색상"},{"dateOffset","날짜 X 오프셋"},{"textPadding","텍스트 왼쪽 여백"},{"backColor","배경색"},{"width","너비"},{"height","높이"},{"presetPosition","기본 위치"},{"bottomLeft","왼쪽 아래"},{"bottomRight","오른쪽 아래"},{"topLeft","왼쪽 위"},{"topRight","오른쪽 위"},{"custom","사용자 좌표"},{"x","좌표 X"},{"y","좌표 Y"},{"offsetX","X 오프셋"},{"offsetY","Y 오프셋"},{"autoStart","Windows 시작 시 실행"},{"clickThrough","클릭 통과 허용"},{"clickThroughNote","켜면 위젯이 마우스를 가로채지 않으며 우클릭, 더블클릭, 클릭 접기를 처리할 수 없습니다."},{"autoCollapse","호버 시 자동 접기"},{"collapseDelay","호버 지연 ms"},{"collapsedWidth","접힌 너비"},{"reset","기본값 복원"},{"enabled","사용"},{"palette","팔레트"},{"screenPick","화면 색상 선택"},{"pickHint","마우스를 이동하고 클릭해 선택"},{"moreFonts","더 많은 글꼴..."}}},
            {"de-DE", new Dictionary<string, string>{{"settings","Taskbar World Clock Einstellungen"},{"close","Schließen"},{"apply","Anwenden"},{"save","Speichern"},{"languageTimeZone","Sprache & Zeitzone"},{"display","Anzeige"},{"text","Text"},{"background","Hintergrund"},{"position","Position"},{"behavior","Verhalten"},{"language","Sprache"},{"timezone","Zeitzone"},{"timezoneHint","Klicken und nach Land, Stadt, englischem Namen oder UTC-Versatz suchen."},{"layout","Layout"},{"timeAbove","Zeit über Datum"},{"dateAbove","Datum über Zeit"},{"timeFormat","Zeitformat"},{"ampm","AM/PM"},{"suffix","Nach der Zeit"},{"prefix","Vor der Zeit"},{"hidden","Ausblenden"},{"dateFormat","Datumsformat"},{"showWeekday","Wochentag anzeigen"},{"weekdayFormat","Wochentagformat"},{"localized","Oberflächensprache verwenden"},{"enShort","Englisch kurz"},{"enLong","Englisch vollständig"},{"timeFont","Zeit-Schrift"},{"timeSize","Zeitgröße"},{"timeColor","Zeitfarbe"},{"timeOffset","Zeit X-Versatz"},{"dateFont","Datums-Schrift"},{"dateSize","Datumsgröße"},{"dateColor","Datumsfarbe"},{"dateOffset","Datum X-Versatz"},{"textPadding","Text links Abstand"},{"backColor","Hintergrundfarbe"},{"width","Breite"},{"height","Höhe"},{"presetPosition","Voreinstellung"},{"bottomLeft","Unten links"},{"bottomRight","Unten rechts"},{"topLeft","Oben links"},{"topRight","Oben rechts"},{"custom","Eigene Koordinaten"},{"x","X"},{"y","Y"},{"offsetX","X-Versatz"},{"offsetY","Y-Versatz"},{"autoStart","Mit Windows starten"},{"clickThrough","Klicks durchlassen"},{"clickThroughNote","Wenn aktiviert, fängt das Widget keine Mausereignisse ab und reagiert nicht auf Rechtsklick, Doppelklick oder Einklappen per Klick."},{"autoCollapse","Bei Hover einklappen"},{"collapseDelay","Hover-Verzögerung ms"},{"collapsedWidth","Eingeklappte Breite"},{"reset","Standard wiederherstellen"},{"enabled","Aktiviert"},{"palette","Palette"},{"screenPick","Bildschirmfarbe wählen"},{"pickHint","Maus bewegen, klicken zum Wählen"},{"moreFonts","Weitere Schriftarten..."}}},
            {"fr-FR", new Dictionary<string, string>{{"settings","Paramètres Taskbar World Clock"},{"close","Fermer"},{"apply","Appliquer"},{"save","Enregistrer"},{"languageTimeZone","Langue et fuseau horaire"},{"display","Affichage"},{"text","Texte"},{"background","Arrière-plan"},{"position","Position"},{"behavior","Comportement"},{"language","Langue"},{"timezone","Fuseau horaire"},{"timezoneHint","Cliquez puis recherchez par pays, ville, nom anglais ou décalage UTC."},{"layout","Disposition"},{"timeAbove","Heure au-dessus de la date"},{"dateAbove","Date au-dessus de l'heure"},{"timeFormat","Format de l'heure"},{"ampm","AM/PM"},{"suffix","Après l'heure"},{"prefix","Avant l'heure"},{"hidden","Masqué"},{"dateFormat","Format de date"},{"showWeekday","Afficher le jour"},{"weekdayFormat","Format du jour"},{"localized","Langue de l'interface"},{"enShort","Anglais court"},{"enLong","Anglais complet"},{"timeFont","Police de l'heure"},{"timeSize","Taille de l'heure"},{"timeColor","Couleur de l'heure"},{"timeOffset","Décalage X de l'heure"},{"dateFont","Police de la date"},{"dateSize","Taille de la date"},{"dateColor","Couleur de la date"},{"dateOffset","Décalage X de la date"},{"textPadding","Marge gauche du texte"},{"backColor","Couleur d'arrière-plan"},{"width","Largeur"},{"height","Hauteur"},{"presetPosition","Position prédéfinie"},{"bottomLeft","Bas gauche"},{"bottomRight","Bas droite"},{"topLeft","Haut gauche"},{"topRight","Haut droite"},{"custom","Coordonnées perso"},{"x","X"},{"y","Y"},{"offsetX","Décalage X"},{"offsetY","Décalage Y"},{"autoStart","Démarrer avec Windows"},{"clickThrough","Clic traversant"},{"clickThroughNote","Si activé, le widget ne bloque pas la souris et ne répond pas au clic droit, double-clic ou clic pour réduire."},{"autoCollapse","Réduire au survol"},{"collapseDelay","Délai de survol ms"},{"collapsedWidth","Largeur réduite"},{"reset","Restaurer par défaut"},{"enabled","Activé"},{"palette","Palette"},{"screenPick","Prélever couleur écran"},{"pickHint","Déplacez la souris, cliquez pour prélever"},{"moreFonts","Plus de polices..."}}},
            {"es-ES", new Dictionary<string, string>{{"settings","Configuración de Taskbar World Clock"},{"close","Cerrar"},{"apply","Aplicar"},{"save","Guardar"},{"languageTimeZone","Idioma y zona horaria"},{"display","Visualización"},{"text","Texto"},{"background","Fondo"},{"position","Posición"},{"behavior","Comportamiento"},{"language","Idioma"},{"timezone","Zona horaria"},{"timezoneHint","Haga clic y busque por país, ciudad, nombre en inglés o desfase UTC."},{"layout","Diseño"},{"timeAbove","Hora arriba, fecha abajo"},{"dateAbove","Fecha arriba, hora abajo"},{"timeFormat","Formato de hora"},{"ampm","AM/PM"},{"suffix","Después de la hora"},{"prefix","Antes de la hora"},{"hidden","Oculto"},{"dateFormat","Formato de fecha"},{"showWeekday","Mostrar día"},{"weekdayFormat","Formato de día"},{"localized","Idioma de interfaz"},{"enShort","Inglés corto"},{"enLong","Inglés completo"},{"timeFont","Fuente de hora"},{"timeSize","Tamaño de hora"},{"timeColor","Color de hora"},{"timeOffset","Desplazamiento X de hora"},{"dateFont","Fuente de fecha"},{"dateSize","Tamaño de fecha"},{"dateColor","Color de fecha"},{"dateOffset","Desplazamiento X de fecha"},{"textPadding","Margen izquierdo texto"},{"backColor","Color de fondo"},{"width","Ancho"},{"height","Alto"},{"presetPosition","Posición predefinida"},{"bottomLeft","Abajo izquierda"},{"bottomRight","Abajo derecha"},{"topLeft","Arriba izquierda"},{"topRight","Arriba derecha"},{"custom","Coordenadas"},{"x","X"},{"y","Y"},{"offsetX","Desplazamiento X"},{"offsetY","Desplazamiento Y"},{"autoStart","Iniciar con Windows"},{"clickThrough","Permitir clic a través"},{"clickThroughNote","Al activarlo, el widget no intercepta el ratón y no responde a clic derecho, doble clic ni clic para contraer."},{"autoCollapse","Contraer al pasar el ratón"},{"collapseDelay","Retardo ms"},{"collapsedWidth","Ancho contraído"},{"reset","Restaurar valores"},{"enabled","Activado"},{"palette","Paleta"},{"screenPick","Tomar color de pantalla"},{"pickHint","Mueva el ratón, clic para tomar"},{"moreFonts","Más fuentes..."}}},
            {"pt-BR", new Dictionary<string, string>{{"settings","Configurações do Taskbar World Clock"},{"close","Fechar"},{"apply","Aplicar"},{"save","Salvar"},{"languageTimeZone","Idioma e fuso horário"},{"display","Exibição"},{"text","Texto"},{"background","Fundo"},{"position","Posição"},{"behavior","Comportamento"},{"language","Idioma"},{"timezone","Fuso horário"},{"timezoneHint","Clique e pesquise por país, cidade, nome em inglês ou deslocamento UTC."},{"layout","Layout"},{"timeAbove","Hora acima da data"},{"dateAbove","Data acima da hora"},{"timeFormat","Formato da hora"},{"ampm","AM/PM"},{"suffix","Depois da hora"},{"prefix","Antes da hora"},{"hidden","Oculto"},{"dateFormat","Formato da data"},{"showWeekday","Mostrar dia"},{"weekdayFormat","Formato do dia"},{"localized","Idioma da interface"},{"enShort","Inglês curto"},{"enLong","Inglês completo"},{"timeFont","Fonte da hora"},{"timeSize","Tamanho da hora"},{"timeColor","Cor da hora"},{"timeOffset","Deslocamento X da hora"},{"dateFont","Fonte da data"},{"dateSize","Tamanho da data"},{"dateColor","Cor da data"},{"dateOffset","Deslocamento X da data"},{"textPadding","Margem esquerda do texto"},{"backColor","Cor de fundo"},{"width","Largura"},{"height","Altura"},{"presetPosition","Posição predefinida"},{"bottomLeft","Inferior esquerda"},{"bottomRight","Inferior direita"},{"topLeft","Superior esquerda"},{"topRight","Superior direita"},{"custom","Coordenadas"},{"x","X"},{"y","Y"},{"offsetX","Deslocamento X"},{"offsetY","Deslocamento Y"},{"autoStart","Iniciar com Windows"},{"clickThrough","Permitir clique atravessar"},{"clickThroughNote","Quando ativado, o widget não intercepta o mouse e não responde a clique direito, duplo clique ou clique para recolher."},{"autoCollapse","Recolher ao passar o mouse"},{"collapseDelay","Atraso ms"},{"collapsedWidth","Largura recolhida"},{"reset","Restaurar padrões"},{"enabled","Ativado"},{"palette","Paleta"},{"screenPick","Capturar cor da tela"},{"pickHint","Mova o mouse, clique para capturar"},{"moreFonts","Mais fontes..."}}},
            {"ru-RU", new Dictionary<string, string>{{"settings","Настройки Taskbar World Clock"},{"close","Закрыть"},{"apply","Применить"},{"save","Сохранить"},{"languageTimeZone","Язык и часовой пояс"},{"display","Отображение"},{"text","Текст"},{"background","Фон"},{"position","Позиция"},{"behavior","Поведение"},{"language","Язык"},{"timezone","Часовой пояс"},{"timezoneHint","Нажмите и ищите по стране, городу, английскому названию или смещению UTC."},{"layout","Компоновка"},{"timeAbove","Время над датой"},{"dateAbove","Дата над временем"},{"timeFormat","Формат времени"},{"ampm","AM/PM"},{"suffix","После времени"},{"prefix","Перед временем"},{"hidden","Скрыто"},{"dateFormat","Формат даты"},{"showWeekday","Показывать день"},{"weekdayFormat","Формат дня"},{"localized","Язык интерфейса"},{"enShort","Англ. кратко"},{"enLong","Англ. полностью"},{"timeFont","Шрифт времени"},{"timeSize","Размер времени"},{"timeColor","Цвет времени"},{"timeOffset","Смещение X времени"},{"dateFont","Шрифт даты"},{"dateSize","Размер даты"},{"dateColor","Цвет даты"},{"dateOffset","Смещение X даты"},{"textPadding","Левый отступ текста"},{"backColor","Цвет фона"},{"width","Ширина"},{"height","Высота"},{"presetPosition","Позиция"},{"bottomLeft","Снизу слева"},{"bottomRight","Снизу справа"},{"topLeft","Сверху слева"},{"topRight","Сверху справа"},{"custom","Координаты"},{"x","X"},{"y","Y"},{"offsetX","Смещение X"},{"offsetY","Смещение Y"},{"autoStart","Запуск с Windows"},{"clickThrough","Пропускать клики"},{"clickThroughNote","Если включено, виджет не перехватывает мышь и не реагирует на правый клик, двойной клик или сворачивание кликом."},{"autoCollapse","Сворачивать при наведении"},{"collapseDelay","Задержка мс"},{"collapsedWidth","Ширина свернутого"},{"reset","Сбросить настройки"},{"enabled","Включено"},{"palette","Палитра"},{"screenPick","Взять цвет с экрана"},{"pickHint","Двигайте мышь, клик для выбора"},{"moreFonts","Другие шрифты..."}}}
        };

        public static string T(string language, string key)
        {
            if (key == "settings")
            {
                return SettingsTitle(language);
            }
            if (key == "settingsMenu")
            {
                return SettingsMenu(language);
            }
            if (key == "exitMenu")
            {
                return ExitMenu(language);
            }
            if (key == "time12")
            {
                return TimeFormatName(language, false);
            }
            if (key == "time24")
            {
                return TimeFormatName(language, true);
            }
            if (key == "timezoneHint")
            {
                return TimeZoneHint(language);
            }
            if (key == "backgroundNote")
            {
                return BackgroundNote(language);
            }
            if (key == "clickThroughNote")
            {
                return ClickThroughNote(language);
            }
            if (key == "resetNote")
            {
                return ResetNote(language);
            }
            if (key == "display")
            {
                return TimeTab(language);
            }
            Dictionary<string, string> dict;
            if (!Texts.TryGetValue(language, out dict))
            {
                dict = Texts["en-US"];
            }
            string value;
            if (dict.TryGetValue(key, out value))
            {
                return value;
            }
            return Texts["en-US"].ContainsKey(key) ? Texts["en-US"][key] : key;
        }

        public static string AppName(string language)
        {
            if (language == "zh-CN") return "任务栏世界时钟";
            if (language == "zh-TW") return "工作列世界時鐘";
            if (language == "ja-JP") return "タスクバー世界時計";
            if (language == "ko-KR") return "작업 표시줄 세계 시계";
            if (language == "de-DE") return "Taskleisten-Weltuhr";
            if (language == "fr-FR") return "Horloge mondiale de la barre des tâches";
            if (language == "es-ES") return "Reloj mundial de la barra de tareas";
            if (language == "pt-BR") return "Relógio mundial da barra de tarefas";
            if (language == "ru-RU") return "Мировые часы панели задач";
            return "Taskbar World Clock";
        }

        private static string SettingsTitle(string language)
        {
            if (language == "zh-CN") return AppName(language) + " 设置";
            if (language == "zh-TW") return AppName(language) + " 設定";
            if (language == "ja-JP") return AppName(language) + " 設定";
            if (language == "ko-KR") return AppName(language) + " 설정";
            if (language == "de-DE") return AppName(language) + " Einstellungen";
            if (language == "fr-FR") return "Paramètres " + AppName(language);
            if (language == "es-ES") return "Configuración de " + AppName(language);
            if (language == "pt-BR") return "Configurações do " + AppName(language);
            if (language == "ru-RU") return "Настройки " + AppName(language);
            return AppName(language) + " Settings";
        }

        private static string SettingsMenu(string language)
        {
            if (language == "zh-CN") return "设置";
            if (language == "zh-TW") return "設定";
            if (language == "ja-JP") return "設定";
            if (language == "ko-KR") return "설정";
            if (language == "de-DE") return "Einstellungen";
            if (language == "fr-FR") return "Paramètres";
            if (language == "es-ES") return "Configuración";
            if (language == "pt-BR") return "Configurações";
            if (language == "ru-RU") return "Настройки";
            return "Settings";
        }

        private static string ExitMenu(string language)
        {
            if (language == "zh-CN") return "退出";
            if (language == "zh-TW") return "結束";
            if (language == "ja-JP") return "終了";
            if (language == "ko-KR") return "종료";
            if (language == "de-DE") return "Beenden";
            if (language == "fr-FR") return "Quitter";
            if (language == "es-ES") return "Salir";
            if (language == "pt-BR") return "Sair";
            if (language == "ru-RU") return "Выход";
            return "Exit";
        }

        private static string TimeZoneHint(string language)
        {
            if (language == "zh-CN") return "点击后可输入搜索，支持国家、城市和 UTC 偏移";
            if (language == "zh-TW") return "點選後可輸入搜尋，支援國家、城市和 UTC 偏移";
            if (language == "ja-JP") return "クリックして国、都市、UTC オフセットで検索";
            if (language == "ko-KR") return "클릭 후 국가, 도시, UTC 오프셋으로 검색";
            if (language == "de-DE") return "Klicken und nach Land, Stadt oder UTC-Versatz suchen";
            if (language == "fr-FR") return "Cliquez puis recherchez par pays, ville ou décalage UTC";
            if (language == "es-ES") return "Haga clic y busque por país, ciudad o desfase UTC";
            if (language == "pt-BR") return "Clique e pesquise por país, cidade ou deslocamento UTC";
            if (language == "ru-RU") return "Нажмите и ищите по стране, городу или смещению UTC";
            return "Click to search by country, city, or UTC offset";
        }

        private static string TimeFormatName(string language, bool use24Hour)
        {
            if (language == "zh-CN") return use24Hour ? "24 小时" : "12 小时";
            if (language == "zh-TW") return use24Hour ? "24 小時" : "12 小時";
            if (language == "ja-JP") return use24Hour ? "24時間" : "12時間";
            if (language == "ko-KR") return use24Hour ? "24시간" : "12시간";
            if (language == "de-DE") return use24Hour ? "24 Stunden" : "12 Stunden";
            if (language == "fr-FR") return use24Hour ? "24 heures" : "12 heures";
            if (language == "es-ES") return use24Hour ? "24 horas" : "12 horas";
            if (language == "pt-BR") return use24Hour ? "24 horas" : "12 horas";
            if (language == "ru-RU") return use24Hour ? "24 часа" : "12 часов";
            return use24Hour ? "24-hour" : "12-hour";
        }

        private static string BackgroundNote(string language)
        {
            if (language == "zh-CN") return "屏幕取色会读取鼠标所在像素，也就是你肉眼看到的最终颜色";
            if (language == "zh-TW") return "螢幕取色會讀取滑鼠所在像素，也就是你肉眼看到的最終顏色";
            if (language == "ja-JP") return "画面からの色取得は、マウス位置の表示済みピクセルを読み取ります";
            if (language == "ko-KR") return "화면 색상 선택은 마우스 위치의 실제 표시 픽셀을 읽습니다";
            if (language == "de-DE") return "Die Bildschirmaufnahme liest das sichtbare Pixel an der Mausposition";
            if (language == "fr-FR") return "Le prélèvement lit le pixel visible sous le pointeur de la souris";
            if (language == "es-ES") return "El selector lee el píxel visible bajo el puntero del ratón";
            if (language == "pt-BR") return "A captura lê o pixel visível sob o ponteiro do mouse";
            if (language == "ru-RU") return "Выбор с экрана считывает видимый пиксель под указателем мыши";
            return "Screen picking reads the visible pixel under the mouse pointer";
        }

        private static string ClickThroughNote(string language)
        {
            if (language == "zh-CN") return "开启后鼠标可以穿透组件，但也不能响应右键、双击和点击收起";
            if (language == "zh-TW") return "開啟後滑鼠可以穿透小元件，但也不能回應右鍵、雙擊和點擊收起";
            if (language == "ja-JP") return "有効にするとマウスは透過しますが、右クリック、ダブルクリック、クリック折りたたみは使えません";
            if (language == "ko-KR") return "켜면 마우스가 위젯을 통과하지만 우클릭, 더블클릭, 클릭 접기는 사용할 수 없습니다";
            if (language == "de-DE") return "Wenn aktiviert, geht die Maus durch das Widget; Rechtsklick, Doppelklick und Klick zum Einklappen funktionieren dann nicht";
            if (language == "fr-FR") return "Si activé, la souris traverse le widget; clic droit, double-clic et clic pour réduire ne fonctionnent plus";
            if (language == "es-ES") return "Al activarlo, el ratón atraviesa el widget; no funcionarán clic derecho, doble clic ni clic para contraer";
            if (language == "pt-BR") return "Quando ativado, o mouse atravessa o widget; clique direito, duplo clique e clique para recolher deixam de funcionar";
            if (language == "ru-RU") return "Если включено, мышь проходит сквозь виджет; правый клик, двойной клик и сворачивание кликом не работают";
            return "When enabled, the mouse can pass through the widget, but right-click, double-click, and click-to-collapse will not work";
        }

        private static string ResetNote(string language)
        {
            if (language == "zh-CN") return "恢复整个软件的默认设置";
            if (language == "zh-TW") return "恢復整個軟體的預設設定";
            if (language == "ja-JP") return "ソフト全体の設定を既定値に戻します";
            if (language == "ko-KR") return "프로그램 전체 설정을 기본값으로 복원합니다";
            if (language == "de-DE") return "Setzt alle Einstellungen der App auf Standardwerte zurück";
            if (language == "fr-FR") return "Restaure tous les paramètres du logiciel";
            if (language == "es-ES") return "Restaura toda la configuración del programa";
            if (language == "pt-BR") return "Restaura todas as configurações do programa";
            if (language == "ru-RU") return "Сбрасывает все настройки программы";
            return "Restores defaults for the whole app";
        }

        private static string TimeTab(string language)
        {
            if (language == "zh-CN") return "时间";
            if (language == "zh-TW") return "時間";
            if (language == "ja-JP") return "時刻";
            if (language == "ko-KR") return "시간";
            if (language == "de-DE") return "Zeit";
            if (language == "fr-FR") return "Heure";
            if (language == "es-ES") return "Hora";
            if (language == "pt-BR") return "Hora";
            if (language == "ru-RU") return "Время";
            return "Time";
        }
    }

    internal sealed class ModernSettingsForm : Form
    {
        public event Action<ClockSettings> SettingsApplied;

        private const int RowHeight = 38;
        private const int NoteRowHeight = 56;
        private const int CheckLabelTopPadding = 5;
        private const int ModuleGap = 56;
        private readonly ClockSettings source;
        private readonly ComboBox languageCombo = new ComboBox();
        private readonly SearchableTimeZoneBox timeZoneBox = new SearchableTimeZoneBox();
        private readonly ComboBox layoutCombo = new ComboBox();
        private readonly ComboBox timeFormatCombo = new ComboBox();
        private readonly ComboBox ampmCombo = new ComboBox();
        private readonly Label ampmLabel = new Label();
        private readonly ComboBox dateFormatCombo = new ComboBox();
        private readonly CheckBox showWeekdayCheck = new CenteredCheckBox();
        private readonly ComboBox weekdayFormatCombo = new ComboBox();
        private readonly ComboBox timeFontCombo = new ComboBox();
        private readonly ComboBox dateFontCombo = new ComboBox();
        private readonly NumericUpDown timeFontSizeBox = new NumericUpDown();
        private readonly NumericUpDown dateFontSizeBox = new NumericUpDown();
        private readonly Panel timeColorPreview = new Panel();
        private readonly Panel dateColorPreview = new Panel();
        private readonly NumericUpDown timeOffsetXBox = new NumericUpDown();
        private readonly NumericUpDown dateOffsetXBox = new NumericUpDown();
        private readonly Panel backColorPreview = new Panel();
        private readonly NumericUpDown widthBox = new NumericUpDown();
        private readonly NumericUpDown heightBox = new NumericUpDown();
        private readonly ComboBox positionCombo = new ComboBox();
        private readonly NumericUpDown offsetXBox = new NumericUpDown();
        private readonly NumericUpDown offsetYBox = new NumericUpDown();
        private readonly NumericUpDown xBox = new NumericUpDown();
        private readonly NumericUpDown yBox = new NumericUpDown();
        private readonly NumericUpDown paddingLeftBox = new NumericUpDown();
        private readonly CheckBox autoStartCheck = new CenteredCheckBox();
        private readonly CheckBox clickThroughCheck = new CenteredCheckBox();
        private readonly CheckBox autoCollapseCheck = new CenteredCheckBox();
        private readonly NumericUpDown collapseDelayBox = new NumericUpDown();
        private readonly NumericUpDown collapsedWidthBox = new NumericUpDown();
        private TabControl tabs;
        private Button closeButton;
        private Button applyButton;
        private Button saveButton;
        private Label clickThroughNote;
        private Label timeZoneHint;
        private Label backgroundNote;
        private int timeForeColorArgb;
        private int dateForeColorArgb;
        private int backColorArgb;
        private bool suppressEvents;
        private bool fontEventsAttached;

        public ModernSettingsForm(ClockSettings settings)
        {
            source = Copy(settings);
            Width = Math.Max(settings.SettingsWindowWidth, 700);
            Height = Math.Max(settings.SettingsWindowHeight, 620);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(700, 620);
            Font = CreateUiFont(9.0f);
            ResizeEnd += delegate { SaveWindowSize(); };
            FormClosing += delegate { SaveWindowSize(); };

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(14),
                BackColor = Color.FromArgb(250, 250, 250)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            Controls.Add(root);

            tabs = new TabControl { Dock = DockStyle.Fill, Font = CreateUiFont(9.0f) };
            root.Controls.Add(tabs, 0, 0);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            closeButton = new Button { Width = 86, Height = 30 };
            applyButton = new Button { Width = 86, Height = 30 };
            saveButton = new Button { Width = 86, Height = 30 };
            closeButton.Click += delegate { Hide(); };
            applyButton.Click += delegate { Apply(); };
            saveButton.Click += delegate
            {
                Apply();
                Hide();
            };
            buttons.Controls.Add(closeButton);
            buttons.Controls.Add(applyButton);
            buttons.Controls.Add(saveButton);
            root.Controls.Add(buttons, 0, 1);

            FillControls();
            LoadFromSettings(source);
            RebuildTabs();
        }

        private void RebuildTabs()
        {
            string lang = SelectedValue(languageCombo, source.LanguageCode);
            UiText.CurrentLanguage = lang;
            Text = UiText.T(lang, "settings");
            int selected = tabs.SelectedIndex < 0 ? 0 : tabs.SelectedIndex;
            tabs.SuspendLayout();
            tabs.Visible = false;
            try
            {
                tabs.TabPages.Clear();
                tabs.TabPages.Add(CreateLanguageTimeZoneTab(lang));
                tabs.TabPages.Add(CreateDisplayTab(lang));
                tabs.TabPages.Add(CreateTextTab(lang));
                tabs.TabPages.Add(CreateBackgroundTab(lang));
                tabs.TabPages.Add(CreatePositionTab(lang));
                tabs.TabPages.Add(CreateBehaviorTab(lang));
                if (tabs.TabPages.Count > 0) tabs.SelectedIndex = Math.Min(selected, tabs.TabPages.Count - 1);
                closeButton.Text = UiText.T(lang, "close");
                applyButton.Text = UiText.T(lang, "apply");
                saveButton.Text = UiText.T(lang, "save");
                UpdateAmPmVisibility();
            }
            finally
            {
                tabs.Visible = true;
                tabs.ResumeLayout(true);
            }
        }

        private TabPage CreateLanguageTimeZoneTab(string lang)
        {
            var page = CreatePage(UiText.T(lang, "languageTimeZone"));
            var grid = CreateGrid();
            page.Controls.Add(grid);
            int row = 0;
            AddRow(grid, ref row, UiText.T(lang, "language"), languageCombo);
            AddSpacer(grid, ref row);
            AddRow(grid, ref row, UiText.T(lang, "timezone"), timeZoneBox);
            timeZoneHint = NoteLabel(UiText.T(lang, "timezoneHint"));
            AddRow(grid, ref row, "", timeZoneHint, 42);
            return page;
        }

        private TabPage CreateDisplayTab(string lang)
        {
            var page = CreatePage(UiText.T(lang, "display"));
            var grid = CreateGrid();
            page.Controls.Add(grid);
            int row = 0;
            AddRow(grid, ref row, UiText.T(lang, "layout"), layoutCombo);
            AddSpacer(grid, ref row);
            AddRow(grid, ref row, UiText.T(lang, "timeFormat"), timeFormatCombo);
            if (SelectedValue(timeFormatCombo, source.Use24Hour ? "24" : "12") == "12")
            {
                AddRow(grid, ref row, UiText.T(lang, "ampm"), ampmCombo, RowHeight, ampmLabel);
            }
            AddSpacer(grid, ref row);
            AddRow(grid, ref row, UiText.T(lang, "dateFormat"), dateFormatCombo);
            AddRow(grid, ref row, UiText.T(lang, "showWeekday"), showWeekdayCheck);
            AddRow(grid, ref row, UiText.T(lang, "weekdayFormat"), weekdayFormatCombo);
            return page;
        }

        private TabPage CreateTextTab(string lang)
        {
            var page = CreatePage(UiText.T(lang, "text"));
            var grid = CreateGrid();
            page.Controls.Add(grid);
            int row = 0;
            AddRow(grid, ref row, UiText.T(lang, "timeFont"), timeFontCombo);
            AddRow(grid, ref row, UiText.T(lang, "timeSize"), timeFontSizeBox);
            AddRow(grid, ref row, UiText.T(lang, "timeColor"), ColorPickerRow(timeColorPreview, "time", lang));
            AddRow(grid, ref row, UiText.T(lang, "timeOffset"), timeOffsetXBox);
            AddSpacer(grid, ref row);
            AddRow(grid, ref row, UiText.T(lang, "dateFont"), dateFontCombo);
            AddRow(grid, ref row, UiText.T(lang, "dateSize"), dateFontSizeBox);
            AddRow(grid, ref row, UiText.T(lang, "dateColor"), ColorPickerRow(dateColorPreview, "date", lang));
            AddRow(grid, ref row, UiText.T(lang, "dateOffset"), dateOffsetXBox);
            AddSpacer(grid, ref row);
            AddRow(grid, ref row, UiText.T(lang, "textPadding"), paddingLeftBox);
            return page;
        }

        private TabPage CreateBackgroundTab(string lang)
        {
            var page = CreatePage(UiText.T(lang, "background"));
            var grid = CreateGrid();
            page.Controls.Add(grid);
            int row = 0;
            AddRow(grid, ref row, UiText.T(lang, "backColor"), ColorPickerRow(backColorPreview, "background", lang));
            AddRow(grid, ref row, UiText.T(lang, "width"), widthBox);
            AddRow(grid, ref row, UiText.T(lang, "height"), heightBox);
            backgroundNote = NoteLabel(UiText.T(lang, "backgroundNote"));
            AddRow(grid, ref row, "", backgroundNote, 50);
            return page;
        }

        private TabPage CreatePositionTab(string lang)
        {
            var page = CreatePage(UiText.T(lang, "position"));
            var grid = CreateGrid();
            page.Controls.Add(grid);
            int row = 0;
            AddRow(grid, ref row, UiText.T(lang, "presetPosition"), positionCombo);
            AddRow(grid, ref row, UiText.T(lang, "x"), xBox);
            AddRow(grid, ref row, UiText.T(lang, "y"), yBox);
            return page;
        }

        private TabPage CreateBehaviorTab(string lang)
        {
            var page = CreatePage(UiText.T(lang, "behavior"));
            var grid = CreateGrid();
            page.Controls.Add(grid);
            int row = 0;
            AddCheckRow(grid, ref row, UiText.T(lang, "autoStart"), ShiftedCheckBox(autoStartCheck, 4));
            AddSpacer(grid, ref row);
            AddCheckNoteRow(grid, ref row, UiText.T(lang, "clickThrough"), clickThroughCheck, UiText.T(lang, "clickThroughNote"));
            AddSpacer(grid, ref row);
            AddCheckRow(grid, ref row, UiText.T(lang, "autoCollapse"), ShiftedCheckBox(autoCollapseCheck, -2));
            AddRow(grid, ref row, UiText.T(lang, "collapseDelay"), collapseDelayBox);
            AddRow(grid, ref row, UiText.T(lang, "collapsedWidth"), collapsedWidthBox);
            AddSpacer(grid, ref row);
            AddFullWidthModule(grid, ref row, ResetModule(lang), 94);
            return page;
        }

        private static TabPage CreatePage(string title)
        {
            return new TabPage { Text = title, BackColor = Color.FromArgb(250, 250, 250), Padding = new Padding(14) };
        }

        private static TableLayoutPanel CreateGrid()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Padding = new Padding(0, 6, 0, 0)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return grid;
        }

        private void FillControls()
        {
            FillLanguageCombo();
            FillValueCombo(layoutCombo, new ValueItem("TimeAboveDate", "timeAbove"), new ValueItem("DateAboveTime", "dateAbove"));
            FillValueCombo(timeFormatCombo, new ValueItem("24", "time24"), new ValueItem("12", "time12"));
            FillValueCombo(ampmCombo, new ValueItem("Suffix", "suffix"), new ValueItem("Prefix", "prefix"), new ValueItem("Hidden", "hidden"));
            FillValueCombo(weekdayFormatCombo, new ValueItem("Localized", "localized"), new ValueItem("ddd", "enShort"), new ValueItem("dddd", "enLong"));
            FillValueCombo(positionCombo, new ValueItem("BottomLeft", "bottomLeft"), new ValueItem("BottomRight", "bottomRight"), new ValueItem("TopLeft", "topLeft"), new ValueItem("TopRight", "topRight"), new ValueItem("Custom", "custom"));
            FillFontCombo(timeFontCombo);
            FillFontCombo(dateFontCombo);

            dateFormatCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            dateFormatCombo.Items.AddRange(new object[] { "MM/dd", "yyyy/MM/dd", "dd/MM", "yyyy-MM-dd" });
            showWeekdayCheck.Text = "";
            autoStartCheck.Text = "";
            clickThroughCheck.Text = "";
            autoCollapseCheck.Text = "";
            PrepareCheckBox(showWeekdayCheck);
            PrepareCheckBox(autoStartCheck);
            PrepareCheckBox(clickThroughCheck);
            PrepareCheckBox(autoCollapseCheck);

            ConfigureNumber(timeFontSizeBox, 6, 28, 0.25m);
            ConfigureNumber(dateFontSizeBox, 6, 28, 0.25m);
            ConfigureNumber(widthBox, 20, 600, 1);
            ConfigureNumber(heightBox, 20, 300, 1);
            ConfigureNumber(offsetXBox, 0, 3000, 1);
            ConfigureNumber(offsetYBox, 0, 3000, 1);
            ConfigureNumber(xBox, -5000, 5000, 1);
            ConfigureNumber(yBox, -5000, 5000, 1);
            ConfigureNumber(paddingLeftBox, 0, 120, 1);
            ConfigureNumber(timeOffsetXBox, -100, 200, 1);
            ConfigureNumber(dateOffsetXBox, -100, 200, 1);
            ConfigureNumber(collapseDelayBox, 100, 10000, 100);
            ConfigureNumber(collapsedWidthBox, 2, 80, 1);

            languageCombo.SelectedIndexChanged += delegate
            {
                if (!suppressEvents)
                {
                    string lang = SelectedValue(languageCombo, source.LanguageCode);
                    suppressEvents = true;
                    UiText.CurrentLanguage = lang;
                    RefreshLocalizedOptions(lang);
                    timeZoneBox.SetLanguage(lang);
                    suppressEvents = false;
                    RebuildTabs();
                    Apply();
                }
            };
            timeFormatCombo.SelectedIndexChanged += delegate
            {
                if (!suppressEvents)
                {
                    RebuildTabs();
                    if (tabs.TabPages.Count > 1) tabs.SelectedIndex = 1;
                    Apply();
                }
            };
            positionCombo.SelectedIndexChanged += delegate { if (!suppressEvents) { UpdateCoordinatePreview(); Apply(); } };
            offsetXBox.ValueChanged += delegate { if (!suppressEvents) { UpdateCoordinatePreview(); Apply(); } };
            offsetYBox.ValueChanged += delegate { if (!suppressEvents) { UpdateCoordinatePreview(); Apply(); } };
            xBox.ValueChanged += delegate { if (!suppressEvents) { SelectValue(positionCombo, "Custom"); Apply(); } };
            yBox.ValueChanged += delegate { if (!suppressEvents) { SelectValue(positionCombo, "Custom"); Apply(); } };
            clickThroughCheck.CheckedChanged += delegate { if (!suppressEvents) Apply(); };
            layoutCombo.SelectedIndexChanged += delegate { if (!suppressEvents) Apply(); };
            ampmCombo.SelectedIndexChanged += delegate { if (!suppressEvents) Apply(); };
            dateFormatCombo.SelectedIndexChanged += delegate { if (!suppressEvents) Apply(); };
            showWeekdayCheck.CheckedChanged += delegate { if (!suppressEvents) Apply(); };
            weekdayFormatCombo.SelectedIndexChanged += delegate { if (!suppressEvents) Apply(); };
            timeFontCombo.SelectedIndexChanged += delegate { if (!suppressEvents) Apply(); };
            dateFontCombo.SelectedIndexChanged += delegate { if (!suppressEvents) Apply(); };
            timeFontSizeBox.ValueChanged += delegate { if (!suppressEvents) Apply(); };
            dateFontSizeBox.ValueChanged += delegate { if (!suppressEvents) Apply(); };
            widthBox.ValueChanged += delegate { if (!suppressEvents) { UpdateCoordinatePreview(); Apply(); } };
            heightBox.ValueChanged += delegate { if (!suppressEvents) { UpdateCoordinatePreview(); Apply(); } };
            paddingLeftBox.ValueChanged += delegate { if (!suppressEvents) Apply(); };
            timeOffsetXBox.ValueChanged += delegate { if (!suppressEvents) Apply(); };
            dateOffsetXBox.ValueChanged += delegate { if (!suppressEvents) Apply(); };
            autoStartCheck.CheckedChanged += delegate { if (!suppressEvents) Apply(); };
            autoCollapseCheck.CheckedChanged += delegate { if (!suppressEvents) Apply(); };
            collapseDelayBox.ValueChanged += delegate { if (!suppressEvents) Apply(); };
            collapsedWidthBox.ValueChanged += delegate { if (!suppressEvents) Apply(); };
            timeZoneBox.SelectionCommitted += delegate { if (!suppressEvents) Apply(); };
        }

        private void FillLanguageCombo()
        {
            languageCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            languageCombo.Items.AddRange(new object[]
            {
                new ValueItem("zh-CN", "简体中文"),
                new ValueItem("zh-TW", "繁體中文"),
                new ValueItem("en-US", "English"),
                new ValueItem("ja-JP", "日本語"),
                new ValueItem("ko-KR", "한국어"),
                new ValueItem("de-DE", "Deutsch"),
                new ValueItem("fr-FR", "Français"),
                new ValueItem("es-ES", "Español"),
                new ValueItem("pt-BR", "Português"),
                new ValueItem("ru-RU", "Русский")
            });
        }

        private static void FillValueCombo(ComboBox combo, params ValueItem[] items)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Items.Clear();
            combo.Items.AddRange(items);
        }

        private static void PrepareCheckBox(CheckBox checkBox)
        {
            checkBox.AutoSize = false;
            checkBox.Size = new Size(22, 22);
            checkBox.CheckAlign = ContentAlignment.MiddleLeft;
            checkBox.Margin = new Padding(0);
            checkBox.Padding = new Padding(0);
        }

        private void FillFontCombo(ComboBox combo)
        {
            string selected = SelectedValue(combo, "");
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Items.Clear();
            foreach (var item in GetRecommendedFonts(SelectedValue(languageCombo, source.LanguageCode)))
            {
                combo.Items.Add(item);
            }
            combo.Items.Add(new ValueItem("__more__", "moreFonts"));
            SelectFont(combo, string.IsNullOrEmpty(selected) ? "Segoe UI Variable Text" : selected);
            if (!fontEventsAttached)
            {
                combo.SelectedIndexChanged += delegate
                {
                    var item = combo.SelectedItem as ValueItem;
                    if (item != null && item.Value == "__more__")
                    {
                        using (var dialog = new FontDialog())
                        {
                            dialog.ShowEffects = false;
                            if (dialog.ShowDialog(this) == DialogResult.OK)
                            {
                                combo.Items.Insert(combo.Items.Count - 1, new ValueItem(dialog.Font.Name, dialog.Font.Name));
                                combo.SelectedIndex = combo.Items.Count - 2;
                            }
                            else
                            {
                                combo.SelectedIndex = 0;
                            }
                        }
                    }
                };
                if (combo == dateFontCombo) fontEventsAttached = true;
            }
        }

        private void RefreshLocalizedOptions(string lang)
        {
            string layout = SelectedValue(layoutCombo, source.Layout);
            string timeFormat = SelectedValue(timeFormatCombo, source.Use24Hour ? "24" : "12");
            string ampm = SelectedValue(ampmCombo, source.AmPmMode);
            string weekday = SelectedValue(weekdayFormatCombo, source.WeekdayFormat);
            string position = SelectedValue(positionCombo, source.Position);
            string timeFont = SelectedValue(timeFontCombo, source.TimeFontName);
            string dateFont = SelectedValue(dateFontCombo, source.DateFontName);

            FillValueCombo(layoutCombo, new ValueItem("TimeAboveDate", "timeAbove"), new ValueItem("DateAboveTime", "dateAbove"));
            FillValueCombo(timeFormatCombo, new ValueItem("24", "time24"), new ValueItem("12", "time12"));
            FillValueCombo(ampmCombo, new ValueItem("Suffix", "suffix"), new ValueItem("Prefix", "prefix"), new ValueItem("Hidden", "hidden"));
            FillValueCombo(weekdayFormatCombo, new ValueItem("Localized", "localized"), new ValueItem("ddd", "enShort"), new ValueItem("dddd", "enLong"));
            FillValueCombo(positionCombo, new ValueItem("BottomLeft", "bottomLeft"), new ValueItem("BottomRight", "bottomRight"), new ValueItem("TopLeft", "topLeft"), new ValueItem("TopRight", "topRight"), new ValueItem("Custom", "custom"));
            SelectValue(layoutCombo, layout);
            SelectValue(timeFormatCombo, timeFormat);
            SelectValue(ampmCombo, ampm);
            SelectValue(weekdayFormatCombo, weekday);
            SelectValue(positionCombo, position);
            FillFontCombo(timeFontCombo);
            FillFontCombo(dateFontCombo);
            SelectFont(timeFontCombo, timeFont);
            SelectFont(dateFontCombo, dateFont);
            layoutCombo.Invalidate();
            timeFormatCombo.Invalidate();
            ampmCombo.Invalidate();
            weekdayFormatCombo.Invalidate();
            positionCombo.Invalidate();
            timeFontCombo.Invalidate();
            dateFontCombo.Invalidate();
        }

        private static IEnumerable<ValueItem> GetRecommendedFonts(string language)
        {
            yield return new ValueItem("Segoe UI Variable Text", "Segoe UI Variable Text");
            yield return new ValueItem("Segoe UI", "Segoe UI");
            yield return new ValueItem("Microsoft YaHei UI", language == "zh-CN" ? "微软雅黑 UI" : "Microsoft YaHei UI");
            yield return new ValueItem("Microsoft JhengHei UI", language == "zh-TW" ? "微軟正黑體 UI" : "Microsoft JhengHei UI");
            yield return new ValueItem("DengXian", language == "zh-CN" ? "等线" : "DengXian");
            yield return new ValueItem("Arial", "Arial");
        }

        private void LoadFromSettings(ClockSettings settings)
        {
            suppressEvents = true;
            CopyInto(source, settings);
            SelectValue(languageCombo, settings.LanguageCode);
            timeZoneBox.SetLanguage(settings.LanguageCode);
            timeZoneBox.SetSelectedTimeZone(settings.TimeZoneId);
            SelectValue(layoutCombo, settings.Layout);
            SelectValue(timeFormatCombo, settings.Use24Hour ? "24" : "12");
            SelectValue(ampmCombo, settings.AmPmMode);
            SelectComboText(dateFormatCombo, settings.DateFormat);
            showWeekdayCheck.Checked = settings.ShowWeekday;
            SelectValue(weekdayFormatCombo, settings.WeekdayFormat);
            SelectFont(timeFontCombo, settings.TimeFontName);
            SelectFont(dateFontCombo, settings.DateFontName);
            timeFontSizeBox.Value = Clamp((decimal)settings.TimeFontSize, timeFontSizeBox.Minimum, timeFontSizeBox.Maximum);
            dateFontSizeBox.Value = Clamp((decimal)settings.DateFontSize, dateFontSizeBox.Minimum, dateFontSizeBox.Maximum);
            timeForeColorArgb = settings.TimeForeColorArgb;
            dateForeColorArgb = settings.DateForeColorArgb;
            backColorArgb = settings.BackColorArgb;
            timeColorPreview.BackColor = Color.FromArgb(timeForeColorArgb);
            dateColorPreview.BackColor = Color.FromArgb(dateForeColorArgb);
            backColorPreview.BackColor = Color.FromArgb(backColorArgb);
            widthBox.Value = Clamp(settings.Width, widthBox.Minimum, widthBox.Maximum);
            heightBox.Value = Clamp(settings.Height, heightBox.Minimum, heightBox.Maximum);
            SelectValue(positionCombo, settings.Position);
            offsetXBox.Value = Clamp(settings.OffsetX, offsetXBox.Minimum, offsetXBox.Maximum);
            offsetYBox.Value = Clamp(settings.OffsetY, offsetYBox.Minimum, offsetYBox.Maximum);
            xBox.Value = Clamp(settings.X, xBox.Minimum, xBox.Maximum);
            yBox.Value = Clamp(ScreenToUserY(settings.Y, settings.Height), yBox.Minimum, yBox.Maximum);
            paddingLeftBox.Value = Clamp(settings.PaddingLeft, paddingLeftBox.Minimum, paddingLeftBox.Maximum);
            timeOffsetXBox.Value = Clamp(settings.TimeOffsetX, timeOffsetXBox.Minimum, timeOffsetXBox.Maximum);
            dateOffsetXBox.Value = Clamp(settings.DateOffsetX, dateOffsetXBox.Minimum, dateOffsetXBox.Maximum);
            autoStartCheck.Checked = StartupManager.IsEnabled();
            clickThroughCheck.Checked = settings.ClickThrough;
            autoCollapseCheck.Checked = settings.AutoCollapse;
            collapseDelayBox.Value = Clamp(settings.CollapseDelayMs, collapseDelayBox.Minimum, collapseDelayBox.Maximum);
            collapsedWidthBox.Value = Clamp(settings.CollapsedWidth, collapsedWidthBox.Minimum, collapsedWidthBox.Maximum);
            suppressEvents = false;
            UpdateAmPmVisibility();
        }

        private void Apply()
        {
            var updated = ReadSettings();
            updated.Save();
            StartupManager.SetEnabled(autoStartCheck.Checked);
            CopyInto(source, updated);
            if (SettingsApplied != null) SettingsApplied(updated);
        }

        private void SaveWindowSize()
        {
            if (WindowState != FormWindowState.Normal)
            {
                return;
            }
            source.SettingsWindowWidth = Width;
            source.SettingsWindowHeight = Height;
            source.Save();
        }

        private ClockSettings ReadSettings()
        {
            var updated = Copy(source);
            updated.LanguageCode = SelectedValue(languageCombo, "zh-CN");
            string zoneId = timeZoneBox.SelectedTimeZoneId;
            if (!string.IsNullOrEmpty(zoneId)) updated.TimeZoneId = zoneId;
            updated.Layout = SelectedValue(layoutCombo, "TimeAboveDate");
            updated.Use24Hour = SelectedValue(timeFormatCombo, "24") == "24";
            updated.AmPmMode = SelectedValue(ampmCombo, "Suffix");
            updated.DateFormat = dateFormatCombo.Text;
            updated.ShowWeekday = showWeekdayCheck.Checked;
            updated.WeekdayFormat = SelectedValue(weekdayFormatCombo, "ddd");
            updated.TimeFontName = SelectedValue(timeFontCombo, "Segoe UI Variable Text");
            updated.DateFontName = SelectedValue(dateFontCombo, "Segoe UI Variable Text");
            updated.TimeFontSize = (float)timeFontSizeBox.Value;
            updated.DateFontSize = (float)dateFontSizeBox.Value;
            updated.TimeForeColorArgb = timeForeColorArgb;
            updated.DateForeColorArgb = dateForeColorArgb;
            updated.ForeColorArgb = timeForeColorArgb;
            updated.BackColorArgb = backColorArgb;
            updated.Width = (int)widthBox.Value;
            updated.Height = (int)heightBox.Value;
            updated.Position = SelectedValue(positionCombo, "BottomLeft");
            updated.OffsetX = (int)offsetXBox.Value;
            updated.OffsetY = (int)offsetYBox.Value;
            updated.X = (int)xBox.Value;
            updated.Y = UserToScreenY((int)yBox.Value, updated.Height);
            updated.PaddingLeft = (int)paddingLeftBox.Value;
            updated.TimeOffsetX = (int)timeOffsetXBox.Value;
            updated.DateOffsetX = (int)dateOffsetXBox.Value;
            updated.ClickThrough = clickThroughCheck.Checked;
            updated.AutoCollapse = autoCollapseCheck.Checked;
            updated.CollapseDelayMs = (int)collapseDelayBox.Value;
            updated.CollapsedWidth = (int)collapsedWidthBox.Value;
            updated.SettingsWindowWidth = Width;
            updated.SettingsWindowHeight = Height;
            return updated;
        }

        private void UpdateAmPmVisibility()
        {
            bool visible = SelectedValue(timeFormatCombo, "24") == "12";
            ampmCombo.Visible = visible;
            ampmLabel.Visible = visible;
        }

        private void UpdateCoordinatePreview()
        {
            string position = SelectedValue(positionCombo, "BottomLeft");
            if (position == "Custom") return;
            Rectangle screen = Screen.PrimaryScreen.Bounds;
            Rectangle taskbar = new Rectangle(screen.Left, screen.Bottom - 48, screen.Width, 48);
            int width = (int)widthBox.Value;
            int height = (int)heightBox.Value;
            int ox = (int)offsetXBox.Value;
            int oy = (int)offsetYBox.Value;
            int x = taskbar.Left + ox;
            int y = taskbar.Bottom - height - oy;
            if (position == "BottomRight") x = taskbar.Right - width + ox;
            if (position == "TopLeft") y = screen.Top - oy;
            if (position == "TopRight") { x = screen.Right - width + ox; y = screen.Top - oy; }
            suppressEvents = true;
            xBox.Value = Clamp(x, xBox.Minimum, xBox.Maximum);
            yBox.Value = Clamp(ScreenToUserY(y, height), yBox.Minimum, yBox.Maximum);
            suppressEvents = false;
        }

        private static int ScreenToUserY(int screenY, int height)
        {
            Rectangle screen = Screen.PrimaryScreen.Bounds;
            return screen.Bottom - height - screenY;
        }

        private static int UserToScreenY(int userY, int height)
        {
            Rectangle screen = Screen.PrimaryScreen.Bounds;
            return screen.Bottom - height - userY;
        }

        private Control ColorPickerRow(Panel preview, string target, string lang)
        {
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0), Padding = new Padding(0) };
            preview.Width = 70;
            preview.Height = 24;
            preview.BorderStyle = BorderStyle.FixedSingle;
            preview.Margin = new Padding(0, 2, 8, 2);
            var palette = new Button { Text = UiText.T(lang, "palette"), Width = 76, Height = 26, Margin = new Padding(0, 1, 6, 1) };
            var picker = new Button { Text = UiText.T(lang, "screenPick"), Width = 110, Height = 26, Margin = new Padding(0, 1, 0, 1) };
            palette.Click += delegate
            {
                using (var dialog = new ColorDialog())
                {
                    dialog.Color = preview.BackColor;
                    dialog.FullOpen = true;
                    if (dialog.ShowDialog(this) == DialogResult.OK) SetColor(preview, target, dialog.Color);
                }
            };
            picker.Click += delegate { ScreenColorPickerOverlay.Pick(this, UiText.T(lang, "pickHint"), delegate(Color color) { SetColor(preview, target, color); }); };
            panel.Controls.Add(preview);
            panel.Controls.Add(palette);
            panel.Controls.Add(picker);
            return panel;
        }

        private void SetColor(Panel preview, string target, Color color)
        {
            preview.BackColor = color;
            if (target == "background") backColorArgb = color.ToArgb();
            else if (target == "time") timeForeColorArgb = color.ToArgb();
            else if (target == "date") dateForeColorArgb = color.ToArgb();
            if (!suppressEvents) Apply();
        }

        private Control ResetModule(string lang)
        {
            var outer = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            var panel = new TableLayoutPanel
            {
                Width = 252,
                Height = 66,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            outer.Resize += delegate
            {
                panel.Width = Math.Min(outer.ClientSize.Width, 252);
                panel.Location = new Point(Math.Max(0, outer.ClientSize.Width - panel.Width), Math.Max(0, outer.ClientSize.Height - panel.Height));
            };
            var resetButton = new Button { Text = UiText.T(lang, "reset"), Dock = DockStyle.Fill, Height = 28, Margin = new Padding(0, 0, 0, 0) };
            resetButton.Click += delegate
            {
                var defaults = new ClockSettings();
                LoadFromSettings(defaults);
                Apply();
            };
            var note = NoteLabel(UiText.T(lang, "resetNote"));
            note.TextAlign = ContentAlignment.MiddleCenter;
            panel.Controls.Add(resetButton, 0, 0);
            panel.Controls.Add(note, 0, 1);
            outer.Controls.Add(panel);
            return outer;
        }

        private static Label NoteLabel(string text)
        {
            return new Label { Text = text, ForeColor = Color.FromArgb(120, 120, 120), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        }

        private static Control ShiftedCheckBox(CheckBox checkBox, int yOffset)
        {
            PrepareCheckBox(checkBox);
            var panel = new Panel
            {
                Height = RowHeight,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            panel.Resize += delegate
            {
                checkBox.Location = new Point(0, Math.Max(0, (panel.ClientSize.Height - checkBox.Height) / 2) + yOffset);
            };
            panel.Controls.Add(checkBox);
            return panel;
        }

        private void AddRow(TableLayoutPanel root, ref int row, string labelText, Control control)
        {
            AddRow(root, ref row, labelText, control, RowHeight, null);
        }

        private void AddRow(TableLayoutPanel root, ref int row, string labelText, Control control, int height)
        {
            AddRow(root, ref row, labelText, control, height, null);
        }

        private void AddRow(TableLayoutPanel root, ref int row, string labelText, Control control, int height, Label labelOverride)
        {
            AddRow(root, ref row, labelText, control, height, labelOverride, 0);
        }

        private void AddCheckRow(TableLayoutPanel root, ref int row, string labelText, Control control)
        {
            AddRow(root, ref row, labelText, control, RowHeight, null, CheckLabelTopPadding);
        }

        private void AddCheckNoteRow(TableLayoutPanel root, ref int row, string labelText, CheckBox checkBox, string noteText)
        {
            root.RowCount = row + 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, NoteRowHeight));

            Label label = new Label();
            label.Text = labelText;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.TopLeft;
            label.ForeColor = Color.FromArgb(45, 45, 45);
            label.Margin = new Padding(0);
            label.Padding = new Padding(0, 11, 0, 0);

            var host = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(0) };
            PrepareCheckBox(checkBox);
            checkBox.Location = new Point(0, 12);
            clickThroughNote = NoteLabel(noteText);
            clickThroughNote.Dock = DockStyle.None;
            clickThroughNote.Margin = new Padding(0);
            clickThroughNote.Padding = new Padding(0);
            clickThroughNote.TextAlign = ContentAlignment.TopLeft;
            clickThroughNote.AutoEllipsis = false;
            clickThroughNote.AutoSize = false;
            clickThroughNote.Location = new Point(38, 13);
            clickThroughNote.Height = 40;
            host.Resize += delegate
            {
                checkBox.Location = new Point(0, 12);
                clickThroughNote.Width = Math.Max(0, host.ClientSize.Width - clickThroughNote.Left);
            };
            host.Controls.Add(checkBox);
            host.Controls.Add(clickThroughNote);

            root.Controls.Add(label, 0, row);
            root.Controls.Add(host, 1, row);
            row++;
        }

        private void AddRow(TableLayoutPanel root, ref int row, string labelText, Control control, int height, Label labelOverride, int labelTopPadding)
        {
            root.RowCount = row + 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            Label label = labelOverride ?? new Label();
            label.Text = labelText;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = Color.FromArgb(45, 45, 45);
            label.Margin = new Padding(0);
            label.Padding = new Padding(0, labelTopPadding, 0, 0);
            root.Controls.Add(label, 0, row);
            root.Controls.Add(CreateCellHost(control), 1, row);
            row++;
        }

        private static Control CreateCellHost(Control control)
        {
            var host = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(0) };
            control.Dock = DockStyle.None;
            control.Margin = new Padding(0);
            host.Controls.Add(control);
            host.Resize += delegate
            {
                var checkBox = control as CheckBox;
                if (checkBox != null)
                {
                    control.Location = new Point(0, Math.Max(0, (host.ClientSize.Height - control.Height) / 2));
                    return;
                }
                control.Width = Math.Max(0, host.ClientSize.Width);
                control.Location = new Point(0, Math.Max(0, (host.ClientSize.Height - control.Height) / 2));
            };
            return host;
        }

        private void AddSpacer(TableLayoutPanel root, ref int row)
        {
            root.RowCount = row + 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, ModuleGap));
            root.Controls.Add(new Label { Dock = DockStyle.Fill }, 0, row);
            root.Controls.Add(new Label { Dock = DockStyle.Fill }, 1, row);
            row++;
        }

        private void AddFullWidthModule(TableLayoutPanel root, ref int row, Control control, int height)
        {
            root.RowCount = row + 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            control.Margin = new Padding(0);
            control.Dock = DockStyle.Fill;
            root.Controls.Add(control, 0, row);
            root.SetColumnSpan(control, 2);
            row++;
        }

        private static void ConfigureNumber(NumericUpDown box, decimal min, decimal max, decimal step)
        {
            box.Minimum = min;
            box.Maximum = max;
            box.Increment = step;
            box.DecimalPlaces = step < 1 ? 2 : 0;
        }

        private static void SelectComboText(ComboBox combo, string text)
        {
            int index = combo.FindStringExact(text);
            if (index >= 0) combo.SelectedIndex = index;
            else combo.Text = text;
        }

        private static void SelectValue(ComboBox combo, string value)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                var item = combo.Items[i] as ValueItem;
                if (item != null && item.Value == value) { combo.SelectedIndex = i; return; }
            }
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private static void SelectFont(ComboBox combo, string value)
        {
            SelectValue(combo, value);
            if (combo.SelectedIndex < 0)
            {
                combo.Items.Insert(combo.Items.Count - 1, new ValueItem(value, value));
                combo.SelectedIndex = combo.Items.Count - 2;
            }
        }

        private static string SelectedValue(ComboBox combo, string fallback)
        {
            var item = combo.SelectedItem as ValueItem;
            return item == null ? fallback : item.Value;
        }

        private static decimal Clamp(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static Font CreateUiFont(float size)
        {
            try { return new Font("Segoe UI Variable Text", size, FontStyle.Regular, GraphicsUnit.Point); }
            catch { return new Font("Segoe UI", size, FontStyle.Regular, GraphicsUnit.Point); }
        }

        private static ClockSettings Copy(ClockSettings s)
        {
            return new ClockSettings
            {
                LanguageCode = s.LanguageCode,
                TimeZoneId = s.TimeZoneId,
                Layout = s.Layout,
                Use24Hour = s.Use24Hour,
                AmPmMode = s.AmPmMode,
                DateFormat = s.DateFormat,
                ShowWeekday = s.ShowWeekday,
                WeekdayFormat = s.WeekdayFormat,
                TimeFontName = s.TimeFontName,
                TimeFontSize = s.TimeFontSize,
                DateFontName = s.DateFontName,
                DateFontSize = s.DateFontSize,
                ForeColorArgb = s.ForeColorArgb,
                TimeForeColorArgb = s.TimeForeColorArgb,
                DateForeColorArgb = s.DateForeColorArgb,
                BackColorArgb = s.BackColorArgb,
                Position = s.Position,
                OffsetX = s.OffsetX,
                OffsetY = s.OffsetY,
                Width = s.Width,
                Height = s.Height,
                PaddingLeft = s.PaddingLeft,
                PaddingTop = s.PaddingTop,
                PaddingRight = s.PaddingRight,
                PaddingBottom = s.PaddingBottom,
                TimeOffsetX = s.TimeOffsetX,
                DateOffsetX = s.DateOffsetX,
                X = s.X,
                Y = s.Y,
                ClickThrough = s.ClickThrough,
                AutoCollapse = s.AutoCollapse,
                CollapseDelayMs = s.CollapseDelayMs,
                CollapsedWidth = s.CollapsedWidth,
                SettingsWindowWidth = s.SettingsWindowWidth,
                SettingsWindowHeight = s.SettingsWindowHeight
            };
        }

        private static void CopyInto(ClockSettings target, ClockSettings s)
        {
            target.LanguageCode = s.LanguageCode;
            target.TimeZoneId = s.TimeZoneId;
            target.Layout = s.Layout;
            target.Use24Hour = s.Use24Hour;
            target.AmPmMode = s.AmPmMode;
            target.DateFormat = s.DateFormat;
            target.ShowWeekday = s.ShowWeekday;
            target.WeekdayFormat = s.WeekdayFormat;
            target.TimeFontName = s.TimeFontName;
            target.TimeFontSize = s.TimeFontSize;
            target.DateFontName = s.DateFontName;
            target.DateFontSize = s.DateFontSize;
            target.ForeColorArgb = s.ForeColorArgb;
            target.TimeForeColorArgb = s.TimeForeColorArgb;
            target.DateForeColorArgb = s.DateForeColorArgb;
            target.BackColorArgb = s.BackColorArgb;
            target.Position = s.Position;
            target.OffsetX = s.OffsetX;
            target.OffsetY = s.OffsetY;
            target.Width = s.Width;
            target.Height = s.Height;
            target.PaddingLeft = s.PaddingLeft;
            target.PaddingTop = s.PaddingTop;
            target.PaddingRight = s.PaddingRight;
            target.PaddingBottom = s.PaddingBottom;
            target.TimeOffsetX = s.TimeOffsetX;
            target.DateOffsetX = s.DateOffsetX;
            target.X = s.X;
            target.Y = s.Y;
            target.ClickThrough = s.ClickThrough;
            target.AutoCollapse = s.AutoCollapse;
            target.CollapseDelayMs = s.CollapseDelayMs;
            target.CollapsedWidth = s.CollapsedWidth;
            target.SettingsWindowWidth = s.SettingsWindowWidth;
            target.SettingsWindowHeight = s.SettingsWindowHeight;
        }
    }

    internal sealed class SearchableTimeZoneBox : UserControl, IMessageFilter
    {
        private readonly TextBox textBox = new TextBox();
        private readonly DropDownGlyphButton dropButton = new DropDownGlyphButton();
        private readonly TimeZoneDropDownList listBox = new TimeZoneDropDownList();
        private readonly Panel dropPanel = new Panel();
        private readonly List<TimeZoneListItem> allItems = new List<TimeZoneListItem>();
        private string language = "zh-CN";
        private string selectedId = "China Standard Time";
        private bool suppressText;
        private Form parentForm;
        private Control popupHost;
        private bool messageFilterInstalled;

        public event Action SelectionCommitted;

        public string SelectedTimeZoneId { get { return selectedId; } }

        public SearchableTimeZoneBox()
        {
            Height = 23;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Dock = DockStyle.Fill;
            dropButton.Dock = DockStyle.Right;
            dropButton.Width = 24;
            dropButton.TabStop = false;
            Controls.Add(textBox);
            Controls.Add(dropButton);

            listBox.BorderStyle = BorderStyle.None;
            listBox.DrawMode = DrawMode.OwnerDrawFixed;
            listBox.IntegralHeight = false;
            listBox.ItemHeight = 32;
            listBox.Dock = DockStyle.Fill;
            listBox.MouseMove += delegate(object sender, MouseEventArgs e)
            {
                int index = listBox.IndexFromPoint(e.Location);
                if (index >= 0 && index < listBox.Items.Count && listBox.SelectedIndex != index)
                {
                    listBox.SelectedIndex = index;
                }
            };
            listBox.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                int index = listBox.IndexFromPoint(e.Location);
                if (index >= 0)
                {
                    listBox.SelectedIndex = index;
                    CommitSelection();
                }
            };
            listBox.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    CommitSelection();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    CancelEditing(true);
                }
            };

            dropPanel.Visible = false;
            dropPanel.BorderStyle = BorderStyle.FixedSingle;
            dropPanel.BackColor = SystemColors.Window;
            dropPanel.Margin = new Padding(0);
            dropPanel.Padding = new Padding(0);
            dropPanel.Controls.Add(listBox);

            textBox.Enter += delegate
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    textBox.SelectAll();
                    Filter("");
                    ShowDropDown();
                });
            };
            textBox.MouseDown += delegate
            {
                bool wasFocused = textBox.Focused;
                BeginInvoke((MethodInvoker)delegate
                {
                    if (!wasFocused) textBox.SelectAll();
                    if (!dropPanel.Visible) Filter("");
                    ShowDropDown();
                });
            };
            textBox.TextChanged += delegate
            {
                if (!suppressText)
                {
                    Filter(textBox.Text);
                    ShowDropDown();
                }
            };
            textBox.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Down)
                {
                    if (listBox.Items.Count > 0)
                    {
                        listBox.Focus();
                        if (listBox.SelectedIndex < 0) listBox.SelectedIndex = 0;
                    }
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    CancelEditing(true);
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    if (listBox.Items.Count > 0)
                    {
                        if (listBox.SelectedIndex < 0) listBox.SelectedIndex = 0;
                        CommitSelection();
                    }
                    e.Handled = true;
                }
            };
            textBox.Leave += delegate
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    if (!listBox.Focused && !dropButton.Focused && !dropPanel.ContainsFocus)
                    {
                        CancelEditing(false);
                    }
                });
            };
            dropButton.Click += delegate
            {
                if (dropPanel.Visible)
                {
                    CancelEditing(true);
                    return;
                }
                textBox.Focus();
                textBox.SelectAll();
                Filter("");
                ShowDropDown();
            };
            LoadItems();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            if (parentForm != null)
            {
                parentForm.Move -= ParentFormMoved;
                parentForm.Deactivate -= ParentFormMoved;
                parentForm.MouseDown -= ParentFormMouseDown;
            }
            if (popupHost != null && !popupHost.IsDisposed)
            {
                popupHost.Controls.Remove(dropPanel);
            }
            base.OnParentChanged(e);
            parentForm = FindForm();
            popupHost = FindPopupHost();
            if (parentForm != null)
            {
                parentForm.Move += ParentFormMoved;
                parentForm.Deactivate += ParentFormMoved;
                parentForm.MouseDown += ParentFormMouseDown;
            }
            if (popupHost != null)
            {
                popupHost.Controls.Add(dropPanel);
                dropPanel.BringToFront();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Height = 23;
            UpdateDropDownSize();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (parentForm != null)
                {
                    parentForm.Move -= ParentFormMoved;
                    parentForm.Deactivate -= ParentFormMoved;
                    parentForm.MouseDown -= ParentFormMouseDown;
                }
                if (popupHost != null && !popupHost.IsDisposed)
                {
                    popupHost.Controls.Remove(dropPanel);
                }
                UninstallMessageFilter();
                dropPanel.Dispose();
            }
            base.Dispose(disposing);
        }

        public void SetLanguage(string languageCode)
        {
            language = languageCode;
            LoadItems();
            SetSelectedTimeZone(selectedId);
        }

        public void SetSelectedTimeZone(string id)
        {
            selectedId = id;
            TimeZoneListItem item = allItems.Find(x => x.Id == id);
            suppressText = true;
            textBox.Text = item == null ? id : item.ToString();
            ClearTextSelection();
            suppressText = false;
            Filter("");
        }

        private void LoadItems()
        {
            allItems.Clear();
            foreach (TimeZoneInfo zone in TimeZoneInfo.GetSystemTimeZones())
            {
                allItems.Add(new TimeZoneListItem(zone, language));
            }
            Filter("");
        }

        private void Filter(string filter)
        {
            listBox.BeginUpdate();
            listBox.Items.Clear();
            foreach (TimeZoneListItem item in allItems)
            {
                if (string.IsNullOrWhiteSpace(filter) || item.Matches(filter))
                {
                    listBox.Items.Add(item);
                }
            }
            int selectedIndex = FindSelectedIndex();
            if (listBox.Items.Count > 0)
            {
                listBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            }
            listBox.EndUpdate();
            if (dropPanel.Visible)
            {
                UpdateDropDownSize();
                PositionDropPanel();
            }
        }

        private int FindSelectedIndex()
        {
            for (int i = 0; i < listBox.Items.Count; i++)
            {
                var item = listBox.Items[i] as TimeZoneListItem;
                if (item != null && item.Id == selectedId) return i;
            }
            return -1;
        }

        private void ShowDropDown()
        {
            UpdateDropDownSize();
            PositionDropPanel();
            if (!dropPanel.Visible)
            {
                dropPanel.Visible = true;
            }
            InstallMessageFilter();
            dropPanel.BringToFront();
            textBox.Focus();
        }

        private void UpdateDropDownSize()
        {
            if (dropPanel == null)
            {
                return;
            }
            int height = Math.Min(320, Math.Max(96, listBox.ItemHeight * Math.Min(Math.Max(1, listBox.Items.Count), 10)));
            var size = new Size(Math.Max(Width, 280), height);
            dropPanel.Size = size;
            listBox.Size = new Size(Math.Max(0, size.Width - 2), Math.Max(0, size.Height - 2));
            listBox.Location = new Point(1, 1);
        }

        private void PositionDropPanel()
        {
            if (popupHost == null || popupHost.IsDisposed)
            {
                popupHost = FindPopupHost();
                if (popupHost != null && dropPanel.Parent != popupHost)
                {
                    popupHost.Controls.Add(dropPanel);
                }
            }

            if (popupHost == null)
            {
                return;
            }

            Point screenPoint = PointToScreen(new Point(0, Height));
            Point hostPoint = popupHost.PointToClient(screenPoint);
            Rectangle client = popupHost.ClientRectangle;
            int x = Math.Max(0, Math.Min(hostPoint.X, Math.Max(0, client.Width - dropPanel.Width)));
            int y = hostPoint.Y;
            if (y + dropPanel.Height > client.Height)
            {
                y = Math.Max(0, popupHost.PointToClient(PointToScreen(Point.Empty)).Y - dropPanel.Height);
            }
            dropPanel.Location = new Point(x, y);
        }

        private Control FindPopupHost()
        {
            Control current = Parent;
            while (current != null)
            {
                if (current is TabPage)
                {
                    return current;
                }
                current = current.Parent;
            }
            return FindForm();
        }

        private void CommitSelection()
        {
            var item = listBox.SelectedItem as TimeZoneListItem;
            if (item == null)
            {
                return;
            }
            selectedId = item.Id;
            suppressText = true;
            textBox.Text = item.ToString();
            ClearTextSelection();
            suppressText = false;
            Filter("");
            dropPanel.Visible = false;
            UninstallMessageFilter();
            dropButton.Focus();
            if (parentForm != null) parentForm.ActiveControl = null;
            if (SelectionCommitted != null) SelectionCommitted();
        }

        private void CancelEditing(bool moveFocus)
        {
            TimeZoneListItem item = allItems.Find(x => x.Id == selectedId);
            suppressText = true;
            textBox.Text = item == null ? selectedId : item.ToString();
            ClearTextSelection();
            suppressText = false;
            Filter("");
            dropPanel.Visible = false;
            ClearTextSelection();
            UninstallMessageFilter();
            if (moveFocus)
            {
                dropButton.Focus();
            }
            if (parentForm != null) parentForm.ActiveControl = null;
        }

        private void ClearTextSelection()
        {
            textBox.SelectionStart = Math.Min(textBox.Text.Length, textBox.SelectionStart);
            textBox.SelectionLength = 0;
        }

        private void ParentFormMoved(object sender, EventArgs e)
        {
            if (dropPanel.Visible)
            {
                PositionDropPanel();
                dropPanel.BringToFront();
            }
        }

        private void ParentFormMouseDown(object sender, MouseEventArgs e)
        {
            if (!textBox.Focused && !dropPanel.Visible)
            {
                return;
            }
            Point screen = parentForm.PointToScreen(e.Location);
            if (!ClientRectangle.Contains(PointToClient(screen)) && !dropPanel.Bounds.Contains(dropPanel.Parent.PointToClient(screen)))
            {
                CancelEditing(true);
            }
        }

        private void InstallMessageFilter()
        {
            if (messageFilterInstalled)
            {
                return;
            }

            Application.AddMessageFilter(this);
            messageFilterInstalled = true;
        }

        private void UninstallMessageFilter()
        {
            if (!messageFilterInstalled)
            {
                return;
            }

            Application.RemoveMessageFilter(this);
            messageFilterInstalled = false;
        }

        public bool PreFilterMessage(ref Message m)
        {
            const int wmLButtonDown = 0x0201;
            const int wmRButtonDown = 0x0204;
            const int wmMButtonDown = 0x0207;

            if (!dropPanel.Visible || (m.Msg != wmLButtonDown && m.Msg != wmRButtonDown && m.Msg != wmMButtonDown))
            {
                return false;
            }

            Control target = Control.FromHandle(m.HWnd);
            if (IsControlOrChild(target, this) || IsControlOrChild(target, dropPanel))
            {
                return false;
            }

            CancelEditing(true);
            return false;
        }

        private static bool IsControlOrChild(Control target, Control root)
        {
            Control current = target;
            while (current != null)
            {
                if (current == root)
                {
                    return true;
                }
                current = current.Parent;
            }
            return false;
        }

        private sealed class DropDownGlyphButton : Button
        {
            public DropDownGlyphButton()
            {
                FlatStyle = FlatStyle.Standard;
            }

            protected override void OnPaint(PaintEventArgs pevent)
            {
                System.Windows.Forms.VisualStyles.ComboBoxState state = Enabled ? System.Windows.Forms.VisualStyles.ComboBoxState.Normal : System.Windows.Forms.VisualStyles.ComboBoxState.Disabled;
                ComboBoxRenderer.DrawDropDownButton(pevent.Graphics, ClientRectangle, state);
            }
        }

        private sealed class TimeZoneDropDownList : ListBox
        {
            public TimeZoneDropDownList()
            {
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                int visibleRows = Math.Max(1, ClientSize.Height / Math.Max(1, ItemHeight));
                int maxTop = Math.Max(0, Items.Count - visibleRows);
                int lines = Math.Max(5, SystemInformation.MouseWheelScrollLines);
                int direction = e.Delta > 0 ? -lines : lines;
                TopIndex = Math.Max(0, Math.Min(maxTop, TopIndex + direction));
            }

            protected override void OnDrawItem(DrawItemEventArgs e)
            {
                if (e.Index < 0) return;
                bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color back = selected ? SystemColors.Highlight : SystemColors.Window;
                Color fore = selected ? SystemColors.HighlightText : SystemColors.WindowText;
                using (var backBrush = new SolidBrush(back))
                {
                    e.Graphics.FillRectangle(backBrush, e.Bounds);
                    Rectangle textBounds = new Rectangle(e.Bounds.Left + 10, e.Bounds.Top + 7, e.Bounds.Width - 20, e.Bounds.Height - 14);
                    TextRenderer.DrawText(e.Graphics, Items[e.Index].ToString(), Font, textBounds, fore, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                }
            }
        }
    }

    internal sealed class ScreenColorPickerOverlay : Form
    {
        private readonly Action<Color> picked;
        private readonly Panel swatch;
        private readonly Label valueLabel;
        private readonly Label hintLabel;
        private readonly System.Windows.Forms.Timer timer;
        private readonly NativeMethods.LowLevelMouseProc mouseProc;
        private IntPtr mouseHook = IntPtr.Zero;
        private bool closed;
        private bool readyForClick;

        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;

        private ScreenColorPickerOverlay(string hintText, Action<Color> picked)
        {
            this.picked = picked;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            Size = new Size(204, 58);
            BackColor = Color.FromArgb(245, 245, 245);
            KeyPreview = true;

            swatch = new Panel { Size = new Size(26, 26), Location = new Point(10, 10), BackColor = Color.White };
            using (GraphicsPathHolder.Round(swatch, 13)) { }
            valueLabel = new Label
            {
                AutoSize = false,
                Location = new Point(46, 8),
                Size = new Size(148, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.Black
            };
            hintLabel = new Label
            {
                AutoSize = false,
                Location = new Point(46, 29),
                Size = new Size(148, 18),
                Text = hintText,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(110, 110, 110)
            };
            Controls.Add(swatch);
            Controls.Add(valueLabel);
            Controls.Add(hintLabel);

            timer = new System.Windows.Forms.Timer { Interval = 30 };
            timer.Tick += delegate { TickPicker(); };
            mouseProc = MouseHookCallback;
            KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    ClosePicker();
                }
            };
            Shown += delegate { StartPicker(); };
        }

        public static void Pick(Form owner, string hintText, Action<Color> picked)
        {
            var overlay = new ScreenColorPickerOverlay(hintText, picked);
            overlay.Show(owner);
        }

        private void StartPicker()
        {
            InstallHook();
            timer.Start();
            TickPicker();
        }

        private void TickPicker()
        {
            Point cursor = Cursor.Position;
            Color color = PickPixel(cursor);
            swatch.BackColor = color;
            valueLabel.Text = ToHex(color) + "  RGB " + color.R + ", " + color.G + ", " + color.B;
            Location = CalculatePopupLocation(cursor);

            bool leftDown = IsMouseDown(Keys.LButton);
            bool rightDown = IsMouseDown(Keys.RButton);
            if (!readyForClick)
            {
                readyForClick = !leftDown && !rightDown;
                return;
            }
            if (leftDown)
            {
                PickCurrentColor();
            }
            else if (rightDown)
            {
                ClosePicker();
            }
        }

        private Point CalculatePopupLocation(Point cursor)
        {
            Rectangle screen = SystemInformation.VirtualScreen;
            int x = cursor.X + 18;
            int y = cursor.Y + 18;
            if (x + Width > screen.Right) x = cursor.X - Width - 18;
            if (y + Height > screen.Bottom) y = cursor.Y - Height - 18;
            if (x < screen.Left) x = screen.Left;
            if (y < screen.Top) y = screen.Top;
            return new Point(x, y);
        }

        private void InstallHook()
        {
            IntPtr module = IntPtr.Zero;
            try
            {
                using (Process process = Process.GetCurrentProcess())
                using (ProcessModule currentModule = process.MainModule)
                {
                    module = NativeMethods.GetModuleHandle(currentModule.ModuleName);
                }
            }
            catch
            {
                module = IntPtr.Zero;
            }
            mouseHook = NativeMethods.SetWindowsHookEx(WH_MOUSE_LL, mouseProc, module, 0);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && readyForClick)
            {
                int message = wParam.ToInt32();
                if (message == WM_LBUTTONDOWN)
                {
                    PickCurrentColor();
                    return (IntPtr)1;
                }
                if (message == WM_RBUTTONDOWN)
                {
                    ClosePicker();
                    return (IntPtr)1;
                }
            }
            return NativeMethods.CallNextHookEx(mouseHook, nCode, wParam, lParam);
        }

        private void PickCurrentColor()
        {
            Color color = PickPixel(Cursor.Position);
            picked(color);
            ClosePicker();
        }

        private void ClosePicker()
        {
            if (closed) return;
            closed = true;
            timer.Stop();
            if (mouseHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(mouseHook);
                mouseHook = IntPtr.Zero;
            }
            Close();
            Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (mouseHook != IntPtr.Zero)
                {
                    NativeMethods.UnhookWindowsHookEx(mouseHook);
                    mouseHook = IntPtr.Zero;
                }
                timer.Dispose();
            }
            base.Dispose(disposing);
        }

        private static Color PickPixel(Point point)
        {
            IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
            if (hdc == IntPtr.Zero) return Color.Empty;
            try
            {
                int colorRef = NativeMethods.GetPixel(hdc, point.X, point.Y);
                if (colorRef < 0) return Color.Empty;
                int r = colorRef & 0x000000FF;
                int g = (colorRef & 0x0000FF00) >> 8;
                int b = (colorRef & 0x00FF0000) >> 16;
                return Color.FromArgb(r, g, b);
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        private static bool IsMouseDown(Keys key)
        {
            return (NativeMethods.GetAsyncKeyState((int)key) & unchecked((short)0x8000)) != 0;
        }

        private static string ToHex(Color color)
        {
            return "#" + color.R.ToString("X2", CultureInfo.InvariantCulture) + color.G.ToString("X2", CultureInfo.InvariantCulture) + color.B.ToString("X2", CultureInfo.InvariantCulture);
        }
    }

    internal sealed class GraphicsPathHolder : IDisposable
    {
        public static GraphicsPathHolder Round(Control control, int radius)
        {
            var holder = new GraphicsPathHolder();
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, control.Width - 1, control.Height - 1);
            control.Region = new Region(path);
            holder.path = path;
            return holder;
        }

        private System.Drawing.Drawing2D.GraphicsPath path;

        public void Dispose()
        {
            if (path != null) path.Dispose();
        }
    }

    internal sealed class TimeZoneListItem
    {
        public readonly string Id;
        private readonly string display;
        private readonly string searchable;

        public TimeZoneListItem(TimeZoneInfo zone, string languageCode)
        {
            Id = zone.Id;
            string offset = FormatOffset(zone.BaseUtcOffset);
            string commonName = CommonZoneName(zone.BaseUtcOffset, languageCode);
            string name = FriendlyName(zone, languageCode);
            display = offset + (commonName.Length == 0 ? "" : "  " + commonName) + "  " + name;
            searchable = (zone.Id + " " + zone.DisplayName + " " + zone.StandardName + " " + zone.DaylightName + " " + offset + " " + name + " " + SearchAliases(zone.Id)).ToLowerInvariant();
        }

        public bool Matches(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }
            return searchable.Contains(filter.Trim().ToLowerInvariant());
        }

        public override string ToString()
        {
            return display;
        }

        private static string FormatOffset(TimeSpan offset)
        {
            string sign = offset < TimeSpan.Zero ? "-" : "+";
            offset = offset.Duration();
            return "UTC" + sign + offset.Hours.ToString("00", CultureInfo.InvariantCulture) + ":" + offset.Minutes.ToString("00", CultureInfo.InvariantCulture);
        }

        private static string FriendlyName(TimeZoneInfo zone, string languageCode)
        {
            string localized = LocalizedCommonName(zone.Id, languageCode);
            if (localized.Length > 0)
            {
                return localized;
            }

            if (languageCode == "zh-CN" || languageCode == "zh-TW")
            {
                if (zone.Id == "China Standard Time") return languageCode == "zh-TW" ? "北京、香港、烏魯木齊" : "北京、香港、乌鲁木齐";
                if (zone.Id == "US Mountain Standard Time") return languageCode == "zh-TW" ? "美國、亞利桑那" : "美国、亚利桑那";
                if (zone.Id == "Pacific Standard Time") return languageCode == "zh-TW" ? "美國、加拿大、太平洋時間" : "美国、加拿大、太平洋时间";
                if (zone.Id == "Eastern Standard Time") return languageCode == "zh-TW" ? "美國、加拿大、東部時間" : "美国、加拿大、东部时间";
                if (zone.Id == "Central Standard Time") return languageCode == "zh-TW" ? "美國、加拿大、中部時間" : "美国、加拿大、中部时间";
                if (zone.Id == "Tokyo Standard Time") return languageCode == "zh-TW" ? "東京、大阪、札幌" : "东京、大阪、札幌";
                if (zone.Id == "Korea Standard Time") return languageCode == "zh-TW" ? "首爾" : "首尔";
            }
            if (languageCode == "zh-CN" || languageCode == "zh-TW")
            {
                return StripWindowsOffset(zone.DisplayName);
            }
            return EnglishFallbackName(zone.Id);
        }

        private static readonly Dictionary<string, Dictionary<string, string>> LocalizedWindowsTimeZoneNames = new Dictionary<string, Dictionary<string, string>>
        {
            {"zh-CN", new Dictionary<string, string>
            {
                {"Afghanistan Standard Time", "阿富汗时间 - 喀布尔"},
                {"Alaskan Standard Time", "阿拉斯加时间 - 安克雷奇"},
                {"Aleutian Standard Time", "夏威夷-阿留申时间 - 埃达克"},
                {"Altai Standard Time", "克拉斯诺亚尔斯克时间 - 巴尔瑙尔"},
                {"Arab Standard Time", "阿拉伯时间 - 利雅得"},
                {"Arabian Standard Time", "海湾标准时间 - 迪拜"},
                {"Arabic Standard Time", "阿拉伯时间 - 巴格达"},
                {"Argentina Standard Time", "阿根廷时间 - 布宜诺斯艾利斯"},
                {"Astrakhan Standard Time", "萨马拉时间 - 阿斯特拉罕"},
                {"Atlantic Standard Time", "大西洋时间 - 哈利法克斯"},
                {"AUS Central Standard Time", "澳大利亚中部时间 - 达尔文"},
                {"Aus Central W. Standard Time", "澳大利亚中西部时间 - 尤克拉"},
                {"AUS Eastern Standard Time", "澳大利亚东部时间 - 悉尼"},
                {"Azerbaijan Standard Time", "阿塞拜疆时间 - 巴库"},
                {"Azores Standard Time", "亚速尔群岛时间"},
                {"Bahia Standard Time", "巴西利亚时间 - 巴伊亚"},
                {"Bangladesh Standard Time", "孟加拉时间 - 达卡"},
                {"Belarus Standard Time", "莫斯科时间 - 明斯克"},
                {"Bougainville Standard Time", "巴布亚新几内亚时间 - 布干维尔"},
                {"Canada Central Standard Time", "北美中部时间 - 里贾纳"},
                {"Cape Verde Standard Time", "佛得角时间"},
                {"Caucasus Standard Time", "亚美尼亚时间 - 埃里温"},
                {"Cen. Australia Standard Time", "澳大利亚中部时间 - 阿德莱德"},
                {"Central America Standard Time", "北美中部时间 - 危地马拉"},
                {"Central Asia Standard Time", "吉尔吉斯斯坦时间 - 比什凯克"},
                {"Central Brazilian Standard Time", "亚马逊时间 - 库亚巴"},
                {"Central Europe Standard Time", "中欧时间 - 布达佩斯"},
                {"Central European Standard Time", "中欧时间 - 华沙"},
                {"Central Pacific Standard Time", "所罗门群岛时间 - 瓜达尔卡纳尔"},
                {"Central Standard Time", "北美中部时间 - 芝加哥"},
                {"Central Standard Time (Mexico)", "北美中部时间 - 墨西哥城"},
                {"Chatham Islands Standard Time", "查塔姆时间"},
                {"China Standard Time", "中国时间 - 上海"},
                {"Cuba Standard Time", "古巴时间 - 哈瓦那"},
                {"Dateline Standard Time", "UTC-12:00"},
                {"E. Africa Standard Time", "东部非洲时间 - 内罗毕"},
                {"E. Australia Standard Time", "澳大利亚东部时间 - 布里斯班"},
                {"E. Europe Standard Time", "东欧时间 - 基希讷乌"},
                {"E. South America Standard Time", "巴西利亚时间 - 圣保罗"},
                {"Easter Island Standard Time", "复活节岛时间"},
                {"Eastern Standard Time", "北美东部时间 - 纽约"},
                {"Eastern Standard Time (Mexico)", "北美东部时间 - 坎昆"},
                {"Egypt Standard Time", "东欧时间 - 开罗"},
                {"Ekaterinburg Standard Time", "叶卡捷琳堡时间"},
                {"Fiji Standard Time", "斐济时间"},
                {"FLE Standard Time", "东欧时间 - 基辅"},
                {"Georgian Standard Time", "格鲁吉亚时间 - 第比利斯"},
                {"GMT Standard Time", "格林尼治标准时间 - 伦敦"},
                {"Greenland Standard Time", "努克"},
                {"Greenwich Standard Time", "格林尼治标准时间 - 雷克雅未克"},
                {"GTB Standard Time", "东欧时间 - 布加勒斯特"},
                {"Haiti Standard Time", "北美东部时间 - 太子港"},
                {"Hawaiian Standard Time", "夏威夷-阿留申标准时间 - 檀香山"},
                {"India Standard Time", "印度时间 - 加尔各答"},
                {"Iran Standard Time", "伊朗时间 - 德黑兰"},
                {"Israel Standard Time", "以色列时间 - 耶路撒冷"},
                {"Jordan Standard Time", "东欧时间 - 安曼"},
                {"Kaliningrad Standard Time", "东欧时间 - 加里宁格勒"},
                {"Korea Standard Time", "韩国时间 - 首尔"},
                {"Libya Standard Time", "东欧时间 - 的黎波里"},
                {"Line Islands Standard Time", "莱恩群岛时间 - 基里地马地岛"},
                {"Lord Howe Standard Time", "豪勋爵岛时间"},
                {"Magadan Standard Time", "马加丹时间"},
                {"Magallanes Standard Time", "智利时间 - 蓬塔阿雷纳斯"},
                {"Marquesas Standard Time", "马克萨斯群岛时间"},
                {"Mauritius Standard Time", "毛里求斯时间"},
                {"Middle East Standard Time", "东欧时间 - 贝鲁特"},
                {"Montevideo Standard Time", "乌拉圭时间 - 蒙得维的亚"},
                {"Morocco Standard Time", "西欧时间 - 卡萨布兰卡"},
                {"Mountain Standard Time", "北美山区时间 - 丹佛"},
                {"Mountain Standard Time (Mexico)", "墨西哥太平洋时间 - 马萨特兰"},
                {"Myanmar Standard Time", "缅甸时间 - 仰光"},
                {"N. Central Asia Standard Time", "克拉斯诺亚尔斯克时间 - 新西伯利亚"},
                {"Namibia Standard Time", "中部非洲时间 - 温得和克"},
                {"Nepal Standard Time", "尼泊尔时间 - 加德满都"},
                {"New Zealand Standard Time", "新西兰时间 - 奥克兰"},
                {"Newfoundland Standard Time", "纽芬兰时间 - 圣约翰斯"},
                {"Norfolk Standard Time", "诺福克岛时间"},
                {"North Asia East Standard Time", "伊尔库茨克时间"},
                {"North Asia Standard Time", "克拉斯诺亚尔斯克时间"},
                {"North Korea Standard Time", "韩国时间 - 平壤"},
                {"Omsk Standard Time", "鄂木斯克时间"},
                {"Pacific SA Standard Time", "智利时间 - 圣地亚哥"},
                {"Pacific Standard Time", "北美太平洋时间 - 洛杉矶"},
                {"Pacific Standard Time (Mexico)", "北美太平洋时间 - 蒂华纳"},
                {"Pakistan Standard Time", "巴基斯坦时间 - 卡拉奇"},
                {"Paraguay Standard Time", "巴拉圭时间 - 亚松森"},
                {"Qyzylorda Standard Time", "哈萨克斯坦时间 - 克孜洛尔达"},
                {"Romance Standard Time", "中欧时间 - 巴黎"},
                {"Russia Time Zone 10", "马加丹时间 - 中科雷姆斯克"},
                {"Russia Time Zone 11", "彼得罗巴甫洛夫斯克-堪察加时间"},
                {"Russia Time Zone 3", "萨马拉时间"},
                {"Russian Standard Time", "莫斯科时间"},
                {"SA Eastern Standard Time", "法属圭亚那标准时间 - 卡宴"},
                {"SA Pacific Standard Time", "哥伦比亚时间 - 波哥大"},
                {"SA Western Standard Time", "玻利维亚标准时间 - 拉巴斯"},
                {"Saint Pierre Standard Time", "圣皮埃尔和密克隆群岛时间"},
                {"Sakhalin Standard Time", "马加丹时间 - 萨哈林"},
                {"Samoa Standard Time", "阿皮亚时间"},
                {"Sao Tome Standard Time", "格林尼治标准时间 - 圣多美"},
                {"Saratov Standard Time", "萨马拉时间 - 萨拉托夫"},
                {"SE Asia Standard Time", "中南半岛时间 - 曼谷"},
                {"Singapore Standard Time", "新加坡标准时间"},
                {"South Africa Standard Time", "南非标准时间 - 约翰内斯堡"},
                {"South Sudan Standard Time", "中部非洲时间 - 朱巴"},
                {"Sri Lanka Standard Time", "印度时间 - 科伦坡"},
                {"Sudan Standard Time", "中部非洲时间 - 喀土穆"},
                {"Syria Standard Time", "东欧时间 - 大马士革"},
                {"Taipei Standard Time", "台北时间"},
                {"Tasmania Standard Time", "澳大利亚东部时间 - 霍巴特"},
                {"Tocantins Standard Time", "巴西利亚时间 - 阿拉瓜伊纳"},
                {"Tokyo Standard Time", "日本时间 - 东京"},
                {"Tomsk Standard Time", "克拉斯诺亚尔斯克时间 - 托木斯克"},
                {"Tonga Standard Time", "汤加时间 - 东加塔布"},
                {"Transbaikal Standard Time", "雅库茨克时间 - 赤塔"},
                {"Turkey Standard Time", "土耳其时间 - 伊斯坦布尔"},
                {"Turks And Caicos Standard Time", "北美东部时间 - 大特克"},
                {"Ulaanbaatar Standard Time", "乌兰巴托时间"},
                {"US Eastern Standard Time", "北美东部时间 - 印第安纳波利斯"},
                {"US Mountain Standard Time", "北美山区时间 - 凤凰城"},
                {"UTC", "UTC"},
                {"UTC+12", "UTC+12:00"},
                {"UTC+13", "UTC+13:00"},
                {"UTC-02", "UTC-02:00"},
                {"UTC-08", "UTC-08:00"},
                {"UTC-09", "UTC-09:00"},
                {"UTC-11", "UTC-11:00"},
                {"Venezuela Standard Time", "委内瑞拉时间 - 加拉加斯"},
                {"Vladivostok Standard Time", "海参崴时间"},
                {"Volgograd Standard Time", "莫斯科时间 - 伏尔加格勒"},
                {"W. Australia Standard Time", "澳大利亚西部时间 - 珀斯"},
                {"W. Central Africa Standard Time", "西部非洲时间 - 拉各斯"},
                {"W. Europe Standard Time", "中欧时间 - 柏林"},
                {"W. Mongolia Standard Time", "科布多时间"},
                {"West Asia Standard Time", "乌兹别克斯坦时间 - 塔什干"},
                {"West Bank Standard Time", "东欧时间 - 希伯伦"},
                {"West Pacific Standard Time", "巴布亚新几内亚时间 - 莫尔兹比港"},
                {"Yakutsk Standard Time", "雅库茨克时间"},
                {"Yukon Standard Time", "育空时间 - 怀特霍斯"},
            }},
            {"zh-TW", new Dictionary<string, string>
            {
                {"Afghanistan Standard Time", "阿富汗時間 - 喀布爾"},
                {"Alaskan Standard Time", "阿拉斯加時間 - 安克拉治"},
                {"Aleutian Standard Time", "夏威夷-阿留申時間 - 艾達克"},
                {"Altai Standard Time", "克拉斯諾亞爾斯克時間 - 巴爾瑙爾"},
                {"Arab Standard Time", "阿拉伯時間 - 利雅德"},
                {"Arabian Standard Time", "波斯灣海域標準時間 - 杜拜"},
                {"Arabic Standard Time", "阿拉伯時間 - 巴格達"},
                {"Argentina Standard Time", "阿根廷時間 - 布宜諾斯艾利斯"},
                {"Astrakhan Standard Time", "薩馬拉時間 - 阿斯特拉罕"},
                {"Atlantic Standard Time", "大西洋時間 - 哈里法克斯"},
                {"AUS Central Standard Time", "澳洲中部時間 - 達爾文"},
                {"Aus Central W. Standard Time", "澳洲中西部時間 - 尤克拉"},
                {"AUS Eastern Standard Time", "澳洲東部時間 - 雪梨"},
                {"Azerbaijan Standard Time", "亞塞拜然時間 - 巴庫"},
                {"Azores Standard Time", "亞速爾群島時間"},
                {"Bahia Standard Time", "巴西利亞時間 - 巴伊阿"},
                {"Bangladesh Standard Time", "孟加拉時間 - 達卡"},
                {"Belarus Standard Time", "莫斯科時間 - 明斯克"},
                {"Bougainville Standard Time", "巴布亞紐幾內亞時間 - 布干維爾"},
                {"Canada Central Standard Time", "中部時間 - 里賈納"},
                {"Cape Verde Standard Time", "維德角時間"},
                {"Caucasus Standard Time", "亞美尼亞時間 - 葉里溫"},
                {"Cen. Australia Standard Time", "澳洲中部時間 - 阿得雷德"},
                {"Central America Standard Time", "中部時間 - 瓜地馬拉"},
                {"Central Asia Standard Time", "吉爾吉斯時間 - 比什凱克"},
                {"Central Brazilian Standard Time", "亞馬遜時間 - 古雅巴"},
                {"Central Europe Standard Time", "中歐時間 - 布達佩斯"},
                {"Central European Standard Time", "中歐時間 - 華沙"},
                {"Central Pacific Standard Time", "索羅門群島時間 - 瓜達康納爾島"},
                {"Central Standard Time", "中部時間 - 芝加哥"},
                {"Central Standard Time (Mexico)", "中部時間 - 墨西哥市"},
                {"Chatham Islands Standard Time", "查坦群島時間"},
                {"China Standard Time", "中國時間 - 上海"},
                {"Cuba Standard Time", "古巴時間 - 哈瓦那"},
                {"Dateline Standard Time", "UTC-12:00"},
                {"E. Africa Standard Time", "東非時間 - 奈洛比"},
                {"E. Australia Standard Time", "澳洲東部時間 - 布利斯班"},
                {"E. Europe Standard Time", "東歐時間 - 基西紐"},
                {"E. South America Standard Time", "巴西利亞時間 - 聖保羅"},
                {"Easter Island Standard Time", "復活節島時間 - 復活島"},
                {"Eastern Standard Time", "東部時間 - 紐約"},
                {"Eastern Standard Time (Mexico)", "東部時間 - 坎昆"},
                {"Egypt Standard Time", "東歐時間 - 開羅"},
                {"Ekaterinburg Standard Time", "葉卡捷琳堡時間 - 葉卡捷林堡"},
                {"Fiji Standard Time", "斐濟時間"},
                {"FLE Standard Time", "東歐時間 - 基輔"},
                {"Georgian Standard Time", "喬治亞時間 - 第比利斯"},
                {"GMT Standard Time", "格林威治標準時間 - 倫敦"},
                {"Greenland Standard Time", "努克"},
                {"Greenwich Standard Time", "格林威治標準時間 - 雷克雅維克"},
                {"GTB Standard Time", "東歐時間 - 布加勒斯特"},
                {"Haiti Standard Time", "東部時間 - 太子港"},
                {"Hawaiian Standard Time", "夏威夷-阿留申標準時間 - 檀香山"},
                {"India Standard Time", "印度標準時間 - 加爾各答"},
                {"Iran Standard Time", "伊朗時間 - 德黑蘭"},
                {"Israel Standard Time", "以色列時間 - 耶路撒冷"},
                {"Jordan Standard Time", "東歐時間 - 安曼"},
                {"Kaliningrad Standard Time", "東歐時間 - 加里寧格勒"},
                {"Korea Standard Time", "韓國時間 - 首爾"},
                {"Libya Standard Time", "東歐時間 - 的黎波里"},
                {"Line Islands Standard Time", "萊恩群島時間 - 基里地馬地島"},
                {"Lord Howe Standard Time", "豪勳爵島時間"},
                {"Magadan Standard Time", "馬加丹時間"},
                {"Magallanes Standard Time", "智利時間 - 蓬塔阿雷納斯"},
                {"Marquesas Standard Time", "馬可薩斯時間 - 馬可薩斯島"},
                {"Mauritius Standard Time", "模里西斯時間"},
                {"Middle East Standard Time", "東歐時間 - 貝魯特"},
                {"Montevideo Standard Time", "烏拉圭時間 - 蒙特維多"},
                {"Morocco Standard Time", "西歐時間 - 卡薩布蘭卡"},
                {"Mountain Standard Time", "山區時間 - 丹佛"},
                {"Mountain Standard Time (Mexico)", "墨西哥太平洋時間 - 馬薩特蘭"},
                {"Myanmar Standard Time", "緬甸時間 - 仰光"},
                {"N. Central Asia Standard Time", "克拉斯諾亞爾斯克時間 - 新西伯利亞"},
                {"Namibia Standard Time", "中非時間 - 溫得和克"},
                {"Nepal Standard Time", "尼泊爾時間 - 加德滿都"},
                {"New Zealand Standard Time", "紐西蘭時間 - 奧克蘭"},
                {"Newfoundland Standard Time", "紐芬蘭時間 - 聖約翰"},
                {"Norfolk Standard Time", "諾福克島時間"},
                {"North Asia East Standard Time", "伊爾庫次克時間"},
                {"North Asia Standard Time", "克拉斯諾亞爾斯克時間"},
                {"North Korea Standard Time", "韓國時間 - 平壤"},
                {"Omsk Standard Time", "鄂木斯克時間"},
                {"Pacific SA Standard Time", "智利時間 - 聖地牙哥"},
                {"Pacific Standard Time", "太平洋時間 - 洛杉磯"},
                {"Pacific Standard Time (Mexico)", "太平洋時間 - 提華納"},
                {"Pakistan Standard Time", "巴基斯坦時間 - 喀拉蚩"},
                {"Paraguay Standard Time", "巴拉圭時間 - 亞松森"},
                {"Qyzylorda Standard Time", "哈薩克時間 - 克孜勒奧爾達"},
                {"Romance Standard Time", "中歐時間 - 巴黎"},
                {"Russia Time Zone 10", "馬加丹時間 - 中科雷姆斯克"},
                {"Russia Time Zone 11", "彼得羅巴甫洛夫斯克時間 - 堪察加"},
                {"Russia Time Zone 3", "薩馬拉時間 - 沙馬拉"},
                {"Russian Standard Time", "莫斯科時間"},
                {"SA Eastern Standard Time", "法屬圭亞那時間 - 開雲"},
                {"SA Pacific Standard Time", "哥倫比亞時間 - 波哥大"},
                {"SA Western Standard Time", "玻利維亞時間 - 拉巴斯"},
                {"Saint Pierre Standard Time", "聖皮埃與密克隆群島時間 - 密啟崙"},
                {"Sakhalin Standard Time", "馬加丹時間 - 庫頁島"},
                {"Samoa Standard Time", "阿皮亞時間"},
                {"Sao Tome Standard Time", "格林威治標準時間 - 聖多美"},
                {"Saratov Standard Time", "薩馬拉時間 - 薩拉托夫"},
                {"SE Asia Standard Time", "中南半島時間 - 曼谷"},
                {"Singapore Standard Time", "新加坡標準時間"},
                {"South Africa Standard Time", "南非標準時間 - 約翰尼斯堡"},
                {"South Sudan Standard Time", "中非時間 - 朱巴"},
                {"Sri Lanka Standard Time", "印度標準時間 - 可倫坡"},
                {"Sudan Standard Time", "中非時間 - 喀土穆"},
                {"Syria Standard Time", "東歐時間 - 大馬士革"},
                {"Taipei Standard Time", "台北時間"},
                {"Tasmania Standard Time", "澳洲東部時間 - 荷巴特"},
                {"Tocantins Standard Time", "巴西利亞時間 - 阿拉圭那"},
                {"Tokyo Standard Time", "日本時間 - 東京"},
                {"Tomsk Standard Time", "克拉斯諾亞爾斯克時間 - 托木斯克"},
                {"Tonga Standard Time", "東加時間 - 東加塔布島"},
                {"Transbaikal Standard Time", "雅庫次克時間 - 赤塔"},
                {"Turkey Standard Time", "土耳其時間 - 伊斯坦堡"},
                {"Turks And Caicos Standard Time", "東部時間 - 大特克島"},
                {"Ulaanbaatar Standard Time", "烏蘭巴托時間"},
                {"US Eastern Standard Time", "東部時間 - 印第安那波里斯"},
                {"US Mountain Standard Time", "山區時間 - 鳳凰城"},
                {"UTC", "UTC"},
                {"UTC+12", "UTC+12:00"},
                {"UTC+13", "UTC+13:00"},
                {"UTC-02", "UTC-02:00"},
                {"UTC-08", "UTC-08:00"},
                {"UTC-09", "UTC-09:00"},
                {"UTC-11", "UTC-11:00"},
                {"Venezuela Standard Time", "委內瑞拉時間 - 卡拉卡斯"},
                {"Vladivostok Standard Time", "海參崴時間"},
                {"Volgograd Standard Time", "莫斯科時間 - 伏爾加格勒"},
                {"W. Australia Standard Time", "澳洲西部時間 - 伯斯"},
                {"W. Central Africa Standard Time", "西非時間 - 拉哥斯"},
                {"W. Europe Standard Time", "中歐時間 - 柏林"},
                {"W. Mongolia Standard Time", "科布多時間"},
                {"West Asia Standard Time", "烏茲別克時間 - 塔什干"},
                {"West Bank Standard Time", "東歐時間 - 赫布隆"},
                {"West Pacific Standard Time", "巴布亞紐幾內亞時間 - 莫士比港"},
                {"Yakutsk Standard Time", "雅庫次克時間"},
                {"Yukon Standard Time", "育空地區時間 - 懷特霍斯"},
            }},
            {"en-US", new Dictionary<string, string>
            {
                {"Afghanistan Standard Time", "Afghanistan Time - Kabul"},
                {"Alaskan Standard Time", "Alaska Time - Anchorage"},
                {"Aleutian Standard Time", "Hawaii-Aleutian Time - Adak"},
                {"Altai Standard Time", "Krasnoyarsk Time - Barnaul"},
                {"Arab Standard Time", "Arabian Time - Riyadh"},
                {"Arabian Standard Time", "Gulf Standard Time - Dubai"},
                {"Arabic Standard Time", "Arabian Time - Baghdad"},
                {"Argentina Standard Time", "Argentina Time - Buenos Aires"},
                {"Astrakhan Standard Time", "Samara Time - Astrakhan"},
                {"Atlantic Standard Time", "Atlantic Time - Halifax"},
                {"AUS Central Standard Time", "Australian Central Time - Darwin"},
                {"Aus Central W. Standard Time", "Australian Central Western Time - Eucla"},
                {"AUS Eastern Standard Time", "Australian Eastern Time - Sydney"},
                {"Azerbaijan Standard Time", "Azerbaijan Time - Baku"},
                {"Azores Standard Time", "Azores Time"},
                {"Bahia Standard Time", "Brasilia Time - Bahia"},
                {"Bangladesh Standard Time", "Bangladesh Time - Dhaka"},
                {"Belarus Standard Time", "Moscow Time - Minsk"},
                {"Bougainville Standard Time", "Papua New Guinea Time - Bougainville"},
                {"Canada Central Standard Time", "Central Time - Regina"},
                {"Cape Verde Standard Time", "Cape Verde Time"},
                {"Caucasus Standard Time", "Armenia Time - Yerevan"},
                {"Cen. Australia Standard Time", "Australian Central Time - Adelaide"},
                {"Central America Standard Time", "Central Time - Guatemala"},
                {"Central Asia Standard Time", "Kyrgyzstan Time - Bishkek"},
                {"Central Brazilian Standard Time", "Amazon Time - Cuiabá"},
                {"Central Europe Standard Time", "Central European Time - Budapest"},
                {"Central European Standard Time", "Central European Time - Warsaw"},
                {"Central Pacific Standard Time", "Solomon Islands Time - Guadalcanal"},
                {"Central Standard Time", "Central Time - Chicago"},
                {"Central Standard Time (Mexico)", "Central Time - Mexico City"},
                {"Chatham Islands Standard Time", "Chatham Time - Chatham Islands"},
                {"China Standard Time", "China Time - Shanghai"},
                {"Cuba Standard Time", "Cuba Time - Havana"},
                {"Dateline Standard Time", "UTC-12:00"},
                {"E. Africa Standard Time", "East Africa Time - Nairobi"},
                {"E. Australia Standard Time", "Australian Eastern Time - Brisbane"},
                {"E. Europe Standard Time", "Eastern European Time - Chișinău"},
                {"E. South America Standard Time", "Brasilia Time - São Paulo"},
                {"Easter Island Standard Time", "Easter Island Time"},
                {"Eastern Standard Time", "Eastern Time - New York"},
                {"Eastern Standard Time (Mexico)", "Eastern Time - Cancún"},
                {"Egypt Standard Time", "Eastern European Time - Cairo"},
                {"Ekaterinburg Standard Time", "Yekaterinburg Time"},
                {"Fiji Standard Time", "Fiji Time"},
                {"FLE Standard Time", "Eastern European Time - Kyiv"},
                {"Georgian Standard Time", "Georgia Time - Tbilisi"},
                {"GMT Standard Time", "Greenwich Mean Time - London"},
                {"Greenland Standard Time", "Greenland Time - Nuuk"},
                {"Greenwich Standard Time", "Greenwich Mean Time - Reykjavik"},
                {"GTB Standard Time", "Eastern European Time - Bucharest"},
                {"Haiti Standard Time", "Eastern Time - Port-au-Prince"},
                {"Hawaiian Standard Time", "Hawaii-Aleutian Standard Time - Honolulu"},
                {"India Standard Time", "India Standard Time - Kolkata"},
                {"Iran Standard Time", "Iran Time - Tehran"},
                {"Israel Standard Time", "Israel Time - Jerusalem"},
                {"Jordan Standard Time", "Eastern European Time - Amman"},
                {"Kaliningrad Standard Time", "Eastern European Time - Kaliningrad"},
                {"Korea Standard Time", "Korean Time - Seoul"},
                {"Libya Standard Time", "Eastern European Time - Tripoli"},
                {"Line Islands Standard Time", "Line Islands Time - Kiritimati"},
                {"Lord Howe Standard Time", "Lord Howe Time - Lord Howe Island"},
                {"Magadan Standard Time", "Magadan Time"},
                {"Magallanes Standard Time", "Chile Time - Punta Arenas"},
                {"Marquesas Standard Time", "Marquesas Time - Marquesas Islands"},
                {"Mauritius Standard Time", "Mauritius Time"},
                {"Middle East Standard Time", "Eastern European Time - Beirut"},
                {"Montevideo Standard Time", "Uruguay Time - Montevideo"},
                {"Morocco Standard Time", "Western European Time - Casablanca"},
                {"Mountain Standard Time", "Mountain Time - Denver"},
                {"Mountain Standard Time (Mexico)", "Mexican Pacific Time - Mazatlán"},
                {"Myanmar Standard Time", "Myanmar Time - Yangon"},
                {"N. Central Asia Standard Time", "Krasnoyarsk Time - Novosibirsk"},
                {"Namibia Standard Time", "Central Africa Time - Windhoek"},
                {"Nepal Standard Time", "Nepal Time - Kathmandu"},
                {"New Zealand Standard Time", "New Zealand Time - Auckland"},
                {"Newfoundland Standard Time", "Newfoundland Time - St. John’s"},
                {"Norfolk Standard Time", "Norfolk Island Time"},
                {"North Asia East Standard Time", "Irkutsk Time"},
                {"North Asia Standard Time", "Krasnoyarsk Time"},
                {"North Korea Standard Time", "Korean Time - Pyongyang"},
                {"Omsk Standard Time", "Omsk Time"},
                {"Pacific SA Standard Time", "Chile Time - Santiago"},
                {"Pacific Standard Time", "Pacific Time - Los Angeles"},
                {"Pacific Standard Time (Mexico)", "Pacific Time - Tijuana"},
                {"Pakistan Standard Time", "Pakistan Time - Karachi"},
                {"Paraguay Standard Time", "Paraguay Time - Asunción"},
                {"Qyzylorda Standard Time", "Kazakhstan Time - Kyzylorda"},
                {"Romance Standard Time", "Central European Time - Paris"},
                {"Russia Time Zone 10", "Magadan Time - Srednekolymsk"},
                {"Russia Time Zone 11", "Kamchatka Time"},
                {"Russia Time Zone 3", "Samara Time"},
                {"Russian Standard Time", "Moscow Time"},
                {"SA Eastern Standard Time", "French Guiana Time - Cayenne"},
                {"SA Pacific Standard Time", "Colombia Time - Bogotá"},
                {"SA Western Standard Time", "Bolivia Time - La Paz"},
                {"Saint Pierre Standard Time", "St. Pierre & Miquelon Time - Saint-Pierre"},
                {"Sakhalin Standard Time", "Magadan Time - Sakhalin"},
                {"Samoa Standard Time", "Samoa Time - Apia"},
                {"Sao Tome Standard Time", "Greenwich Mean Time - São Tomé"},
                {"Saratov Standard Time", "Samara Time - Saratov"},
                {"SE Asia Standard Time", "Indochina Time - Bangkok"},
                {"Singapore Standard Time", "Singapore Standard Time"},
                {"South Africa Standard Time", "South Africa Standard Time - Johannesburg"},
                {"South Sudan Standard Time", "Central Africa Time - Juba"},
                {"Sri Lanka Standard Time", "India Standard Time - Colombo"},
                {"Sudan Standard Time", "Central Africa Time - Khartoum"},
                {"Syria Standard Time", "Eastern European Time - Damascus"},
                {"Taipei Standard Time", "Taiwan Time - Taipei"},
                {"Tasmania Standard Time", "Australian Eastern Time - Hobart"},
                {"Tocantins Standard Time", "Brasilia Time - Araguaína"},
                {"Tokyo Standard Time", "Japan Time - Tokyo"},
                {"Tomsk Standard Time", "Krasnoyarsk Time - Tomsk"},
                {"Tonga Standard Time", "Tonga Time - Tongatapu"},
                {"Transbaikal Standard Time", "Yakutsk Time - Chita"},
                {"Turkey Standard Time", "Türkiye Time - Istanbul"},
                {"Turks And Caicos Standard Time", "Eastern Time - Grand Turk"},
                {"Ulaanbaatar Standard Time", "Ulaanbaatar Time"},
                {"US Eastern Standard Time", "Eastern Time - Indianapolis"},
                {"US Mountain Standard Time", "Mountain Time - Phoenix"},
                {"UTC", "UTC"},
                {"UTC+12", "UTC+12:00"},
                {"UTC+13", "UTC+13:00"},
                {"UTC-02", "UTC-02:00"},
                {"UTC-08", "UTC-08:00"},
                {"UTC-09", "UTC-09:00"},
                {"UTC-11", "UTC-11:00"},
                {"Venezuela Standard Time", "Venezuela Time - Caracas"},
                {"Vladivostok Standard Time", "Vladivostok Time"},
                {"Volgograd Standard Time", "Moscow Time - Volgograd"},
                {"W. Australia Standard Time", "Australian Western Time - Perth"},
                {"W. Central Africa Standard Time", "West Africa Time - Lagos"},
                {"W. Europe Standard Time", "Central European Time - Berlin"},
                {"W. Mongolia Standard Time", "Khovd Time"},
                {"West Asia Standard Time", "Uzbekistan Time - Tashkent"},
                {"West Bank Standard Time", "Eastern European Time - Hebron"},
                {"West Pacific Standard Time", "Papua New Guinea Time - Port Moresby"},
                {"Yakutsk Standard Time", "Yakutsk Time"},
                {"Yukon Standard Time", "Yukon Time - Whitehorse"},
            }},
            {"ja-JP", new Dictionary<string, string>
            {
                {"Afghanistan Standard Time", "アフガニスタン時間 - カブール"},
                {"Alaskan Standard Time", "アラスカ時間 - アンカレッジ"},
                {"Aleutian Standard Time", "ハワイ・アリューシャン時間 - アダック"},
                {"Altai Standard Time", "クラスノヤルスク時間 - バルナウル"},
                {"Arab Standard Time", "アラビア時間 - リヤド"},
                {"Arabian Standard Time", "湾岸標準時 - ドバイ"},
                {"Arabic Standard Time", "アラビア時間 - バグダッド"},
                {"Argentina Standard Time", "アルゼンチン時間 - ブエノスアイレス"},
                {"Astrakhan Standard Time", "サマラ時間 - アストラハン"},
                {"Atlantic Standard Time", "大西洋時間 - ハリファクス"},
                {"AUS Central Standard Time", "オーストラリア中部時間 - ダーウィン"},
                {"Aus Central W. Standard Time", "オーストラリア中西部時間 - ユークラ"},
                {"AUS Eastern Standard Time", "オーストラリア東部時間 - シドニー"},
                {"Azerbaijan Standard Time", "アゼルバイジャン時間 - バクー"},
                {"Azores Standard Time", "アゾレス時間"},
                {"Bahia Standard Time", "ブラジリア時間 - バイーア"},
                {"Bangladesh Standard Time", "バングラデシュ時間 - ダッカ"},
                {"Belarus Standard Time", "モスクワ時間 - ミンスク"},
                {"Bougainville Standard Time", "パプアニューギニア時間 - ブーゲンビル"},
                {"Canada Central Standard Time", "米国中部時間 - レジャイナ"},
                {"Cape Verde Standard Time", "カーボベルデ時間"},
                {"Caucasus Standard Time", "アルメニア時間 - エレバン"},
                {"Cen. Australia Standard Time", "オーストラリア中部時間 - アデレード"},
                {"Central America Standard Time", "米国中部時間 - グアテマラ"},
                {"Central Asia Standard Time", "キルギス時間 - ビシュケク"},
                {"Central Brazilian Standard Time", "アマゾン時間 - クイアバ"},
                {"Central Europe Standard Time", "中央ヨーロッパ時間 - ブダペスト"},
                {"Central European Standard Time", "中央ヨーロッパ時間 - ワルシャワ"},
                {"Central Pacific Standard Time", "ソロモン諸島時間 - ガダルカナル"},
                {"Central Standard Time", "米国中部時間 - シカゴ"},
                {"Central Standard Time (Mexico)", "米国中部時間 - メキシコシティー"},
                {"Chatham Islands Standard Time", "チャタム時間"},
                {"China Standard Time", "中国時間 - 上海"},
                {"Cuba Standard Time", "キューバ時間 - ハバナ"},
                {"Dateline Standard Time", "UTC-12:00"},
                {"E. Africa Standard Time", "東アフリカ時間 - ナイロビ"},
                {"E. Australia Standard Time", "オーストラリア東部時間 - ブリスベン"},
                {"E. Europe Standard Time", "東ヨーロッパ時間 - キシナウ"},
                {"E. South America Standard Time", "ブラジリア時間 - サンパウロ"},
                {"Easter Island Standard Time", "イースター島時間"},
                {"Eastern Standard Time", "米国東部時間 - ニューヨーク"},
                {"Eastern Standard Time (Mexico)", "米国東部時間 - カンクン"},
                {"Egypt Standard Time", "東ヨーロッパ時間 - カイロ"},
                {"Ekaterinburg Standard Time", "エカテリンブルグ時間"},
                {"Fiji Standard Time", "フィジー時間"},
                {"FLE Standard Time", "東ヨーロッパ時間 - キーウ"},
                {"Georgian Standard Time", "ジョージア時間 - トビリシ"},
                {"GMT Standard Time", "グリニッジ標準時 - ロンドン"},
                {"Greenland Standard Time", "ヌーク"},
                {"Greenwich Standard Time", "グリニッジ標準時 - レイキャビク"},
                {"GTB Standard Time", "東ヨーロッパ時間 - ブカレスト"},
                {"Haiti Standard Time", "米国東部時間 - ポルトープランス"},
                {"Hawaiian Standard Time", "ハワイ・アリューシャン標準時 - ホノルル"},
                {"India Standard Time", "インド標準時 - コルカタ"},
                {"Iran Standard Time", "イラン時間 - テヘラン"},
                {"Israel Standard Time", "イスラエル時間 - エルサレム"},
                {"Jordan Standard Time", "東ヨーロッパ時間 - アンマン"},
                {"Kaliningrad Standard Time", "東ヨーロッパ時間 - カリーニングラード"},
                {"Korea Standard Time", "韓国時間 - ソウル"},
                {"Libya Standard Time", "東ヨーロッパ時間 - トリポリ"},
                {"Line Islands Standard Time", "ライン諸島時間 - キリスィマスィ島"},
                {"Lord Howe Standard Time", "ロードハウ時間"},
                {"Magadan Standard Time", "マガダン時間"},
                {"Magallanes Standard Time", "チリ時間 - プンタアレナス"},
                {"Marquesas Standard Time", "マルキーズ時間"},
                {"Mauritius Standard Time", "モーリシャス時間"},
                {"Middle East Standard Time", "東ヨーロッパ時間 - ベイルート"},
                {"Montevideo Standard Time", "ウルグアイ時間 - モンテビデオ"},
                {"Morocco Standard Time", "西ヨーロッパ時間 - カサブランカ"},
                {"Mountain Standard Time", "米国山岳部時間 - デンバー"},
                {"Mountain Standard Time (Mexico)", "メキシコ太平洋時間 - マサトラン"},
                {"Myanmar Standard Time", "ミャンマー時間 - ヤンゴン"},
                {"N. Central Asia Standard Time", "クラスノヤルスク時間 - ノヴォシビルスク"},
                {"Namibia Standard Time", "中央アフリカ時間 - ウィントフック"},
                {"Nepal Standard Time", "ネパール時間 - カトマンズ"},
                {"New Zealand Standard Time", "ニュージーランド時間 - オークランド"},
                {"Newfoundland Standard Time", "ニューファンドランド時間 - セントジョンズ"},
                {"Norfolk Standard Time", "ノーフォーク島時間"},
                {"North Asia East Standard Time", "イルクーツク時間"},
                {"North Asia Standard Time", "クラスノヤルスク時間"},
                {"North Korea Standard Time", "韓国時間 - 平壌"},
                {"Omsk Standard Time", "オムスク時間"},
                {"Pacific SA Standard Time", "チリ時間 - サンチアゴ"},
                {"Pacific Standard Time", "米国太平洋時間 - ロサンゼルス"},
                {"Pacific Standard Time (Mexico)", "米国太平洋時間 - ティフアナ"},
                {"Pakistan Standard Time", "パキスタン時間 - カラチ"},
                {"Paraguay Standard Time", "パラグアイ時間 - アスンシオン"},
                {"Qyzylorda Standard Time", "カザフスタン時間 - クズロルダ"},
                {"Romance Standard Time", "中央ヨーロッパ時間 - パリ"},
                {"Russia Time Zone 10", "マガダン時間 - スレドネコリムスク"},
                {"Russia Time Zone 11", "ペトロパブロフスク・カムチャツキー時間 - カムチャッカ"},
                {"Russia Time Zone 3", "サマラ時間"},
                {"Russian Standard Time", "モスクワ時間"},
                {"SA Eastern Standard Time", "仏領ギアナ時間 - カイエンヌ"},
                {"SA Pacific Standard Time", "コロンビア時間 - ボゴタ"},
                {"SA Western Standard Time", "ボリビア時間 - ラパス"},
                {"Saint Pierre Standard Time", "サンピエール島・ミクロン島時間"},
                {"Sakhalin Standard Time", "マガダン時間 - サハリン"},
                {"Samoa Standard Time", "サモア時間 - アピア"},
                {"Sao Tome Standard Time", "グリニッジ標準時 - サントメ"},
                {"Saratov Standard Time", "サマラ時間 - サラトフ"},
                {"SE Asia Standard Time", "インドシナ時間 - バンコク"},
                {"Singapore Standard Time", "シンガポール標準時"},
                {"South Africa Standard Time", "南アフリカ標準時 - ヨハネスブルグ"},
                {"South Sudan Standard Time", "中央アフリカ時間 - ジュバ"},
                {"Sri Lanka Standard Time", "インド標準時 - コロンボ"},
                {"Sudan Standard Time", "中央アフリカ時間 - ハルツーム"},
                {"Syria Standard Time", "東ヨーロッパ時間 - ダマスカス"},
                {"Taipei Standard Time", "台湾時間 - 台北"},
                {"Tasmania Standard Time", "オーストラリア東部時間 - ホバート"},
                {"Tocantins Standard Time", "ブラジリア時間 - アラグァイナ"},
                {"Tokyo Standard Time", "日本時間 - 東京"},
                {"Tomsk Standard Time", "クラスノヤルスク時間 - トムスク"},
                {"Tonga Standard Time", "トンガ時間 - トンガタプ"},
                {"Transbaikal Standard Time", "ヤクーツク時間 - チタ"},
                {"Turkey Standard Time", "トルコ時間 - イスタンブール"},
                {"Turks And Caicos Standard Time", "米国東部時間 - グランドターク"},
                {"Ulaanbaatar Standard Time", "ウランバートル時間"},
                {"US Eastern Standard Time", "米国東部時間 - インディアナポリス"},
                {"US Mountain Standard Time", "米国山岳部時間 - フェニックス"},
                {"UTC", "UTC"},
                {"UTC+12", "UTC+12:00"},
                {"UTC+13", "UTC+13:00"},
                {"UTC-02", "UTC-02:00"},
                {"UTC-08", "UTC-08:00"},
                {"UTC-09", "UTC-09:00"},
                {"UTC-11", "UTC-11:00"},
                {"Venezuela Standard Time", "ベネズエラ時間 - カラカス"},
                {"Vladivostok Standard Time", "ウラジオストク時間"},
                {"Volgograd Standard Time", "モスクワ時間 - ボルゴグラード"},
                {"W. Australia Standard Time", "オーストラリア西部時間 - パース"},
                {"W. Central Africa Standard Time", "西アフリカ時間 - ラゴス"},
                {"W. Europe Standard Time", "中央ヨーロッパ時間 - ベルリン"},
                {"W. Mongolia Standard Time", "ホブド時間"},
                {"West Asia Standard Time", "ウズベキスタン時間 - タシケント"},
                {"West Bank Standard Time", "東ヨーロッパ時間 - ヘブロン"},
                {"West Pacific Standard Time", "パプアニューギニア時間 - ポートモレスビー"},
                {"Yakutsk Standard Time", "ヤクーツク時間"},
                {"Yukon Standard Time", "ユーコン時間 - ホワイトホース"},
            }},
            {"ko-KR", new Dictionary<string, string>
            {
                {"Afghanistan Standard Time", "아프가니스탄 시간 - 카불"},
                {"Alaskan Standard Time", "알래스카 시간 - 앵커리지"},
                {"Aleutian Standard Time", "하와이 알류샨 시간 - 에이닥"},
                {"Altai Standard Time", "크라스노야르스크 시간 - 바르나울"},
                {"Arab Standard Time", "아라비아 시간 - 리야드"},
                {"Arabian Standard Time", "걸프만 표준시 - 두바이"},
                {"Arabic Standard Time", "아라비아 시간 - 바그다드"},
                {"Argentina Standard Time", "아르헨티나 시간 - 부에노스 아이레스"},
                {"Astrakhan Standard Time", "사마라 시간 - 아스트라한"},
                {"Atlantic Standard Time", "대서양 시간 - 핼리팩스"},
                {"AUS Central Standard Time", "호주 중부 시간 - 다윈"},
                {"Aus Central W. Standard Time", "호주 중서부 시간 - 유클라"},
                {"AUS Eastern Standard Time", "호주 동부 시간 - 시드니"},
                {"Azerbaijan Standard Time", "아제르바이잔 시간 - 바쿠"},
                {"Azores Standard Time", "아조레스 시간"},
                {"Bahia Standard Time", "브라질리아 시간 - 바히아"},
                {"Bangladesh Standard Time", "방글라데시 시간 - 다카"},
                {"Belarus Standard Time", "모스크바 시간 - 민스크"},
                {"Bougainville Standard Time", "파푸아뉴기니 시간 - 부갱빌"},
                {"Canada Central Standard Time", "미 중부 시간 - 리자이나"},
                {"Cape Verde Standard Time", "카보 베르데 시간"},
                {"Caucasus Standard Time", "아르메니아 시간 - 예레반"},
                {"Cen. Australia Standard Time", "호주 중부 시간 - 애들레이드"},
                {"Central America Standard Time", "미 중부 시간 - 과테말라"},
                {"Central Asia Standard Time", "키르기스스탄 시간 - 비슈케크"},
                {"Central Brazilian Standard Time", "아마존 시간 - 쿠이아바"},
                {"Central Europe Standard Time", "중부유럽 시간 - 부다페스트"},
                {"Central European Standard Time", "중부유럽 시간 - 바르샤바"},
                {"Central Pacific Standard Time", "솔로몬 제도 시간 - 과달카날"},
                {"Central Standard Time", "미 중부 시간 - 시카고"},
                {"Central Standard Time (Mexico)", "미 중부 시간 - 멕시코 시티"},
                {"Chatham Islands Standard Time", "채텀 시간"},
                {"China Standard Time", "중국 시간 - 상하이"},
                {"Cuba Standard Time", "쿠바 시간 - 하바나"},
                {"Dateline Standard Time", "UTC-12:00"},
                {"E. Africa Standard Time", "동아프리카 시간 - 나이로비"},
                {"E. Australia Standard Time", "호주 동부 시간 - 브리스베인"},
                {"E. Europe Standard Time", "동유럽 시간 - 키시나우"},
                {"E. South America Standard Time", "브라질리아 시간 - 상파울루"},
                {"Easter Island Standard Time", "이스터섬 시간 - 이스터 섬"},
                {"Eastern Standard Time", "미 동부 시간 - 뉴욕"},
                {"Eastern Standard Time (Mexico)", "미 동부 시간 - 칸쿤"},
                {"Egypt Standard Time", "동유럽 시간 - 카이로"},
                {"Ekaterinburg Standard Time", "예카테린부르크 시간"},
                {"Fiji Standard Time", "피지 시간"},
                {"FLE Standard Time", "동유럽 시간 - 키예프"},
                {"Georgian Standard Time", "조지아 시간 - 트빌리시"},
                {"GMT Standard Time", "그리니치 표준시 - 런던"},
                {"Greenland Standard Time", "고드호프"},
                {"Greenwich Standard Time", "그리니치 표준시 - 레이캬비크"},
                {"GTB Standard Time", "동유럽 시간 - 부쿠레슈티"},
                {"Haiti Standard Time", "미 동부 시간 - 포르토프랭스"},
                {"Hawaiian Standard Time", "하와이 알류샨 표준시 - 호놀룰루"},
                {"India Standard Time", "인도 표준시 - 콜카타"},
                {"Iran Standard Time", "이란 시간 - 테헤란"},
                {"Israel Standard Time", "이스라엘 시간 - 예루살렘"},
                {"Jordan Standard Time", "동유럽 시간 - 암만"},
                {"Kaliningrad Standard Time", "동유럽 시간 - 칼리닌그라드"},
                {"Korea Standard Time", "한국 시간 - 서울"},
                {"Libya Standard Time", "동유럽 시간 - 트리폴리"},
                {"Line Islands Standard Time", "라인 제도 시간 - 키리티마티"},
                {"Lord Howe Standard Time", "로드 하우 시간"},
                {"Magadan Standard Time", "마가단 시간"},
                {"Magallanes Standard Time", "칠레 시간 - 푼타아레나스"},
                {"Marquesas Standard Time", "마르키즈 제도 시간 - 마퀘사스"},
                {"Mauritius Standard Time", "모리셔스 시간"},
                {"Middle East Standard Time", "동유럽 시간 - 베이루트"},
                {"Montevideo Standard Time", "우루과이 시간 - 몬테비데오"},
                {"Morocco Standard Time", "서유럽 시간 - 카사블랑카"},
                {"Mountain Standard Time", "미 산지 시간 - 덴버"},
                {"Mountain Standard Time (Mexico)", "멕시코 태평양 시간 - 마사틀란"},
                {"Myanmar Standard Time", "미얀마 시간 - 랑군"},
                {"N. Central Asia Standard Time", "크라스노야르스크 시간 - 노보시비르스크"},
                {"Namibia Standard Time", "중앙아프리카 시간 - 빈트후크"},
                {"Nepal Standard Time", "네팔 시간 - 카트만두"},
                {"New Zealand Standard Time", "뉴질랜드 시간 - 오클랜드"},
                {"Newfoundland Standard Time", "뉴펀들랜드 시간 - 세인트존스"},
                {"Norfolk Standard Time", "노퍽섬 시간"},
                {"North Asia East Standard Time", "이르쿠츠크 시간"},
                {"North Asia Standard Time", "크라스노야르스크 시간"},
                {"North Korea Standard Time", "한국 시간 - 평양"},
                {"Omsk Standard Time", "옴스크 시간"},
                {"Pacific SA Standard Time", "칠레 시간 - 산티아고"},
                {"Pacific Standard Time", "미 태평양 시간 - 로스앤젤레스"},
                {"Pacific Standard Time (Mexico)", "미 태평양 시간 - 티후아나"},
                {"Pakistan Standard Time", "파키스탄 시간 - 카라치"},
                {"Paraguay Standard Time", "파라과이 시간 - 아순시온"},
                {"Qyzylorda Standard Time", "카자흐스탄 시간 - 키질로르다"},
                {"Romance Standard Time", "중부유럽 시간 - 파리"},
                {"Russia Time Zone 10", "마가단 시간 - 스레드네콜림스크"},
                {"Russia Time Zone 11", "페트로파블롭스크-캄차츠키 시간 - 캄차카"},
                {"Russia Time Zone 3", "사마라 시간"},
                {"Russian Standard Time", "모스크바 시간"},
                {"SA Eastern Standard Time", "프랑스령 가이아나 시간 - 카옌"},
                {"SA Pacific Standard Time", "콜롬비아 시간 - 보고타"},
                {"SA Western Standard Time", "볼리비아 시간 - 라파스"},
                {"Saint Pierre Standard Time", "세인트피에르 미클롱 시간"},
                {"Sakhalin Standard Time", "마가단 시간 - 사할린"},
                {"Samoa Standard Time", "아피아 시간"},
                {"Sao Tome Standard Time", "그리니치 표준시 - 상투메"},
                {"Saratov Standard Time", "사마라 시간 - 사라토프"},
                {"SE Asia Standard Time", "인도차이나 시간 - 방콕"},
                {"Singapore Standard Time", "싱가포르 표준시"},
                {"South Africa Standard Time", "남아프리카 시간 - 요하네스버그"},
                {"South Sudan Standard Time", "중앙아프리카 시간 - 주바"},
                {"Sri Lanka Standard Time", "인도 표준시 - 콜롬보"},
                {"Sudan Standard Time", "중앙아프리카 시간 - 카르툼"},
                {"Syria Standard Time", "동유럽 시간 - 다마스쿠스"},
                {"Taipei Standard Time", "대만 시간 - 타이베이"},
                {"Tasmania Standard Time", "호주 동부 시간 - 호바트"},
                {"Tocantins Standard Time", "브라질리아 시간 - 아라과이나"},
                {"Tokyo Standard Time", "일본 시간 - 도쿄"},
                {"Tomsk Standard Time", "크라스노야르스크 시간 - 톰스크"},
                {"Tonga Standard Time", "통가 시간 - 통가타푸"},
                {"Transbaikal Standard Time", "야쿠츠크 시간 - 치타"},
                {"Turkey Standard Time", "이스탄불"},
                {"Turks And Caicos Standard Time", "미 동부 시간 - 그랜드 터크"},
                {"Ulaanbaatar Standard Time", "울란바토르 시간"},
                {"US Eastern Standard Time", "미 동부 시간 - 인디애나폴리스"},
                {"US Mountain Standard Time", "미 산지 시간 - 피닉스"},
                {"UTC", "UTC"},
                {"UTC+12", "UTC+12:00"},
                {"UTC+13", "UTC+13:00"},
                {"UTC-02", "UTC-02:00"},
                {"UTC-08", "UTC-08:00"},
                {"UTC-09", "UTC-09:00"},
                {"UTC-11", "UTC-11:00"},
                {"Venezuela Standard Time", "베네수엘라 시간 - 카라카스"},
                {"Vladivostok Standard Time", "블라디보스토크 시간"},
                {"Volgograd Standard Time", "모스크바 시간 - 볼고그라트"},
                {"W. Australia Standard Time", "호주 서부 시간 - 퍼스"},
                {"W. Central Africa Standard Time", "서아프리카 시간 - 라고스"},
                {"W. Europe Standard Time", "중부유럽 시간 - 베를린"},
                {"W. Mongolia Standard Time", "호브드 시간"},
                {"West Asia Standard Time", "우즈베키스탄 시간 - 타슈켄트"},
                {"West Bank Standard Time", "동유럽 시간 - 헤브론"},
                {"West Pacific Standard Time", "파푸아뉴기니 시간 - 포트모르즈비"},
                {"Yakutsk Standard Time", "야쿠츠크 시간"},
                {"Yukon Standard Time", "유콘 시간 - 화이트호스"},
            }},
            {"de-DE", new Dictionary<string, string>
            {
                {"Afghanistan Standard Time", "Afghanistan-Zeit"},
                {"Alaskan Standard Time", "Alaska-Zeit"},
                {"Aleutian Standard Time", "Hawaii-Aleuten-Zeit"},
                {"Altai Standard Time", "Krasnojarsker Zeit"},
                {"Arab Standard Time", "Arabische Zeit - Riad"},
                {"Arabian Standard Time", "Golf-Zeit"},
                {"Arabic Standard Time", "Arabische Zeit - Bagdad"},
                {"Argentina Standard Time", "Argentinische Zeit"},
                {"Astrakhan Standard Time", "Samara-Zeit - Astrachan"},
                {"Atlantic Standard Time", "Atlantik-Zeit"},
                {"AUS Central Standard Time", "Zentralaustralische Zeit"},
                {"Aus Central W. Standard Time", "Zentral-/Westaustralische Zeit"},
                {"AUS Eastern Standard Time", "Ostaustralische Zeit"},
                {"Azerbaijan Standard Time", "Aserbaidschanische Zeit"},
                {"Azores Standard Time", "Azoren-Zeit"},
                {"Bahia Standard Time", "Brasília-Zeit"},
                {"Bangladesh Standard Time", "Bangladesch-Zeit"},
                {"Belarus Standard Time", "Moskauer Zeit"},
                {"Bougainville Standard Time", "Papua-Neuguinea-Zeit"},
                {"Canada Central Standard Time", "Nordamerikanische Zentralzeit"},
                {"Cape Verde Standard Time", "Cabo-Verde-Zeit - Cabo Verde"},
                {"Caucasus Standard Time", "Armenische Zeit - Eriwan"},
                {"Cen. Australia Standard Time", "Zentralaustralische Zeit"},
                {"Central America Standard Time", "Nordamerikanische Zentralzeit"},
                {"Central Asia Standard Time", "Kirgisische Zeit - Bischkek"},
                {"Central Brazilian Standard Time", "Amazonas-Zeit - Cuiabá"},
                {"Central Europe Standard Time", "Mitteleuropäische Zeit"},
                {"Central European Standard Time", "Mitteleuropäische Zeit - Warschau"},
                {"Central Pacific Standard Time", "Salomonen-Zeit"},
                {"Central Standard Time", "Nordamerikanische Zentralzeit"},
                {"Central Standard Time (Mexico)", "Nordamerikanische Zentralzeit - Mexiko-Stadt"},
                {"Chatham Islands Standard Time", "Chatham-Zeit"},
                {"China Standard Time", "Chinesische Zeit"},
                {"Cuba Standard Time", "Kubanische Zeit - Havanna"},
                {"Dateline Standard Time", "UTC-12:00"},
                {"E. Africa Standard Time", "Ostafrikanische Zeit"},
                {"E. Australia Standard Time", "Ostaustralische Zeit"},
                {"E. Europe Standard Time", "Osteuropäische Zeit - Chișinău"},
                {"E. South America Standard Time", "Brasília-Zeit - São Paulo"},
                {"Easter Island Standard Time", "Osterinsel-Zeit"},
                {"Eastern Standard Time", "Nordamerikanische Ostküstenzeit"},
                {"Eastern Standard Time (Mexico)", "Nordamerikanische Ostküstenzeit - Cancún"},
                {"Egypt Standard Time", "Osteuropäische Zeit - Kairo"},
                {"Ekaterinburg Standard Time", "Jekaterinburger Zeit"},
                {"Fiji Standard Time", "Fidschi-Zeit"},
                {"FLE Standard Time", "Osteuropäische Zeit - Kiew"},
                {"Georgian Standard Time", "Georgische Zeit - Tiflis"},
                {"GMT Standard Time", "Mittlere Greenwich-Zeit"},
                {"Greenland Standard Time", "Nuuk"},
                {"Greenwich Standard Time", "Mittlere Greenwich-Zeit - Reyk­ja­vík"},
                {"GTB Standard Time", "Osteuropäische Zeit - Bukarest"},
                {"Haiti Standard Time", "Nordamerikanische Ostküstenzeit"},
                {"Hawaiian Standard Time", "Hawaii-Aleuten-Normalzeit - Honolulu"},
                {"India Standard Time", "Indische Normalzeit - Kalkutta"},
                {"Iran Standard Time", "Iranische Zeit - Teheran"},
                {"Israel Standard Time", "Israelische Zeit"},
                {"Jordan Standard Time", "Osteuropäische Zeit"},
                {"Kaliningrad Standard Time", "Osteuropäische Zeit"},
                {"Korea Standard Time", "Koreanische Zeit"},
                {"Libya Standard Time", "Osteuropäische Zeit - Tripolis"},
                {"Line Islands Standard Time", "Linieninseln-Zeit"},
                {"Lord Howe Standard Time", "Lord-Howe-Zeit"},
                {"Magadan Standard Time", "Magadan-Zeit"},
                {"Magallanes Standard Time", "Chilenische Zeit"},
                {"Marquesas Standard Time", "Marquesas-Zeit"},
                {"Mauritius Standard Time", "Mauritius-Zeit"},
                {"Middle East Standard Time", "Osteuropäische Zeit"},
                {"Montevideo Standard Time", "Uruguayische Zeit"},
                {"Morocco Standard Time", "Westeuropäische Zeit"},
                {"Mountain Standard Time", "Rocky-Mountains-Zeit"},
                {"Mountain Standard Time (Mexico)", "Mexikanische Pazifikzeit - Mazatlán"},
                {"Myanmar Standard Time", "Myanmar-Zeit - Rangun"},
                {"N. Central Asia Standard Time", "Krasnojarsker Zeit - Nowosibirsk"},
                {"Namibia Standard Time", "Zentralafrikanische Zeit"},
                {"Nepal Standard Time", "Nepalesische Zeit - Kathmandu"},
                {"New Zealand Standard Time", "Neuseeland-Zeit"},
                {"Newfoundland Standard Time", "Neufundland-Zeit - St. John’s"},
                {"Norfolk Standard Time", "Norfolkinsel-Zeit"},
                {"North Asia East Standard Time", "Irkutsker Zeit"},
                {"North Asia Standard Time", "Krasnojarsker Zeit"},
                {"North Korea Standard Time", "Koreanische Zeit - Pjöngjang"},
                {"Omsk Standard Time", "Omsker Zeit"},
                {"Pacific SA Standard Time", "Chilenische Zeit"},
                {"Pacific Standard Time", "Nordamerikanische Westküstenzeit"},
                {"Pacific Standard Time (Mexico)", "Nordamerikanische Westküstenzeit"},
                {"Pakistan Standard Time", "Pakistanische Zeit - Karatschi"},
                {"Paraguay Standard Time", "Paraguayische Zeit - Asunción"},
                {"Qyzylorda Standard Time", "Kasachische Zeit - Qysylorda"},
                {"Romance Standard Time", "Mitteleuropäische Zeit"},
                {"Russia Time Zone 10", "Magadan-Zeit"},
                {"Russia Time Zone 11", "Kamtschatka-Zeit"},
                {"Russia Time Zone 3", "Samara-Zeit"},
                {"Russian Standard Time", "Moskauer Zeit"},
                {"SA Eastern Standard Time", "Französisch-Guayana-Zeit"},
                {"SA Pacific Standard Time", "Kolumbianische Zeit - Bogotá"},
                {"SA Western Standard Time", "Bolivianische Zeit"},
                {"Saint Pierre Standard Time", "St.-Pierre-und-Miquelon-Zeit - Saint-Pierre"},
                {"Sakhalin Standard Time", "Magadan-Zeit - Sachalin"},
                {"Samoa Standard Time", "Apia-Zeit"},
                {"Sao Tome Standard Time", "Mittlere Greenwich-Zeit - São Tomé"},
                {"Saratov Standard Time", "Samara-Zeit - Saratow"},
                {"SE Asia Standard Time", "Indochina-Zeit"},
                {"Singapore Standard Time", "Singapurische Normalzeit"},
                {"South Africa Standard Time", "Südafrikanische Zeit"},
                {"South Sudan Standard Time", "Zentralafrikanische Zeit"},
                {"Sri Lanka Standard Time", "Indische Normalzeit"},
                {"Sudan Standard Time", "Zentralafrikanische Zeit - Khartum"},
                {"Syria Standard Time", "Osteuropäische Zeit - Damaskus"},
                {"Taipei Standard Time", "Taipeh-Zeit"},
                {"Tasmania Standard Time", "Ostaustralische Zeit"},
                {"Tocantins Standard Time", "Brasília-Zeit - Araguaína"},
                {"Tokyo Standard Time", "Japanische Zeit - Tokio"},
                {"Tomsk Standard Time", "Krasnojarsker Zeit"},
                {"Tonga Standard Time", "Tongaische Zeit"},
                {"Transbaikal Standard Time", "Jakutsker Zeit - Tschita"},
                {"Turkey Standard Time", "Türkische Zeit"},
                {"Turks And Caicos Standard Time", "Nordamerikanische Ostküstenzeit"},
                {"Ulaanbaatar Standard Time", "Ulaanbaatar-Zeit"},
                {"US Eastern Standard Time", "Nordamerikanische Ostküstenzeit"},
                {"US Mountain Standard Time", "Rocky-Mountains-Zeit"},
                {"UTC", "UTC"},
                {"UTC+12", "UTC+12:00"},
                {"UTC+13", "UTC+13:00"},
                {"UTC-02", "UTC-02:00"},
                {"UTC-08", "UTC-08:00"},
                {"UTC-09", "UTC-09:00"},
                {"UTC-11", "UTC-11:00"},
                {"Venezuela Standard Time", "Venezuela-Zeit"},
                {"Vladivostok Standard Time", "Wladiwostoker Zeit"},
                {"Volgograd Standard Time", "Moskauer Zeit - Wolgograd"},
                {"W. Australia Standard Time", "Westaustralische Zeit"},
                {"W. Central Africa Standard Time", "Westafrikanische Zeit"},
                {"W. Europe Standard Time", "Mitteleuropäische Zeit"},
                {"W. Mongolia Standard Time", "Chowd-Zeit"},
                {"West Asia Standard Time", "Usbekische Zeit - Taschkent"},
                {"West Bank Standard Time", "Osteuropäische Zeit"},
                {"West Pacific Standard Time", "Papua-Neuguinea-Zeit"},
                {"Yakutsk Standard Time", "Jakutsker Zeit"},
                {"Yukon Standard Time", "Yukon-Zeit"},
            }},
            {"fr-FR", new Dictionary<string, string>
            {
                {"Afghanistan Standard Time", "heure de l’Afghanistan - Kaboul"},
                {"Alaskan Standard Time", "heure de l’Alaska"},
                {"Aleutian Standard Time", "heure d’Hawaï - Aléoutiennes"},
                {"Altai Standard Time", "heure de Krasnoïarsk"},
                {"Arab Standard Time", "heure de l’Arabie - Riyad"},
                {"Arabian Standard Time", "heure du Golfe - Dubaï"},
                {"Arabic Standard Time", "heure de l’Arabie - Bagdad"},
                {"Argentina Standard Time", "heure de l’Argentine"},
                {"Astrakhan Standard Time", "heure de Samara"},
                {"Atlantic Standard Time", "heure de l’Atlantique"},
                {"AUS Central Standard Time", "heure du centre de l’Australie"},
                {"Aus Central W. Standard Time", "heure du centre-ouest de l’Australie"},
                {"AUS Eastern Standard Time", "heure de l’Est de l’Australie"},
                {"Azerbaijan Standard Time", "heure de l’Azerbaïdjan - Bakou"},
                {"Azores Standard Time", "heure des Açores"},
                {"Bahia Standard Time", "heure de Brasilia"},
                {"Bangladesh Standard Time", "heure du Bangladesh"},
                {"Belarus Standard Time", "heure de Moscou"},
                {"Bougainville Standard Time", "heure de la Papouasie-Nouvelle-Guinée"},
                {"Canada Central Standard Time", "heure du centre nord-américain"},
                {"Cape Verde Standard Time", "heure du Cap-Vert"},
                {"Caucasus Standard Time", "heure de l’Arménie - Erevan"},
                {"Cen. Australia Standard Time", "heure du centre de l’Australie - Adélaïde"},
                {"Central America Standard Time", "heure du centre nord-américain"},
                {"Central Asia Standard Time", "heure du Kirghizistan - Bichkek"},
                {"Central Brazilian Standard Time", "heure de l’Amazonie - Cuiabá"},
                {"Central Europe Standard Time", "heure d’Europe centrale"},
                {"Central European Standard Time", "heure d’Europe centrale - Varsovie"},
                {"Central Pacific Standard Time", "heure des îles Salomon"},
                {"Central Standard Time", "heure du centre nord-américain"},
                {"Central Standard Time (Mexico)", "heure du centre nord-américain - Mexico"},
                {"Chatham Islands Standard Time", "heure des îles Chatham"},
                {"China Standard Time", "heure de la Chine"},
                {"Cuba Standard Time", "heure de Cuba - La Havane"},
                {"Dateline Standard Time", "UTC-12:00"},
                {"E. Africa Standard Time", "heure normale d’Afrique de l’Est"},
                {"E. Australia Standard Time", "heure de l’Est de l’Australie"},
                {"E. Europe Standard Time", "heure d’Europe de l’Est - Chișinău"},
                {"E. South America Standard Time", "heure de Brasilia - São Paulo"},
                {"Easter Island Standard Time", "heure de l’île de Pâques"},
                {"Eastern Standard Time", "heure de l’Est nord-américain"},
                {"Eastern Standard Time (Mexico)", "heure de l’Est nord-américain - Cancún"},
                {"Egypt Standard Time", "heure d’Europe de l’Est - Le Caire"},
                {"Ekaterinburg Standard Time", "heure d’Ekaterinbourg"},
                {"Fiji Standard Time", "heure des îles Fidji"},
                {"FLE Standard Time", "heure d’Europe de l’Est - Kiev"},
                {"Georgian Standard Time", "heure de la Géorgie - Tbilissi"},
                {"GMT Standard Time", "heure moyenne de Greenwich - Londres"},
                {"Greenland Standard Time", "Nuuk"},
                {"Greenwich Standard Time", "heure moyenne de Greenwich"},
                {"GTB Standard Time", "heure d’Europe de l’Est - Bucarest"},
                {"Haiti Standard Time", "heure de l’Est nord-américain"},
                {"Hawaiian Standard Time", "heure normale d’Hawaï - Aléoutiennes - Honolulu"},
                {"India Standard Time", "heure de l’Inde - Calcutta"},
                {"Iran Standard Time", "heure de l’Iran - Téhéran"},
                {"Israel Standard Time", "heure d’Israël - Jérusalem"},
                {"Jordan Standard Time", "heure d’Europe de l’Est"},
                {"Kaliningrad Standard Time", "heure d’Europe de l’Est"},
                {"Korea Standard Time", "heure de la Corée - Séoul"},
                {"Libya Standard Time", "heure d’Europe de l’Est - Tripoli (Libye)"},
                {"Line Islands Standard Time", "heure des îles de la Ligne"},
                {"Lord Howe Standard Time", "heure de Lord Howe"},
                {"Magadan Standard Time", "heure de Magadan"},
                {"Magallanes Standard Time", "heure du Chili"},
                {"Marquesas Standard Time", "heure des îles Marquises"},
                {"Mauritius Standard Time", "heure de Maurice"},
                {"Middle East Standard Time", "heure d’Europe de l’Est - Beyrouth"},
                {"Montevideo Standard Time", "heure de l’Uruguay"},
                {"Morocco Standard Time", "heure d’Europe de l’Ouest"},
                {"Mountain Standard Time", "heure des Rocheuses"},
                {"Mountain Standard Time (Mexico)", "heure du Pacifique mexicain - Mazatlán"},
                {"Myanmar Standard Time", "heure du Myanmar - Rangoun"},
                {"N. Central Asia Standard Time", "heure de Krasnoïarsk - Novossibirsk"},
                {"Namibia Standard Time", "heure normale d’Afrique centrale"},
                {"Nepal Standard Time", "heure du Népal - Katmandou"},
                {"New Zealand Standard Time", "heure de la Nouvelle-Zélande"},
                {"Newfoundland Standard Time", "heure de Terre-Neuve - Saint-Jean de Terre-Neuve"},
                {"Norfolk Standard Time", "heure de l’île Norfolk"},
                {"North Asia East Standard Time", "heure d’Irkoutsk"},
                {"North Asia Standard Time", "heure de Krasnoïarsk"},
                {"North Korea Standard Time", "heure de la Corée"},
                {"Omsk Standard Time", "heure de Omsk"},
                {"Pacific SA Standard Time", "heure du Chili"},
                {"Pacific Standard Time", "heure du Pacifique nord-américain"},
                {"Pacific Standard Time (Mexico)", "heure du Pacifique nord-américain"},
                {"Pakistan Standard Time", "heure du Pakistan"},
                {"Paraguay Standard Time", "heure du Paraguay - Asunción"},
                {"Qyzylorda Standard Time", "heure du Kazakhstan - Kzyl Orda"},
                {"Romance Standard Time", "heure d’Europe centrale"},
                {"Russia Time Zone 10", "heure de Magadan"},
                {"Russia Time Zone 11", "heure de Petropavlovsk-Kamchatski - Kamtchatka"},
                {"Russia Time Zone 3", "heure de Samara"},
                {"Russian Standard Time", "heure de Moscou"},
                {"SA Eastern Standard Time", "heure de la Guyane française"},
                {"SA Pacific Standard Time", "heure de Colombie - Bogotá"},
                {"SA Western Standard Time", "heure de Bolivie"},
                {"Saint Pierre Standard Time", "heure de Saint-Pierre-et-Miquelon"},
                {"Sakhalin Standard Time", "heure de Magadan - Sakhaline"},
                {"Samoa Standard Time", "heure d’Apia"},
                {"Sao Tome Standard Time", "heure moyenne de Greenwich - São Tomé"},
                {"Saratov Standard Time", "heure de Samara"},
                {"SE Asia Standard Time", "heure d’Indochine"},
                {"Singapore Standard Time", "heure de Singapour"},
                {"South Africa Standard Time", "heure normale d’Afrique méridionale"},
                {"South Sudan Standard Time", "heure normale d’Afrique centrale"},
                {"Sri Lanka Standard Time", "heure de l’Inde"},
                {"Sudan Standard Time", "heure normale d’Afrique centrale"},
                {"Syria Standard Time", "heure d’Europe de l’Est - Damas"},
                {"Taipei Standard Time", "heure de Taipei"},
                {"Tasmania Standard Time", "heure de l’Est de l’Australie"},
                {"Tocantins Standard Time", "heure de Brasilia - Araguaína"},
                {"Tokyo Standard Time", "heure du Japon"},
                {"Tomsk Standard Time", "heure de Krasnoïarsk"},
                {"Tonga Standard Time", "heure des Tonga"},
                {"Transbaikal Standard Time", "heure de Iakoutsk - Tchita"},
                {"Turkey Standard Time", "heure de Turquie"},
                {"Turks And Caicos Standard Time", "heure de l’Est nord-américain"},
                {"Ulaanbaatar Standard Time", "heure d’Oulan-Bator"},
                {"US Eastern Standard Time", "heure de l’Est nord-américain"},
                {"US Mountain Standard Time", "heure des Rocheuses"},
                {"UTC", "UTC"},
                {"UTC+12", "UTC+12:00"},
                {"UTC+13", "UTC+13:00"},
                {"UTC-02", "UTC-02:00"},
                {"UTC-08", "UTC-08:00"},
                {"UTC-09", "UTC-09:00"},
                {"UTC-11", "UTC-11:00"},
                {"Venezuela Standard Time", "heure du Venezuela"},
                {"Vladivostok Standard Time", "heure de Vladivostok"},
                {"Volgograd Standard Time", "heure de Moscou"},
                {"W. Australia Standard Time", "heure de l’Ouest de l’Australie"},
                {"W. Central Africa Standard Time", "heure d’Afrique de l’Ouest"},
                {"W. Europe Standard Time", "heure d’Europe centrale"},
                {"W. Mongolia Standard Time", "heure de Hovd - Khovd"},
                {"West Asia Standard Time", "heure de l’Ouzbékistan - Tachkent"},
                {"West Bank Standard Time", "heure d’Europe de l’Est - Hébron"},
                {"West Pacific Standard Time", "heure de la Papouasie-Nouvelle-Guinée"},
                {"Yakutsk Standard Time", "heure de Iakoutsk"},
                {"Yukon Standard Time", "heure normale du Yukon"},
            }},
            {"es-ES", new Dictionary<string, string>
            {
                {"Afghanistan Standard Time", "hora de Afganistán"},
                {"Alaskan Standard Time", "hora de Alaska"},
                {"Aleutian Standard Time", "hora de Hawái-Aleutianas"},
                {"Altai Standard Time", "hora de Krasnoyarsk - Barnaúl"},
                {"Arab Standard Time", "hora de Arabia - Riad"},
                {"Arabian Standard Time", "hora estándar del Golfo - Dubái"},
                {"Arabic Standard Time", "hora de Arabia - Bagdad"},
                {"Argentina Standard Time", "hora de Argentina"},
                {"Astrakhan Standard Time", "hora de Samara - Astracán"},
                {"Atlantic Standard Time", "hora del Atlántico"},
                {"AUS Central Standard Time", "hora de Australia central"},
                {"Aus Central W. Standard Time", "hora de Australia centroccidental"},
                {"AUS Eastern Standard Time", "hora de Australia oriental - Sídney"},
                {"Azerbaijan Standard Time", "hora de Azerbaiyán - Bakú"},
                {"Azores Standard Time", "hora de las Azores"},
                {"Bahia Standard Time", "hora de Brasilia - Bahía"},
                {"Bangladesh Standard Time", "hora de Bangladés - Daca"},
                {"Belarus Standard Time", "hora de Moscú"},
                {"Bougainville Standard Time", "hora de Papúa Nueva Guinea"},
                {"Canada Central Standard Time", "hora central"},
                {"Cape Verde Standard Time", "hora de Cabo Verde"},
                {"Caucasus Standard Time", "hora de Armenia - Ereván"},
                {"Cen. Australia Standard Time", "hora de Australia central - Adelaida"},
                {"Central America Standard Time", "hora central"},
                {"Central Asia Standard Time", "hora de Kirguistán"},
                {"Central Brazilian Standard Time", "hora del Amazonas - Cuiabá"},
                {"Central Europe Standard Time", "hora de Europa central"},
                {"Central European Standard Time", "hora de Europa central - Varsovia"},
                {"Central Pacific Standard Time", "hora de las Islas Salomón"},
                {"Central Standard Time", "hora central"},
                {"Central Standard Time (Mexico)", "hora central - Ciudad de México"},
                {"Chatham Islands Standard Time", "hora de Chatham"},
                {"China Standard Time", "hora de China - Shanghái"},
                {"Cuba Standard Time", "hora de Cuba - La Habana"},
                {"Dateline Standard Time", "UTC-12:00"},
                {"E. Africa Standard Time", "hora de África oriental"},
                {"E. Australia Standard Time", "hora de Australia oriental"},
                {"E. Europe Standard Time", "hora de Europa oriental - Chisináu"},
                {"E. South America Standard Time", "hora de Brasilia - São Paulo"},
                {"Easter Island Standard Time", "hora de la isla de Pascua"},
                {"Eastern Standard Time", "hora oriental - Nueva York"},
                {"Eastern Standard Time (Mexico)", "hora oriental - Cancún"},
                {"Egypt Standard Time", "hora de Europa oriental - El Cairo"},
                {"Ekaterinburg Standard Time", "hora de Ekaterimburgo"},
                {"Fiji Standard Time", "hora de Fiyi"},
                {"FLE Standard Time", "hora de Europa oriental - Kiev"},
                {"Georgian Standard Time", "hora de Georgia - Tiflis"},
                {"GMT Standard Time", "hora del meridiano de Greenwich - Londres"},
                {"Greenland Standard Time", "Nuuk"},
                {"Greenwich Standard Time", "hora del meridiano de Greenwich - Reikiavik"},
                {"GTB Standard Time", "hora de Europa oriental - Bucarest"},
                {"Haiti Standard Time", "hora oriental - Puerto Príncipe"},
                {"Hawaiian Standard Time", "hora estándar de Hawái-Aleutianas - Honolulú"},
                {"India Standard Time", "hora estándar de la India - Calcuta"},
                {"Iran Standard Time", "hora de Irán - Teherán"},
                {"Israel Standard Time", "hora de Israel - Jerusalén"},
                {"Jordan Standard Time", "hora de Europa oriental - Ammán"},
                {"Kaliningrad Standard Time", "hora de Europa oriental - Kaliningrado"},
                {"Korea Standard Time", "hora de Corea - Seúl"},
                {"Libya Standard Time", "hora de Europa oriental - Trípoli"},
                {"Line Islands Standard Time", "hora de las Espóradas Ecuatoriales"},
                {"Lord Howe Standard Time", "hora de Lord Howe"},
                {"Magadan Standard Time", "hora de Magadán"},
                {"Magallanes Standard Time", "hora de Chile"},
                {"Marquesas Standard Time", "hora de Marquesas"},
                {"Mauritius Standard Time", "hora de Mauricio"},
                {"Middle East Standard Time", "hora de Europa oriental"},
                {"Montevideo Standard Time", "hora de Uruguay"},
                {"Morocco Standard Time", "hora de Europa occidental"},
                {"Mountain Standard Time", "hora de las Montañas Rocosas"},
                {"Mountain Standard Time (Mexico)", "hora del Pacífico de México - Mazatlán"},
                {"Myanmar Standard Time", "hora de Myanmar - Yangón (Rangún)"},
                {"N. Central Asia Standard Time", "hora de Krasnoyarsk"},
                {"Namibia Standard Time", "hora de África central"},
                {"Nepal Standard Time", "hora de Nepal - Katmandú"},
                {"New Zealand Standard Time", "hora de Nueva Zelanda"},
                {"Newfoundland Standard Time", "hora de Terranova - San Juan de Terranova"},
                {"Norfolk Standard Time", "hora de la isla Norfolk"},
                {"North Asia East Standard Time", "hora de Irkutsk"},
                {"North Asia Standard Time", "hora de Krasnoyarsk"},
                {"North Korea Standard Time", "hora de Corea"},
                {"Omsk Standard Time", "hora de Omsk"},
                {"Pacific SA Standard Time", "hora de Chile - Santiago de Chile"},
                {"Pacific Standard Time", "hora del Pacífico - Los Ángeles"},
                {"Pacific Standard Time (Mexico)", "hora del Pacífico"},
                {"Pakistan Standard Time", "hora de Pakistán"},
                {"Paraguay Standard Time", "hora de Paraguay - Asunción"},
                {"Qyzylorda Standard Time", "hora de Kazajistán - Kyzylorda"},
                {"Romance Standard Time", "hora de Europa central - París"},
                {"Russia Time Zone 10", "hora de Magadán - Srednekolimsk"},
                {"Russia Time Zone 11", "hora de Kamchatka"},
                {"Russia Time Zone 3", "hora de Samara"},
                {"Russian Standard Time", "hora de Moscú"},
                {"SA Eastern Standard Time", "hora de la Guayana Francesa - Cayena"},
                {"SA Pacific Standard Time", "hora de Colombia - Bogotá"},
                {"SA Western Standard Time", "hora de Bolivia"},
                {"Saint Pierre Standard Time", "hora de San Pedro y Miquelón"},
                {"Sakhalin Standard Time", "hora de Magadán - Sajalín"},
                {"Samoa Standard Time", "hora de Apia"},
                {"Sao Tome Standard Time", "hora del meridiano de Greenwich - Santo Tomé"},
                {"Saratov Standard Time", "hora de Samara - Sarátov"},
                {"SE Asia Standard Time", "hora de Indochina"},
                {"Singapore Standard Time", "hora de Singapur"},
                {"South Africa Standard Time", "hora de Sudáfrica - Johannesburgo"},
                {"South Sudan Standard Time", "hora de África central"},
                {"Sri Lanka Standard Time", "hora estándar de la India"},
                {"Sudan Standard Time", "hora de África central - Jartum"},
                {"Syria Standard Time", "hora de Europa oriental - Damasco"},
                {"Taipei Standard Time", "hora de Taipéi"},
                {"Tasmania Standard Time", "hora de Australia oriental"},
                {"Tocantins Standard Time", "hora de Brasilia - Araguaína"},
                {"Tokyo Standard Time", "hora de Japón - Tokio"},
                {"Tomsk Standard Time", "hora de Krasnoyarsk"},
                {"Tonga Standard Time", "hora de Tonga"},
                {"Transbaikal Standard Time", "hora de Yakutsk - Chitá"},
                {"Turkey Standard Time", "Hora de Turquía - Estambul"},
                {"Turks And Caicos Standard Time", "hora oriental - Gran Turca"},
                {"Ulaanbaatar Standard Time", "hora de Ulán Bator"},
                {"US Eastern Standard Time", "hora oriental - Indianápolis"},
                {"US Mountain Standard Time", "hora de las Montañas Rocosas"},
                {"UTC", "UTC"},
                {"UTC+12", "UTC+12:00"},
                {"UTC+13", "UTC+13:00"},
                {"UTC-02", "UTC-02:00"},
                {"UTC-08", "UTC-08:00"},
                {"UTC-09", "UTC-09:00"},
                {"UTC-11", "UTC-11:00"},
                {"Venezuela Standard Time", "hora de Venezuela"},
                {"Vladivostok Standard Time", "hora de Vladivostok"},
                {"Volgograd Standard Time", "hora de Moscú - Volgogrado"},
                {"W. Australia Standard Time", "hora de Australia occidental"},
                {"W. Central Africa Standard Time", "hora de África occidental"},
                {"W. Europe Standard Time", "hora de Europa central - Berlín"},
                {"W. Mongolia Standard Time", "hora de Hovd - Khovd"},
                {"West Asia Standard Time", "hora de Uzbekistán - Taskent"},
                {"West Bank Standard Time", "hora de Europa oriental - Hebrón"},
                {"West Pacific Standard Time", "hora de Papúa Nueva Guinea"},
                {"Yakutsk Standard Time", "hora de Yakutsk"},
                {"Yukon Standard Time", "hora de Yukón"},
            }},
            {"pt-BR", new Dictionary<string, string>
            {
                {"Afghanistan Standard Time", "Horário do Afeganistão - Cabul"},
                {"Alaskan Standard Time", "Horário do Alasca"},
                {"Aleutian Standard Time", "Horário do Havaí e Ilhas Aleutas"},
                {"Altai Standard Time", "Horário de Krasnoyarsk"},
                {"Arab Standard Time", "Horário da Arábia - Riade"},
                {"Arabian Standard Time", "Horário do Golfo"},
                {"Arabic Standard Time", "Horário da Arábia - Bagdá"},
                {"Argentina Standard Time", "Horário da Argentina"},
                {"Astrakhan Standard Time", "Horário de Samara - Astracã"},
                {"Atlantic Standard Time", "Horário do Atlântico"},
                {"AUS Central Standard Time", "Horário da Austrália Central"},
                {"Aus Central W. Standard Time", "Horário da Austrália Centro-Ocidental"},
                {"AUS Eastern Standard Time", "Horário da Austrália Oriental"},
                {"Azerbaijan Standard Time", "Horário do Arzeibaijão"},
                {"Azores Standard Time", "Horário dos Açores"},
                {"Bahia Standard Time", "Horário de Brasília"},
                {"Bangladesh Standard Time", "Horário de Bangladesh - Dacca"},
                {"Belarus Standard Time", "Horário de Moscou"},
                {"Bougainville Standard Time", "Horário de Papua-Nova Guiné"},
                {"Canada Central Standard Time", "Horário Central"},
                {"Cape Verde Standard Time", "Horário de Cabo Verde"},
                {"Caucasus Standard Time", "Horário da Armênia"},
                {"Cen. Australia Standard Time", "Horário da Austrália Central"},
                {"Central America Standard Time", "Horário Central"},
                {"Central Asia Standard Time", "Horário do Quirguistão"},
                {"Central Brazilian Standard Time", "Horário do Amazonas - Cuiabá"},
                {"Central Europe Standard Time", "Horário da Europa Central - Budapeste"},
                {"Central European Standard Time", "Horário da Europa Central - Varsóvia"},
                {"Central Pacific Standard Time", "Horário das Ilhas Salomão"},
                {"Central Standard Time", "Horário Central"},
                {"Central Standard Time (Mexico)", "Horário Central - Cidade do México"},
                {"Chatham Islands Standard Time", "Horário de Chatham - Chatnam"},
                {"China Standard Time", "Horário da China - Xangai"},
                {"Cuba Standard Time", "Horário de Cuba"},
                {"Dateline Standard Time", "UTC-12:00"},
                {"E. Africa Standard Time", "Horário da África Oriental - Nairóbi"},
                {"E. Australia Standard Time", "Horário da Austrália Oriental"},
                {"E. Europe Standard Time", "Horário da Europa Oriental - Chișinău"},
                {"E. South America Standard Time", "Horário de Brasília - São Paulo"},
                {"Easter Island Standard Time", "Horário da Ilha de Páscoa"},
                {"Eastern Standard Time", "Horário do Leste - Nova York"},
                {"Eastern Standard Time (Mexico)", "Horário do Leste - Cancún"},
                {"Egypt Standard Time", "Horário da Europa Oriental"},
                {"Ekaterinburg Standard Time", "Horário de Ecaterimburgo"},
                {"Fiji Standard Time", "Horário de Fiji"},
                {"FLE Standard Time", "Horário da Europa Oriental - Kiev"},
                {"Georgian Standard Time", "Horário da Geórgia"},
                {"GMT Standard Time", "Horário do Meridiano de Greenwich - Londres"},
                {"Greenland Standard Time", "Nuuk"},
                {"Greenwich Standard Time", "Horário do Meridiano de Greenwich - Reykjavík"},
                {"GTB Standard Time", "Horário da Europa Oriental - Bucareste"},
                {"Haiti Standard Time", "Horário do Leste - Porto Príncipe"},
                {"Hawaiian Standard Time", "Horário Padrão do Havaí e Ilhas Aleutas - Honolulu"},
                {"India Standard Time", "Horário Padrão da Índia - Calcutá"},
                {"Iran Standard Time", "Horário do Irã - Teerã"},
                {"Israel Standard Time", "Horário de Israel - Jerusalém"},
                {"Jordan Standard Time", "Horário da Europa Oriental - Amã"},
                {"Kaliningrad Standard Time", "Horário da Europa Oriental - Kaliningrado"},
                {"Korea Standard Time", "Horário da Coreia - Seul"},
                {"Libya Standard Time", "Horário da Europa Oriental - Trípoli"},
                {"Line Islands Standard Time", "Horário das Ilhas da Linha"},
                {"Lord Howe Standard Time", "Horário de Lord Howe"},
                {"Magadan Standard Time", "Horário de Magadan"},
                {"Magallanes Standard Time", "Horário do Chile"},
                {"Marquesas Standard Time", "Horário das Marquesas"},
                {"Mauritius Standard Time", "Horário de Maurício"},
                {"Middle East Standard Time", "Horário da Europa Oriental - Beirute"},
                {"Montevideo Standard Time", "Horário do Uruguai - Montevidéu"},
                {"Morocco Standard Time", "Horário da Europa Ocidental"},
                {"Mountain Standard Time", "Horário das Montanhas"},
                {"Mountain Standard Time (Mexico)", "Horário do Pacífico Mexicano - Mazatlán"},
                {"Myanmar Standard Time", "Horário de Mianmar - Rangum"},
                {"N. Central Asia Standard Time", "Horário de Krasnoyarsk"},
                {"Namibia Standard Time", "Horário da África Central"},
                {"Nepal Standard Time", "Horário do Nepal - Katmandu"},
                {"New Zealand Standard Time", "Horário da Nova Zelândia"},
                {"Newfoundland Standard Time", "Horário da Terra Nova - Saint John’s"},
                {"Norfolk Standard Time", "Horário da Ilha Norfolk"},
                {"North Asia East Standard Time", "Horário de Irkutsk"},
                {"North Asia Standard Time", "Horário de Krasnoyarsk"},
                {"North Korea Standard Time", "Horário da Coreia"},
                {"Omsk Standard Time", "Horário de Omsk"},
                {"Pacific SA Standard Time", "Horário do Chile"},
                {"Pacific Standard Time", "Horário do Pacífico"},
                {"Pacific Standard Time (Mexico)", "Horário do Pacífico"},
                {"Pakistan Standard Time", "Horário do Paquistão"},
                {"Paraguay Standard Time", "Horário do Paraguai - Assunção"},
                {"Qyzylorda Standard Time", "Horário do Cazaquistão"},
                {"Romance Standard Time", "Horário da Europa Central"},
                {"Russia Time Zone 10", "Horário de Magadan"},
                {"Russia Time Zone 11", "Horário de Petropavlovsk-Kamchatski"},
                {"Russia Time Zone 3", "Horário de Samara"},
                {"Russian Standard Time", "Horário de Moscou"},
                {"SA Eastern Standard Time", "Horário da Guiana Francesa - Caiena"},
                {"SA Pacific Standard Time", "Horário da Colômbia - Bogotá"},
                {"SA Western Standard Time", "Horário da Bolívia"},
                {"Saint Pierre Standard Time", "Horário de São Pedro e Miquelão - Saint-Pierre"},
                {"Sakhalin Standard Time", "Horário de Magadan - Sacalina"},
                {"Samoa Standard Time", "Horário de Apia"},
                {"Sao Tome Standard Time", "Horário do Meridiano de Greenwich - São Tomé"},
                {"Saratov Standard Time", "Horário de Samara"},
                {"SE Asia Standard Time", "Horário da Indochina"},
                {"Singapore Standard Time", "Horário Padrão de Singapura"},
                {"South Africa Standard Time", "Horário da África do Sul - Joanesburgo"},
                {"South Sudan Standard Time", "Horário da África Central"},
                {"Sri Lanka Standard Time", "Horário Padrão da Índia"},
                {"Sudan Standard Time", "Horário da África Central - Cartum"},
                {"Syria Standard Time", "Horário da Europa Oriental - Damasco"},
                {"Taipei Standard Time", "Horário de Taipei"},
                {"Tasmania Standard Time", "Horário da Austrália Oriental"},
                {"Tocantins Standard Time", "Horário de Brasília - Araguaína"},
                {"Tokyo Standard Time", "Horário do Japão - Tóquio"},
                {"Tomsk Standard Time", "Horário de Krasnoyarsk"},
                {"Tonga Standard Time", "Horário de Tonga"},
                {"Transbaikal Standard Time", "Horário de Yakutsk"},
                {"Turkey Standard Time", "Horário da Turquia - Istambul"},
                {"Turks And Caicos Standard Time", "Horário do Leste"},
                {"Ulaanbaatar Standard Time", "Horário de Ulan Bator"},
                {"US Eastern Standard Time", "Horário do Leste - Indianápolis"},
                {"US Mountain Standard Time", "Horário das Montanhas"},
                {"UTC", "UTC"},
                {"UTC+12", "UTC+12:00"},
                {"UTC+13", "UTC+13:00"},
                {"UTC-02", "UTC-02:00"},
                {"UTC-08", "UTC-08:00"},
                {"UTC-09", "UTC-09:00"},
                {"UTC-11", "UTC-11:00"},
                {"Venezuela Standard Time", "Horário da Venezuela"},
                {"Vladivostok Standard Time", "Horário de Vladivostok"},
                {"Volgograd Standard Time", "Horário de Moscou - Volgogrado"},
                {"W. Australia Standard Time", "Horário da Austrália Ocidental"},
                {"W. Central Africa Standard Time", "Horário da África Ocidental"},
                {"W. Europe Standard Time", "Horário da Europa Central - Berlim"},
                {"W. Mongolia Standard Time", "Horário de Hovd - Khovd"},
                {"West Asia Standard Time", "Horário do Uzbequistão"},
                {"West Bank Standard Time", "Horário da Europa Oriental"},
                {"West Pacific Standard Time", "Horário de Papua-Nova Guiné"},
                {"Yakutsk Standard Time", "Horário de Yakutsk"},
                {"Yukon Standard Time", "Horário do Yukon"},
            }},
            {"ru-RU", new Dictionary<string, string>
            {
                {"Afghanistan Standard Time", "Афганистан - Кабул"},
                {"Alaskan Standard Time", "Аляска - Анкоридж"},
                {"Aleutian Standard Time", "Гавайско-алеутское время - Адак"},
                {"Altai Standard Time", "Красноярск - Барнаул"},
                {"Arab Standard Time", "Саудовская Аравия - Эр-Рияд"},
                {"Arabian Standard Time", "Персидский залив - Дубай"},
                {"Arabic Standard Time", "Саудовская Аравия - Багдад"},
                {"Argentina Standard Time", "Аргентина - Буэнос-Айрес"},
                {"Astrakhan Standard Time", "Время в Самаре - Астрахань"},
                {"Atlantic Standard Time", "Атлантическое время - Галифакс"},
                {"AUS Central Standard Time", "Центральная Австралия - Дарвин"},
                {"Aus Central W. Standard Time", "Центральная Австралия, западное время - Юкла"},
                {"AUS Eastern Standard Time", "Восточная Австралия - Сидней"},
                {"Azerbaijan Standard Time", "Азербайджан - Баку"},
                {"Azores Standard Time", "Азорские о-ва"},
                {"Bahia Standard Time", "Бразилия - Баия"},
                {"Bangladesh Standard Time", "Бангладеш - Дакка"},
                {"Belarus Standard Time", "Москва - Минск"},
                {"Bougainville Standard Time", "Папуа – Новая Гвинея - Бугенвиль"},
                {"Canada Central Standard Time", "Центральная Америка - Реджайна"},
                {"Cape Verde Standard Time", "Кабо-Верде"},
                {"Caucasus Standard Time", "Армения - Ереван"},
                {"Cen. Australia Standard Time", "Центральная Австралия - Аделаида"},
                {"Central America Standard Time", "Центральная Америка - Гватемала"},
                {"Central Asia Standard Time", "Киргизия - Бишкек"},
                {"Central Brazilian Standard Time", "Амазонка - Куяба"},
                {"Central Europe Standard Time", "Центральная Европа - Будапешт"},
                {"Central European Standard Time", "Центральная Европа - Варшава"},
                {"Central Pacific Standard Time", "Соломоновы Острова - Гуадалканал"},
                {"Central Standard Time", "Центральная Америка - Чикаго"},
                {"Central Standard Time (Mexico)", "Центральная Америка - Мехико"},
                {"Chatham Islands Standard Time", "Чатем"},
                {"China Standard Time", "Китай - Шанхай"},
                {"Cuba Standard Time", "Куба - Гавана"},
                {"Dateline Standard Time", "UTC-12:00"},
                {"E. Africa Standard Time", "Восточная Африка - Найроби"},
                {"E. Australia Standard Time", "Восточная Австралия - Брисбен"},
                {"E. Europe Standard Time", "Восточная Европа - Кишинев"},
                {"E. South America Standard Time", "Бразилия - Сан-Паулу"},
                {"Easter Island Standard Time", "О-в Пасхи"},
                {"Eastern Standard Time", "Восточная Америка - Нью-Йорк"},
                {"Eastern Standard Time (Mexico)", "Восточная Америка - Канкун"},
                {"Egypt Standard Time", "Восточная Европа - Каир"},
                {"Ekaterinburg Standard Time", "Екатеринбург"},
                {"Fiji Standard Time", "Фиджи"},
                {"FLE Standard Time", "Восточная Европа - Киев"},
                {"Georgian Standard Time", "Грузия - Тбилиси"},
                {"GMT Standard Time", "Среднее время по Гринвичу - Лондон"},
                {"Greenland Standard Time", "Нуук"},
                {"Greenwich Standard Time", "Среднее время по Гринвичу - Рейкьявик"},
                {"GTB Standard Time", "Восточная Европа - Бухарест"},
                {"Haiti Standard Time", "Восточная Америка - Порт-о-Пренс"},
                {"Hawaiian Standard Time", "Гавайско-алеутское стандартное время - Гонолулу"},
                {"India Standard Time", "Индия - Калькутта"},
                {"Iran Standard Time", "Иран - Тегеран"},
                {"Israel Standard Time", "Израиль - Иерусалим"},
                {"Jordan Standard Time", "Восточная Европа - Амман"},
                {"Kaliningrad Standard Time", "Восточная Европа - Калининград"},
                {"Korea Standard Time", "Корея - Сеул"},
                {"Libya Standard Time", "Восточная Европа - Триполи"},
                {"Line Islands Standard Time", "о-ва Лайн - Киритимати"},
                {"Lord Howe Standard Time", "Лорд-Хау"},
                {"Magadan Standard Time", "Магадан"},
                {"Magallanes Standard Time", "Чили - Пунта-Аренас"},
                {"Marquesas Standard Time", "Маркизские о-ва"},
                {"Mauritius Standard Time", "Маврикий"},
                {"Middle East Standard Time", "Восточная Европа - Бейрут"},
                {"Montevideo Standard Time", "Уругвай - Монтевидео"},
                {"Morocco Standard Time", "Западная Европа - Касабланка"},
                {"Mountain Standard Time", "Горное время (Северная Америка) - Денвер"},
                {"Mountain Standard Time (Mexico)", "Тихоокеанское мексиканское время - Масатлан"},
                {"Myanmar Standard Time", "Мьянма - Янгон"},
                {"N. Central Asia Standard Time", "Красноярск - Новосибирск"},
                {"Namibia Standard Time", "Центральная Африка - Виндхук"},
                {"Nepal Standard Time", "Непал - Катманду"},
                {"New Zealand Standard Time", "Новая Зеландия - Окленд"},
                {"Newfoundland Standard Time", "Ньюфаундленд - Сент-Джонс"},
                {"Norfolk Standard Time", "Норфолк"},
                {"North Asia East Standard Time", "Иркутск"},
                {"North Asia Standard Time", "Красноярск"},
                {"North Korea Standard Time", "Корея - Пхеньян"},
                {"Omsk Standard Time", "Омск"},
                {"Pacific SA Standard Time", "Чили - Сантьяго"},
                {"Pacific Standard Time", "Тихоокеанское время - Лос-Анджелес"},
                {"Pacific Standard Time (Mexico)", "Тихоокеанское время - Тихуана"},
                {"Pakistan Standard Time", "Пакистан - Карачи"},
                {"Paraguay Standard Time", "Парагвай - Асунсьон"},
                {"Qyzylorda Standard Time", "Казахстан - Кызылорда"},
                {"Romance Standard Time", "Центральная Европа - Париж"},
                {"Russia Time Zone 10", "Магадан - Среднеколымск"},
                {"Russia Time Zone 11", "Петропавловск-Камчатский"},
                {"Russia Time Zone 3", "Время в Самаре - Самара"},
                {"Russian Standard Time", "Москва"},
                {"SA Eastern Standard Time", "Французская Гвиана - Кайенна"},
                {"SA Pacific Standard Time", "Колумбия - Богота"},
                {"SA Western Standard Time", "Боливия - Ла-Пас"},
                {"Saint Pierre Standard Time", "Сен-Пьер и Микелон"},
                {"Sakhalin Standard Time", "Магадан - о-в Сахалин"},
                {"Samoa Standard Time", "Апиа"},
                {"Sao Tome Standard Time", "Среднее время по Гринвичу - Сан-Томе"},
                {"Saratov Standard Time", "Время в Самаре - Саратов"},
                {"SE Asia Standard Time", "Индокитай - Бангкок"},
                {"Singapore Standard Time", "Сингапур"},
                {"South Africa Standard Time", "Южная Африка - Йоханнесбург"},
                {"South Sudan Standard Time", "Центральная Африка - Джуба"},
                {"Sri Lanka Standard Time", "Индия - Коломбо"},
                {"Sudan Standard Time", "Центральная Африка - Хартум"},
                {"Syria Standard Time", "Восточная Европа - Дамаск"},
                {"Taipei Standard Time", "Тайвань - Тайбэй"},
                {"Tasmania Standard Time", "Восточная Австралия - Хобарт"},
                {"Tocantins Standard Time", "Бразилия - Арагуаина"},
                {"Tokyo Standard Time", "Япония - Токио"},
                {"Tomsk Standard Time", "Красноярск - Томск"},
                {"Tonga Standard Time", "Тонга - Тонгатапу"},
                {"Transbaikal Standard Time", "Якутск - Чита"},
                {"Turkey Standard Time", "Турецкое время - Стамбул"},
                {"Turks And Caicos Standard Time", "Восточная Америка - Гранд-Терк"},
                {"Ulaanbaatar Standard Time", "Улан-Батор"},
                {"US Eastern Standard Time", "Восточная Америка - Индианаполис"},
                {"US Mountain Standard Time", "Горное время (Северная Америка) - Финикс"},
                {"UTC", "UTC"},
                {"UTC+12", "UTC+12:00"},
                {"UTC+13", "UTC+13:00"},
                {"UTC-02", "UTC-02:00"},
                {"UTC-08", "UTC-08:00"},
                {"UTC-09", "UTC-09:00"},
                {"UTC-11", "UTC-11:00"},
                {"Venezuela Standard Time", "Венесуэла - Каракас"},
                {"Vladivostok Standard Time", "Владивосток"},
                {"Volgograd Standard Time", "Москва - Волгоград"},
                {"W. Australia Standard Time", "Западная Австралия - Перт"},
                {"W. Central Africa Standard Time", "Западная Африка - Лагос"},
                {"W. Europe Standard Time", "Центральная Европа - Берлин"},
                {"W. Mongolia Standard Time", "Ховд"},
                {"West Asia Standard Time", "Узбекистан - Ташкент"},
                {"West Bank Standard Time", "Восточная Европа - Хеврон"},
                {"West Pacific Standard Time", "Папуа – Новая Гвинея - Порт-Морсби"},
                {"Yakutsk Standard Time", "Якутск"},
                {"Yukon Standard Time", "Юкон - Уайтхорс"},
            }},
        };

        private static string LocalizedCommonName(string id, string languageCode)
        {
            Dictionary<string, string> names;
            if (!LocalizedWindowsTimeZoneNames.TryGetValue(languageCode, out names))
            {
                names = LocalizedWindowsTimeZoneNames["en-US"];
            }
            string name;
            return names.TryGetValue(id, out name) ? name : string.Empty;
        }

        private static string EnglishFallbackName(string id)
        {
            string text = id;
            text = text.Replace(" Standard Time", "");
            text = text.Replace(" Daylight Time", "");
            text = text.Replace("_", " ");
            return text;
        }

        private static string StripWindowsOffset(string displayName)
        {
            if (displayName.StartsWith("(UTC", StringComparison.OrdinalIgnoreCase))
            {
                int index = displayName.IndexOf(')');
                if (index >= 0 && index + 1 < displayName.Length)
                {
                    return displayName.Substring(index + 1).Trim();
                }
            }
            return displayName;
        }

        private static string CommonZoneName(TimeSpan offset, string languageCode)
        {
            if (!(languageCode == "zh-CN" || languageCode == "zh-TW"))
            {
                return "";
            }

            if (offset.Minutes != 0 || offset.Seconds != 0)
            {
                return "";
            }

            int hours = offset.Hours;
            if (hours == 0)
            {
                return "零时区";
            }
            if (hours > 0)
            {
                return "东" + hours.ToString(CultureInfo.InvariantCulture) + "区";
            }
            return "西" + Math.Abs(hours).ToString(CultureInfo.InvariantCulture) + "区";
        }

        private static string SearchAliases(string id)
        {
            if (id == "China Standard Time") return "china hong kong beijing macau shanghai taiwan 中国 香港 北京 澳门 上海 台湾 中國 澳門 台灣";
            if (id == "US Mountain Standard Time") return "united states usa america arizona phoenix 美国 美國 亞利桑那 亚利桑那 凤凰城 鳳凰城";
            if (id == "Pacific Standard Time") return "united states usa america canada pacific california los angeles seattle 美国 美國 太平洋 加州 洛杉矶 洛杉磯 西雅图 西雅圖";
            if (id == "Eastern Standard Time") return "united states usa america canada eastern new york washington toronto 美国 美國 东部 東部 纽约 紐約 华盛顿 華盛頓 多伦多 多倫多";
            if (id == "Central Standard Time") return "united states usa america central chicago mexico 美国 美國 中部 芝加哥 墨西哥";
            if (id == "Tokyo Standard Time") return "japan tokyo osaka sapporo 日本 东京 東京 大阪 札幌";
            if (id == "Korea Standard Time") return "korea seoul 韩国 韓國 首尔 首爾";
            if (id == "GMT Standard Time") return "uk united kingdom britain london ireland 英国 英國 伦敦 倫敦 爱尔兰 愛爾蘭";
            if (id == "W. Europe Standard Time") return "germany france netherlands europe berlin paris 德国 德國 法国 法國 欧洲 歐洲 柏林 巴黎";
            if (id == "Romance Standard Time") return "france spain italy paris madrid rome 法国 法國 西班牙 意大利 巴黎 马德里 馬德里 罗马 羅馬";
            return "";
        }
    }

    internal static class StartupManager
    {
        private const string ShortcutName = "TaskbarWorldClock.lnk";
        private const string LegacyShortcutName = "ChinaTaskbarClock.lnk";

        public static bool IsEnabled()
        {
            MigrateLegacyShortcut();
            return File.Exists(ShortcutPath) || File.Exists(LegacyShortcutPath);
        }

        public static void SetEnabled(bool enabled)
        {
            if (enabled)
            {
                CreateShortcut();
                if (File.Exists(LegacyShortcutPath))
                {
                    File.Delete(LegacyShortcutPath);
                }
            }
            else
            {
                if (File.Exists(ShortcutPath))
                {
                    File.Delete(ShortcutPath);
                }
                if (File.Exists(LegacyShortcutPath))
                {
                    File.Delete(LegacyShortcutPath);
                }
            }
        }

        private static string ShortcutPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), ShortcutName);
            }
        }

        private static string LegacyShortcutPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), LegacyShortcutName);
            }
        }

        private static void CreateShortcut()
        {
            string exe = Application.ExecutablePath;
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(shellType);
            object shortcut = shellType.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { ShortcutPath });
            Type shortcutType = shortcut.GetType();
            shortcutType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { exe });
            shortcutType.InvokeMember("Arguments", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { "" });
            shortcutType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { Path.GetDirectoryName(exe) });
            shortcutType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);
        }

        private static void MigrateLegacyShortcut()
        {
            if (!File.Exists(LegacyShortcutPath))
            {
                return;
            }

            if (!File.Exists(ShortcutPath))
            {
                File.Move(LegacyShortcutPath, ShortcutPath);
            }
            else
            {
                File.Delete(LegacyShortcutPath);
            }
        }
    }

    internal static class NativeMethods
    {
        public static readonly IntPtr HwndBroadcast = new IntPtr(0xffff);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern int GetPixel(IntPtr hdc, int x, int y);

        public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
