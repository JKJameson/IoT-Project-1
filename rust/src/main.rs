mod ffi;
mod epd;
mod icons;

use epd::{Epd, Font, BLACK, WHITE, DrawFill};

fn main() {
    println!("Initialising display...");
    let mut display = Epd::new().expect("Failed to initialise EPD");

    display.fill(WHITE);

    // Draw a bell icon at top-left, then label it
    display.draw_icon(4, 4, &icons::BELL, icons::BELL_W, icons::BELL_H);
    display.draw_text(24, 6, "Notifications", Font::F16, BLACK, WHITE);

    // Draw a checkmark icon next to a status line
    display.draw_icon(4, 30, &icons::CHECK, icons::CHECK_W, icons::CHECK_H);
    display.draw_text(24, 34, "System OK", Font::F12, BLACK, WHITE);

    // Divider
    display.draw_line(0, 52, 249, 52, BLACK);

    display.draw_text(4, 58, "Rust + lgpio", Font::F12, BLACK, WHITE);
    display.draw_rect(2, 2, 248, 120, BLACK, DrawFill::Empty);

    println!("Displaying...");
    display.display();

    display.delay_ms(2000);

    println!("Sleeping display.");
    // Drop calls sleep() + DEV_Module_Exit() automatically
}
