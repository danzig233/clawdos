using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Clawdos.Models;
using Clawdos.Native;
namespace Clawdos.Services;
/// <summary>
/// Window management service: List visible windows and bring target windows to front.
/// </summary>
public sealed class WindowManagementService
{
    // ── Window List ────────────────────────────────────────
    public WindowListResponse ListWindows()
    {
        var fg = User32.GetForegroundWindow();
        var result = new List<WindowInfo>();
        User32.EnumWindows((hwnd, _) =>
        {
            // Collect only visible windows
            if (!User32.IsWindowVisible(hwnd)) return true;
            // Filter windows without titles
            var title = User32.GetWindowTextString(hwnd);
            if (string.IsNullOrWhiteSpace(title)) return true;
            // Filter minimized/invisible windows
            User32.GetWindowRect(hwnd, out var rect);
            int w = rect.Right - rect.Left;
            int h = rect.Bottom - rect.Top;
            if (w <= 0 || h <= 0) return true;
            // Get process name
            string procName = "unknown";
            User32.GetWindowThreadProcessId(hwnd, out var pid);
            try { procName = Process.GetProcessById((int)pid).ProcessName; }
            catch { /* Process may have exited */ }
            result.Add(new WindowInfo(
                Hwnd:        $"0x{hwnd:X8}",
                Title:       title,
                ProcessName: procName,
                Rect:        new WindowRect(rect.Left, rect.Top, w, h),
                IsActive:    hwnd == fg));
            return true; // Continue enumeration
        }, IntPtr.Zero);
        return new WindowListResponse(result.ToArray());
    }
    // ── Focus Window ────────────────────────────────────────
    public bool FocusWindow(string? titleContains, string? processName)
    {
        IntPtr target = IntPtr.Zero;
        User32.EnumWindows((hwnd, _) =>
        {
            if (!User32.IsWindowVisible(hwnd)) return true;
            var title = User32.GetWindowTextString(hwnd);
            bool titleMatch = titleContains == null ||
                (title?.Contains(titleContains, StringComparison.OrdinalIgnoreCase) ?? false);
            bool procMatch = true;
            if (processName != null)
            {
                User32.GetWindowThreadProcessId(hwnd, out var pid);
                try
                {
                    var proc = Process.GetProcessById((int)pid);
                    procMatch = proc.ProcessName.Equals(
                        processName, StringComparison.OrdinalIgnoreCase);
                }
                catch { procMatch = false; }
            }
            if (titleMatch && procMatch)
            {
                target = hwnd;
                return false; // Found the first match, stop enumeration
            }
            return true;
        }, IntPtr.Zero);
        if (target == IntPtr.Zero)
            return false;
        // If the window is minimized, restore it first
        if (User32.IsIconic(target))
            User32.ShowWindow(target, User32.SW_RESTORE);
        // Bring to front
        User32.SetForegroundWindow(target);
        return true;
    }
}