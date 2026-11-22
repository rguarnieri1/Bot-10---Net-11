using BotCripto.Models;

namespace BotCripto.Strategies;

// Strategia SMA 200 su candele chiuse (timeframe configurato, tipicamente 1d), long e short:
//  LONG : la candela chiude sopra la SMA200 dopo che la precedente aveva chiuso sotto o sulla SMA200 (incrocio al rialzo),
//         candela rialzista, chiusura almeno MinCrossPercent sopra la SMA200, RSI14 tra 50 e RsiMaxLong
//  SHORT: specchio del long (incrocio al ribasso, candela ribassista, chiusura sotto la SMA200, RSI14 tra RsiMinShort e 50)
// Stop oltre l'estremo dello swing recente (più un buffer ATR); poi trailing stop ATR (vedi UseTrailingStop),
// oppure target a R:R configurato (RiskManager) se il trailing è disattivato.
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

    // Filtro regime BTC sui long: disattivato (backtest 4 anni su 1d: senza filtro il P&L e il profit factor sono migliori)
    private const bool BtcRegimeFilterOnLong = false;

    // Gestione dell'uscita: trailing stop a ATR invece del target fisso a R:R (backtest 4 anni su 1d: PF 1,63 -> 1,99).
    // Lo stop iniziale è quello dello swing; a ogni candela chiusa si alza (long) / abbassa (short) a close -/+ TrailAtrMultiple × ATR.
    public const bool UseTrailingStop = true;
    public const decimal TrailAtrMultiple = 3m;
    // Numero massimo di posizioni aperte contemporaneamente (riduce i grappoli di trade correlati)
    public const int MaxOpenPositions = 15;

    // Solo i primi N simboli per volume medio in USDT (20 candele precedenti a quella del segnale): meno slippage sui token illiquidi
    // (backtest 4 anni su 1d: top 50 = PF 2,49, tutti gli anni positivi, regge allo stress slippage 0,5%).
    public const int TopLiquidSymbols = 50;
    private const int VolumeAveragePeriod = 20;

    // Volume medio in USDT delle VolumeAveragePeriod candele che precedono l'ultima (0 se i dati non bastano)
    public static decimal AvgDollarVolume(List<Candle> candles)
    {
        if (candles.Count < VolumeAveragePeriod + 1)
            return 0m;
        decimal sum = 0m;
        for (int i = candles.Count - 1 - VolumeAveragePeriod; i < candles.Count - 1; i++)
            sum += candles[i].Volume * candles[i].Close;
        return sum / VolumeAveragePeriod;
    }

    // Soglia di volume per entrare nella classifica dei primi TopLiquidSymbols (0 = nessun filtro / dati insufficienti)
    public static decimal TopLiquidityThreshold(IEnumerable<decimal> dollarVolumes)
    {
        var sorted = dollarVolumes.Where(v => v > 0).OrderByDescending(v => v).ToList();
        return TopLiquidSymbols > 0 && sorted.Count >= TopLiquidSymbols ? sorted[TopLiquidSymbols - 1] : 0m;
    }

    // Prossimo livello dello stop dopo una candela chiusa (lo stop non si allontana mai dal prezzo)
    public static decimal TrailStop(decimal currentStop, bool isLong, List<Candle> candles, int idx)
    {
        if (idx < AtrPeriod)
            return currentStop;
        var atr = AtrAt(candles, idx, AtrPeriod);
        return isLong
            ? Math.Max(currentStop, candles[idx].Close - TrailAtrMultiple * atr)
            : Math.Min(currentStop, candles[idx].Close + TrailAtrMultiple * atr);
    }

    // ATR semplice (media dei true range) delle ultime `period` candele fino a idx incluso
    public static decimal AtrAt(List<Candle> candles, int idx, int period)
    {
        decimal sum = 0m;
        for (int i = idx - period + 1; i <= idx; i++)
        {
            var c = candles[i];
            var prevClose = candles[i - 1].Close;
            sum += Math.Max(c.High - c.Low, Math.Max(Math.Abs(c.High - prevClose), Math.Abs(c.Low - prevClose)));
        }
        return sum / period;
    }

    private const int BtcRegimeEmaPeriod = 50;

    // Regime di mercato: true se BTC chiude sopra la sua EMA50, false se sotto, null se dati insufficienti.
    public static bool? IsBtcBullish(List<Candle> btcCandles)
    {
        var ema = TechnicalIndicators.CalculateEMA(btcCandles.Select(c => c.Close).ToList(), BtcRegimeEmaPeriod);
        if (ema.Count == 0)
            return null;
        return btcCandles.Last().Close > ema.Last();
    }

    // btcBullish: filtro di mercato (BTC sopra la EMA50), applicato ai long solo se BtcRegimeFilterOnLong è attivo; lo short non è filtrato.
    public AnalysisResult Analyze(string symbol, List<Candle> candles, bool? btcBullish)
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

        // Filtro regime BTC solo sul lato long
        if (BtcRegimeFilterOnLong && crossUp && btcBullish != true)
        {
            result.Rejection = RejectionReason.AgainstBtcRegime;
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
