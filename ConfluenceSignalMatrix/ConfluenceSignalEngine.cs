namespace WinFormsApp5;

internal sealed class ConfluenceSignalEngine
{
    private static readonly IReadOnlyDictionary<string, decimal> AnchorPrices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
    {
        ["EURUSD"] = 1.0845M,
        ["GBPUSD"] = 1.2710M,
        ["USDJPY"] = 156.42M,
        ["XAUUSD"] = 2358.80M,
        ["US30"] = 39120M,
        ["NAS100"] = 18870M,
        ["BTCUSD"] = 68250M
    };

    private readonly Random _random = new();

    public Dictionary<string, TimeframeReading> CreateBaseline(string symbol, IEnumerable<string> timeframes)
    {
        var index = Math.Abs(symbol.GetHashCode());
        var direction = (index % 3) switch
        {
            0 => MarketBias.Bullish,
            1 => MarketBias.Bearish,
            _ => MarketBias.Neutral
        };

        return timeframes.ToDictionary(
            timeframe => timeframe,
            timeframe => CreateReading(symbol, timeframe, BlendDirection(direction, timeframe, index), index),
            StringComparer.OrdinalIgnoreCase);
    }

    public Dictionary<string, TimeframeReading> CreateSyntheticPulse(string symbol, IReadOnlyDictionary<string, TimeframeReading> current)
    {
        var next = new Dictionary<string, TimeframeReading>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in current)
        {
            var reading = pair.Value;
            var pulseDirection = _random.NextDouble() switch
            {
                > 0.76 => MarketBias.Bullish,
                < 0.24 => MarketBias.Bearish,
                _ => reading.Bias
            };

            var movement = (decimal)(_random.NextDouble() - 0.48D);
            var pointValue = symbol.Contains("JPY", StringComparison.OrdinalIgnoreCase) ? 0.04M : 0.0004M;
            if (symbol is "XAUUSD")
            {
                pointValue = 2.6M;
            }
            else if (symbol is "US30" or "NAS100" or "BTCUSD")
            {
                pointValue = 24M;
            }

            next[pair.Key] = reading with
            {
                Price = Math.Max(0.0001M, reading.Price + movement * pointValue),
                Bias = pulseDirection,
                LiquiditySweep = _random.NextDouble() > 0.34D,
                BreakOfStructure = _random.NextDouble() > 0.42D,
                VolumeDelta = Math.Clamp(reading.VolumeDelta + (_random.NextDouble() - 0.5D) * 0.32D, -1D, 1D),
                OrderBlockQuality = Math.Clamp(reading.OrderBlockQuality + (_random.NextDouble() - 0.42D) * 0.22D, 0D, 1D),
                Timestamp = DateTimeOffset.Now
            };
        }

        return next;
    }

    public MatrixEvaluation Evaluate(string asset, IReadOnlyDictionary<string, TimeframeReading> readings)
    {
        var bullishWeight = 0D;
        var bearishWeight = 0D;
        var structureScore = 0D;
        var weightTotal = 0D;

        foreach (var reading in readings.Values)
        {
            var weight = TimeframeWeight(reading.Timeframe);
            weightTotal += weight;

            if (reading.Bias == MarketBias.Bullish)
            {
                bullishWeight += weight;
            }
            else if (reading.Bias == MarketBias.Bearish)
            {
                bearishWeight += weight;
            }

            structureScore += weight * StructureQuality(reading);
        }

        var direction = bullishWeight > bearishWeight
            ? MarketBias.Bullish
            : bearishWeight > bullishWeight
                ? MarketBias.Bearish
                : MarketBias.Neutral;

        var directionalWeight = Math.Max(bullishWeight, bearishWeight);
        var alignmentRatio = weightTotal == 0D ? 0D : directionalWeight / weightTotal;
        var qualityRatio = weightTotal == 0D ? 0D : structureScore / weightTotal;
        var confluenceScore = Math.Clamp((alignmentRatio * 6.2D) + (qualityRatio * 3.8D), 0D, 10D);
        var confidence = Math.Clamp((confluenceScore / 10D) * 0.92D + 0.05D, 0.05D, 0.99D);

        var signalText = ResolveSignal(direction, confluenceScore, qualityRatio);
        var status = confluenceScore switch
        {
            >= 8.2D => "SECURE",
            >= 6.7D => "WATCH",
            >= 5.2D => "PENDING",
            _ => "NO TRADE"
        };

        var rationale = BuildRationale(readings, direction, alignmentRatio, qualityRatio);
        return new MatrixEvaluation(asset, readings, direction, confluenceScore, confidence, signalText, status, rationale);
    }

    private static TimeframeReading CreateReading(string symbol, string timeframe, MarketBias bias, int seed)
    {
        var price = AnchorPrices.GetValueOrDefault(symbol, 1M);
        var scale = timeframe switch
        {
            "M15" => 0.0008M,
            "H1" => 0.0014M,
            "H4" => 0.0023M,
            "D1" => 0.0035M,
            _ => 0.001M
        };

        if (symbol.Contains("JPY", StringComparison.OrdinalIgnoreCase))
        {
            scale *= 100M;
        }
        else if (symbol is "XAUUSD")
        {
            scale *= 2000M;
        }
        else if (symbol is "US30" or "NAS100" or "BTCUSD")
        {
            scale *= 18000M;
        }

        var offset = ((seed % 11) - 5) * scale;
        var quality = 0.48D + ((seed + timeframe.Length * 7) % 47) / 100D;
        return new TimeframeReading(
            timeframe,
            Math.Max(0.0001M, price + offset),
            bias,
            (seed + timeframe.Length) % 2 == 0,
            (seed + timeframe.Length) % 3 != 0,
            Math.Clamp(((seed % 200) - 100) / 100D, -1D, 1D),
            Math.Clamp(quality, 0D, 1D),
            DateTimeOffset.Now);
    }

    private static MarketBias BlendDirection(MarketBias dominant, string timeframe, int seed)
    {
        if (timeframe is "H4" or "D1")
        {
            return dominant;
        }

        if ((seed + timeframe.Length) % 5 == 0)
        {
            return dominant == MarketBias.Bullish ? MarketBias.Neutral : MarketBias.Bullish;
        }

        return dominant;
    }

    private static double TimeframeWeight(string timeframe) => timeframe switch
    {
        "M15" => 0.95D,
        "H1" => 1.2D,
        "H4" => 1.65D,
        "D1" => 2.05D,
        _ => 1D
    };

    private static double StructureQuality(TimeframeReading reading)
    {
        var liquidity = reading.LiquiditySweep ? 0.24D : 0.08D;
        var breakOfStructure = reading.BreakOfStructure ? 0.3D : 0.1D;
        var volume = Math.Abs(reading.VolumeDelta) * 0.18D;
        var orderBlock = reading.OrderBlockQuality * 0.28D;
        return Math.Clamp(liquidity + breakOfStructure + volume + orderBlock, 0D, 1D);
    }

    private static string ResolveSignal(MarketBias direction, double confluenceScore, double qualityRatio)
    {
        if (direction == MarketBias.Bullish && confluenceScore >= 8D && qualityRatio >= 0.6D)
        {
            return "OPTIMAL ACCUMULATION ZONE / LONG ENTRY SECURE";
        }

        if (direction == MarketBias.Bearish && confluenceScore >= 8D && qualityRatio >= 0.6D)
        {
            return "OPTIMAL DISTRIBUTION ZONE / SHORT ENTRY SECURE";
        }

        if (direction == MarketBias.Bullish && confluenceScore >= 6.4D)
        {
            return "BULLISH STRUCTURE ALIGNMENT / LONG WATCH";
        }

        if (direction == MarketBias.Bearish && confluenceScore >= 6.4D)
        {
            return "BEARISH STRUCTURE ALIGNMENT / SHORT WATCH";
        }

        return "LIQUIDITY BALANCE / WAIT FOR CONFIRMATION";
    }

    private static string BuildRationale(
        IReadOnlyDictionary<string, TimeframeReading> readings,
        MarketBias direction,
        double alignmentRatio,
        double qualityRatio)
    {
        var alignedFrames = readings.Values.Count(reading => reading.Bias == direction && direction != MarketBias.Neutral);
        var sweepFrames = readings.Values.Count(reading => reading.LiquiditySweep);
        var bosFrames = readings.Values.Count(reading => reading.BreakOfStructure);
        return $"{alignedFrames}/{readings.Count} frames aligned, {sweepFrames} liquidity sweeps, {bosFrames} break-of-structure prints, alignment {alignmentRatio:P0}, structure quality {qualityRatio:P0}.";
    }
}
