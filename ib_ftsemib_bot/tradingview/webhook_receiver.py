"""
Ricevitore locale per gli alert webhook di TradingView.

Espone un endpoint HTTP: quando un alert di TradingView scatta (vedi
sma_cross_alert.pine), TradingView invia una richiesta POST con il testo
del messaggio. Questo script la riceve e la stampa in console, come
faceva il bot basato su Interactive Brokers.

TradingView non può raggiungere "localhost": serve un tunnel pubblico
(es. ngrok) puntato su questa porta. Vedi README.md per i dettagli.
"""

from datetime import datetime

from flask import Flask, request

app = Flask(__name__)
PORT = 5001


@app.route("/tv-webhook", methods=["POST"])
def tv_webhook():
    payload = request.get_data(as_text=True)
    now = datetime.now().isoformat(timespec="seconds")

    print("=" * 70)
    print(f"[SEGNALE DA TRADINGVIEW] ricevuto alle {now}")
    print(payload)
    print("=" * 70)

    return "", 200


if __name__ == "__main__":
    print(f"In ascolto su http://0.0.0.0:{PORT}/tv-webhook ... Ctrl+C per uscire.")
    app.run(host="0.0.0.0", port=PORT)
