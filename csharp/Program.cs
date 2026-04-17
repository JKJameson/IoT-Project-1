using static Ffi;

class Program {
    static void Main() {
        Console.WriteLine("Initialising display...");
        using var display = new Epd();
        var predictor = new RainPredictor();

        PressureSensor? pressureSensor = null;
        try
        {
            pressureSensor = new PressureSensor();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"BMP280 init failed: {e.Message}");
        }

        display.Fill(WHITE);

        display.DrawIcon(4, 4, Icons.Bell, Icons.BellW, Icons.BellH);
        display.DrawText(24, 6, "Loading...", Font.F16, BLACK, WHITE);

        display.DrawIcon(4, 30, Icons.Check, Icons.CheckW, Icons.CheckH);
        display.DrawText(24, 34, "System OK", Font.F12, BLACK, WHITE);

        display.DrawLine(0, 52, 249, 52, BLACK);

        Console.WriteLine("Setting base frame...");
        display.DisplayBase();

        const ushort line3X = 0, line3Y = 56;
        const ushort line4X = 0, line4Y = 72;
        const ushort line5X = 0, line5Y = 88;
        const ushort line6X = 0, line6Y = 104;
        const ushort screenW = 244, screenH = 14;
        Dht11.Reading dht11Reading;
        float tempC, humidity;
        string line3, line4, line5, line6;

        while (true)
        {
            try
            {
                dht11Reading = Dht11.Read();
                tempC = dht11Reading.TemperatureC;
                humidity = dht11Reading.Humidity;

                line3 = $"Temperature: {tempC}C";
                line4 = $"Humidity: {humidity}% RH";
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"DHT11: {e.Message}");
                tempC = 0f;
                humidity = 0f;
                line3 = "-- sensor error --";
                line4 = "-- sensor error --";
            }

            float? pressureHpa = null;
            try
            {
                if (pressureSensor != null)
                {
                    var r = pressureSensor.Read();
                    pressureHpa = r.PressureHpa;
                    line5 = $"Pressure: {r.PressureHpa:F1}hPa ({r.TemperatureC:F1}C)";
                    predictor.AddPressureSample(r.PressureHpa);
                }
                else
                {
                    line5 = "-- pressure unavailable --";
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"BMP280: {e.Message}");
                line5 = "-- pressure error --";
            }

            var prediction = predictor.Predict(tempC, humidity, pressureHpa);
            line6 = $"Rain: {prediction.Likelihood.Label()} (~{prediction.ConfidencePct}%)";

            display.ClearWindow(24, 6, 24 + screenW, 6 + 16, WHITE);
            display.DrawText(24, 6, string.Format("{0:HH:mm tt}", DateTime.Now), Font.F16, BLACK, WHITE);

            display.ClearWindow(line3X, line3Y, line3X + screenW, line3Y + screenH, WHITE);
            display.DrawText(line3X, line3Y, line3, Font.F12, BLACK, WHITE);
            display.ClearWindow(line4X, line4Y, line4X + screenW, line4Y + screenH, WHITE);
            display.DrawText(line4X, line4Y, line4, Font.F12, BLACK, WHITE);
            display.ClearWindow(line5X, line5Y, line5X + screenW, line5Y + screenH, WHITE);
            display.DrawText(line5X, line5Y, line5, Font.F12, BLACK, WHITE);
            display.ClearWindow(line6X, line6Y, line6X + screenW, line6Y + screenH, WHITE);
            display.DrawText(line6X, line6Y, line6, Font.F12, BLACK, WHITE);

            display.DisplayPartial();

            Console.WriteLine(line3);
            Console.WriteLine(line4);
            Console.WriteLine(line5);
            Console.WriteLine(line6);

            Thread.Sleep(TimeSpan.FromSeconds(1));
        }
    }
}
