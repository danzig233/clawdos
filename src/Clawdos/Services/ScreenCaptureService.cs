using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Clawdos.Native;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using MapFlags = Vortice.Direct3D11.MapFlags;
namespace Clawdos.Services;
/// <summary>
/// Screen capture service. Prioritizes DXGI Desktop Duplication, with GDI as a fallback.
/// </summary>
public sealed class ScreenCaptureService : IDisposable
{
    private readonly object _lock = new();
    // DXGI resources (lazily initialized)
    private ID3D11Device?          _device;
    private ID3D11DeviceContext?    _context;
    private IDXGIOutputDuplication? _duplication;
    private bool _dxgiInitialized;
    private bool _dxgiFailed;
    // ── Common Interface ─────────────────────────────────────────
    /// <summary>Capture the current desktop, returning a PNG or JPEG byte stream</summary>
    /// <param name="format">"png" or "jpg"</param>
    /// <param name="quality">JPEG quality 1-100, only effective for jpg</param>
    /// <returns>Image byte array, or null if all methods fail</returns>
    public byte[]? Capture(string format = "png", int quality = 80)
    {
        // Try DXGI
        var bitmap = TryCaptureDxgi();

        // Fallback GDI
        bitmap ??= TryCaptureGdi();

        if (bitmap is null)
            return null;

        using (bitmap)
        {
            DrawCursor(bitmap);
            return EncodeBitmap(bitmap, format, quality);
        }
    }

    private static void DrawCursor(Bitmap bitmap)
    {
        try
        {
            var cursorInfo = new User32.CURSORINFO { cbSize = Marshal.SizeOf<User32.CURSORINFO>() };
            if (User32.GetCursorInfo(ref cursorInfo) && (cursorInfo.flags & User32.CURSOR_SHOWING) == User32.CURSOR_SHOWING)
            {
                if (User32.GetIconInfo(cursorInfo.hCursor, out var iconInfo))
                {
                    try
                    {
                        using var icon = Icon.FromHandle(cursorInfo.hCursor);
                        using var g = Graphics.FromImage(bitmap);
                        g.DrawIcon(icon, cursorInfo.ptScreenPos.x - iconInfo.xHotspot, cursorInfo.ptScreenPos.y - iconInfo.yHotspot);
                    }
                    finally
                    {
                        if (iconInfo.hbmMask != IntPtr.Zero) Gdi32.DeleteObject(iconInfo.hbmMask);
                        if (iconInfo.hbmColor != IntPtr.Zero) Gdi32.DeleteObject(iconInfo.hbmColor);
                    }
                }
            }
        }
        catch
        {
            // Ignore cursor drawing errors
        }
    }

    // ── DXGI Desktop Duplication ─────────────────────────
    private Bitmap? TryCaptureDxgi()
    {
        lock (_lock)
        {
            try
            {
                EnsureDxgiInitialized();
                if (_dxgiFailed || _duplication is null || _device is null || _context is null)
                    return null;
                // Try to acquire the next frame (timeout 500ms)
                var hr = _duplication.AcquireNextFrame(500,
                    out var frameInfo, out var desktopResource);
                if (hr.Failure)
                {
                    // Reinitialize when timeout or access is lost
                    if (hr == Vortice.DXGI.ResultCode.WaitTimeout)
                        return null;
                    ResetDxgi();
                    return null;
                }
                using var srcTexture = desktopResource!.QueryInterface<ID3D11Texture2D>();
                var desc = srcTexture.Description;
                // Create a CPU-readable staging texture
                var stagingDesc = new Texture2DDescription
                {
                    Width             = desc.Width,
                    Height            = desc.Height,
                    MipLevels         = 1,
                    ArraySize         = 1,
                    Format            = desc.Format,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage             = ResourceUsage.Staging,
                    BindFlags         = BindFlags.None,
                    CPUAccessFlags    = CpuAccessFlags.Read,
                    MiscFlags         = ResourceOptionFlags.None
                };
                using var staging = _device.CreateTexture2D(stagingDesc);
                _context.CopyResource(staging, srcTexture);
                // Map to CPU memory
                var mapped = _context.Map(staging, 0, MapMode.Read, MapFlags.None);
                var bitmap = new Bitmap((int)desc.Width, (int)desc.Height,
                    PixelFormat.Format32bppArgb);
                var bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);
                // Copy row by row (mapped.RowPitch may differ from bmpData.Stride)
                for (int y = 0; y < bitmap.Height; y++)
                unsafe
                {
                    var src = mapped.DataPointer + y * mapped.RowPitch;
                    var dst = bmpData.Scan0 + y * bmpData.Stride;
                    Buffer.MemoryCopy(
                        (void*)src, (void*)dst,
                        bmpData.Stride, bitmap.Width * 4);
                }
                bitmap.UnlockBits(bmpData);
                _context.Unmap(staging, 0);
                desktopResource.Dispose();
                _duplication.ReleaseFrame();
                return bitmap;
            }
            catch
            {
                ResetDxgi();
                return null;
            }
        }
    }
    private void EnsureDxgiInitialized()
    {
        if (_dxgiInitialized) return;
        _dxgiInitialized = true;
        try
        {
            DXGI.CreateDXGIFactory1(out IDXGIFactory1? factory);
            if (factory is null) { _dxgiFailed = true; return; }
            using (factory)
            {
                factory.EnumAdapters1(0, out var adapter);
                if (adapter is null) { _dxgiFailed = true; return; }
                using (adapter)
                {
                    D3D11.D3D11CreateDevice(
                        adapter,
                        DriverType.Unknown,
                        DeviceCreationFlags.BgraSupport,
                        new[] { FeatureLevel.Level_11_0 },
                        out _device,
                        out _context);
                    if (_device is null) { _dxgiFailed = true; return; }
                    adapter.EnumOutputs(0, out var output);
                    if (output is null) { _dxgiFailed = true; return; }
                    using var output1 = output.QueryInterface<IDXGIOutput1>();
                    output.Dispose();
                    _duplication = output1.DuplicateOutput(_device);
                }
            }
        }
        catch
        {
            _dxgiFailed = true;
        }
    }
    private void ResetDxgi()
    {
        _duplication?.Dispose(); _duplication = null;
        _context?.Dispose();     _context = null;
        _device?.Dispose();      _device = null;
        _dxgiInitialized = false;
        _dxgiFailed = false;
    }
    // ── GDI Fallback ────────────────────────────────────
    private static Bitmap? TryCaptureGdi()
    {
        try
        {
            var w = User32.GetSystemMetrics(User32.SM_CXSCREEN);
            var h = User32.GetSystemMetrics(User32.SM_CYSCREEN);
            if (w <= 0 || h <= 0) return null;
            var bitmap = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(0, 0, 0, 0, new Size(w, h), CopyPixelOperation.SourceCopy);
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
    // ── Image Encoding ────────────────────────────────────────
    private static byte[] EncodeBitmap(Bitmap bmp, string format, int quality)
    {
        using var ms = new MemoryStream();
        if (format.Equals("jpg", StringComparison.OrdinalIgnoreCase) ||
            format.Equals("jpeg", StringComparison.OrdinalIgnoreCase))
        {
            var encoder = ImageCodecInfo.GetImageEncoders()
                .First(e => e.FormatID == ImageFormat.Jpeg.Guid);
            var encParams = new EncoderParameters(1)
            {
                Param = { [0] = new EncoderParameter(Encoder.Quality, Math.Clamp(quality, 1, 100)) }
            };
            bmp.Save(ms, encoder, encParams);
        }
        else
        {
            bmp.Save(ms, ImageFormat.Png);
        }
        return ms.ToArray();
    }
    public void Dispose()
    {
        lock (_lock) { ResetDxgi(); }
    }
}