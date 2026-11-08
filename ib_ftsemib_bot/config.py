"""Configurazione del bot di notifica FTSE MIB."""

# --- Connessione Interactive Brokers ---
IB_HOST = "127.0.0.1"
IB_PORT = 7497          # 7497 = TWS paper, 7496 = TWS live, 4002 = Gateway paper, 4001 = Gateway live
IB_CLIENT_ID = 17       # id arbitrario, deve essere diverso da altre connessioni API attive

# --- Paniere titoli monitorati (FTSE MIB) ---
# symbol = ticker Interactive Brokers, exchange = "SMART" instrada l'ordine/dato
# tramite il primaryExchange indicato. Se un titolo non viene risolto da IB,
# il bot lo segnala e lo salta: puoi correggerlo cercando il ticker esatto in
# TWS (menu Trading > Contract Search).
WATCHLIST = [
    "ENI", "ISP", "UCG", "ENEL", "STM", "G", "RACE", "TIT", "PST", "MB",
    "SRG", "TRN", "PIRC", "CPR", "MONC", "REC", "AMP", "BAMI", "BPE", "FBK",
    "IG", "INW", "LDO", "NEXI", "PRY", "TEN", "UNI", "DIA", "HER", "IP", "BC",
]
EXCHANGE = "SMART"
PRIMARY_EXCHANGE = "BVME"   # Borsa Italiana su IB
CURRENCY = "EUR"

# --- Parametri strategia (incrocio medie mobili) ---
BAR_SIZE = "5 mins"     # dimensione barra storica (es. "1 min", "5 mins", "15 mins", "1 day")
HISTORY_DURATION = "2 D"  # quanta storia richiedere ad ogni interrogazione
SMA_SHORT = 20           # periodi della media mobile breve
SMA_LONG = 50            # periodi della media mobile lunga

# --- Parametri di guadagno (solo informativo, nessun ordine viene inviato) ---
REFERENCE_CAPITAL_EUR = 100.0
TARGET_GAIN_EUR = 3.0
TARGET_GAIN_PCT = TARGET_GAIN_EUR / REFERENCE_CAPITAL_EUR * 100  # 3%

# --- Polling ---
POLL_INTERVAL_SECONDS = 60   # ogni quanto ricontrollare tutti i titoli

# --- Stato ---
STATE_FILE = "notified_state.json"  # evita di rinotificare lo stesso incrocio più volte
