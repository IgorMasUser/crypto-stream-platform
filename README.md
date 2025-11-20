# Crypto Stream Platform

## Solution Layout

- `TradingApp.sln` – root solution containing every project.
- `Directory.Build.props` – shared .NET 9 settings (nullable, implicit usings).
- `src/BuildingBlocks`
  - `TradingApp.Contracts` – shared DTOs/events (e.g., `RawMarketTradeEvent`).
  - `TradingApp.Kafka` – Confluent.Kafka wrapper with DI-ready producer implementation.
  - `TradingApp.Elastic` – placeholder for Elasticsearch client factory.
- `src/Services`
  - `MarketData.Ingestor` – .NET worker streaming Binance trades and producing Kafka events.
  - `MarketAnalytics.Service` – placeholder worker for downstream analytics.
  - `Dashboard.Web` – Blazor Server host for future dashboards.

## Getting Started

1. Ensure the .NET 9 SDK is installed.
2. Restore and build:
   ```bash
   dotnet restore TradingApp.sln
   dotnet build TradingApp.sln
   ```
3. Configure `appsettings.json` files (Kafka, Binance, etc.) before running services.