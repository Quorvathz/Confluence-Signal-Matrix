namespace WinFormsApp5;

internal enum MarketBias
{
    Neutral,
    Bullish,
    Bearish
}

internal sealed record TimeframeReading(
    string Timeframe,
    decimal Price,
    MarketBias Bias,
    bool LiquiditySweep,
    bool BreakOfStructure,
    double VolumeDelta,
    double OrderBlockQuality,
    DateTimeOffset Timestamp);

internal sealed record MatrixEvaluation(
    string Asset,
    IReadOnlyDictionary<string, TimeframeReading> Readings,
    MarketBias Direction,
    double ConfluenceScore,
    double Confidence,
    string SignalText,
    string Status,
    string Rationale);

internal static class MarketBiasParser
{
    public static MarketBias Parse(string value)
    {
        if (value.Contains("bull", StringComparison.OrdinalIgnoreCase) || value.Contains("long", StringComparison.OrdinalIgnoreCase))
        {
            return MarketBias.Bullish;
        }

        if (value.Contains("bear", StringComparison.OrdinalIgnoreCase) || value.Contains("short", StringComparison.OrdinalIgnoreCase))
        {
            return MarketBias.Bearish;
        }

        return MarketBias.Neutral;
    }
}
