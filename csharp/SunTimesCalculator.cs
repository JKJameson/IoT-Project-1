public sealed class SunTimesCalculator
{
    private readonly double _latitude;
    private readonly double _longitude;

    public SunTimesCalculator()
    {
        _latitude = ParseDoubleOrDefault(Environment.GetEnvironmentVariable("SUN_LATITUDE"), 53.2707);
        _longitude = ParseDoubleOrDefault(Environment.GetEnvironmentVariable("SUN_LONGITUDE"), -9.0568);
    }

    public (DateTime Sunrise, DateTime Sunset) GetSunriseSunset(DateTime dateLocal)
    {
        var sunriseUtc = CalculateUtc(dateLocal, true);
        var sunsetUtc = CalculateUtc(dateLocal, false);
        return (sunriseUtc.ToLocalTime(), sunsetUtc.ToLocalTime());
    }

    private DateTime CalculateUtc(DateTime dateLocal, bool isSunrise)
    {
        int dayOfYear = dateLocal.DayOfYear;
        double lngHour = _longitude / 15.0;
        double t = dayOfYear + ((isSunrise ? 6.0 : 18.0) - lngHour) / 24.0;

        double m = (0.9856 * t) - 3.289;
        double l = m + (1.916 * SinDeg(m)) + (0.020 * SinDeg(2 * m)) + 282.634;
        l = Normalize360(l);

        double ra = RadToDeg(Math.Atan(0.91764 * TanDeg(l)));
        ra = Normalize360(ra);

        double lQuadrant = Math.Floor(l / 90.0) * 90.0;
        double raQuadrant = Math.Floor(ra / 90.0) * 90.0;
        ra += (lQuadrant - raQuadrant);
        ra /= 15.0;

        double sinDec = 0.39782 * SinDeg(l);
        double cosDec = Math.Cos(Math.Asin(sinDec));

        double cosH = (CosDeg(90.833) - (sinDec * SinDeg(_latitude))) / (cosDec * CosDeg(_latitude));
        cosH = Math.Clamp(cosH, -1.0, 1.0);

        double h = isSunrise
            ? 360.0 - RadToDeg(Math.Acos(cosH))
            : RadToDeg(Math.Acos(cosH));
        h /= 15.0;

        double localMeanTime = h + ra - (0.06571 * t) - 6.622;
        double utcHour = Normalize24(localMeanTime - lngHour);

        int hour = (int)Math.Floor(utcHour);
        int minute = (int)Math.Floor((utcHour - hour) * 60.0);
        int second = (int)Math.Round((((utcHour - hour) * 60.0) - minute) * 60.0);
        if (second == 60) { second = 0; minute++; }
        if (minute == 60) { minute = 0; hour = (hour + 1) % 24; }

        return new DateTime(dateLocal.Year, dateLocal.Month, dateLocal.Day, hour, minute, second, DateTimeKind.Utc);
    }

    private static double ParseDoubleOrDefault(string? text, double fallback)
        => double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;

    private static double Normalize360(double deg)
    {
        deg %= 360.0;
        if (deg < 0) deg += 360.0;
        return deg;
    }

    private static double Normalize24(double hour)
    {
        hour %= 24.0;
        if (hour < 0) hour += 24.0;
        return hour;
    }

    private static double SinDeg(double deg) => Math.Sin(DegToRad(deg));
    private static double CosDeg(double deg) => Math.Cos(DegToRad(deg));
    private static double TanDeg(double deg) => Math.Tan(DegToRad(deg));
    private static double DegToRad(double deg) => deg * Math.PI / 180.0;
    private static double RadToDeg(double rad) => rad * 180.0 / Math.PI;
}
