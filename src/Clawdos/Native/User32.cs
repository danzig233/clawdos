using System.Runtime.InteropServices;
using System.Text;
namespace Clawdos.Native;
/// <summary>
/// user32.dll / shell32.dll / imm32.dll P/Invoke signatures and related constants/structs.
/// </summary>
public static class User32
{
    // ━━ Constants ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public const int SM_CXSCREEN = 0;
    public const int SM_CYSCREEN = 1;
    public const int SW_RESTORE  = 9;
    // Mouse event flags
    public const uint MOUSEEVENTF_MOVE       = 0x0001;
    public const uint MOUSEEVENTF_LEFTDOWN   = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP     = 0x0004;
    public const uint MOUSEEVENTF_RIGHTDOWN  = 0x0008;
    public const uint MOUSEEVENTF_RIGHTUP    = 0x0010;
    public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    public const uint MOUSEEVENTF_MIDDLEUP   = 0x0040;
    public const uint MOUSEEVENTF_ABSOLUTE   = 0x8000;
    // Keyboard event flags
    public const uint KEYEVENTF_KEYUP   = 0x0002;
    public const uint KEYEVENTF_UNICODE = 0x0004;
    // INPUT type
    public const int INPUT_MOUSE    = 0;
    public const int INPUT_KEYBOARD = 1;
    // Clipboard
    public const uint CF_UNICODETEXT = 13;
    // Keyboard layout
    public const uint KLF_ACTIVATE      = 0x00000001;
    public const uint KLF_SETFORPROCESS = 0x00000100;
    // Window messages
    public const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;
    // ━━ Structures ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public int type;
        public InputUnion u;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int    dx;
        public int    dy;
        public uint   mouseData;
        public uint   dwFlags;
        public uint   time;
        public IntPtr dwExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint   dwFlags;
        public uint   time;
        public IntPtr dwExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct APPBARDATA
    {
        public uint   cbSize;
        public IntPtr hWnd;
        public uint   uCallbackMessage;
        public uint   uEdge;
        public RECT   rc;
        public int    lParam;
    }
    // ━━ Factory Methods ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public static INPUT CreateMouseInput(int dx, int dy, uint flags) => new()
    {
        type = INPUT_MOUSE,
        u = new InputUnion
        {
            mi = new MOUSEINPUT
            {
                dx = dx, dy = dy, dwFlags = flags,
                mouseData = 0, time = 0, dwExtraInfo = IntPtr.Zero
            }
        }
    };
    public static INPUT CreateKeyInput(ushort vk, bool isKeyUp) => new()
    {
        type = INPUT_KEYBOARD,
        u = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = vk, wScan = 0,
                dwFlags = isKeyUp ? KEYEVENTF_KEYUP : 0,
                time = 0, dwExtraInfo = IntPtr.Zero
            }
        }
    };
    public static INPUT CreateUnicodeKeyInput(char c, bool isKeyUp) => new()
    {
        type = INPUT_KEYBOARD,
        u = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = 0, wScan = (ushort)c,
                dwFlags = KEYEVENTF_UNICODE | (isKeyUp ? KEYEVENTF_KEYUP : 0),
                time = 0, dwExtraInfo = IntPtr.Zero
            }
        }
    };
    // ━━ Helper Methods ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public static string GetWindowTextString(IntPtr hWnd)
    {
        int len = GetWindowTextLength(hWnd);
        if (len <= 0) return string.Empty;
        var sb = new StringBuilder(len + 1);
        GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }
    // ━━ P/Invoke ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    [DllImport("user32.dll")]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);
    [DllImport("user32.dll")]
    public static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")]
    public static extern int GetWindowTextLength(IntPtr hWnd);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    // Clipboard
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool OpenClipboard(IntPtr hWndNewOwner);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseClipboard();
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EmptyClipboard();
    [DllImport("user32.dll")]
    public static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
    [DllImport("user32.dll")]
    public static extern IntPtr GetClipboardData(uint uFormat);
    // Keyboard layout
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint flags);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    // IME (imm32.dll)
    [DllImport("imm32.dll")]
    public static extern IntPtr ImmGetContext(IntPtr hWnd);
    [DllImport("imm32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ImmGetOpenStatus(IntPtr hIMC);
    [DllImport("imm32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);
    // Shell (shell32.dll)
    [DllImport("shell32.dll")]
    public static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);
}