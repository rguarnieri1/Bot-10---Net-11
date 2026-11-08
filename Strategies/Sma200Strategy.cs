using BotCripto.Models;

namespace BotCripto.Strategies;

// Strategia SMA 200 su candele chiuse (timeframe configurato, tipicamente 1d), long e short:
//  LONG : la candela chiude sopra la SMA200 dopo che la precedente aveva chiuso sotto o sulla SMA200 (incrocio al rialzo),
//         candela rialzista, chiusura almeno MinCrossPercent sopra la SMA200, RSI14 tra 50 e RsiMaxLong
//  SHORT: specchio del long (incrocio al ribasso, candela ribassista, chiusura sotto la SMA200, RSI14 tra RsiMinShort e 50)
// Stop oltre l'estremo dello swing recente (più un buffer ATR); il target lo calcola il RiskManager (R:R configurato).
public class Sma200Strategy
{
    private const int SmaPeriod = 200;
    private const int SwingLookback = 10;          // candele per il minimo/massimo dello swing (stop)
    private const int AtrPeriod = 14;
    private const decimal AtrStopBuffer = 0.5m;    // buffer oltre lo swing, in ATR
    private const decimal MinCrossPercent = 0.005m; // la chiusura deve superare la SMA200 di almeno lo 0,5%
    private const decimal RsiMaxLong = 75m;        // long scartato se l'RSI è già ipercomprato
    private const decimal RsiMinShort = 25m;       // short scartato se l'RSI è già ipervenduto
    private const decimal MaxStopPercent = 0.15m;  // stop più largo del 15% = setup scartato
    private const bool AllowLong = true;
    private const bool AllowShort = true;
    public const int MinCandles = SmaPeriod + 20;

    public AnalysisResult Analyze(string symbol, List<Candle> candles)
    {
        var result = new AnalysisResult
        {
            Symbol = symbol,
            StrategyName = "SMA 200",
            IsSignal = false,
            Signal = "No Signal",
            AnalysisTime = DateTime.UtcNow
        };

        if (candles.Count < MinCandles)
        {
            result.Rejection = RejectionReason.InsufficientData;
            return result;
        }

        var closes = candles.Select(c => c.Close).ToList();
        var sma = TechnicalIndicators.CalculateSMA(closes, SmaPeriod);
        var rsi = TechnicalIndicators.CalculateRSI(closes);
        if (sma.Count < 2 || rsi.Count < 1)
        {
            result.Rejection = RejectionReason.InsufficientData;
            return result;
        }

        var last = candles.Last();
        var prev = candles[candles.Count - 2];
        var smaNow = sma[^1];
        var smaPrev = sma[^2];
        var rsiNow = rsi[^1];
        result.CurrentPrice = last.Close;
        result.Indicators["SMA200"] = smaNow;
        result.Indicators["RSI"] = rsiNow;

        // 1) Incrocio della SMA200 sull'ultima candela chiusa
        bool crossUp = AllowLong && prev.Close <= smaPrev && last.Close > smaNow * (1 + MinCrossPercent);
        bool crossDown = AllowShort && prev.Close >= smaPrev && last.Close < smaNow * (1 - MinCrossPercent);
        if (!crossUp && !crossDown)
        {
            result.Rejection = RejectionReason.NoBreakout;
            return result;
        }

        // 2) Conferma: candela nella direzione dell'incrocio
        if (crossUp ? last.Close <= last.Open : last.Close >= last.Open)
        {
            result.Rejection = RejectionReason.CandleNotStrong;
            return result;
        }

        // 3) RSI coerente con la direzione e non estremo
        if (crossUp ? (rsiNow <= 50m || rsiNow >= RsiMaxLong) : (rsiNow >= 50m || rsiNow <= RsiMinShort))
        {
            result.Rejection = RejectionReason.RsiExtreme;
            return result;
        }

        // Stop oltre lo swing recente, con buffer ATR
        var atr = CalculateAtr(candles, AtrPeriod);
        var swing = candles.Skip(candles.Count - SwingLookback).ToList();
        var stop = crossUp
            ? swing.Min(c => c.Low) - atr * AtrStopBuffer
            : swing.Max(c => c.High) + atr * AtrStopBuffer;
        var stopDistance = crossUp ? last.Close - stop : stop - last.Close;
        if (stop <= 0 || stopDistance <= 0 || stopDistance / last.Close > MaxStopPercent)
        {
            result.Rejection = RejectionReason.StopLossError;
            return result;
        }

        result.IsSignal = true;
        result.Signal = crossUp ? "BUY" : "SELL";
        result.Indicators["StopLoss"] = stop;
        result.Indicators["ATR"] = atr;
        // Più forte = chiusura più lontana dalla SMA200 e RSI più lontano da 50 (nella direzione del trade)
        var distance = Math.Abs(last.Close - smaNow) / smaNow * 100m;
        result.Indicators["SignalStrength"] = distance + Math.Abs(rsiNow - 50m) / 10m;
        return result;
    }

    private static decimal CalculateAtr(List<Candle> candles, int period)
    {
        var trs = new List<decimal>();
        for (int i = candles.Count - period; i < candles.Count; i++)
        {
            var c = candles[i];
            var prevClose = candles[i - 1].Close;
            trs.Add(Math.Max(c.High - c.Low, Math.Max(Math.Abs(c.High - prevClose), Math.Abs(c.Low - prevClose))));
        }
        return trs.Average();
    }
}
