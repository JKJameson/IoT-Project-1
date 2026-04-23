public enum RainLikelihood
{
    Unlikely,
    Possible,
    Likely,
    VeryLikely,
}

public static class RainLikelihoodExt
{
    public static string Label(this RainLikelihood l) => l switch
    {
        RainLikelihood.Unlikely    => "No rain",
        RainLikelihood.Possible    => "Maybe rain",
        RainLikelihood.Likely      => "Rain likely",
        RainLikelihood.VeryLikely  => "Rain!",
        _ => "???"
    };
}

public record RainPrediction(
    RainLikelihood Likelihood,
    int ConfidencePct,
    float? PressureHpa,
    float? PressureTrendHpaPerHour,
    float? DewPointC
);

public sealed class RainPredictor
{
    private readonly Queue<(DateTime Time, float Pressure)> _history = new();
    private const int MaxAgeMinutes = 10;

    public void AddPressureSample(float pressureHpa)
    {
        var now = DateTime.UtcNow;
        _history.Enqueue((now, pressureHpa));

        while (_history.Count > 0 && (now - _history.Peek().Time).TotalMinutes > MaxAgeMinutes)
            _history.Dequeue();
    }

    public RainPrediction Predict(float tempC, float humidity, float? pressureHpa)
    {
        float? trend = PressureTrend();

        float dewPoint = DewPoint(tempC, humidity);

        float score = 0f;

        if (humidity > 95f)
        {
            score += 35f;
            float spread = tempC - dewPoint;
            if (spread < 2f)
                score -= 15f;
            else if (spread < 5f)
                score -= 5f;
        }
        else if (humidity > 90f)
        {
            score += 25f;
        }
        else if (humidity > 85f)
        {
            score += 15f;
        }
        else if (humidity > 75f)
        {
            score += 5f;
        }

        if (trend.HasValue)
        {
            float t = trend.Value;
            if (t < -2f)
                score += 30f;
            else if (t < -1f)
                score += 20f;
            else if (t < -0.5f)
                score += 10f;
            else if (t > 1f)
                score -= 10f;
        }

        if (pressureHpa.HasValue)
        {
            float p = pressureHpa.Value;
            if (p < 1000f)
                score += 15f;
            else if (p < 1009f)
                score += 8f;
            else if (p > 1020f)
                score -= 5f;
        }

        if (humidity > 85f && trend.HasValue && trend.Value < -1f)
            score += 10f;

        int clamped = (int)Math.Clamp(score, 0f, 100f);

        var likelihood = clamped switch
        {
            < 20 => RainLikelihood.Unlikely,
            < 45 => RainLikelihood.Possible,
            < 70 => RainLikelihood.Likely,
            _    => RainLikelihood.VeryLikely,
        };

        return new RainPrediction(likelihood, clamped, pressureHpa, trend, dewPoint);
    }

    private float? PressureTrend()
    {
        if (_history.Count < 5)
            return null;

        // Use index as x (assuming roughly equal time intervals)
        int n = _history.Count;
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

        int i = 0;
        foreach (var (_, p) in _history)
        {
            sumX += i;
            sumY += p;
            sumXY += i * p;
            sumX2 += i * i;
            i++;
        }

        double denom = n * sumX2 - sumX * sumX;
        if (Math.Abs(denom) < 0.0001)
            return null;

        // slope is change in pressure per step
        double slopePerStep = (n * sumXY - sumX * sumY) / denom;
        double totalMinutes = (_history.Last().Time - _history.First().Time).TotalMinutes;
        if (totalMinutes < 0.5)
            return null;

        // Convert to hPa per hour
        return (float)(slopePerStep * 60.0 / totalMinutes * n);
    }

    private static float DewPoint(float tempC, float humidity)
    {
        const float a = 17.27f;
        const float b = 237.7f;
        float alpha = (a * tempC) / (b + tempC) + MathF.Log(humidity / 100f);
        return (b * alpha) / (a - alpha);
    }
}
