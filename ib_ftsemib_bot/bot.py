"""
Bot di notifica (solo console, nessun ordine automatico) per azioni del FTSE MIB.

Segnale: incrocio rialzista tra SMA breve e SMA lunga calcolato su barre
storiche richieste a Interactive Brokers. Quando scatta, stampa a console
il prezzo attuale e un prezzo target informativo pari a +3% (obiettivo di
guadagno di 3 EUR su 100 EUR di capitale), scelto dall'utente in config.py.

Richiede TWS o IB Gateway avviato con l'API abilitata (vedi README.md).
"""

import json
import os
from datetime import datetime

from ib_async import IB, Stock, util

import config


def load_state() -> dict:
    if os.path.exists(config.STATE_FILE):
        with open(config.STATE_FILE, "r", encoding="utf-8") as f:
            return json.load(f)
    return {}


def save_state(state: dict) -> None:
    with open(config.STATE_FILE, "w", encoding="utf-8") as f:
        json.dump(state, f, indent=2)


def qualify_watchlist(ib: IB) -> list[Stock]:
    contracts = []
    for symbol in config.WATCHLIST:
        stock = Stock(symbol, config.EXCHANGE, config.CURRENCY, primaryExchange=config.PRIMARY_EXCHANGE)
        qualified = ib.qualifyContracts(stock)
        if not qualified:
            print(f"[AVVISO] Impossibile risolvere il ticker '{symbol}' su IB: verificarlo in TWS. Saltato.")
            continue
        contracts.append(qualified[0])
    return contracts


def compute_sma_crossover(ib: IB, contract: Stock):
    bars = ib.reqHistoricalData(
        contract,
        endDateTime="",
        durationStr=config.HISTORY_DURATION,
        barSizeSetting=config.BAR_SIZE,
        whatToShow="TRADES",
        useRTH=True,
        formatDate=1,
    )
    if len(bars) < config.SMA_LONG + 1:
        return None

    df = util.df(bars)
    df["sma_short"] = df["close"].rolling(config.SMA_SHORT).mean()
    df["sma_long"] = df["close"].rolling(config.SMA_LONG).mean()

    prev, last = df.iloc[-2], df.iloc[-1]
    if any(map(lambda x: x != x, [prev.sma_short, prev.sma_long, last.sma_short, last.sma_long])):
        return None  # SMA non ancora disponibile (NaN)

    bullish_cross = prev.sma_short <= prev.sma_long and last.sma_short > last.sma_long
    return {
        "price": last.close,
        "sma_short": last.sma_short,
        "sma_long": last.sma_long,
        "bar_time": str(last.date),
        "bullish_cross": bullish_cross,
    }


def notify(symbol: str, info: dict) -> None:
    price = info["price"]
    target_price = price * (1 + config.TARGET_GAIN_PCT / 100)
    shares_for_capital = config.REFERENCE_CAPITAL_EUR / price

    print("=" * 70)
    print(f"[SEGNALE ACQUISTO] {symbol}  @ {info['bar_time']}")
    print(f"  Prezzo attuale:      {price:.4f} EUR")
    print(f"  SMA{config.SMA_SHORT}: {info['sma_short']:.4f}   SMA{config.SMA_LONG}: {info['sma_long']:.4f}")
    print(f"  Target (+{config.TARGET_GAIN_PCT:.1f}%): {target_price:.4f} EUR")
    print(f"  Su {config.REFERENCE_CAPITAL_EUR:.0f} EUR di capitale (~{shares_for_capital:.2f} azioni) "
          f"=> guadagno stimato {config.TARGET_GAIN_EUR:.2f} EUR se il target viene raggiunto")
    print("  (Nessun ordine è stato inviato: notifica informativa)")
    print("=" * 70)


def run() -> None:
    ib = IB()
    ib.connect(config.IB_HOST, config.IB_PORT, clientId=config.IB_CLIENT_ID)
    ib.reqMarketDataType(3)  # dati delayed se non si dispone di un abbonamento real-time

    contracts = qualify_watchlist(ib)
    if not contracts:
        print("Nessun titolo valido nel watchlist. Controllare config.py.")
        return

    state = load_state()
    print(f"Bot avviato alle {datetime.now().isoformat(timespec='seconds')} "
          f"su {len(contracts)} titoli. Ctrl+C per uscire.")

    try:
        while True:
            for contract in contracts:
                symbol = contract.symbol
                try:
                    info = compute_sma_crossover(ib, contract)
                except Exception as exc:
                    print(f"[ERRORE] {symbol}: {exc}")
                    continue

                if info is None:
                    continue

                if info["bullish_cross"]:
                    last_notified = state.get(symbol)
                    if last_notified != info["bar_time"]:
                        notify(symbol, info)
                        state[symbol] = info["bar_time"]
                        save_state(state)

            ib.sleep(config.POLL_INTERVAL_SECONDS)
    except KeyboardInterrupt:
        print("\nInterrotto dall'utente.")
    finally:
        ib.disconnect()


if __name__ == "__main__":
    run()
