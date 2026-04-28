using static Ffi;

class Program {
    static void Main() {
        Console.WriteLine("Initialising display...");
        using var display = new Epd();
        var lightSensor = new LightSensor();
        var sunTimes = new SunTimesCalculator();

        string emailTo = Environment.GetEnvironmentVariable("ALERT_EMAIL") ?? "your-email@gmail.com";
        var alertService = new AlertService(emailTo);

        string dataDir = Path.Combine(AppContext.BaseDirectory, "data");
        var sensorData = new SensorData(dataDir);

        var htmlPath = Path.Combine(AppContext.BaseDirectory, "web", "index.html");
        using var webServer = new WebServer(sensorData, alertService, htmlPath);
        webServer.Start();

        const ushort chkX = 4, lblX = 24;
        const ushort sensorStartY = 30, rowH = 16;

        display.Fill(WHITE);
        display.DrawIcon(4, 4, Icons.Bell, Icons.BellW, Icons.BellH);
        display.DrawText(24, 6, "Startup", Font.F16, BLACK, WHITE);

        var ip = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up
                        && n.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211
                        && n.GetIPProperties().UnicastAddresses.Any(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
        if (ip != null)
            display.DrawText(24, 24, $"http://{ip.Address}", Font.F12, BLACK, WHITE);

        string[] sensors = ["E-Paper Display", "DHT11 Temp/Humidity", "BMP280 Pressure", "Light Sensor"];
        for (int i = 0; i < sensors.Length; i++)
            display.DrawText(lblX, (ushort)(sensorStartY + i * rowH), sensors[i], Font.F12, BLACK, WHITE);

        display.DrawIcon(chkX, sensorStartY, Icons.Check, Icons.CheckW, Icons.CheckH);
        display.DisplayBase();
        Console.WriteLine("EPD: OK");

        bool dht11Ok = true;
        try { Dht11.Read(); } catch { dht11Ok = false; }
        display.DrawIcon(chkX, (ushort)(sensorStartY + rowH), dht11Ok ? Icons.Check : Icons.Cross,
                         (ushort)(dht11Ok ? Icons.CheckW : Icons.CrossW),
                         (ushort)(dht11Ok ? Icons.CheckH : Icons.CrossH));
        display.DisplayPartial();
        Console.WriteLine($"DHT11: {(dht11Ok ? "OK" : "FAIL")}");

        PressureSensor? pressureSensor = null;
        bool bmpOk = false;
        try { pressureSensor = new PressureSensor(); bmpOk = true; }
        catch (Exception e) { Console.Error.WriteLine($"BMP280: {e.Message}"); }
        display.DrawIcon(chkX, (ushort)(sensorStartY + rowH * 2), bmpOk ? Icons.Check : Icons.Cross,
                         (ushort)(bmpOk ? Icons.CheckW : Icons.CrossW),
                         (ushort)(bmpOk ? Icons.CheckH : Icons.CrossH));
        display.DisplayPartial();
        Console.WriteLine($"BMP280: {(bmpOk ? "OK" : "FAIL")}");

        bool lightOk = false;
        try
        {
            if (lightSensor.IsAvailable)
            {
                _ = lightSensor.Read();
                lightOk = true;
            }
        }
        catch (Exception e) { Console.Error.WriteLine($"LIGHT: {e.Message}"); }
        display.DrawIcon(chkX, (ushort)(sensorStartY + rowH * 3), lightOk ? Icons.Check : Icons.Cross,
                         (ushort)(lightOk ? Icons.CheckW : Icons.CrossW),
                         (ushort)(lightOk ? Icons.CheckH : Icons.CrossH));
        display.DisplayPartial();
        Console.WriteLine($"LIGHT: {(lightOk ? "OK" : "FAIL")}");

        Sen0545? rainSensor = null;
        try
        {
            var rainPort = Environment.GetEnvironmentVariable("SEN0545_PORT");
            if (!string.IsNullOrEmpty(rainPort))
            {
                rainSensor = new Sen0545(rainPort);
                var rd = rainSensor.Read();
                Console.WriteLine($"SEN0545: OK (Rain: {rd.Level}, Temp: {rd.TemperatureC}C, Light: {rd.LightLux})");
            }
            else
            {
                Console.WriteLine("SEN0545: Not configured (set SEN0545_PORT env var)");
            }
        }
        catch (Exception e) { Console.Error.WriteLine($"SEN0545: {e.Message}"); }

        Thread.Sleep(1500);

        display.Fill(WHITE);
        display.DrawIcon(4, 4, Icons.Bell, Icons.BellW, Icons.BellH);
        display.DrawText(24, 6, "", Font.F16, BLACK, WHITE);
        display.DisplayBase();

        const ushort iconX = 0, textX = 20;
        const ushort timeX = 24, timeY = 6;
        const ushort dateY = 8;
        const ushort headerRightEdgeX = 244;
        const ushort line3Y = 40;
        const ushort line4Y = 56;
        const ushort line5Y = 72;
        const ushort line6Y = 88;
        const ushort line7Y = 104;
        const ushort screenW = 244, screenH = 16;
        string line3, line4, line5, line6, line7;
        RainData? lastRainData = null;
        string sunriseLocal = "--:--", sunsetLocal = "--:--";
        DateTime lastSunCalcDate = DateTime.MinValue;

        while (true)
        {
            var today = DateTime.Today;
            if (today != lastSunCalcDate)
            {
                var (sunrise, sunset) = sunTimes.GetSunriseSunset(today);
                sunriseLocal = sunrise.ToString("HH:mm");
                sunsetLocal = sunset.ToString("HH:mm");
                lastSunCalcDate = today;
            }

            float tempC = 0f, humidity = 0f;
            try
            {
                var dht11Reading = Dht11.Read();
                tempC = dht11Reading.TemperatureC;
                humidity = dht11Reading.Humidity;
                line3 = $"Temperature: {tempC}C";
                line4 = $"Humidity: {humidity}% RH";
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"DHT11: {e.Message}");
                line3 = "-- sensor error --";
                line4 = "-- sensor error --";
            }

            float? pressureHpa = null;
            float? pressureTempC = null;
            try
            {
                if (pressureSensor != null)
                {
                    var r = pressureSensor.Read();
                    pressureHpa = r.PressureHpa;
                    pressureTempC = r.TemperatureC;
                    line6 = $"Pressure: {r.PressureHpa:F1}hPa ({r.TemperatureC:F1}C)";
                }
                else
                {
                    line6 = "-- pressure unavailable --";
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"BMP280: {e.Message}");
                line6 = "-- pressure error --";
            }

            int? lightRaw = null;
            string lightCondition = "N/A";
            try
            {
                if (lightSensor.IsAvailable)
                {
                    var lr = lightSensor.Read();
                    lightRaw = lr.RawValue;
                    lightCondition = lr.Condition;
                }
                else
                {
                    lightCondition = "Unavailable";
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"LIGHT: {e.Message}");
                lightCondition = "Error";
            }

            line5 = lightRaw.HasValue
                ? $"Light: {lightCondition} ({lightRaw.Value})"
                : $"Light: {lightCondition}";

            string rainLevel = "--";
            int rainConfidence = 0;
            if (rainSensor != null)
            {
                try
                {
                    lastRainData = rainSensor.Read();
                    rainLevel = lastRainData.Level.ToString();
                    rainConfidence = lastRainData.Level switch
                    {
                        RainLevel.Heavy => 90,
                        RainLevel.Moderate => 70,
                        RainLevel.Light => 40,
                        RainLevel.NoRain => 0,
                        _ => 0
                    };
                    Console.WriteLine($"SEN0545: {lastRainData.Level} | {lastRainData.TemperatureC}C | {lastRainData.LightLux} lux");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"SEN0545: {e.Message}");
                    rainSensor?.Dispose();
                    rainSensor = null;
                }
            }

            line7 = $"Rain: {rainLevel}";

            var (alertMessage, isRaining) = alertService.CheckWeather(
                lastRainData, tempC, humidity, pressureHpa, lightRaw);

            sensorData.Update(tempC, humidity, pressureHpa, pressureTempC, lastRainData, rainLevel, rainConfidence, alertMessage, isRaining, sunriseLocal, sunsetLocal);

            var now = DateTime.Now;
            var timeText = now.ToString("HH:mm");
            var dateText = now.ToString("ddd, dd MMM");
            int dateXInt = headerRightEdgeX - (dateText.Length * 7);
            ushort dateX = (ushort)Math.Max(timeX + 52, dateXInt);

            display.ClearWindow(timeX, timeY, timeX + screenW, timeY + 16, WHITE);
            display.DrawText(timeX, timeY, timeText, Font.F16, BLACK, WHITE);
            display.DrawText(dateX, dateY, dateText, Font.F12, BLACK, WHITE);

            display.ClearWindow(iconX, line3Y, iconX + screenW, line3Y + screenH, WHITE);
            display.DrawIcon(iconX, line3Y, Icons.Thermo, Icons.ThermoW, Icons.ThermoH);
            display.DrawText(textX, line3Y, line3, Font.F12, BLACK, WHITE);

            display.ClearWindow(iconX, line4Y, iconX + screenW, line4Y + screenH, WHITE);
            display.DrawIcon(iconX, line4Y, Icons.Drop, Icons.DropW, Icons.DropH);
            display.DrawText(textX, line4Y, line4, Font.F12, BLACK, WHITE);

            display.ClearWindow(iconX, line5Y, iconX + screenW, line5Y + screenH, WHITE);
            display.DrawIcon(iconX, line5Y, Icons.Light, Icons.LightW, Icons.LightH);
            display.DrawText(textX, line5Y, line5, Font.F12, BLACK, WHITE);

            display.ClearWindow(iconX, line6Y, iconX + screenW, line6Y + screenH, WHITE);
            display.DrawIcon(iconX, line6Y, Icons.Gauge, Icons.GaugeW, Icons.GaugeH);
            display.DrawText(textX, line6Y, line6, Font.F12, BLACK, WHITE);

            display.ClearWindow(iconX, line7Y, iconX + screenW, line7Y + screenH, WHITE);
            display.DrawIcon(iconX, line7Y, Icons.Rain, Icons.RainW, Icons.RainH);
            display.DrawText(textX, line7Y, line7, Font.F12, BLACK, WHITE);

            display.DisplayPartial();

            Console.WriteLine(line3);
            Console.WriteLine(line4);
            Console.WriteLine(line5);
            Console.WriteLine(line6);
            Console.WriteLine(line7);

            Thread.Sleep(TimeSpan.FromSeconds(1));
        }
    }
}