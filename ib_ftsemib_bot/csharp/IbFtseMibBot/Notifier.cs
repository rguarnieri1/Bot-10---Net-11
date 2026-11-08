namespace IbFtseMibBot;

internal static class Notifier
{
    public static void Print(string symbol, CrossInfo info)
    {
        var targetPrice = info.Price * (1 + Config.TargetGainPct / 100.0);
        var sharesForCapital = Config.ReferenceCapitalEur / info.Price;

        Console.WriteLine(new string('=', 70));
        Console.WriteLine($"[SEGNALE ACQUISTO] {symbol}  @ {info.BarTime}");
        Console.WriteLine($"  Prezzo attuale:      {info.Price:F4} EUR");
        Console.WriteLine($"  SMA{Config.SmaShort}: {info.SmaShort:F4}   SMA{Config.SmaLong}: {info.SmaLong:F4}");
        Console.WriteLine($"  Target (+{Config.TargetGainPct:F1}%): {targetPrice:F4} EUR");
        Console.WriteLine($"  Su {Config.ReferenceCapitalEur:F0} EUR di capitale (~{sharesForCapital:F2} azioni) " +
                           $"=> guadagno stimato {Config.TargetGainEur:F2} EUR se il target viene raggiunto");
        Console.WriteLine("  (Nessun ordine è stato inviato: notifica informativa)");
        Console.WriteLine(new string('=', 70));
    }
}
