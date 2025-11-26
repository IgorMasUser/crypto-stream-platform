# Crypto Stream Platform

## Solution Layout

- `TradingApp.sln` – root solution containing every project.
- `Directory.Build.props` – shared .NET 9 settings (nullable, implicit usings).
- `src/BuildingBlocks`
  - `TradingApp.Contracts` – shared DTOs/events (e.g., `RawMarketTradeEvent`).
  - `TradingApp.Kafka` – Confluent.Kafka wrapper with DI-ready producer implementation.
  - `TradingApp.Elastic` – placeholder for Elasticsearch client factory.
- `src/Services`
  - `MarketData.Ingestor` – .NET worker streaming Binance trades and producing Kafka events with resilient reconnect + Kafka retry policies.
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

## Configuration Highlights

- `Kafka.Resilience` – enables exponential backoff retries when Kafka is temporarily unavailable (tunable attempts/delays/jitter). After the retry budget is exhausted the event is redirected to retry/DLQ topics so the worker keeps running.
- `Binance` – includes reconnect/backoff settings (`ReconnectDelaySeconds`, `MaxReconnectDelaySeconds`, `ReconnectJitterSeconds`) to smooth out websocket reconnect storms.
- `Ingestor.KafkaTopic` / `RetryTopic` / `DeadLetterTopic` – primary, retry, and DLQ Kafka topics used by the ingestion pipeline when transient or fatal failures occur.

## Containerizing MarketData.Ingestor

1. Build the Docker image (from the repo root so that all project references are available):
   ```bash
   docker build -t trading/marketdata-ingestor:dev -f src/Services/MarketData.Ingestor/Dockerfile .
   ```
   > If you prefer using the service folder as the build context, the same Dockerfile also works with:
   > `docker build -t trading/marketdata-ingestor:dev src/Services/MarketData.Ingestor`
2. Load the image into your local cluster (example with kind):
   ```bash
   kind load docker-image trading/marketdata-ingestor:dev
   ```
3. Deploy the manifests (ConfigMap + Deployment + Service):
   ```bash
   kubectl apply -f k8s/marketdata-ingestor/
   ```
   The default `ConfigMap` provides placeholder settings (`kafka:9092`, several Binance symbols); update `k8s/marketdata-ingestor/configmap.yaml` before applying if your environment differs.
4. Update or remove the deployment by reapplying or deleting the same manifest directory:
   ```bash
   kubectl delete -f k8s/marketdata-ingestor/
   ```