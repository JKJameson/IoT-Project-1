// Raw P/Invoke bindings to the Waveshare C library (libepd.so).
// Use the Epd wrapper class instead of calling these directly.

using System.Runtime.InteropServices;

internal static class Ffi
{
    const string Lib = "epd";

    public const ushort WHITE = 0xFF;
    public const ushort BLACK = 0x00;
    public const ushort ROTATE_270 = 270;

    // ── DEV_Config ───────────────────────────────────────────────────────────

    [DllImport(Lib)] public static extern byte DEV_Module_Init();
    [DllImport(Lib)] public static extern void DEV_Module_Exit();
    [DllImport(Lib)] public static extern void DEV_Delay_ms(uint ms);

    // ── EPD_2in13_V4 ─────────────────────────────────────────────────────────

    [DllImport(Lib)] public static extern void EPD_2in13_V4_Init();
    [DllImport(Lib)] public static extern void EPD_2in13_V4_Init_Fast();
    [DllImport(Lib)] public static extern void EPD_2in13_V4_Clear();
    [DllImport(Lib)] public static extern void EPD_2in13_V4_Display(IntPtr image);
    [DllImport(Lib)] public static extern void EPD_2in13_V4_Display_Fast(IntPtr image);
    [DllImport(Lib)] public static extern void EPD_2in13_V4_Display_Base(IntPtr image);
    [DllImport(Lib)] public static extern void EPD_2in13_V4_Display_Partial(IntPtr image);
    [DllImport(Lib)] public static extern void EPD_2in13_V4_Sleep();

    // ── GUI_Paint ────────────────────────────────────────────────────────────

    [DllImport(Lib)] public static extern void Paint_NewImage(IntPtr image, ushort width, ushort height, ushort rotate, ushort color);
    [DllImport(Lib)] public static extern void Paint_Clear(ushort color);
    [DllImport(Lib)] public static extern void Paint_ClearWindows(ushort xstart, ushort ystart, ushort xend, ushort yend, ushort color);
    [DllImport(Lib)] public static extern void Paint_DrawPoint(ushort x, ushort y, ushort color, uint dotPixel, uint dotStyle);
    [DllImport(Lib)] public static extern void Paint_DrawLine(ushort xstart, ushort ystart, ushort xend, ushort yend, ushort color, uint lineWidth, uint lineStyle);
    [DllImport(Lib)] public static extern void Paint_DrawRectangle(ushort xstart, ushort ystart, ushort xend, ushort yend, ushort color, uint lineWidth, uint drawFill);
    [DllImport(Lib)] public static extern void Paint_DrawCircle(ushort xCenter, ushort yCenter, ushort radius, ushort color, uint lineWidth, uint drawFill);
    [DllImport(Lib, CharSet = CharSet.Ansi)]
    public static extern void Paint_DrawString_EN(ushort xstart, ushort ystart, string str, IntPtr font, ushort colorFg, ushort colorBg);

    // ── Font helpers (epd_helpers.c) ─────────────────────────────────────────

    [DllImport(Lib)] public static extern IntPtr EPD_GetFont8();
    [DllImport(Lib)] public static extern IntPtr EPD_GetFont12();
    [DllImport(Lib)] public static extern IntPtr EPD_GetFont16();
    [DllImport(Lib)] public static extern IntPtr EPD_GetFont20();
    [DllImport(Lib)] public static extern IntPtr EPD_GetFont24();
}
