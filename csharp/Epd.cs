// Safe wrapper around the Waveshare EPD 2.13" V4 C library.
// The image buffer is pinned for the lifetime of this object because the
// C library stores the pointer from Paint_NewImage and uses it internally.

using System.Runtime.InteropServices;

public sealed class Epd : IDisposable
{
    const int Width   = 122;
    const int Height  = 250;
    const int BufSize = ((Width + 7) / 8) * Height; // 4000 bytes

    private readonly byte[]   _buf;
    private readonly GCHandle _pin;
    private readonly IntPtr   _ptr;
    private bool _disposed;

    public Epd()
    {
        _buf = new byte[BufSize];
        _pin = GCHandle.Alloc(_buf, GCHandleType.Pinned);
        _ptr = _pin.AddrOfPinnedObject();

        if (Ffi.DEV_Module_Init() != 0)
        {
            _pin.Free();
            throw new Exception("DEV_Module_Init failed");
        }

        Ffi.EPD_2in13_V4_Init();
        Ffi.Paint_NewImage(_ptr, Width, Height, Ffi.ROTATE_270, Ffi.WHITE);
    }

    public void Fill(ushort color) => Ffi.Paint_Clear(color);

    public void DrawText(ushort x, ushort y, string text, Font font, ushort fg, ushort bg)
        => Ffi.Paint_DrawString_EN(x, y, text, font.Ptr(), fg, bg);

    public void DrawRect(ushort x0, ushort y0, ushort x1, ushort y1, ushort color, bool filled)
        => Ffi.Paint_DrawRectangle(x0, y0, x1, y1, color, 1, filled ? 1u : 0u);

    public void DrawLine(ushort x0, ushort y0, ushort x1, ushort y1, ushort color)
        => Ffi.Paint_DrawLine(x0, y0, x1, y1, color, 1, 0);

    public void DrawIcon(ushort x, ushort y, ReadOnlySpan<byte> data, ushort width, ushort height)
    {
        int bytesPerRow = (width + 7) / 8;
        for (ushort row = 0; row < height; row++)
            for (ushort col = 0; col < width; col++)
            {
                int byteIdx = row * bytesPerRow + col / 8;
                int bit = 7 - (col % 8);
                if ((data[byteIdx] & (1 << bit)) != 0)
                    Ffi.Paint_DrawPoint((ushort)(x + col), (ushort)(y + row), Ffi.BLACK, 1, 1);
            }
    }

    public void ClearWindow(ushort x0, ushort y0, ushort x1, ushort y1, ushort color)
        => Ffi.Paint_ClearWindows(x0, y0, x1, y1, color);

    public void DisplayBase()    => Ffi.EPD_2in13_V4_Display_Base(_ptr);
    public void DisplayPartial() => Ffi.EPD_2in13_V4_Display_Partial(_ptr);
    public void Display()        => Ffi.EPD_2in13_V4_Display(_ptr);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Ffi.EPD_2in13_V4_Sleep();
        Ffi.DEV_Module_Exit();
        _pin.Free();
    }
}

public enum Font { F8, F12, F16, F20, F24 }

internal static class FontExt
{
    public static IntPtr Ptr(this Font f) => f switch
    {
        Font.F8  => Ffi.EPD_GetFont8(),
        Font.F12 => Ffi.EPD_GetFont12(),
        Font.F16 => Ffi.EPD_GetFont16(),
        Font.F20 => Ffi.EPD_GetFont20(),
        Font.F24 => Ffi.EPD_GetFont24(),
        _        => throw new ArgumentOutOfRangeException(nameof(f))
    };
}
