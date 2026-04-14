using static Ffi;

class Program {
    static void Main() {
        Console.WriteLine("Initialising display...");
        using var display = new Epd();

        // test4

        display.Fill(WHITE);

        // draw bell icon
        display.DrawIcon(4, 4, Icons.Bell, Icons.BellW, Icons.BellH);

        // write Loading...
        display.DrawText(24, 6, "Loading...", Font.F16, BLACK, WHITE);

        // draw check icon
        display.DrawIcon(4, 30, Icons.Check, Icons.CheckW, Icons.CheckH);

        // write "System OK"
        display.DrawText(24, 34, "System OK - test4", Font.F12, BLACK, WHITE);

        // draw a horizontal line
        display.DrawLine(0, 52, 249, 52, BLACK);

        // draw a rectangle around the edge of the screen
        display.DrawRect(2, 2, 248, 120, BLACK, filled: false);

        Console.WriteLine("Setting base frame...");
        display.DisplayBase();

        const ushort line3X = 4, line3Y = 58, screenW = 244, screenH = 14;
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

            display.ClearWindow(24, 6, 24 + screenW, 6 + 16, WHITE);
            display.DrawText(24, 6, string.Format("{0:HH:mm tt}", DateTime.Now), Font.F16, BLACK, WHITE);

            display.ClearWindow(line3X, line3Y, line3X + screenW, line3Y + screenH, WHITE);
            display.DrawText(line3X, line3Y, line3, Font.F12, BLACK, WHITE);

            display.DisplayPartial();

            Console.WriteLine(line3);
            
            // sleep for 1 second
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }
    }
}
