using static Ffi;

class Program {
    static void Main() {
        Console.WriteLine("Initialising display...");
        using var display = new Epd();

        display.Fill(WHITE);

        // draw bell icon
        display.DrawIcon(4, 4, Icons.Bell, Icons.BellW, Icons.BellH);

        // write Loading...
        display.DrawText(24, 6, "Loading...", Font.F16, BLACK, WHITE);

        // draw check icon
        display.DrawIcon(4, 30, Icons.Check, Icons.CheckW, Icons.CheckH);

        // write "System OK"
        display.DrawText(24, 34, "System OK", Font.F12, BLACK, WHITE);

        // draw a horizontal line
        display.DrawLine(0, 52, 249, 52, BLACK);

        // draw a rectangle around the edge of the screen
        display.DrawRect(2, 2, 248, 120, BLACK, filled: false);

        Console.WriteLine("Setting base frame...");
        display.DisplayBase();

        const ushort SensorX = 4, SensorY = 58, SensorW = 244, SensorH = 14;
        Dht11.Reading dht11Reading;
        float tempC, humidity;
        string line3;

        while (true)
        {
            try
            {
                dht11Reading = Dht11.Read();
                tempC = dht11Reading.TemperatureC;
                humidity = dht11Reading.Humidity;
                
                line3 = $"{tempC:F1}C {humidity:F0}% RH";
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"DHT11: {e.Message}");
                line3 = "-- sensor error --";
            }

            display.ClearWindow(24, 6, 24 + SensorW, 6 + 16, WHITE);
            display.DrawText(24, 6, string.Format("{0:HH:mm tt}", DateTime.Now), Font.F16, BLACK, WHITE);

            display.ClearWindow(SensorX, SensorY, SensorX + SensorW, SensorY + SensorH, WHITE);
            display.DrawText(SensorX, SensorY, line3, Font.F12, BLACK, WHITE);

            display.DisplayPartial();

            Console.WriteLine(line3);
            
            // sleep for 1 second
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }
    }
}
