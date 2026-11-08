using IBApi;

namespace IbFtseMibBot;

internal sealed record CrossInfo(double Price, double SmaShort, double SmaLong, string BarTime, bool BullishCross);

internal static class SmaCross
{
    /// Calcola SMA breve/lunga sulle barre ricevute e verifica se, tra la
    /// penultima e l'ultima barra, la SMA breve ha incrociato al rialzo
    /// la SMA lunga (stessa logica del bot Python).
    public static CrossInfo? Evaluate(List<Bar> bars, int smaShortLen, int smaLongLen)
    {
        if (bars.Count < smaLongLen + 1) return null;

        double Sma(int endIndexExclusive, int length)
        {
            double sum = 0;
            for (int i = endIndexExclusive - length; i < endIndexExclusive; i++)
                sum += bars[i].Close;
            return sum / length;
        }

        var lastIdx = bars.Count;
        var prevIdx = bars.Count - 1;

        double prevShort = Sma(prevIdx, smaShortLen);
        double prevLong = Sma(prevIdx, smaLongLen);
        double lastShort = Sma(lastIdx, smaShortLen);
        double lastLong = Sma(lastIdx, smaLongLen);

        bool bullishCross = prevShort <= prevLong && lastShort > lastLong;
        var lastBar = bars[^1];

        return new CrossInfo(lastBar.Close, lastShort, lastLong, lastBar.Time, bullishCross);
    }
}
