mod dht11;
mod epd;
mod ffi;
mod icons;

use epd::{DrawFill, Epd, Font, BLACK, WHITE};

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
    display.draw_rect(2, 2, 248, 120, BLACK, DrawFill::Empty);

    // Full refresh to set the base frame, then switch to partial mode
    println!("Setting base frame...");
    display.display_base();

    // Sensor update loop — only the sensor region refreshes each iteration
    const SENSOR_X: u16 = 4;
    const SENSOR_Y: u16 = 58;
    const SENSOR_W: u16 = 244;
    const SENSOR_H: u16 = 14; // Font::F12 is 12px tall + 2px margin

    loop {
        let line = match dht11::read() {
            Ok(r) => format!("{:.1}C  {:.0}% RH", r.temperature_c, r.humidity),
            Err(e) => {
                eprintln!("DHT11: {e}");
                "-- sensor error --".to_string()
            }
        };

        display.clear_window(SENSOR_X, SENSOR_Y, SENSOR_X + SENSOR_W, SENSOR_Y + SENSOR_H, WHITE);
        display.draw_text(SENSOR_X, SENSOR_Y, &line, Font::F12, BLACK, WHITE);
        display.display_partial();

        println!("{line}");
        std::thread::sleep(std::time::Duration::from_secs(5));
    }
}
