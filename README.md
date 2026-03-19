# Crypto Stream Platform

A learning project that builds a **real-time market data streaming platform** from scratch — ingesting live trade events from Binance, aggregating them into OHLCV windows, persisting and indexing the results, and displaying everything on a live dashboard.

The goal is to practice event-driven microservice architecture, Kafka, Kubernetes, Elasticsearch and Blazor all in one coherent system.

---

## What it does

1. **Connects to Binance WebSocket** and receives a stream of raw trade events (symbol, price, quantity, timestamp) for configured trading pairs.
2. **Publishes every trade to Kafka** so any number of downstream consumers can react independently.
3. **Aggregates trades into 1-minute OHLCV windows** (Open / High / Low / Close / Volume) and persists each window to PostgreSQL. Completed windows are also published back to Kafka.
4. **Indexes aggregated windows into Elasticsearch** to serve as a fast, queryable read model.
5. **Shows a live dashboard** in the browser with KPI cards, Top Gainers / Losers, and a sortable table of all symbols — auto-refreshing every few seconds.
6. **Sends structured application logs to Kibana** so you can filter, search, and analyse service behaviour without `kubectl logs`.

---

## Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                        Binance WebSocket                         					   │
└────────────────────────────┬─────────────────────────────────────┘
                             │  raw trade events
                             ▼
                  ┌─────────────────────┐
                  │  MarketData.Ingestor │  .NET Worker
                  │  (WebSocket client) │  Resilient reconnect
                  └──────────┬──────────┘  Kafka retry / DLQ
                             │
                    topic: raw-market-trades
                             │
                             ▼
                  ┌─────────────────────┐
                  │ MarketAnalytics     	 │  .NET Worker
                  │ Service             	 │  Aggregates trades → OHLCV
                  └──────┬──────────┬──┘  EF Core + Npgsql
                            │            │
               ┌─────────┘          └──────────────┐
               ▼                                   ▼
        ┌────────────┐              topic: aggregated-market-analytics
        │ PostgreSQL 	 │
        │ (OHLCV DB) 	 │                             │
        └────────────┘                             ▼
                                    ┌──────────────────────────┐
                                    │  AnalyticsIndexer.Worker │  .NET Worker
                                    │  (Kafka → Elasticsearch) │
                                    └────────────┬─────────────┘
                                                 │
                                    ┌────────────▼─────────────┐
                                    │      Elasticsearch       │
                                    │  (aggregated windows     │
                                    │   + application logs)    │
                                    └────────────┬─────────────┘
                                                 │
                                    ┌────────────▼─────────────┐
                                    │    Dashboard.Web         │  Blazor Server
                                    │    (live UI)             │  MudBlazor
                                    └──────────────────────────┘

                                    ┌──────────────────────────┐
                                    │  Kibana                  │  Log explorer
                                    │  (structured logs)       │
                                    └──────────────────────────┘
```

---

## Services

| Service | Type | Role |
|---------|------|------|
| **MarketData.Ingestor** | .NET Worker | Connects to Binance WebSocket, publishes `RawMarketTradeEvent` to Kafka. Handles reconnects and Kafka failures with exponential backoff and a dead-letter queue. |
| **MarketAnalytics.Service** | .NET Worker | Consumes raw trades, maintains rolling 1-minute OHLCV windows per symbol, persists to PostgreSQL, publishes aggregated events back to Kafka. |
| **AnalyticsIndexer.Worker** | .NET Worker | Consumes aggregated events and upserts them into an Elasticsearch index — the read model for the dashboard. |
| **Dashboard.Web** | Blazor Server | Live dashboard showing KPI cards (Biggest Gainer / Loser / Most Active), Top Gainers & Losers panels, and a sortable per-symbol table. Data comes from Elasticsearch via `AnalyticsService`. |

## Building Blocks (shared libraries)

| Library | Purpose |
|---------|---------|
| **TradingApp.Contracts** | Shared record types for Kafka messages (`RawMarketTradeEvent`, `AggregatedMarketAnalyticsEvent`). Keeps producer and consumer contracts in sync. |
| **TradingApp.Kafka** | Thin DI-friendly wrapper around `Confluent.Kafka`. Provides `IKafkaProducer<TKey, TValue>` and resilience options. |
| **TradingApp.Elastic** | Elasticsearch client factory shared across services. |

---

## Infrastructure (Kubernetes)

All components run inside a local `kind` cluster. Every service has its own directory under `k8s/`.

| Component | Purpose | Internal address |
|-----------|---------|-----------------|
| **Kafka + ZooKeeper** | Message broker | `kafka:9092` |
| **PostgreSQL** | OHLCV persistence for MarketAnalytics.Service | `postgres:5432` |
| **Elasticsearch** | Full-text index + log sink | `elastic:9200` |
| **Kibana** | Log explorer and Elasticsearch UI | `kibana:5601` → `kibana.localdev:8081` |
| **AKHQ** | Kafka topic/consumer-group browser | `akhq:8080` → `akhq.localdev:8081` |
| **ingress-nginx** | Routes `*.localdev` hostnames into the cluster | — |

---

## Technology stack

| Category | Technology |
|----------|-----------|
| Language / runtime | C# 13, .NET 9 |
| Messaging | Apache Kafka (Confluent.Kafka) |
| Search / logging | Elasticsearch 8, Kibana, Serilog |
| Database | PostgreSQL 15, EF Core 9 + Npgsql |
| UI | Blazor Server, MudBlazor |
| Container orchestration | Kubernetes (kind for local dev) |
| Testing | xUnit, NSubstitute |

---

## Key concepts practiced

- **Event-driven architecture** — services communicate only through Kafka topics; no direct service-to-service calls.
- **CQRS read model** — the write model (PostgreSQL OHLCV) is separate from the read model (Elasticsearch) used by the UI.
- **Resilience patterns** — exponential backoff, jitter, retry topics, and dead-letter queues in the ingestion pipeline.
- **Structured logging** — Serilog writes JSON logs directly to Elasticsearch; Kibana provides search and filtering.
- **Kubernetes** — every service is containerised, configured via ConfigMaps, and deployed with Deployments + Services + Ingress.

---

## Quick start

See **[docs/LOCAL_DEVELOPMENT.md](docs/LOCAL_DEVELOPMENT.md)** for the full step-by-step guide to:
- prerequisites (Docker Desktop, kind, kubectl)
- building and loading Docker images
- deploying all k8s manifests
- accessing the dashboard, AKHQ, and Kibana locally

---

## Project layout

```
crypto-stream-platform/
├── src/
│   ├── BuildingBlocks/
│   │   ├── TradingApp.Contracts/       # shared Kafka event records
│   │   ├── TradingApp.Kafka/           # Kafka producer abstraction
│   │   └── TradingApp.Elastic/         # Elasticsearch client factory
│   ├── Services/
│   │   ├── MarketData.Ingestor/        # Binance WS → Kafka
│   │   ├── MarketAnalytics.Service/    # Kafka → OHLCV → PostgreSQL + Kafka
│   │   ├── AnalyticsIndexer.Worker/    # Kafka → Elasticsearch
│   │   └── Dashboard.Web/             # Blazor live dashboard
│   └── Tests/
│       └── MarketAnalytics.Service.Tests/  # xUnit unit tests
├── k8s/                                # Kubernetes manifests per service
├── docs/
│   └── LOCAL_DEVELOPMENT.md           # setup guide
└── TradingApp.sln
```
