using System.Runtime.InteropServices;
namespace Clawdos.Native;
public static class Kernel32
{
    [DllImport("kernel32.dll")]
    public static extern IntPtr GlobalLock(IntPtr hMem);
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GlobalUnlock(IntPtr hMem);
    [DllImport("kernel32.dll")]
    public static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);
    [DllImport("kernel32.dll")]
    public static extern IntPtr GlobalFree(IntPtr hMem);
    public const uint GMEM_MOVEABLE = 0x0002;
}