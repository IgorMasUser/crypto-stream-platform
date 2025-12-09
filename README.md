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
- `AnalyticsIndexer.Worker` – scalable consumer for `aggregated-market-analytics` that will index aggregated events into Elastic (CQRS read model for the dashboard).

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
   The default `ConfigMap` enables Kafka (`KAFKA__ENABLED=true`), points to the in-cluster broker (`kafka:9092`), and turns on the sample publisher (`KAFKA__PUBLISHSAMPLEONSTARTUP=true`) so you can verify end-to-end delivery. Update `k8s/marketdata-ingestor/configmap.yaml` before applying if your environment differs.
4. Update or remove the deployment by reapplying or deleting the same manifest directory:
   ```bash
   kubectl delete -f k8s/marketdata-ingestor/
   ```

## Local Kafka Stack

To test end-to-end ingestion you can spin up a single-broker Kafka cluster (with ZooKeeper) inside the same Kubernetes cluster:

```bash
# deploy
kubectl apply -f k8s/kafka/zookeeper.yaml
kubectl apply -f k8s/kafka/kafka.yaml

# verify
kubectl get pods -l app=zookeeper
kubectl get pods -l app=kafka
```

The Kafka service is exposed as `kafka:9092`, which matches the default `MarketData.Ingestor` configuration. When you're done, remove the stack:

```bash
kubectl delete -f k8s/kafka/kafka.yaml
kubectl delete -f k8s/kafka/zookeeper.yaml
```

## Observability with AKHQ

AKHQ gives you a browser-based view of topics, partitions, and consumer groups. This repo ships lightweight manifests under `k8s/akhq/`:

- **Deployment** (`k8s/akhq/deployment.yaml`) – sets `AKHQ_CONFIGURATION` so AKHQ’s “local” cluster points at `kafka:9092`. If you rename the Kafka service, update the `bootstrap.servers` value here.
- **Service** (`k8s/akhq/service.yaml`) – exposes the AKHQ pod inside the cluster on port 8080.
- **Ingress** (`k8s/akhq/ingress.yaml`) – routes `Host: akhq.localdev` to the AKHQ service via the `nginx` ingress class.

### Installing ingress-nginx

For kind-based clusters run:
```bash
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.11.1/deploy/static/provider/kind/deploy.yaml
kubectl label node dev-cluster-control-plane ingress-ready=true   # once per cluster
```
This installs/labels the NGINX controller so it can host the AKHQ ingress (and future ones).

### Bridging the ingress to localhost

Kind runs inside Docker, so we forward a local port into the cluster:

1. Patch the ingress service to use fixed NodePorts (`80→30080`, `443→30443`). The repo already includes that patch—reapplying `k8s/ingress-nginx` or the above script keeps the ports stable.
2. Run a tiny socat bridge so `localhost:8081` reaches the controller:
   ```bash
   docker run -d --name akhq-proxy --restart=always \
     -p 8081:8080 --network kind alpine/socat \
     TCP-LISTEN:8080,fork TCP:172.18.0.2:30080
   ```
   (Replace `172.18.0.2` with the IP of your `dev-cluster-control-plane` container: `docker inspect -f '{{range.NetworkSettings.Networks}}{{.IPAddress}}{{end}}' dev-cluster-control-plane`.)
3. Add a hosts entry so the browser resolves the hostname:
   ```
   127.0.0.1   akhq.localdev
   ```

Now AKHQ is permanently reachable at `http://akhq.localdev:8081/ui` without `kubectl port-forward`. Remove the helper container with `docker rm -f akhq-proxy` if you no longer need the tunnel.

### How to restore akhq after system restart
1. docker inspect -f "{{range.NetworkSettings.Networks}}{{.IPAddress}}{{end}}" dev-cluster-control-plane
2. docker rm -f akhq-proxy
3. docker run -d --name akhq-proxy --restart=always -p 8081:8080 --network kind alpine/socat TCP-LISTEN:8080,fork TCP:172.18.0.x:30080
4. kubectl get ingress
5. kubectl get nodes --show-labels | findstr ingress-ready

## Quick Kafka Smoke Test

When `Kafka:PublishSampleOnStartup` (or `KAFKA__PUBLISHSAMPLEONSTARTUP`) is `true`, the `MarketData.Ingestor` worker automatically publishes a single `RawMarketTradeEvent` through Kafka on startup (`TestTradePublisherHostedService`). You can confirm end-to-end delivery by running a temporary toolbox pod:

```bash
kubectl run kafka-tools --image=confluentinc/cp-kafka:7.3.2 --restart=Never -- sleep 3600
kubectl exec -it kafka-tools -- kafka-console-consumer --bootstrap-server kafka:9092 --topic raw-market-trades --from-beginning --max-messages 1
```

You should see the sample trade emitted by the worker. Delete the toolbox pod afterwards with `kubectl delete pod kafka-tools`.