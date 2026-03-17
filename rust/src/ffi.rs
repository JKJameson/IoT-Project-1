/// Raw FFI bindings to the Waveshare C library.
/// All functions here are unsafe — use the wrappers in epd.rs instead.
use std::ffi::c_char;

// ── Types matching DEV_Config.h ──────────────────────────────────────────────

pub type UByte = u8;
pub type UWord = u16;

// ── Font structs (fonts.h) ───────────────────────────────────────────────────

#[repr(C)]
pub struct SFont {
    pub table: *const u8,
    pub width: u16,
    pub height: u16,
}

extern "C" {
    pub static Font8: SFont;
    pub static Font12: SFont;
    pub static Font16: SFont;
    pub static Font20: SFont;
    pub static Font24: SFont;
}

// ── DEV_Config.h ─────────────────────────────────────────────────────────────

extern "C" {
    pub fn DEV_Module_Init() -> UByte;
    pub fn DEV_Module_Exit();
    pub fn DEV_Delay_ms(xms: u32);
}

// ── EPD_2in13_V4.h ───────────────────────────────────────────────────────────

// Display is 122 x 250 px. Buffer size = ceil(122/8) * 250 = 16 * 250 = 4000 bytes.
pub const EPD_WIDTH: u16 = 122;
pub const EPD_HEIGHT: u16 = 250;
pub const EPD_BUF_SIZE: usize = ((EPD_WIDTH as usize + 7) / 8) * EPD_HEIGHT as usize;

extern "C" {
    pub fn EPD_2in13_V4_Init();
    pub fn EPD_2in13_V4_Init_Fast();
    pub fn EPD_2in13_V4_Init_GUI();
    pub fn EPD_2in13_V4_Clear();
    pub fn EPD_2in13_V4_Clear_Black();
    pub fn EPD_2in13_V4_Display(image: *mut UByte);
    pub fn EPD_2in13_V4_Display_Fast(image: *mut UByte);
    pub fn EPD_2in13_V4_Display_Base(image: *mut UByte);
    pub fn EPD_2in13_V4_Display_Partial(image: *mut UByte);
    pub fn EPD_2in13_V4_Sleep();
}

// ── GUI_Paint.h ───────────────────────────────────────────────────────────────

pub const WHITE: u16 = 0xFF;
pub const BLACK: u16 = 0x00;

pub const ROTATE_0: u16 = 0;
pub const ROTATE_90: u16 = 90;
pub const ROTATE_180: u16 = 180;
pub const ROTATE_270: u16 = 270;

#[repr(u32)]
#[allow(dead_code)]
pub enum DotPixel {
    Px1x1 = 1,
    Px2x2 = 2,
    Px3x3 = 3,
    Px4x4 = 4,
}

#[repr(u32)]
#[allow(dead_code)]
pub enum DotStyle {
    FillAround = 1,
    FillRightUp = 2,
}

#[repr(u32)]
#[allow(dead_code)]
pub enum LineStyle {
    Solid = 0,
    Dotted = 1,
}

#[repr(u32)]
#[allow(dead_code)]
pub enum DrawFill {
    Empty = 0,
    Full = 1,
}

extern "C" {
    pub fn Paint_NewImage(image: *mut UByte, width: UWord, height: UWord, rotate: UWord, color: UWord);
    pub fn Paint_SelectImage(image: *mut UByte);
    pub fn Paint_SetRotate(rotate: UWord);
    pub fn Paint_SetMirroring(mirror: UByte);
    pub fn Paint_Clear(color: UWord);
    pub fn Paint_ClearWindows(xstart: UWord, ystart: UWord, xend: UWord, yend: UWord, color: UWord);
    pub fn Paint_DrawPoint(x: UWord, y: UWord, color: UWord, dot_pixel: DotPixel, dot_style: DotStyle);
    pub fn Paint_DrawLine(xstart: UWord, ystart: UWord, xend: UWord, yend: UWord, color: UWord, line_width: DotPixel, line_style: LineStyle);
    pub fn Paint_DrawRectangle(xstart: UWord, ystart: UWord, xend: UWord, yend: UWord, color: UWord, line_width: DotPixel, draw_fill: DrawFill);
    pub fn Paint_DrawCircle(x_center: UWord, y_center: UWord, radius: UWord, color: UWord, line_width: DotPixel, draw_fill: DrawFill);
    pub fn Paint_DrawString_EN(xstart: UWord, ystart: UWord, string: *const c_char, font: *const SFont, color_fg: UWord, color_bg: UWord);
    pub fn Paint_DrawNum(x: UWord, y: UWord, number: i32, font: *const SFont, color_fg: UWord, color_bg: UWord);
    pub fn Paint_DrawBitMap(image_buffer: *const u8);
}
