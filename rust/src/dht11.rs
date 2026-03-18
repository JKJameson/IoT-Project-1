use std::fs;

const IIO_BASE: &str = "/sys/bus/iio/devices/iio:device0";

pub struct Reading {
    pub temperature_c: f32,
    pub humidity: f32,
}

impl Reading {
    pub fn temperature_f(&self) -> f32 {
        self.temperature_c * 9.0 / 5.0 + 32.0
    }
}

pub fn read() -> Result<Reading, String> {
    let temp_raw = fs::read_to_string(format!("{}/in_temp_input", IIO_BASE))
        .map_err(|e| format!("DHT11 temp read failed: {e} (is dtoverlay=dht11 enabled?)"))?;
    let hum_raw = fs::read_to_string(format!("{}/in_humidityrelative_input", IIO_BASE))
        .map_err(|e| format!("DHT11 humidity read failed: {e}"))?;

    let temperature_c = temp_raw.trim().parse::<f32>().map_err(|e| format!("DHT11 temp parse: {e}"))? / 1000.0;
    let humidity = hum_raw.trim().parse::<f32>().map_err(|e| format!("DHT11 humidity parse: {e}"))? / 1000.0;

    Ok(Reading { temperature_c, humidity })
}
