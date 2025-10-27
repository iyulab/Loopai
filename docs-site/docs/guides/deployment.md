---
title: Deployment
sidebar_position: 3
---


Complete guide for deploying Loopai Edge Runtime using Docker.

---

## 🎯 Overview

Loopai Edge Runtime can be deployed in three modes:
1. **Docker Compose** (recommended for local/dev)
2. **Docker** (single container)
3. **Kubernetes** (production, coming in v0.3)

This guide covers Docker and Docker Compose deployment.

---

## 📋 Prerequisites

- Docker 20.10+ installed
- Docker Compose 2.0+ (for compose deployment)
- Loopai program artifact stored in artifact cache
- Task ID for the runtime

---

## 🚀 Quick Start with Docker Compose

### 1. Set Environment Variables

Create a `.env` file in the project root:

```bash
# Required
LOOPAI_TASK_ID=your-task-id

# Optional (defaults shown)
LOOPAI_DATA_DIR=/loopai-data
```

### 2. Build and Run

```bash
# Build image
docker-compose build

# Start runtime
docker-compose up -d

# View logs
docker-compose logs -f edge-runtime

# Stop runtime
docker-compose down
```

### 3. Verify Deployment

```bash
# Health check
curl http://localhost:8080/health

# Expected response:
# {
#   "status": "healthy",
#   "version": "0.1.0",
#   "task_id": "your-task-id",
#   "active_version": 1
# }
```

---

## 🐳 Docker Deployment (without Compose)

### 1. Build Image

```bash
docker build -t loopai/edge-runtime:latest .
```

### 2. Create Data Volume

```bash
docker volume create loopai-data
```

### 3. Run Container

```bash
docker run -d \
  --name loopai-edge-runtime \
  -p 8080:8080 \
  -e LOOPAI_TASK_ID=your-task-id \
  -e LOOPAI_DATA_DIR=/loopai-data \
  -v loopai-data:/loopai-data \
  --restart unless-stopped \
  loopai/edge-runtime:latest
```

### 4. Monitor Container

```bash
# View logs
docker logs -f loopai-edge-runtime

# Check health
docker exec loopai-edge-runtime curl http://localhost:8080/health

# Inspect container
docker inspect loopai-edge-runtime
```

---

## 🔧 Configuration

### Environment Variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `LOOPAI_TASK_ID` | Yes | - | Task identifier for this runtime |
| `LOOPAI_DATA_DIR` | No | `/loopai-data` | Data directory path |
| `PYTHONUNBUFFERED` | No | `1` | Python output buffering |

### Volume Mounts

| Container Path | Purpose | Required |
|----------------|---------|----------|
| `/loopai-data` | Dataset storage, artifacts, config | Yes |

**Volume Structure**:
```
/loopai-data/
├── datasets/
│   └── {task_id}/
│       ├── executions/YYYY-MM-DD.jsonl
│       ├── validations/sampled-YYYY-MM-DD.jsonl
│       └── analytics/daily-stats-YYYY-MM-DD.json
├── artifacts/
│   └── {task_id}/
│       ├── v1/program.py, metadata.json
│       └── active -> v2/
└── config/
    └── deployment.yaml
```

### Port Mapping

| Container Port | Host Port | Protocol | Service |
|----------------|-----------|----------|---------|
| 8080 | 8080 | HTTP | REST API |

---

## 🔍 API Endpoints

### POST /execute

Execute program with input data.

**Request**:
```bash
curl -X POST http://localhost:8080/execute \
  -H "Content-Type: application/json" \
  -d '{"input": {"text": "Buy now!"}}'
```

**Response**:
```json
{
  "output": "spam",
  "latency_ms": 4.2
}
```

### GET /health

Runtime health status.

**Request**:
```bash
curl http://localhost:8080/health
```

**Response**:
```json
{
  "status": "healthy",
  "version": "0.1.0",
  "task_id": "test-task",
  "active_version": 1
}
```

### GET /metrics

Execution statistics for today.

**Request**:
```bash
curl http://localhost:8080/metrics
```

**Response**:
```json
{
  "executions_today": 42,
  "avg_latency_ms": 5.3
}
```

---

## 🛠️ Troubleshooting

### Container Won't Start

**Check logs**:
```bash
docker logs loopai-edge-runtime
```

**Common issues**:
- Missing `LOOPAI_TASK_ID` environment variable
- No active artifact for task ID
- Volume mount permission issues

### Health Check Failing

**Verify artifact**:
```bash
docker exec loopai-edge-runtime ls -la /loopai-data/artifacts/{task_id}/
```

**Check active version**:
```bash
docker exec loopai-edge-runtime cat /loopai-data/artifacts/{task_id}/.active_version
```

### Performance Issues

**Check resource usage**:
```bash
docker stats loopai-edge-runtime
```

**Increase memory limit** (if needed):
```bash
docker run -d \
  --memory=512m \
  --cpus=2 \
  ...
```

---

## 📊 Monitoring

### Log Collection

**Follow logs in real-time**:
```bash
docker-compose logs -f edge-runtime
```

**Export logs**:
```bash
docker logs loopai-edge-runtime > runtime.log 2>&1
```

### Health Checks

Docker health checks run every 30 seconds:
```bash
# View health status
docker inspect --format='{{json .State.Health}}' loopai-edge-runtime | jq
```

### Execution Logs

Access JSONL execution logs:
```bash
# Enter container
docker exec -it loopai-edge-runtime bash

# View today's executions
cat /loopai-data/datasets/{task_id}/executions/$(date +%Y-%m-%d).jsonl
```

---

## 🔄 Updates and Maintenance

### Update Runtime Image

```bash
# Pull latest image
docker pull loopai/edge-runtime:latest

# Stop current container
docker-compose down

# Start with new image
docker-compose up -d
```

### Backup Data

```bash
# Backup volume
docker run --rm -v loopai-data:/data -v $(pwd):/backup \
  ubuntu tar czf /backup/loopai-data-backup.tar.gz /data
```

### Restore Data

```bash
# Restore volume
docker run --rm -v loopai-data:/data -v $(pwd):/backup \
  ubuntu tar xzf /backup/loopai-data-backup.tar.gz -C /
```

### Clean Up Old Logs

The Dataset Manager automatically enforces retention policies. Default: 7 days.

**Manual cleanup**:
```bash
docker exec loopai-edge-runtime find /loopai-data/datasets -name "*.jsonl" -mtime +7 -delete
```

---

## 🔐 Security

### Non-Root User

Runtime runs as user `loopai` (UID 1000), not root.

### Network Isolation

Use Docker networks for isolation:
```yaml
networks:
  loopai-network:
    driver: bridge
```

### Secrets Management

**Never commit secrets to git!**

Use environment variables or Docker secrets:
```bash
# Using .env file (git-ignored)
LOOPAI_TASK_ID=secret-task-id

# Using Docker secrets (Swarm mode)
docker secret create loopai_task_id task_id.txt
```

---

## 🎯 Production Deployment

### Recommended Configuration

```yaml
version: '3.8'

services:
  edge-runtime:
    image: loopai/edge-runtime:latest
    restart: always
    environment:
      - LOOPAI_DATA_DIR=/loopai-data
      - LOOPAI_TASK_ID=${LOOPAI_TASK_ID}
    volumes:
      - loopai-data:/loopai-data
    ports:
      - "8080:8080"
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 1G
        reservations:
          cpus: '1'
          memory: 512M
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 5s
      retries: 3
      start_period: 10s
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
```

### Horizontal Scaling

For multiple task IDs, run separate containers:

```bash
# Runtime for task-1
docker run -d --name runtime-task1 \
  -e LOOPAI_TASK_ID=task-1 \
  -p 8081:8080 \
  loopai/edge-runtime:latest

# Runtime for task-2
docker run -d --name runtime-task2 \
  -e LOOPAI_TASK_ID=task-2 \
  -p 8082:8080 \
  loopai/edge-runtime:latest
```

### Reverse Proxy (Nginx)

```nginx
upstream loopai_runtime {
    server localhost:8080;
}

server {
    listen 80;
    server_name api.example.com;

    location / {
        proxy_pass http://loopai_runtime;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

---

## 📈 Performance Tuning

### Uvicorn Workers

Increase workers for better throughput:

**Custom Dockerfile CMD**:
```dockerfile
CMD ["uvicorn", "loopai.runtime.main:app", \
     "--host", "0.0.0.0", \
     "--port", "8080", \
     "--workers", "4"]
```

### Resource Limits

**CPU optimization**:
```bash
docker run --cpus=2 ...
```

**Memory optimization**:
```bash
docker run --memory=1g --memory-swap=2g ...
```

---

## ✅ Testing Deployment

### Smoke Test

```bash
#!/bin/bash

# Health check
curl -f http://localhost:8080/health || exit 1

# Execute test
curl -X POST http://localhost:8080/execute \
  -H "Content-Type: application/json" \
  -d '{"input": {"text": "Test message"}}' || exit 1

# Metrics check
curl -f http://localhost:8080/metrics || exit 1

echo "✅ All checks passed"
```

### Load Test (with Apache Bench)

```bash
# 1000 requests, 10 concurrent
ab -n 1000 -c 10 -p post.json -T application/json \
  http://localhost:8080/execute
```

---

## 🆘 Support

- **Issues**: https://github.com/iyulab/loopai/issues
- **Discussions**: https://github.com/iyulab/loopai/discussions
- **Documentation**: https://docs.loopai.dev

---

**Last Updated**: 2025-10-26
**Version**: Phase 3.5 (Docker Deployment)
