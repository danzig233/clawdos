using System.Runtime.InteropServices;
namespace Clawdos.Native;
public static class Gdi32
{
    public const int LOGPIXELSX = 88;
    public const int LOGPIXELSY = 90;
    [DllImport("gdi32.dll")]
    public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
}