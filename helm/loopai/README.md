# Loopai Helm Chart

Helm chart for deploying Loopai - a program synthesis and execution framework.

## Prerequisites

- Kubernetes 1.20+
- Helm 3.0+
- SQL Server instance (for persistent storage)
- Redis instance (optional, for caching)

## Installation

### Add the chart repository (if published)

```bash
helm repo add loopai https://charts.loopai.dev
helm repo update
```

### Install from local directory

```bash
helm install loopai ./helm/loopai \
  --namespace loopai \
  --create-namespace \
  --set database.host=your-sqlserver-host \
  --set database.existingSecret=loopai-db-secret \
  --set redis.existingSecret=loopai-redis-secret
```

## Configuration

The following table lists the configurable parameters and their default values.

### Core Configuration

| Parameter | Description | Default |
|-----------|-------------|---------|
| `replicaCount` | Number of replicas | `2` |
| `image.repository` | Image repository | `loopai/cloudapi` |
| `image.tag` | Image tag | `1.0.0` |
| `image.pullPolicy` | Image pull policy | `IfNotPresent` |

### Database Configuration

| Parameter | Description | Default |
|-----------|-------------|---------|
| `database.host` | Database host | `loopai-sqlserver` |
| `database.port` | Database port | `1433` |
| `database.name` | Database name | `LoopaiDb` |
| `database.user` | Database user | `sa` |
| `database.existingSecret` | Secret containing DB password | `loopai-db-secret` |
| `database.existingSecretKey` | Key in secret for password | `password` |

### Redis Configuration

| Parameter | Description | Default |
|-----------|-------------|---------|
| `redis.enabled` | Enable Redis caching | `true` |
| `redis.host` | Redis host | `loopai-redis-master` |
| `redis.port` | Redis port | `6379` |
| `redis.existingSecret` | Secret containing Redis password | `loopai-redis-secret` |
| `redis.existingSecretKey` | Key in secret for password | `password` |

### Autoscaling

| Parameter | Description | Default |
|-----------|-------------|---------|
| `autoscaling.enabled` | Enable HPA | `true` |
| `autoscaling.minReplicas` | Minimum replicas | `2` |
| `autoscaling.maxReplicas` | Maximum replicas | `10` |
| `autoscaling.targetCPUUtilizationPercentage` | Target CPU % | `70` |
| `autoscaling.targetMemoryUtilizationPercentage` | Target memory % | `80` |

### Resources

| Parameter | Description | Default |
|-----------|-------------|---------|
| `resources.limits.cpu` | CPU limit | `1000m` |
| `resources.limits.memory` | Memory limit | `1Gi` |
| `resources.requests.cpu` | CPU request | `500m` |
| `resources.requests.memory` | Memory request | `512Mi` |

### Observability

| Parameter | Description | Default |
|-----------|-------------|---------|
| `opentelemetry.enabled` | Enable OpenTelemetry | `true` |
| `opentelemetry.otlpEndpoint` | OTLP endpoint | `http://otel-collector:4317` |
| `serviceMonitor.enabled` | Create ServiceMonitor | `false` |
| `serviceMonitor.interval` | Scrape interval | `30s` |

### Rate Limiting

| Parameter | Description | Default |
|-----------|-------------|---------|
| `rateLimiting.enabled` | Enable rate limiting | `true` |
| `rateLimiting.generalLimitPerMinute` | General requests/min | `60` |
| `rateLimiting.generalLimitPerHour` | General requests/hour | `1000` |
| `rateLimiting.executeLimitPerMinute` | Execute requests/min | `30` |
| `rateLimiting.executeLimitPerHour` | Execute requests/hour | `500` |

## Creating Required Secrets

Before installing, create the required secrets:

```bash
# Database secret
kubectl create secret generic loopai-db-secret \
  --from-literal=password='your-db-password' \
  --namespace loopai

# Redis secret (if enabled)
kubectl create secret generic loopai-redis-secret \
  --from-literal=password='your-redis-password' \
  --namespace loopai
```

## Upgrade

```bash
helm upgrade loopai ./helm/loopai \
  --namespace loopai \
  --reuse-values
```

## Uninstall

```bash
helm uninstall loopai --namespace loopai
```

## Monitoring

### Prometheus Metrics

Metrics are exposed at `/metrics` endpoint on port 8080:

- `loopai_program_executions_total` - Total program executions
- `loopai_program_execution_duration_seconds` - Execution duration
- `loopai_validation_checks_total` - Validation checks performed
- `loopai_program_improvements_total` - Program improvements
- `loopai_abtest_runs_total` - A/B test runs
- `loopai_canary_deployments_total` - Canary deployments
- `loopai_sampling_operations_total` - Sampling operations

### Health Checks

- **Liveness**: `/health` - Basic health check
- **Readiness**: `/health/ready` - Ready to serve traffic

### Logs

View logs using kubectl:

```bash
kubectl logs -f -l app.kubernetes.io/name=loopai --namespace loopai
```

## Production Considerations

1. **Security**:
   - Store secrets in a secure secret manager (Azure Key Vault, AWS Secrets Manager, etc.)
   - Enable network policies to restrict pod communication
   - Use TLS for ingress traffic

2. **High Availability**:
   - Keep `replicaCount` ≥ 2
   - Configure pod anti-affinity (already included)
   - Enable PodDisruptionBudget (already included)

3. **Monitoring**:
   - Enable ServiceMonitor for Prometheus Operator
   - Configure alerting rules for critical metrics
   - Set up log aggregation (ELK, Loki, etc.)

4. **Resource Tuning**:
   - Adjust resource limits based on workload
   - Monitor HPA metrics and adjust thresholds
   - Consider vertical pod autoscaling for stable workloads

## Support

For issues and questions:
- GitHub: https://github.com/your-org/loopai
- Email: support@loopai.dev
