# Local Development Guide

This guide walks you through running the full **Crypto Stream Platform** stack locally using a `kind` Kubernetes cluster.

---

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | ≥ 4.x | Container runtime |
| [kind](https://kind.sigs.k8s.io/docs/user/quick-start/#installation) | ≥ 0.20 | Local Kubernetes cluster |
| [kubectl](https://kubernetes.io/docs/tasks/tools/) | ≥ 1.28 | Cluster management |
| [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9) | 9.x | Build .NET services |

Add to `C:\Windows\System32\drivers\etc\hosts`:
```
127.0.0.1   akhq.localdev
127.0.0.1   dashboard.localdev
127.0.0.1   kibana.localdev
```

---

## 1. Create the kind cluster

```bash
kind create cluster --name dev-cluster
```

---

## 2. Install ingress-nginx

```bash
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.11.1/deploy/static/provider/kind/deploy.yaml
kubectl label node dev-cluster-control-plane ingress-ready=true
```

Wait until the controller pod is ready:
```bash
kubectl wait --namespace ingress-nginx \
  --for=condition=ready pod \
  --selector=app.kubernetes.io/component=controller \
  --timeout=90s
```

---

## 3. Deploy infrastructure

```bash
# Kafka + ZooKeeper
kubectl apply -f k8s/kafka/

# PostgreSQL
kubectl apply -f k8s/postgres/

# Elasticsearch
kubectl apply -f k8s/elastic/

# Kibana
kubectl apply -f k8s/kibana/

# AKHQ (Kafka UI)
kubectl apply -f k8s/akhq/
```

Wait for Elasticsearch to be healthy (can take ~60 s):
```bash
kubectl rollout status deployment/elastic
```

---

## 4. Build and load Docker images

Run from the **repo root** so that all project references are available:

```bash
docker build -t trading/marketdata-ingestor:dev      -f src/Services/MarketData.Ingestor/Dockerfile .
docker build -t trading/marketanalytics-service:dev  -f src/Services/MarketAnalytics.Service/Dockerfile .
docker build -t trading/analytics-indexer-worker:dev -f src/Services/AnalyticsIndexer.Worker/Dockerfile .
docker build -t trading/dashboard-web:dev            -f src/Services/Dashboard.Web/Dockerfile .
```

Load into kind:
```bash
kind load docker-image trading/marketdata-ingestor:dev      --name dev-cluster
kind load docker-image trading/marketanalytics-service:dev  --name dev-cluster
kind load docker-image trading/analytics-indexer-worker:dev --name dev-cluster
kind load docker-image trading/dashboard-web:dev            --name dev-cluster
```

---

## 5. Deploy services

```bash
kubectl apply -f k8s/marketdata-ingestor/
kubectl apply -f k8s/marketanalytics-service/
kubectl apply -f k8s/analytics-indexer-worker/
kubectl apply -f k8s/dashboard-web/
```

Verify all pods are running:
```bash
kubectl get pods
```

---

## 6. Bridge localhost to the cluster

Kind runs inside Docker, so traffic from your browser needs to reach the ingress controller.

Get the kind node IP:
```bash
docker inspect -f "{{range.NetworkSettings.Networks}}{{.IPAddress}}{{end}}" dev-cluster-control-plane
# example output: 172.18.0.2
```

Start the socat proxy (replace `172.18.0.2` with your actual IP):
```bash
docker run -d --name kind-proxy --restart=always \
  -p 8081:8080 --network kind alpine/socat \
  TCP-LISTEN:8080,fork TCP:172.18.0.2:30080
```

---

## 7. Access the UIs

| UI | URL |
|----|-----|
| Dashboard | http://dashboard.localdev:8081 |
| AKHQ (Kafka) | http://akhq.localdev:8081/ui |
| Kibana | http://kibana.localdev:8081 |

---

## After a system restart

Kind and Docker Desktop stop on reboot. To restore everything:

```bash
# 1. Start Docker Desktop (wait until it's ready)

# 2. Get the new kind node IP
docker inspect -f "{{range.NetworkSettings.Networks}}{{.IPAddress}}{{end}}" dev-cluster-control-plane

# 3. Restart the socat proxy with the new IP
docker rm -f kind-proxy
docker run -d --name kind-proxy --restart=always \
  -p 8081:8080 --network kind alpine/socat \
  TCP-LISTEN:8080,fork TCP:172.18.0.x:30080

# 4. Check that all pods are running
kubectl get pods
```

---

## Updating a service

After changing code, rebuild and redeploy without downtime:

```bash
# rebuild
docker build --no-cache -t trading/<service>:dev -f src/Services/<Service>/Dockerfile .

# reload into kind
kind load docker-image trading/<service>:dev --name dev-cluster

# restart the pod to pick up the new image
kubectl delete pod -l app=<service> --force --grace-period=0
```

---

## PostgreSQL — local access

The PostgreSQL service is exposed as a NodePort. Use `kubectl port-forward` for direct access:

```bash
kubectl port-forward service/postgres 5432:5432
```

Connection details:
- **Host**: `localhost`
- **Port**: `5432`
- **Database**: `marketanalytics`
- **User / Password**: `postgres` / `postgres`

---

## Kibana — viewing application logs

Application logs from `MarketAnalytics.Service` are shipped to Elasticsearch via Serilog.

1. Open Kibana → `☰` → **Analytics** → **Discover**
2. Create a Data View:
   - **Index pattern**: `tradingapp-logs-*`
   - **Timestamp field**: `@timestamp`
3. Set time range to **Last 1 hour** to see recent events.

Useful filter fields: `level`, `message`, `service`, `fields.Symbol`.

---

## Running unit tests

```bash
dotnet test src/Tests/MarketAnalytics.Service.Tests/MarketAnalytics.Service.Tests.csproj
```

---

## Kafka smoke test

Verify end-to-end trade delivery with a temporary consumer pod:

```bash
kubectl run kafka-tools --image=confluentinc/cp-kafka:7.3.2 --restart=Never -- sleep 3600
kubectl exec -it kafka-tools -- \
  kafka-console-consumer --bootstrap-server kafka:9092 \
  --topic raw-market-trades --from-beginning --max-messages 5
kubectl delete pod kafka-tools
```
