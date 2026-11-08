using PaperTradingBot.Config;
using PaperTradingBot.MarketData;

namespace PaperTradingBot.Strategy;

public enum SignalType { None, Enter, ExitTrend }

public sealed record StrategySignal(SignalType Type, decimal? Rsi, decimal EmaFast, decimal EmaSlow);

/// <summary>
/// Trend-following: EMA veloce che incrocia sopra la EMA lenta apre una posizione long,
/// filtrato da RSI per evitare ingressi in ipercomprato o falsi segnali in ipervenduto forte.
/// L'incrocio opposto (EMA veloce sotto la lenta) segnala la chiusura per cambio di trend,
/// indipendentemente da stop-loss/take-profit che restano il controllo primario del rischio.
/// </summary>
public sealed class EmaCrossoverStrategy
{
    private readonly BotConfig _config;

    public EmaCrossoverStrategy(BotConfig config) => _config = config;

    public StrategySignal Evaluate(IReadOnlyList<Candle> candles)
    {
        var minRequired = Math.Max(_config.EmaSlowPeriod, _config.RsiPeriod) + 2;
        if (candles.Count < minRequired)
            return new StrategySignal(SignalType.None, null, 0, 0);

        var closes = candles.Select(c => c.Close).ToArray();
        var emaFast = Indicators.Ema(closes, _config.EmaFastPeriod);
        var emaSlow = Indicators.Ema(closes, _config.EmaSlowPeriod);
        var rsi = Indicators.Rsi(closes, _config.RsiPeriod);

        var last = closes.Length - 1;
        var prev = last - 1;

        var crossedUp = emaFast[prev] <= emaSlow[prev] && emaFast[last] > emaSlow[last];
        var crossedDown = emaFast[prev] >= emaSlow[prev] && emaFast[last] < emaSlow[last];

        if (crossedUp && rsi is >= 40m and <= 70m)
            return new StrategySignal(SignalType.Enter, rsi, emaFast[last], emaSlow[last]);

        if (crossedDown)
            return new StrategySignal(SignalType.ExitTrend, rsi, emaFast[last], emaSlow[last]);

        return new StrategySignal(SignalType.None, rsi, emaFast[last], emaSlow[last]);
    }
}
