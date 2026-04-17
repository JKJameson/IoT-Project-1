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
    private readonly Queue<(DateTime Time, float Pressure)> _history = new(12);

    public void AddPressureSample(float pressureHpa)
    {
        _history.Enqueue((DateTime.UtcNow, pressureHpa));
        while (_history.Count > 12)
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
        if (_history.Count < 2)
            return null;

        var oldest = _history.First();
        var newest = _history.Last();
        float hours = (float)(newest.Time - oldest.Time).TotalHours;
        if (hours < 0.01)
            return null;

        return (newest.Pressure - oldest.Pressure) / hours;
    }

    private static float DewPoint(float tempC, float humidity)
    {
        const float a = 17.27f;
        const float b = 237.7f;
        float alpha = (a * tempC) / (b + tempC) + MathF.Log(humidity / 100f);
        return (b * alpha) / (a - alpha);
    }
}
