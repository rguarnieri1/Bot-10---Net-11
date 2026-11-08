namespace PaperTradingBot.Strategy;

public static class Indicators
{
    /// <summary>Calcola l'EMA per ogni punto della serie (stesso numero di elementi in input e output).</summary>
    public static decimal[] Ema(IReadOnlyList<decimal> closes, int period)
    {
        var result = new decimal[closes.Count];
        if (closes.Count == 0) return result;

        var multiplier = 2m / (period + 1);
        result[0] = closes[0];
        for (var i = 1; i < closes.Count; i++)
            result[i] = (closes[i] - result[i - 1]) * multiplier + result[i - 1];

        return result;
    }

    /// <summary>RSI (Wilder smoothing) per l'ultimo punto della serie. Ritorna null se non ci sono abbastanza dati.</summary>
    public static decimal? Rsi(IReadOnlyList<decimal> closes, int period)
    {
        if (closes.Count < period + 1) return null;

        decimal avgGain = 0, avgLoss = 0;
        for (var i = 1; i <= period; i++)
        {
            var change = closes[i] - closes[i - 1];
            if (change > 0) avgGain += change;
            else avgLoss -= change;
        }
        avgGain /= period;
        avgLoss /= period;

        for (var i = period + 1; i < closes.Count; i++)
        {
            var change = closes[i] - closes[i - 1];
            var gain = change > 0 ? change : 0;
            var loss = change < 0 ? -change : 0;
            avgGain = (avgGain * (period - 1) + gain) / period;
            avgLoss = (avgLoss * (period - 1) + loss) / period;
        }

        if (avgLoss == 0) return 100m;
        var rs = avgGain / avgLoss;
        return 100m - 100m / (1 + rs);
    }
}
