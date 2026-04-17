using static Ffi;

class Program {
    static void Main() {
        Console.WriteLine("Initialising display...");
        using var display = new Epd();
        var predictor = new RainPredictor();

        // ── Startup sequence ─────────────────────────────────────────────────
        const ushort chkX = 4, lblX = 24;
        const ushort sensorStartY = 30, rowH = 16;

        display.Fill(WHITE);
        display.DrawIcon(4, 4, Icons.Bell, Icons.BellW, Icons.BellH);
        display.DrawText(24, 6, "Startup", Font.F16, BLACK, WHITE);

        string[] sensors = ["E-Paper Display", "DHT11 Temp/Humidity", "BMP280 Pressure"];
        for (int i = 0; i < sensors.Length; i++)
            display.DrawText(lblX, (ushort)(sensorStartY + i * rowH), sensors[i], Font.F12, BLACK, WHITE);

        // EPD is already working if we got here — check it immediately
        display.DrawIcon(chkX, sensorStartY, Icons.Check, Icons.CheckW, Icons.CheckH);
        display.DisplayBase();
        Console.WriteLine("EPD: OK");

        // Init DHT11 and check immediately
        bool dht11Ok = true;
        try { Dht11.Read(); } catch { dht11Ok = false; }
        display.DrawIcon(chkX, (ushort)(sensorStartY + rowH), dht11Ok ? Icons.Check : Icons.Cross,
                         (ushort)(dht11Ok ? Icons.CheckW : Icons.CrossW),
                         (ushort)(dht11Ok ? Icons.CheckH : Icons.CrossH));
        display.DisplayPartial();
        Console.WriteLine($"DHT11: {(dht11Ok ? "OK" : "FAIL")}");

        // Init BMP280 and check immediately
        PressureSensor? pressureSensor = null;
        bool bmpOk = false;
        try { pressureSensor = new PressureSensor(); bmpOk = true; }
        catch (Exception e) { Console.Error.WriteLine($"BMP280: {e.Message}"); }
        display.DrawIcon(chkX, (ushort)(sensorStartY + rowH * 2), bmpOk ? Icons.Check : Icons.Cross,
                         (ushort)(bmpOk ? Icons.CheckW : Icons.CrossW),
                         (ushort)(bmpOk ? Icons.CheckH : Icons.CrossH));
        display.DisplayPartial();
        Console.WriteLine($"BMP280: {(bmpOk ? "OK" : "FAIL")}");

        // Hold the startup screen for 1.5s
        Thread.Sleep(1500);

        // ── Set base frame for sensor readings ───────────────────────────────
        display.Fill(WHITE);
        display.DrawIcon(4, 4, Icons.Bell, Icons.BellW, Icons.BellH);
        display.DrawText(24, 6, "", Font.F16, BLACK, WHITE);
        display.DisplayBase();

        const ushort iconX = 0, textX = 20;
        const ushort line3Y = 56;
        const ushort line4Y = 72;
        const ushort line5Y = 88;
        const ushort line6Y = 104;
        const ushort screenW = 244, screenH = 16;
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

            display.ClearWindow(iconX, line3Y, iconX + screenW, line3Y + screenH, WHITE);
            display.DrawIcon(iconX, line3Y, Icons.Thermo, Icons.ThermoW, Icons.ThermoH);
            display.DrawText(textX, line3Y, line3, Font.F12, BLACK, WHITE);

            display.ClearWindow(iconX, line4Y, iconX + screenW, line4Y + screenH, WHITE);
            display.DrawIcon(iconX, line4Y, Icons.Drop, Icons.DropW, Icons.DropH);
            display.DrawText(textX, line4Y, line4, Font.F12, BLACK, WHITE);

            display.ClearWindow(iconX, line5Y, iconX + screenW, line5Y + screenH, WHITE);
            display.DrawIcon(iconX, line5Y, Icons.Gauge, Icons.GaugeW, Icons.GaugeH);
            display.DrawText(textX, line5Y, line5, Font.F12, BLACK, WHITE);

            display.ClearWindow(iconX, line6Y, iconX + screenW, line6Y + screenH, WHITE);
            display.DrawIcon(iconX, line6Y, Icons.Rain, Icons.RainW, Icons.RainH);
            display.DrawText(textX, line6Y, line6, Font.F12, BLACK, WHITE);

            display.DisplayPartial();

            Console.WriteLine(line3);
            Console.WriteLine(line4);
            Console.WriteLine(line5);
            Console.WriteLine(line6);

            Thread.Sleep(TimeSpan.FromSeconds(1));
        }
    }
}
