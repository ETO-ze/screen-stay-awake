using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ScreenStayAwake;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    [Flags]
    private enum ExecutionState : uint
    {
        Continuous = 0x80000000,
        SystemRequired = 0x00000001,
        DisplayRequired = 0x00000002
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

    private readonly Label statusLabel = new();
    private readonly Label detailLabel = new();
    private readonly Button toggleButton = new();
    private readonly NotifyIcon trayIcon = new();
    private readonly System.Windows.Forms.Timer heartbeat = new() { Interval = 30_000 };
    private bool enabled;

    public MainForm()
    {
        Text = "屏幕常亮工具";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(420, 255);
        BackColor = Color.FromArgb(248, 250, 252);
        Font = new Font("Microsoft YaHei UI", 10F);

        var titleLabel = new Label
        {
            AutoSize = true,
            Text = "保持屏幕点亮",
            Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(32, 26)
        };

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
        toggleButton.Click += (_, _) => Toggle();

        Controls.AddRange([titleLabel, statusLabel, detailLabel, toggleButton]);

        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("显示窗口", null, (_, _) => ShowFromTray());
        trayMenu.Items.Add("切换状态", null, (_, _) => Toggle());
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("退出", null, (_, _) => Close());
        trayIcon.Icon = SystemIcons.Information;
        trayIcon.Text = "屏幕常亮工具";
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.DoubleClick += (_, _) => ShowFromTray();

        heartbeat.Tick += (_, _) => RefreshExecutionState();
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        FormClosing += (_, _) => Disable();
        Resize += (_, _) =>
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
        if (enabled)
            Disable();
        else
            Enable();
    }

    private void Enable()
    {
        var result = SetThreadExecutionState(ExecutionState.Continuous | ExecutionState.SystemRequired | ExecutionState.DisplayRequired);
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

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (enabled && e.Mode == PowerModes.Resume)
            BeginInvoke(RefreshExecutionState);
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
            detailLabel.Text = "屏幕和系统不会因空闲自动休眠。\n最小化窗口后，常亮模式仍会继续运行。";
            toggleButton.Text = "关闭常亮模式";
            toggleButton.BackColor = Color.FromArgb(22, 163, 74);
        }
        else
        {
            statusLabel.Text = "○ 常亮模式未开启";
            statusLabel.ForeColor = Color.FromArgb(100, 116, 139);
            detailLabel.Text = "按下按钮后，电脑空闲时屏幕仍会保持点亮。\n关闭本工具将自动恢复系统原有电源设置。";
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
