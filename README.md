<div align="center">

Topics: metatrader5, mql5, smart-money-concepts, expert-advisor, mql4, metatrader, forex-trading, indicators, technical-analysis, smart-money, order-blocks, multi-timeframe, market-structure, mt4, mt5, trading-bot, mt5-confluence-matrix, multi-timeframe-confluence, mt5-signal-scoring

# Confluence Signal Matrix

**An AI-style market structure analyzer for MetaTrader 5 workflows. The app evaluates multiple assets across M15, H1, H4, and D1 timeframes, then ranks confluence, confidence, bias, liquidity sweeps, break-of-structure events, and institutional signal quality in a polished desktop matrix.**

<br>

[![Stars](https://img.shields.io/badge/Stars-Repository-00D4AA?style=for-the-badge)](https://github.com/your-username/volume-profile-mt5/stargazers)
[![Forks](https://img.shields.io/badge/Forks-Community-4D9FFF?style=for-the-badge)](https://github.com/your-username/volume-profile-mt5/network)
[![Issues](https://img.shields.io/badge/Issues-Tracker-FF4D6A?style=for-the-badge)](https://github.com/your-username/volume-profile-mt5/issues)
[![Platform](https://img.shields.io/badge/Platform-MetaTrader%205-00D4AA?style=for-the-badge)](https://www.metatrader5.com)
[![License](https://img.shields.io/badge/License-MIT-4D9FFF?style=for-the-badge)](LICENSE)



---

## Screenshot
<img width="1406" height="839" alt="Screenshot_2" src="https://github.com/user-attachments/assets/f13a9d6a-1eee-4e2e-8df5-22e5328ab1e3" />



## 🎬 Demo

<div align="center">

<img src="" alt="Demo">

</div>


## Why This Project

Single-timeframe signals are noisy. **Confluence Signal Matrix** focuses on alignment: when higher and lower timeframes agree, structure quality improves, and the dashboard can rank the best market opportunities first.

It is designed for:

- Multi-timeframe trading analysis demos
- MT5 market structure dashboards
- Forex, gold, index, and crypto signal tools
- Liquidity sweep and break-of-structure visualizations
- Professional .NET WinForms portfolio projects

---

## What It Does

| Module | Description |
|---|---|
| MT5 Signal Bridge | Connects UI state to a WebSocket bridge model |
| Confluence Engine | Scores directional alignment and market structure quality |
| Matrix Grid | Shows each asset across M15, H1, H4, and D1 |
| Confidence Metrics | Displays dominant read, average confidence, and coverage |
| Intelligence Feed | Writes a narrative stream for the highest ranked assets |
| Synthetic Pulse | Simulates market updates when a live bridge is not connected |
| Account Gate | Requires MT5 ID and password before active evaluation |

---

## Feature Highlights

| Feature | Detail |
|---|---|
| Multi-Timeframe Matrix | Evaluates seven symbols across four timeframes |
| Weighted Alignment | Gives higher weight to H4 and D1 structure |
| Liquidity Sweep Flags | Adds quality when sweeps appear in timeframe data |
| Break-of-Structure Flags | Improves confluence when market structure confirms direction |
| Order Block Quality | Uses a 0 to 1 score as part of the structure rating |
| Color-Coded Cells | Highlights bullish, bearish, watch, and pending readings |
| AI-Style Narrative | Converts raw metrics into readable market commentary |
| Synthetic Market Pulse | Generates realistic demo movement for videos and screenshots |

---

## Supported Assets

```text
FX       EURUSD, GBPUSD, USDJPY
Metals   XAUUSD
Indices  US30, NAS100
Crypto   BTCUSD
```

Supported timeframes:

```text
M15, H1, H4, D1
```

---

## Signal Engine

```text
Timeframe readings
   |
   v
Directional weight
   |
   v
Structure quality
   |
   v
Confluence score
   |
   v
Confidence + signal text + status
```

The engine scores each asset using:

| Factor | Role |
|---|---|
| Bias Direction | Bullish, bearish, or neutral alignment |
| Timeframe Weight | Higher frames receive stronger influence |
| Liquidity Sweep | Adds context for potential stop runs |
| Break of Structure | Confirms directional shift |
| Volume Delta | Adds participation pressure |
| Order Block Quality | Adds zone quality to the final reading |

---

## Signal States

```text
SECURE   -> Strong confluence and high structure quality
WATCH    -> Directional alignment is building
PENDING  -> Setup exists but needs more confirmation
NO TRADE -> Market is balanced or unclear
```

Example output:

```text
EURUSD: LONG / SECURE / 87%
3/4 frames aligned, 2 liquidity sweeps, 3 break-of-structure prints.
```

---

## MT5 Payload Format

Example WebSocket payload:

```json
{
  "symbol": "EURUSD",
  "timeframe": "H1",
  "price": 1.0852,
  "bias": "bullish",
  "liquiditySweep": true,
  "breakOfStructure": true,
  "volumeDelta": 0.42,
  "orderBlockQuality": 0.78
}
```

---

## Quick Start

**Requirements:**

- Windows 10 or Windows 11
- .NET 8 SDK
- Visual Studio 2022

```bash
git clone https://github.com/your-username/confluence-signal-matrix.git
cd confluence-signal-matrix
```

Open `WinFormsApp5.slnx` in Visual Studio and press **F5**.

---

## How to Use

1. Launch the application.
2. Enter MT5 ID and password.
3. Click **ACTIVATE SYSTEM**.
4. Click **RUN MATRIX** to refresh evaluation.
5. Click **SIMULATE PULSE** for demo market movement.
6. Feed live MT5 WebSocket payloads into the bridge for real updates.
7. Review ranked signals and the AI structure feed.

---

## Roadmap

- [x] Confluence scoring engine
- [x] Multi-timeframe matrix grid
- [x] Synthetic market pulse
- [x] Intelligence feed narrative
- [ ] Add live endpoint configuration
- [ ] Add strategy preset filters
- [ ] Add alert threshold rules
- [ ] Add exportable signal reports

---

## License

MIT

---

<div align="center">

Confluence Signal Matrix - AI-Style MT5 Market Structure Analyzer

</div>
