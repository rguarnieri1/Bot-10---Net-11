# Bot notifica FTSE MIB (solo console, nessun ordine automatico)

Monitora un paniere di azioni italiane interrogando Interactive Brokers e
stampa a console un "segnale d'acquisto" quando la SMA breve incrocia al
rialzo la SMA lunga, indicando anche un prezzo target informativo pari a
+3% (guadagno di 3 EUR su 100 EUR di capitale). **Il bot non invia mai
ordini**: decide solo cosa stampare a schermo, la decisione di investire
resta tua.

## 1. Abilitare l'API su TWS o IB Gateway

Il bot si collega via API locale, quindi TWS o IB Gateway devono essere
aperti e collegati al tuo account (consigliato: account paper/demo mentre
testi il bot).

1. Apri TWS (o IB Gateway) ed effettua il login.
2. Vai su **File > Global Configuration > API > Settings**.
3. Spunta **"Enable ActiveX and Socket Clients"**.
4. Deseleziona **"Read-Only API"** solo se in futuro vorrai anche inviare
   ordini da codice — per questo bot (solo lettura) puoi lasciarla attiva,
   è più sicuro.
5. Annota la porta socket:
   - TWS paper trading: **7497**
   - TWS live trading: **7496**
   - IB Gateway paper: **4002**
   - IB Gateway live: **4001**
   Imposta la stessa porta in `config.py` (`IB_PORT`).
6. In **"Trusted IP Addresses"** aggiungi `127.0.0.1` se il bot gira sulla
   stessa macchina.
7. Applica e lascia TWS/Gateway aperto e collegato mentre il bot è in
   esecuzione.

## 2. Installazione

```bash
cd ib_ftsemib_bot
pip install -r requirements.txt
```

## 3. Configurazione

Apri [config.py](config.py) e verifica/adatta:

- `IB_PORT` / `IB_CLIENT_ID`: in base a TWS o Gateway, paper o live.
- `WATCHLIST`: lista ticker FTSE MIB (già precompilata con i principali
  titoli). Se un ticker non viene riconosciuto, il bot lo segnala in
  console all'avvio: cercalo in TWS con *Trading > Contract Search* e
  correggi il simbolo.
- `BAR_SIZE`, `SMA_SHORT`, `SMA_LONG`: parametri della strategia di
  incrocio medie mobili.
- `TARGET_GAIN_EUR` / `REFERENCE_CAPITAL_EUR`: la soglia di guadagno
  mostrata nella notifica (default 3 EUR su 100 EUR = 3%).
- `POLL_INTERVAL_SECONDS`: ogni quanti secondi ricontrollare i titoli.

## 4. Avvio

```bash
python bot.py
```

Il bot si connette, valida il watchlist, poi ogni `POLL_INTERVAL_SECONDS`
richiede le barre storiche di ciascun titolo e ricalcola le medie mobili.
Quando la SMA breve incrocia al rialzo la SMA lunga su una nuova barra,
stampa un blocco come questo:

```
======================================================================
[SEGNALE ACQUISTO] ISP  @ 2026-08-26 11:15:00
  Prezzo attuale:      3.4200 EUR
  SMA20: 3.4050   SMA50: 3.4010
  Target (+3.0%): 3.5226 EUR
  Su 100 EUR di capitale (~29.24 azioni) => guadagno stimato 3.00 EUR se il target viene raggiunto
  (Nessun ordine è stato inviato: notifica informativa)
======================================================================
```

Ogni incrocio viene notificato una sola volta (lo stato è salvato in
`notified_state.json`, cancellalo se vuoi resettare la memoria del bot).

Premi `Ctrl+C` per fermarlo.

## Note importanti

- Con account senza abbonamento ai dati real-time di Borsa Italiana, il
  bot usa dati **delayed** (`reqMarketDataType(3)`): la strategia si basa
  comunque su barre storiche, quindi funziona anche senza abbonamento,
  ma i prezzi possono essere ritardati di ~15-20 minuti.
- Verifica sempre a mano i segnali prima di investire: l'incrocio di
  medie mobili è un indicatore tecnico semplice, non una previsione
  garantita, e il target +3% è puramente informativo.
- Se cambi `BAR_SIZE` (es. `"1 day"`), aumenta `HISTORY_DURATION` di
  conseguenza (es. `"6 M"`) perché serve storia sufficiente a calcolare
  `SMA_LONG` periodi.
