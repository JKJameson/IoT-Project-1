// Reads temperature and humidity from the kernel DHT11 IIO driver.
// Requires: sudo dtoverlay dht11 gpiopin=27 (or in /boot/firmware/config.txt)

public static class Dht11
{
    const string IioBase = "/sys/bus/iio/devices/iio:device0";

    public record Reading(float TemperatureC, float Humidity)
    {
        public float TemperatureF => TemperatureC * 9f / 5f + 32f;
    }

    public static Reading Read()
    {
        var tempRaw = File.ReadAllText($"{IioBase}/in_temp_input").Trim();
        var humRaw  = File.ReadAllText($"{IioBase}/in_humidityrelative_input").Trim();
        return new Reading(float.Parse(tempRaw) / 1000f, float.Parse(humRaw) / 1000f);
    }
}
