# Bot FTSE MIB in C# (Interactive Brokers)

Porting in C#/.NET dello stesso bot Python: interroga Interactive Brokers
per il paniere FTSE MIB, calcola l'incrocio SMA20/50 su barre a 5 minuti e
stampa su console un "segnale d'acquisto" con un target informativo di
+3% (3 EUR su 100 EUR di capitale). **Non invia mai ordini.**

Progetto: [IbFtseMibBot/](IbFtseMibBot) — .NET 8, pacchetto NuGet
[`IB.TWS.CSharpApi`](https://www.nuget.org/packages/IB.TWS.CSharpApi)
(client ufficiale TWS API per C#).

## 1. Prerequisiti

- .NET SDK 8.0+ (verificato con `dotnet --version`).
- TWS o IB Gateway aperti, collegati al tuo account, con l'API abilitata.
  Sono gli **stessi passaggi** descritti in
  [../README.md](../README.md#1-abilitare-lapi-su-tws-o-ib-gateway) per
  il bot Python:
  1. File > Global Configuration > API > Settings.
  2. Spunta "Enable ActiveX and Socket Clients".
  3. Annota la porta (7497 TWS paper / 7496 TWS live / 4002 Gateway paper
     / 4001 Gateway live) e impostala in [`Config.cs`](IbFtseMibBot/Config.cs)
     (`Port`).
  4. Aggiungi `127.0.0.1` alle Trusted IP Addresses.

  > Nota: al momento della creazione di questo progetto TWS/IB Gateway
  > non risultavano ancora avviati su questa macchina (nessuna porta
  > 7497/7496/4001/4002 in ascolto). Il bot è stato compilato ed
  > eseguito: gestisce correttamente l'assenza di connessione stampando
  > un errore chiaro invece di bloccarsi, ma per vedere i segnali reali
  > devi prima avviare TWS/Gateway seguendo i passaggi sopra.

- Il `ClientId` di default (`18`) è diverso da quello usato dal bot
  Python (`17`), così puoi tenerli aperti insieme senza conflitti.

## 2. Build

```bash
cd ib_ftsemib_bot/csharp/IbFtseMibBot
dotnet build
```

Il progetto include un `NuGet.Config` locale che usa solo `nuget.org`
come sorgente pacchetti (per evitare sorgenti extra eventualmente
configurate a livello di sistema che potrebbero richiedere credenziali).

## 3. Configurazione

Apri [`Config.cs`](IbFtseMibBot/Config.cs) e adatta se necessario:

- `Port` / `ClientId` — in base a TWS o Gateway, paper o live.
- `Watchlist` — stessi ticker FTSE MIB del bot Python; se IB non risolve
  un simbolo, il bot stampa `[IB ERRORE]` per quel titolo e passa al
  successivo (non blocca l'esecuzione).
- `BarSize`, `SmaShort`, `SmaLong` — parametri della strategia.
- `TargetGainEur` / `ReferenceCapitalEur` — soglia di guadagno mostrata
  nella notifica (default 3 EUR su 100 EUR = 3%).
- `PollIntervalSeconds` — ogni quanti secondi ricontrollare i titoli.

## 4. Esecuzione

```bash
dotnet run
```

Output atteso a connessione riuscita:

```
Connessione a 127.0.0.1:7497 (clientId=18)...
[IB] Connessione accettata dal server.
Connesso. Watchlist: 31 titoli. Ctrl+C per uscire.
======================================================================
[SEGNALE ACQUISTO] ISP  @ 20260826  11:15:00
  Prezzo attuale:      3.4200 EUR
  SMA20: 3.4050   SMA50: 3.4010
  Target (+3.0%): 3.5226 EUR
  Su 100 EUR di capitale (~29.24 azioni) => guadagno stimato 3.00 EUR se il target viene raggiunto
  (Nessun ordine è stato inviato: notifica informativa)
======================================================================
```

Se TWS/Gateway non sono raggiungibili, il bot stampa un errore e termina
subito invece di restare bloccato:

```
Connessione a 127.0.0.1:7497 (clientId=18)...
[IB ERRORE] Impossibile stabilire la connessione. Rifiuto persistente del computer di destinazione. [::ffff:127.0.0.1]:7497
[ERRORE] Impossibile connettersi. Verifica che TWS o IB Gateway siano aperti con l'API abilitata sulla porta configurata (vedi README.md).
```

Lo stato degli incroci già notificati viene salvato in
`notified_state_csharp.json` (file separato da quello del bot Python)
per non ripetere la stessa notifica.

Premi `Ctrl+C` per fermare il bot: si disconnette da IB in modo pulito.

## Differenze rispetto alla versione Python

- Usa il client ufficiale TWS API (`EWrapper`/`EClientSocket`) invece di
  `ib_async`: più verboso (l'interfaccia `EWrapper` richiede l'implementazione
  di tutti i callback, vedi [`IBWrapper.cs`](IbFtseMibBot/IBWrapper.cs)),
  ma senza dipendenze da runtime Python.
- Stessa identica logica di calcolo (SMA20/50, incrocio rialzista, target
  +3%) e stesso formato di notifica su console.
