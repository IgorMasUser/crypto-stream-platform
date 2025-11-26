# Local Kafka Stack

These manifests provide a development-only, single-node Kafka + Zookeeper stack for the `kind-dev-cluster`.

## Deploy

```bash
kubectl apply -f k8s/kafka/zookeeper.yaml
kubectl apply -f k8s/kafka/kafka.yaml
```

Wait until both pods are `Running`:

```bash
kubectl get pods -l app=zookeeper
kubectl get pods -l app=kafka
```

## Tear down

```bash
kubectl delete -f k8s/kafka/kafka.yaml
kubectl delete -f k8s/kafka/zookeeper.yaml
```

## Notes

- The services expose `zookeeper:2181` and `kafka:9092` inside the cluster. `MarketData.Ingestor` already uses `kafka:9092`.
- Storage is ephemeral (`emptyDir`); delete/redeploy to reset state.
- Images are pinned to Confluent's `cp-zookeeper:7.3.2` and `cp-kafka:7.3.2`. Update tags as newer releases become available.
- These images require the cluster nodes to reach Docker Hub (or a Confluent mirror). For air-gapped setups, preload or mirror the images.

