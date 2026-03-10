using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Clawdos.Models;
using Clawdos.Native;

namespace Clawdos.Services;

/// <summary>
/// Collects information about the current Windows desktop environment: resolution, DPI, taskbar position, active window, and IME status.
/// </summary>
public sealed class EnvironmentService
{
    /// <summary>Gets the current environment snapshot.</summary>
    public EnvResponse GetEnv()
    {
        // Resolution
        var screenW = User32.GetSystemMetrics(User32.SM_CXSCREEN);
        var screenH = User32.GetSystemMetrics(User32.SM_CYSCREEN);

        // DPI Scale
        var hdc = User32.GetDC(IntPtr.Zero);
        var dpiX = Gdi32.GetDeviceCaps(hdc, Gdi32.LOGPIXELSX);
        User32.ReleaseDC(IntPtr.Zero, hdc);
        var dpiScale = Math.Round(dpiX / 96.0, 2);

        // Taskbar Position
        var taskbarPos = GetTaskbarPosition();

        // Active Window
        string? activeTitle = null;
        string? activeProc  = null;
        var fg = User32.GetForegroundWindow();
        if (fg != IntPtr.Zero)
        {
            activeTitle = User32.GetWindowTextString(fg);
            User32.GetWindowThreadProcessId(fg, out var pid);
            try { activeProc = Process.GetProcessById((int)pid).ProcessName; }
            catch { /* Process may have exited */ }
        }

        // IME Status
        var imeEnabled = IsImeActive(fg);

        return new EnvResponse(
            ScreenWidth:       screenW,
            ScreenHeight:      screenH,
            DpiScale:          dpiScale,
            TaskbarPosition:   taskbarPos,
            ActiveWindowTitle: activeTitle,
            ActiveProcessName: activeProc,
            ImeEnabled:        imeEnabled);
    }

    // ── Internal Helper Methods ─────────────────────────────────────────

    private static string? GetTaskbarPosition()
    {
        var data = new User32.APPBARDATA { cbSize = (uint)Marshal.SizeOf<User32.APPBARDATA>() };
        User32.SHAppBarMessage(0x00000005 /* ABM_GETTASKBARPOS */, ref data);
        return data.uEdge switch
        {
            0 => "left",
            1 => "top",
            2 => "right",
            3 => "bottom",
            _ => null
        };
    }

    private static bool IsImeActive(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;
        var hImc = User32.ImmGetContext(hwnd);
        if (hImc == IntPtr.Zero) return false;
        var open = User32.ImmGetOpenStatus(hImc);
        User32.ImmReleaseContext(hwnd, hImc);
        return open;
    }
}
