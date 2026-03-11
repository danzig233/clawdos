using System.Runtime.InteropServices;
using System.Text;
using Clawdos.Models;
using Clawdos.Native;
namespace Clawdos.Services;
/// <summary>
/// A service for injecting input events (mouse clicks/moves/drags, keyboard shortcuts, text input).
/// </summary>
public sealed class InputInjectionService
{
    private readonly ScreenCaptureService _capture;
    // Global mutex name for clipboard operations to prevent conflicts with user/other processes
    private const string ClipboardMutexName = "Global\\ClawdosClipboardMutex";
    public InputInjectionService(ScreenCaptureService capture)
    {
        _capture = capture;
    }
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Coordinate Validation and Normalization
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    private static (int w, int h) GetScreenSize()
    {
        var w = User32.GetSystemMetrics(User32.SM_CXSCREEN);
        var h = User32.GetSystemMetrics(User32.SM_CYSCREEN);
        return (w, h);
    }
    private static void ValidateCoord(int x, int y)
    {
        var (w, h) = GetScreenSize();
        if (x < 0 || x >= w || y < 0 || y >= h)
            throw new ArgumentOutOfRangeException(
                $"Coordinate ({x},{y}) out of bounds [0,{w})x[0,{h})");
    }
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Coordinate Normalization for SendInput (0..65535)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    private static (int absX, int absY) NormalizeCoord(int x, int y)
    {
        var (w, h) = GetScreenSize();
        var absX = (int)((x * 65535.0) / (w - 1));
        var absY = (int)((y * 65535.0) / (h - 1));
        return (absX, absY);
    }
    private static uint MouseButtonDown(string button) => button.ToLower() switch
    {
        "right"  => User32.MOUSEEVENTF_RIGHTDOWN,
        "middle" => User32.MOUSEEVENTF_MIDDLEDOWN,
        _        => User32.MOUSEEVENTF_LEFTDOWN
    };
    private static uint MouseButtonUp(string button) => button.ToLower() switch
    {
        "right"  => User32.MOUSEEVENTF_RIGHTUP,
        "middle" => User32.MOUSEEVENTF_MIDDLEUP,
        _        => User32.MOUSEEVENTF_LEFTUP
    };
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Click
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public void Click(int x, int y, string button = "left", int count = 1)
    {
        ValidateCoord(x, y);
        var (absX, absY) = NormalizeCoord(x, y);
        var flags = User32.MOUSEEVENTF_MOVE | User32.MOUSEEVENTF_ABSOLUTE;
        var down  = MouseButtonDown(button);
        var up    = MouseButtonUp(button);
        for (int i = 0; i < Math.Max(count, 1); i++)
        {
            var inputs = new User32.INPUT[]
            {
                User32.CreateMouseInput(absX, absY, flags | down),
                User32.CreateMouseInput(absX, absY, flags | up)
            };
            User32.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<User32.INPUT>());
            if (i < count - 1) Thread.Sleep(50); // multiple clicks with short delay
        }
    }
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Move
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public void Move(int x, int y)
    {
        ValidateCoord(x, y);
        var (absX, absY) = NormalizeCoord(x, y);
        var input = new[] {
            User32.CreateMouseInput(absX, absY,
                User32.MOUSEEVENTF_MOVE | User32.MOUSEEVENTF_ABSOLUTE)
        };
        User32.SendInput(1, input, Marshal.SizeOf<User32.INPUT>());
    }
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Drag
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public void Drag(int fromX, int fromY, int toX, int toY,
        string button = "left", int durationMs = 300)
    {
        ValidateCoord(fromX, fromY);
        ValidateCoord(toX, toY);
        var down = MouseButtonDown(button);
        var up   = MouseButtonUp(button);
        var moveFlag = User32.MOUSEEVENTF_MOVE | User32.MOUSEEVENTF_ABSOLUTE;
        var size = Marshal.SizeOf<User32.INPUT>();
        // 1. move to start point
        var (ax, ay) = NormalizeCoord(fromX, fromY);
        User32.SendInput(1, new[] { User32.CreateMouseInput(ax, ay, moveFlag) }, size);
        Thread.Sleep(30);
        // 2. mouse down
        User32.SendInput(1, new[] { User32.CreateMouseInput(ax, ay, moveFlag | down) }, size);
        Thread.Sleep(30);
        // 3. smooth move to end point
        int steps = Math.Max(durationMs / 10, 5);
        for (int i = 1; i <= steps; i++)
        {
            double t = (double)i / steps;
            int cx = (int)(fromX + (toX - fromX) * t);
            int cy = (int)(fromY + (toY - fromY) * t);
            var (nx, ny) = NormalizeCoord(cx, cy);
            User32.SendInput(1, new[] { User32.CreateMouseInput(nx, ny, moveFlag) }, size);
            Thread.Sleep(10);
        }
        // 4. mouse up at end point
        var (ex, ey) = NormalizeCoord(toX, toY);
        User32.SendInput(1, new[] { User32.CreateMouseInput(ex, ey, moveFlag | up) }, size);
    }
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Scroll
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public void Scroll(int amount, int? x = null, int? y = null)
    {
        uint flags = 0;
        int absX = 0, absY = 0;
        if (x.HasValue && y.HasValue)
        {
            ValidateCoord(x.Value, y.Value);
            var norm = NormalizeCoord(x.Value, y.Value);
            absX = norm.absX;
            absY = norm.absY;
            flags = User32.MOUSEEVENTF_MOVE | User32.MOUSEEVENTF_ABSOLUTE;
        }

        var input = new[] {
            User32.CreateMouseWheelInput(amount * 120, absX, absY, flags) // Standard wheel click is 120
        };
        User32.SendInput(1, input, Marshal.SizeOf<User32.INPUT>());
    }
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Keys (keyboard shortcuts, e.g., Ctrl+S)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public void Keys(string[] combo)
    {
        if (combo.Length == 0) return;
        var vks = combo.Select(MapVirtualKey).ToArray();
        var inputs = new List<User32.INPUT>();
        // Key down in order
        foreach (var vk in vks)
            inputs.Add(User32.CreateKeyInput(vk, false));
        // Key up in reverse order
        foreach (var vk in vks.Reverse())
            inputs.Add(User32.CreateKeyInput(vk, true));
        var arr = inputs.ToArray();
        User32.SendInput((uint)arr.Length, arr, Marshal.SizeOf<User32.INPUT>());
    }
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Type (text input, with optional clipboard fallback for non-ASCII)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public void TypeText(string text, bool useClipboard = false)
    {
        if (string.IsNullOrEmpty(text)) return;
        bool hasNonAscii = text.Any(c => c > 127);
        if (useClipboard || hasNonAscii)
        {
            TypeViaClipboard(text);
        }
        else
        {
            // ASCII characters can be sent directly, but we switch to English IME to avoid issues with active IMEs intercepting input
            SwitchToEnglishIme();
            TypeViaSendInput(text);
        }
    }
    /// <summary>Sending input directly via SendInput</summary>
    private void TypeViaSendInput(string text)
    {
        var inputs = new List<User32.INPUT>();
        foreach (char c in text)
        {
            // KEYEVENTF_UNICODE is required for non-ASCII characters
            inputs.Add(User32.CreateUnicodeKeyInput(c, false));
            inputs.Add(User32.CreateUnicodeKeyInput(c, true));
        }
        var arr = inputs.ToArray();
        User32.SendInput((uint)arr.Length, arr, Marshal.SizeOf<User32.INPUT>());
    }
    /// <summary>Injecting input via clipboard (for non-ASCII characters)</summary>
    private void TypeViaClipboard(string text)
    {
        // Use a global mutex to prevent conflicts with other instances or user actions on the clipboard
        using var mutex = new Mutex(false, ClipboardMutexName);
        try
        {
            mutex.WaitOne(5000);
        }
        catch (AbandonedMutexException) { /* last owner crashed, continue */ }
        try
        {
            // Read current clipboard text to restore later
            string? originalText = null;
            bool hadText = false;
            if (User32.OpenClipboard(IntPtr.Zero))
            {
                var hData = User32.GetClipboardData(User32.CF_UNICODETEXT);
                if (hData != IntPtr.Zero)
                {
                    var ptr = Kernel32.GlobalLock(hData);
                    if (ptr != IntPtr.Zero)
                    {
                        originalText = Marshal.PtrToStringUni(ptr);
                        hadText = true;
                        Kernel32.GlobalUnlock(hData);
                    }
                }
                User32.CloseClipboard();
            }
            // Set our text to clipboard
            if (User32.OpenClipboard(IntPtr.Zero))
            {
                User32.EmptyClipboard();
                var hGlobal = Marshal.StringToHGlobalUni(text);
                User32.SetClipboardData(User32.CF_UNICODETEXT, hGlobal);
                User32.CloseClipboard();
            }
            // Send Ctrl+V to paste
            Thread.Sleep(30);
            Keys(new[] { "CTRL", "V" });
            Thread.Sleep(100); // wait for paste to complete
            // Restore original clipboard content
            if (hadText && originalText != null)
            {
                if (User32.OpenClipboard(IntPtr.Zero))
                {
                    User32.EmptyClipboard();
                    var hGlobal = Marshal.StringToHGlobalUni(originalText);
                    User32.SetClipboardData(User32.CF_UNICODETEXT, hGlobal);
                    User32.CloseClipboard();
                }
            }
            else if (!hadText)
            {
                // If clipboard was originally empty, clear it to avoid leaving our text there
                if (User32.OpenClipboard(IntPtr.Zero))
                {
                    User32.EmptyClipboard();
                    User32.CloseClipboard();
                }
            }
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }
    /// <summary>Switches the input method to English (to avoid IME interception of ASCII input)</summary>
    private static void SwitchToEnglishIme()
    {
        // Switch to English IME (0x0409) to ensure ASCII input is not blocked by active IMEs.
        var hkl = User32.LoadKeyboardLayout("00000409",
            User32.KLF_ACTIVATE | User32.KLF_SETFORPROCESS);
        if (hkl != IntPtr.Zero)
        {
            var fg = User32.GetForegroundWindow();
            if (fg != IntPtr.Zero)
            {
                User32.PostMessage(fg, User32.WM_INPUTLANGCHANGEREQUEST,
                    IntPtr.Zero, hkl);
                Thread.Sleep(50);
            }
        }
    }
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Batch Action Execution
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public (int executedCount, int? failedAtIndex, string? error) ExecuteBatch(
        BatchAction[] actions)
    {
        for (int i = 0; i < actions.Length; i++)
        {
            var a = actions[i];
            try
            {
                switch (a.Type.ToLower())
                {
                    case "click":
                        Click(a.X ?? 0, a.Y ?? 0, a.Button ?? "left", a.Count ?? 1);
                        break;
                    case "move":
                        Move(a.X ?? 0, a.Y ?? 0);
                        break;
                    case "drag":
                        Drag(a.FromX ?? 0, a.FromY ?? 0,
                             a.ToX ?? 0, a.ToY ?? 0,
                             a.Button ?? "left", a.DurationMs ?? 300);
                        break;
                    case "scroll":
                        Scroll(a.Amount ?? 0, a.X, a.Y);
                        break;
                    case "keys":
                        Keys(a.Combo ?? Array.Empty<string>());
                        break;
                    case "type":
                        TypeText(a.Text ?? "", a.UseClipboard ?? false);
                        break;
                    case "wait":
                        Thread.Sleep(a.Ms ?? 100);
                        break;
                    default:
                        return (i, i, $"Unknown action type: {a.Type}");
                }
            }
            catch (Exception ex)
            {
                return (i, i, ex.Message);
            }
        }
        return (actions.Length, null, null);
    }
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // captureAfterMs 
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public AfterCaptureResult? CaptureAfter(int? delayMs, string format = "jpg",
        int quality = 80)
    {
        if (delayMs is null or <= 0) return null;
        Thread.Sleep(delayMs.Value);
        var bytes = _capture.Capture(format, quality);
        if (bytes is null) return null;
        return new AfterCaptureResult(format, Convert.ToBase64String(bytes));
    }
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Virtual Key Mapping
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    private static ushort MapVirtualKey(string name) => name.ToUpper() switch
    {
        "CTRL" or "CONTROL" or "LCTRL"  => 0xA2, // VK_LCONTROL
        "RCTRL" or "RCONTROL"           => 0xA3, // VK_RCONTROL
        "ALT" or "LALT" or "MENU"       => 0xA4, // VK_LMENU
        "RALT"                          => 0xA5, // VK_RMENU
        "SHIFT" or "LSHIFT"             => 0xA0, // VK_LSHIFT
        "RSHIFT"                        => 0xA1, // VK_RSHIFT
        "WIN" or "LWIN"                 => 0x5B, // VK_LWIN
        "RWIN"                          => 0x5C, // VK_RWIN
        "ENTER" or "RETURN"             => 0x0D, // VK_RETURN
        "TAB"                           => 0x09,
        "ESCAPE" or "ESC"               => 0x1B,
        "SPACE"                         => 0x20,
        "BACKSPACE" or "BACK"           => 0x08,
        "DELETE" or "DEL"               => 0x2E,
        "INSERT" or "INS"               => 0x2D,
        "HOME"                          => 0x24,
        "END"                           => 0x23,
        "PAGEUP" or "PGUP"              => 0x21,
        "PAGEDOWN" or "PGDN"            => 0x22,
        "UP"                            => 0x26,
        "DOWN"                          => 0x28,
        "LEFT"                          => 0x25,
        "RIGHT"                         => 0x27,
        "CAPSLOCK"                      => 0x14,
        "NUMLOCK"                       => 0x90,
        "SCROLLLOCK"                    => 0x91,
        "PRINTSCREEN" or "PRTSC"        => 0x2C,
        "F1"  => 0x70, "F2"  => 0x71, "F3"  => 0x72, "F4"  => 0x73,
        "F5"  => 0x74, "F6"  => 0x75, "F7"  => 0x76, "F8"  => 0x77,
        "F9"  => 0x78, "F10" => 0x79, "F11" => 0x7A, "F12" => 0x7B,
        // A-Z, 0-9
        var s when s.Length == 1 && char.IsAsciiLetterOrDigit(s[0])
            => (ushort)char.ToUpper(s[0]),
        _ => throw new ArgumentException($"Unknown key name: {name}")
    };
}