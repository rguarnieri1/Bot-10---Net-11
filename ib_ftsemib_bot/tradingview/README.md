# Bot FTSE MIB via TradingView (alternativa al bot IB)

Stessa strategia del bot Python (incrocio SMA20/50 + target informativo
+3%), ma il segnale viene calcolato da TradingView invece che
interrogando Interactive Brokers. Due modalità, a seconda di quanto vuoi
spendere/programmare:

## Limiti importanti da conoscere prima di scegliere

- **Piano Free**: solo **1 alert attivo**, nessun alert "tecnico" (basato
  su script Pine come questo), **nessun webhook**. In pratica il piano
  gratuito NON permette di usare questo script su più titoli né di
  ricevere l'alert sul tuo bot in console.
- **Essential** (14,95 $/mese): 20 alert attivi, sblocca alert tecnici e
  webhook. È il piano minimo per far funzionare questa soluzione su più
  titoli e per collegarla al ricevitore console.
- Piani superiori (Plus, Premium, Ultimate) alzano solo il numero di
  alert simultanei (100/400/1000).
- Ogni alert in TradingView è legato a **un singolo simbolo**: per
  monitorare più titoli del FTSE MIB devi creare un alert per ciascuno
  (es. 20 alert = 20 titoli monitorabili col piano Essential).

Fonte: confronto piani TradingView aggiornato a metà 2026.

## Modalità A — Solo notifiche native TradingView (nessun ricevitore da gestire)

Più semplice, ma senza output su console: le notifiche arrivano come
popup nel browser, notifica push sull'app mobile, o email.

1. Apri un grafico di un titolo, es. `MIL:ENI` (cerca il ticker in alto,
   prefisso `MIL:` per Borsa Italiana).
2. Apri **Pine Editor** (in basso), incolla il contenuto di
   [sma_cross_alert.pine](sma_cross_alert.pine), clicca **Add to chart**.
3. Clicca sul campanello **Alert** (o tasto `Alt+A`) in alto, scegli come
   condizione lo script appena aggiunto ("FTSE MIB SMA Cross Alert" →
   qualsiasi funzione di alert).
4. In **Notifiche**, spunta "App notification", "Pop-up" e/o "Email" (in
   base al piano).
5. Ripeti per ogni titolo del FTSE MIB che vuoi monitorare (richiede
   almeno il piano Essential se ne vuoi più di uno).

## Modalità B — Notifica reale su console (come il bot originale)

Aggiunge un passaggio: l'alert TradingView invia un webhook al tuo PC,
che stampa il messaggio in console.

### 1. Avvia il ricevitore locale

```bash
cd ib_ftsemib_bot/tradingview
pip install -r requirements.txt
python webhook_receiver.py
```

Resta in ascolto su `http://localhost:5001/tv-webhook`.

### 2. Esponi la porta a Internet con ngrok

TradingView non può contattare `localhost`: serve un URL pubblico.

```bash
ngrok http 5001
```

Copia l'URL `https://xxxxxxx.ngrok-free.app` che ngrok stampa (cambia ad
ogni riavvio se usi il piano gratuito di ngrok).

### 3. Configura l'alert su TradingView

Segui i passi della Modalità A, ma al punto 4 in **Notifiche** spunta
**"Webhook URL"** e incolla:

```
https://xxxxxxx.ngrok-free.app/tv-webhook
```

Il campo webhook richiede almeno il piano **Essential**.

### 4. Verifica

Quando la SMA breve incrocia al rialzo la SMA lunga sul titolo
monitorato, vedrai in console qualcosa come:

```
======================================================================
[SEGNALE DA TRADINGVIEW] ricevuto alle 2026-08-26T15:30:02
ENI - SEGNALE ACQUISTO
Prezzo: 14.5000 EUR
SMA20: 14.4200  SMA50: 14.3900
Target (+3.0%): 14.9350 EUR
Guadagno stimato su 100 EUR: 3.00 EUR
======================================================================
```

## Confronto rapido con il bot Interactive Brokers

| | Bot IB (`../bot.py`) | TradingView |
|---|---|---|
| Costo | Gratis (serve solo conto IB) | Serve piano a pagamento (≥ Essential) per multi-titolo/webhook |
| Dati | Da IB (delayed se senza abbonamento) | Dati di mercato TradingView |
| Watchlist | Un'unica esecuzione copre tutto il paniere | Un alert per titolo, da creare uno a uno |
| Notifica console reale | Sì, nativa | Sì, ma solo tramite webhook + ricevitore locale (Modalità B) |
| Manutenzione | Un solo script Python sempre attivo | Script Pine + alert per titolo + tunnel ngrok sempre attivo |

Per un paniere ampio come il FTSE MIB, il bot IB resta più economico da
mantenere (non richiede abbonamenti aggiuntivi); TradingView conviene se
preferisci un'interfaccia grafica e vuoi evitare di gestire TWS/Gateway.
