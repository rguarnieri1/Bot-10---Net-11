# Paper Trading Bot (simulazione)

Bot di **paper trading** (nessun soldo reale, nessun ordine reale) che monitora 5 coppie
crypto su Crypto.com Exchange 24/7 e simula un portafoglio con una strategia trend-following.

## Come funziona

- **Strategia**: incrocio EMA(12) sopra EMA(26) su candele 15m, filtrato da RSI(14) tra 40 e 70
  per evitare ingressi in ipercomprato. L'incrocio opposto chiude la posizione per cambio trend.
- **Risk management**: max 20% dell'equity simulata per posizione, stop-loss -3%, take-profit +6%.
  Mai "all-in" su una singola posizione.
- **Capitale simulato iniziale**: 10.000 USD (modificabile in `Config/BotConfig.cs`).
- **Persistenza**: stato (cash, posizioni aperte, storico trade) salvato in `state.json` dopo
  ogni ciclo — puoi fermare e riavviare il bot senza perdere lo storico.
- **Log**: ogni trade chiuso viene scritto in `trades.csv`; l'attività va anche a console.

## Avvio

```bash
dotnet run
```

Il bot gira in loop continuo (polling ogni 60 secondi di default) finché non premi `Ctrl+C`,
che salva lo stato prima di uscire.

## Eseguirlo 24/7

Per farlo girare in background in modo continuo su Windows, alcune opzioni:
- Lascialo aperto in un terminale dedicato.
- Registralo come attività pianificata all'avvio con **Task Scheduler**
  (`schtasks` o l'interfaccia grafica), puntando a `dotnet run --project <path>`.
- Usa uno strumento come **NSSM** per eseguirlo come servizio Windows.

## Importante — leggi prima di aspettarti risultati

- **È una simulazione**: nessun ordine viene inviato all'exchange, nessun capitale reale è coinvolto.
- **Il 2% settimanale è un obiettivo di monitoraggio, non un risultato garantito.** Il report
  settimanale (`==== RIEPILOGO SETTIMANALE ====` nel log) mostra il rendimento reale ottenuto
  dalla simulazione, che può essere positivo, negativo, o nullo a seconda dell'andamento del mercato.
- Prima di usare una strategia simile con soldi veri, servono settimane/mesi di paper trading
  per valutare se la strategia ha un edge reale, oltre a considerare fee, slippage e latenza
  di esecuzione — nessuno di questi fattori è simulato qui in dettaglio.

## Configurazione

Tutti i parametri (coppie, timeframe, periodi EMA/RSI, soglie di rischio, intervallo di polling)
sono in `Config/BotConfig.cs`.
