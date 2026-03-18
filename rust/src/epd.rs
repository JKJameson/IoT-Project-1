/// Safe wrapper around the Waveshare EPD 2.13" V4 C library.
use std::ffi::CString;
use crate::ffi;

pub use ffi::{EPD_WIDTH, EPD_HEIGHT, EPD_BUF_SIZE, WHITE, BLACK};
pub use ffi::{DotPixel, DrawFill, LineStyle};
use ffi::{ROTATE_270, DotStyle};

/// Owns the display lifecycle. Calls DEV_Module_Exit on drop.
pub struct Epd {
    buf: Box<[u8; EPD_BUF_SIZE]>,
}

impl Epd {
    /// Initialise the hardware and return an Epd instance.
    /// Returns Err if DEV_Module_Init fails.
    pub fn new() -> Result<Self, &'static str> {
        let rc = unsafe { ffi::DEV_Module_Init() };
        if rc != 0 {
            return Err("DEV_Module_Init failed");
        }
        unsafe { ffi::EPD_2in13_V4_Init() };

        let mut epd = Self {
            buf: Box::new([0xFF; EPD_BUF_SIZE]),
        };

        unsafe {
            ffi::Paint_NewImage(
                epd.buf.as_mut_ptr(),
                EPD_WIDTH,
                EPD_HEIGHT,
                ROTATE_270, // landscape: 250 wide, 122 tall
                WHITE,
            );
        }

        Ok(epd)
    }

    /// Clear the display to white.
    pub fn clear(&mut self) {
        unsafe {
            ffi::EPD_2in13_V4_Clear();
            ffi::Paint_Clear(WHITE);
        }
    }

    /// Fill the image buffer with a solid color without touching the hardware.
    pub fn fill(&mut self, color: u16) {
        unsafe { ffi::Paint_Clear(color) };
    }

    /// Draw a string using an ASCII font.
    pub fn draw_text(&mut self, x: u16, y: u16, text: &str, font: Font, fg: u16, bg: u16) {
        let s = CString::new(text).unwrap();
        let f = font.ptr();
        unsafe { ffi::Paint_DrawString_EN(x, y, s.as_ptr(), f, fg, bg) };
    }

    /// Draw a filled or outlined rectangle.
    pub fn draw_rect(&mut self, x0: u16, y0: u16, x1: u16, y1: u16, color: u16, fill: DrawFill) {
        unsafe {
            ffi::Paint_DrawRectangle(x0, y0, x1, y1, color, DotPixel::Px1x1, fill);
        }
    }

    /// Draw a circle.
    pub fn draw_circle(&mut self, cx: u16, cy: u16, r: u16, color: u16, fill: DrawFill) {
        unsafe {
            ffi::Paint_DrawCircle(cx, cy, r, color, DotPixel::Px1x1, fill);
        }
    }

    /// Draw a line.
    pub fn draw_line(&mut self, x0: u16, y0: u16, x1: u16, y1: u16, color: u16) {
        unsafe {
            ffi::Paint_DrawLine(x0, y0, x1, y1, color, DotPixel::Px1x1, LineStyle::Solid);
        }
    }

    /// Draw a 1bpp icon at (x, y).
    ///
    /// `data` is a packed byte array, MSB = leftmost pixel, 1 = black, 0 = white.
    /// Each row is `ceil(width / 8)` bytes. Use the helpers in `icons.rs` to define icons,
    /// or generate them with `tools/png_to_icon.py`.
    pub fn draw_icon(&mut self, x: u16, y: u16, data: &[u8], width: u16, height: u16) {
        let bytes_per_row = ((width as usize) + 7) / 8;
        for row in 0..height {
            for col in 0..width {
                let byte_idx = row as usize * bytes_per_row + col as usize / 8;
                let bit = 7 - (col % 8);
                if data[byte_idx] & (1 << bit) != 0 {
                    unsafe {
                        ffi::Paint_DrawPoint(
                            x + col, y + row,
                            BLACK,
                            DotPixel::Px1x1,
                            DotStyle::FillAround,
                        );
                    }
                }
            }
        }
    }

    /// Push the image buffer to the display (full refresh).
    pub fn display(&mut self) {
        unsafe { ffi::EPD_2in13_V4_Display(self.buf.as_mut_ptr()) };
    }

    /// Push the image buffer using fast (partial) refresh — less flicker.
    pub fn display_fast(&mut self) {
        unsafe {
            ffi::EPD_2in13_V4_Init_Fast();
            ffi::EPD_2in13_V4_Display_Fast(self.buf.as_mut_ptr());
        }
    }

    /// Set the base image for partial updates. Call once after the initial full draw.
    pub fn display_base(&mut self) {
        unsafe { ffi::EPD_2in13_V4_Display_Base(self.buf.as_mut_ptr()) };
    }

/// Push only changed pixels to the display. Fast, no full-screen flicker.
    pub fn display_partial(&mut self) {
        unsafe { ffi::EPD_2in13_V4_Display_Partial(self.buf.as_mut_ptr()) };
    }

    /// Clear a rectangular region of the image buffer to a solid color.
    pub fn clear_window(&mut self, x0: u16, y0: u16, x1: u16, y1: u16, color: u16) {
        unsafe { ffi::Paint_ClearWindows(x0, y0, x1, y1, color) };
    }

    /// Put the display into deep sleep. Always call this before power-off.
    pub fn sleep(&self) {
        unsafe { ffi::EPD_2in13_V4_Sleep() };
    }

    pub fn delay_ms(&self, ms: u32) {
        unsafe { ffi::DEV_Delay_ms(ms) };
    }
}

impl Drop for Epd {
    fn drop(&mut self) {
        self.sleep();
        unsafe { ffi::DEV_Module_Exit() };
    }
}

/// Available font sizes.
#[derive(Clone, Copy)]
pub enum Font {
    F8,
    F12,
    F16,
    F20,
    F24,
}

impl Font {
    fn ptr(self) -> *const ffi::SFont {
        unsafe {
            match self {
                Font::F8  => &ffi::Font8,
                Font::F12 => &ffi::Font12,
                Font::F16 => &ffi::Font16,
                Font::F20 => &ffi::Font20,
                Font::F24 => &ffi::Font24,
            }
        }
    }
}
