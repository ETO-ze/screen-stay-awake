# 屏幕常亮工具

一个轻量的 Windows 桌面工具，用于在电脑空闲时保持屏幕点亮。

## 使用

1. 下载并运行 `屏幕常亮工具（新版）.exe`。
2. 点击“开启常亮模式”。
3. 需要恢复默认电源策略时，点击“关闭常亮模式”或直接退出程序。

程序最小化后会驻留在系统托盘，双击托盘图标可以恢复窗口。

## 特点

- 不模拟鼠标或键盘操作
- 不修改 Windows 电源计划
- 退出后自动恢复系统默认行为
- 支持休眠唤醒后自动继续常亮

## 构建

使用 Windows 自带的 .NET Framework C# 编译器即可：

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /out:屏幕常亮工具.exe /win32icon:屏幕常亮工具.ico /r:System.Windows.Forms.dll /r:System.Drawing.dll ProgramLegacy.cs
```
