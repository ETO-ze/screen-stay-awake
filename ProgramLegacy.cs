using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ScreenStayAwake
{
    internal static class ProgramLegacy
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainFormLegacy());
        }
    }

    internal sealed class MainFormLegacy : Form
    {
        [Flags]
        private enum ExecutionState : uint
        {
            Continuous = 0x80000000,
            SystemRequired = 0x00000001,
            DisplayRequired = 0x00000002
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern ExecutionState SetThreadExecutionState(ExecutionState flags);

        private readonly Label statusLabel = new Label();
        private readonly Label detailLabel = new Label();
        private readonly Button toggleButton = new Button();
        private readonly NotifyIcon trayIcon = new NotifyIcon();
        private readonly Timer heartbeat = new Timer();
        private bool enabled;

        public MainFormLegacy()
        {
            Text = "屏幕常亮工具";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(420, 255);
            BackColor = Color.FromArgb(248, 250, 252);
            Font = new Font("Microsoft YaHei UI", 10F);

            Label titleLabel = new Label();
            titleLabel.AutoSize = true;
            titleLabel.Text = "保持屏幕点亮";
            titleLabel.Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(15, 23, 42);
            titleLabel.Location = new Point(32, 26);

            statusLabel.AutoSize = true;
            statusLabel.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
            statusLabel.Location = new Point(33, 78);

            detailLabel.AutoSize = false;
            detailLabel.Size = new Size(350, 46);
            detailLabel.ForeColor = Color.FromArgb(71, 85, 105);
            detailLabel.Location = new Point(34, 110);

            toggleButton.Size = new Size(352, 48);
            toggleButton.Location = new Point(34, 175);
            toggleButton.FlatStyle = FlatStyle.Flat;
            toggleButton.FlatAppearance.BorderSize = 0;
            toggleButton.ForeColor = Color.White;
            toggleButton.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
            toggleButton.Cursor = Cursors.Hand;
            toggleButton.Click += delegate { Toggle(); };

            Controls.Add(titleLabel);
            Controls.Add(statusLabel);
            Controls.Add(detailLabel);
            Controls.Add(toggleButton);

            ContextMenuStrip trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("显示窗口", null, delegate { ShowFromTray(); });
            trayMenu.Items.Add("切换状态", null, delegate { Toggle(); });
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("退出", null, delegate { Close(); });
            trayIcon.Icon = SystemIcons.Information;
            trayIcon.Text = "屏幕常亮工具";
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.DoubleClick += delegate { ShowFromTray(); };

            heartbeat.Interval = 30000;
            heartbeat.Tick += delegate { RefreshExecutionState(); };
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            FormClosing += delegate { Disable(); };
            Resize += delegate
            {
                if (WindowState == FormWindowState.Minimized)
                {
                    Hide();
                    trayIcon.Visible = true;
                    trayIcon.ShowBalloonTip(1200, "屏幕常亮工具", "已最小化到系统托盘，双击图标可恢复窗口。", ToolTipIcon.Info);
                }
            };

            UpdateInterface();
        }

        private void Toggle()
        {
            if (enabled) Disable(); else Enable();
        }

        private void Enable()
        {
            ExecutionState result = SetThreadExecutionState(ExecutionState.Continuous | ExecutionState.SystemRequired | ExecutionState.DisplayRequired);
            if (result == 0)
            {
                MessageBox.Show("无法启用常亮模式。请重试或以管理员身份运行。", "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            enabled = true;
            heartbeat.Start();
            UpdateInterface();
        }

        private void Disable()
        {
            SetThreadExecutionState(ExecutionState.Continuous);
            enabled = false;
            heartbeat.Stop();
            UpdateInterface();
        }

        private void RefreshExecutionState()
        {
            if (enabled)
                SetThreadExecutionState(ExecutionState.Continuous | ExecutionState.SystemRequired | ExecutionState.DisplayRequired);
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (enabled && e.Mode == PowerModes.Resume)
                BeginInvoke(new MethodInvoker(RefreshExecutionState));
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            trayIcon.Visible = false;
        }

        private void UpdateInterface()
        {
            if (enabled)
            {
                statusLabel.Text = "● 常亮模式已开启";
                statusLabel.ForeColor = Color.FromArgb(22, 163, 74);
                detailLabel.Text = "屏幕和系统不会因空闲自动休眠。\r\n最小化窗口后，常亮模式仍会继续运行。";
                toggleButton.Text = "关闭常亮模式";
                toggleButton.BackColor = Color.FromArgb(22, 163, 74);
            }
            else
            {
                statusLabel.Text = "○ 常亮模式未开启";
                statusLabel.ForeColor = Color.FromArgb(100, 116, 139);
                detailLabel.Text = "按下按钮后，电脑空闲时屏幕仍会保持点亮。\r\n关闭本工具将自动恢复系统原有电源设置。";
                toggleButton.Text = "开启常亮模式";
                toggleButton.BackColor = Color.FromArgb(37, 99, 235);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SystemEvents.PowerModeChanged -= OnPowerModeChanged;
                trayIcon.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
