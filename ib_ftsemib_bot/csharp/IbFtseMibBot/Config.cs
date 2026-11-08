namespace IbFtseMibBot;

/// Configurazione del bot: stessi parametri della versione Python.
internal static class Config
{
    // --- Connessione Interactive Brokers ---
    public const string Host = "127.0.0.1";
    public const int Port = 7497;       // 7497 = TWS paper, 7496 = TWS live, 4002 = Gateway paper, 4001 = Gateway live
    public const int ClientId = 18;     // diverso dal client id 17 usato dal bot Python, per poterli avviare insieme

    // --- Paniere titoli monitorati (FTSE MIB) ---
    public static readonly string[] Watchlist =
    [
        "ENI", "ISP", "UCG", "ENEL", "STM", "G", "RACE", "TIT", "PST", "MB",
        "SRG", "TRN", "PIRC", "CPR", "MONC", "REC", "AMP", "BAMI", "BPE", "FBK",
        "IG", "INW", "LDO", "NEXI", "PRY", "TEN", "UNI", "DIA", "HER", "IP", "BC",
    ];
    public const string Exchange = "SMART";
    public const string PrimaryExchange = "BVME"; // Borsa Italiana su IB
    public const string Currency = "EUR";

    // --- Parametri strategia (incrocio medie mobili) ---
    public const string BarSize = "5 mins";
    public const string HistoryDuration = "2 D";
    public const int SmaShort = 20;
    public const int SmaLong = 50;

    // --- Parametri di guadagno (solo informativo, nessun ordine viene inviato) ---
    public const double ReferenceCapitalEur = 100.0;
    public const double TargetGainEur = 3.0;
    public const double TargetGainPct = TargetGainEur / ReferenceCapitalEur * 100.0; // 3%

    // --- Polling ---
    public const int PollIntervalSeconds = 60;

    // --- Stato ---
    public const string StateFile = "notified_state_csharp.json";

    // --- Timeout per singola richiesta storica ---
    public const int HistoricalRequestTimeoutSeconds = 15;
}
