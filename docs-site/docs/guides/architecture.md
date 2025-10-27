---
title: Architecture
sidebar_position: 2
---


**Program Synthesis and Execution Framework - Infrastructure Middleware**

**Version**: 0.2.0
**Status**: Production-Ready (Phase 5 Complete)
**Last Updated**: October 26, 2025

---

## 🎯 Architecture Vision

### Core Principle
**"Build Once, Run Anywhere"** - Generate portable program artifacts that execute in any environment with complete data sovereignty.

### Key Capabilities
- ✅ Generate programs from natural language specifications
- ✅ Deploy to cloud API OR customer infrastructure
- ✅ Local dataset management with file system storage
- ✅ Privacy-aware selective telemetry
- ✅ Real-time monitoring and continuous improvement
- ✅ &lt;10ms execution latency anywhere

---

## 🏗️ High-Level Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                   LOOPAI CLOUD PLATFORM                       │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐   │
│  │   Program    │  │   Artifact   │  │   Improvement   │   │
│  │  Generator   │  │  Repository  │  │     Engine      │   │
│  │              │  │              │  │                 │   │
│  │ • LLM calls  │  │ • Versioning │  │ • Analysis      │   │
│  │ • Synthesis  │  │ • Storage    │  │ • Regeneration  │   │
│  └──────────────┘  └──────────────┘  └─────────────────┘   │
│                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐   │
│  │  Telemetry   │  │   SignalR    │  │   Central API   │   │
│  │  Collector   │  │     Hub      │  │   (Optional)    │   │
│  │              │  │              │  │                 │   │
│  │ • Sampling   │  │ • Real-time  │  │ • Cloud exec    │   │
│  │ • Privacy    │  │ • Monitoring │  │ • For dev/test  │   │
│  └──────────────┘  └──────────────┘  └─────────────────┘   │
│                                                               │
└────────────┬──────────────────────────────────────────────────┘
             │
             │ Artifacts, Updates, Telemetry
             ▼
┌──────────────────────────────────────────────────────────────┐
│              LOOPAI EDGE RUNTIME                              │
│              (Customer Infrastructure)                        │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐   │
│  │  Artifact    │  │   Program    │  │    Dataset      │   │
│  │   Cache      │  │   Executor   │  │    Manager      │   │
│  │              │  │              │  │                 │   │
│  │ • Download   │  │ • &lt;10ms exec │  │ • JSONL logs    │   │
│  │ • Versioning │  │ • Sandboxed  │  │ • Analytics     │   │
│  └──────────────┘  └──────────────┘  └─────────────────┘   │
│                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐   │
│  │   Sampling   │  │     Local    │  │  Configuration  │   │
│  │  & Telemetry │  │   Validator  │  │     Manager     │   │
│  │              │  │              │  │                 │   │
│  │ • Privacy    │  │ • Oracle     │  │ • Deployment    │   │
│  │ • Upload     │  │ • Compare    │  │ • Privacy mode  │   │
│  └──────────────┘  └──────────────┘  └─────────────────┘   │
│                                                               │
└────────────┬──────────────────────────────────────────────────┘
             │
             │ All customer data stays local
             ▼
┌──────────────────────────────────────────────────────────────┐
│             LOCAL FILESYSTEM STORAGE                          │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  /loopai-data/                                               │
│  ├── datasets/                                               │
│  │   ├── task-123/                                          │
│  │   │   ├── executions/2025-10-26.jsonl                   │
│  │   │   ├── validations/sampled.jsonl                     │
│  │   │   └── analytics/daily-stats.json                    │
│  │   └── ...                                                │
│  ├── artifacts/                                              │
│  │   ├── program-v1.py                                      │
│  │   ├── program-v2.py                                      │
│  │   └── active -> v2  (symlink)                           │
│  └── config/                                                 │
│      └── deployment.yaml                                     │
│                                                               │
└──────────────────────────────────────────────────────────────┘
```

---

## 💡 Core Concepts

### 1. Three Deployment Modes

#### Mode 1: Central Execution
- Programs execute in Loopai cloud
- REST API interface
- Zero infrastructure for customers
- **Use case**: Development, prototyping, low-volume

#### Mode 2: Edge Deployment
- Programs execute in customer infrastructure
- All data stays local
- Selective telemetry (configurable)
- **Use case**: Production, high-volume, privacy-sensitive

#### Mode 3: Hybrid (Recommended)
- Develop in cloud, deploy to edge
- Continuous improvement via cloud
- Best of both worlds
- **Use case**: Enterprise production

### 2. Portable Program Artifacts

Programs are generated as standalone, deployable artifacts:

```python
# Generated artifact structure
artifact/
├── program.py          # Self-contained Python program
├── Dockerfile          # Container image definition
├── deployment.yaml     # Runtime configuration
├── requirements.txt    # Dependencies
└── README.md          # Deployment instructions
```

**Key Features**:
- No Loopai dependency at runtime
- Can execute offline
- Versioned and immutable
- Portable across environments

### 3. File System-Based Dataset Management

All execution data stored locally in efficient JSONL format:

```jsonl
# /loopai-data/datasets/task-abc/executions/2025-10-26.jsonl
{"execution_id":"uuid","input":{"text":"..."},"output":"spam","latency_ms":5.2,"timestamp":"2025-10-26T10:00:00Z"}
{"execution_id":"uuid","input":{"text":"..."},"output":"ham","latency_ms":4.8,"timestamp":"2025-10-26T10:00:01Z"}
```

**Benefits**:
- Local analytics without cloud dependency
- Continuous learning from accumulated data
- Audit trail and compliance
- No data loss
- Simple backup and archival

### 4. Privacy-Aware Telemetry

**Three Privacy Modes**:

#### Strict Mode (Maximum Privacy)
```yaml
send_to_cloud:
  - execution_hashes_only: true
  - no_inputs: true
  - aggregate_metrics_only: true
```

#### Balanced Mode (Default)
```yaml
send_to_cloud:
  - sampled_inputs: true  (5%)
  - sampled_outputs: true (5%)
  - pii_detection: enabled
  - aggregate_metrics: true
```

#### Permissive Mode (Development)
```yaml
send_to_cloud:
  - full_execution_logs: true
  - all_validations: true
```

### 5. Continuous Improvement Loop

```
┌─────────────────────────────────────────────────────┐
│  Edge Runtime (Customer Infrastructure)             │
│                                                      │
│  1. Execute program locally (&lt;10ms)                 │
│  2. Store ALL data locally (JSONL)                  │
│  3. Sample 5% for validation                        │
│  4. Upload sampled telemetry to cloud               │
└────────────┬─────────────────────────────────────────┘
             │
             │ Sampled data (5%)
             ▼
┌─────────────────────────────────────────────────────┐
│  Cloud Platform                                      │
│                                                      │
│  5. Aggregate telemetry across all deployments     │
│  6. Detect failure patterns                         │
│  7. Regenerate improved program                     │
│  8. Notify edge deployments via SignalR             │
└────────────┬─────────────────────────────────────────┘
             │
             │ New program version
             ▼
┌─────────────────────────────────────────────────────┐
│  Edge Runtime                                        │
│                                                      │
│  9. Download new version                            │
│  10. A/B test (10% → 50% → 100%)                    │
│  11. Promote or rollback based on metrics           │
└─────────────────────────────────────────────────────┘
```

---

## 🧩 Component Details

### Cloud Platform Components

#### 1. Program Generator
**Purpose**: Generate programs from task specifications

**Input**:
```python
TaskSpecification(
    name="spam-detection",
    description="Classify emails as spam or ham",
    input_schema={"type": "object", "properties": {"text": {"type": "string"}}},
    output_schema={"type": "string", "enum": ["spam", "ham"]},
    examples=[...],
    accuracy_target=0.9,
    latency_target_ms=10
)
```

**Output**:
```python
ProgramArtifact(
    code="...",  # Generated Python code
    language="python",
    synthesis_strategy="rule",  # or "ml", "hybrid"
    confidence_score=0.95,
    complexity_metrics={...},
    generation_cost=0.15,
    generation_time_sec=8.5
)
```

**Generation Strategies**:
1. **Rule-based**: Pattern matching, keyword extraction
2. **ML-based**: Traditional ML models (scikit-learn)
3. **Hybrid**: Combination of rules and ML
4. **DSL**: Domain-specific language programs

#### 2. Artifact Repository
**Purpose**: Version-controlled storage for program artifacts

**Features**:
- Semantic versioning (v1, v2, v3...)
- Immutable artifacts
- Downloadable as ZIP archives
- Metadata tracking

**API**:
```python
# Upload artifact
POST /api/v1/artifacts/{task_id}/upload

# Download artifact
GET /api/v1/artifacts/{task_id}/v{version}/download

# List versions
GET /api/v1/artifacts/{task_id}/versions
```

#### 3. Telemetry Collector
**Purpose**: Collect sampled execution data from edge deployments

**Privacy Features**:
- PII detection (email, phone, SSN, credit card)
- Configurable sampling rate (1-20%)
- Hash sensitive fields
- Audit logging

**Batch Upload Format**:
```json
{
  "deployment_id": "customer-123-edge-1",
  "task_id": "task-abc",
  "program_version": 2,
  "sampled_executions": [
    {
      "input_hash": "sha256...",
      "output": "spam",
      "latency_ms": 5.2,
      "match": true
    }
  ]
}
```

#### 4. Improvement Engine
**Purpose**: Analyze failures and trigger program regeneration

**Workflow**:
1. Aggregate telemetry from all deployments
2. Calculate global failure rate
3. If >15%, analyze failure patterns
4. Generate improvement hypothesis
5. Regenerate program with failures as examples
6. Create new version in repository
7. Notify deployments via SignalR

**Human Escalation**:
- Triggered when automatic improvement fails 3+ times
- Create ticket with failure examples
- Wait for specification update
- Resume automatic improvement

#### 5. SignalR Hub
**Purpose**: Real-time communication with edge deployments

**Methods**:
```csharp
// Server -> Client
OnNewArtifactAvailable(taskId, version)
OnMetricUpdate(taskId, metrics)
OnAlertTriggered(alert)

// Client -> Server
Heartbeat(deploymentId, status)
ReportMetrics(metrics)
```

### Edge Runtime Components

#### 1. Artifact Cache
**Purpose**: Local copy of program artifacts

**Features**:
- Download from cloud repository
- Version management with symlinks
- Auto-update notifications
- Offline operation support

**Directory Structure**:
```
/loopai-data/artifacts/
├── task-abc/
│   ├── v1/
│   │   ├── program.py
│   │   └── metadata.json
│   ├── v2/
│   │   └── ...
│   └── active -> v2/  # Symlink to active version
```

#### 2. Program Executor
**Purpose**: Execute programs with &lt;10ms latency

**Features** (inherited from Phase 0-2):
- Safe execution environment
- Timeout enforcement (1s default)
- Error handling
- Memory limits

**Performance**:
- p50 latency: &lt;5ms
- p99 latency: &lt;10ms
- Throughput: 10K+ req/sec per instance

#### 3. Dataset Manager
**Purpose**: File system-based dataset storage

**Storage Format**:
```jsonl
# Daily execution logs (append-only)
/loopai-data/datasets/task-abc/executions/2025-10-26.jsonl

# Validation results (sampled only)
/loopai-data/datasets/task-abc/validations/sampled-2025-10-26.jsonl

# Daily aggregates
/loopai-data/datasets/task-abc/analytics/daily-stats-2025-10-26.json
```

**Analytics**:
```python
# Local analytics without cloud
def analyze_latency_trend(task_id, days=7):
    stats = []
    for i in range(days):
        date = (datetime.now() - timedelta(days=i)).strftime("%Y-%m-%d")
        stats_file = f"/loopai-data/datasets/{task_id}/analytics/daily-stats-{date}.json"
        with open(stats_file) as f:
            stats.append(json.load(f))
    return stats
```

**Retention Policy**:
```yaml
retention:
  executions:
    keep_days: 7        # Raw logs
    compress_after: 1   # Gzip compression
  validations:
    keep_days: 30       # Keep longer
  artifacts:
    keep_versions: 10   # Last 10 versions
```

#### 4. Sampling & Telemetry Service
**Purpose**: Privacy-aware telemetry with intelligent sampling

**Sampling Strategies**:

1. **Random Sampling** (implemented in Phase 1-2)
2. **Uncertainty Sampling** (new)
   - Program outputs confidence score
   - Sample low-confidence cases
3. **Stratified Sampling** (new)
   - Ensure coverage across input distribution
4. **Temporal Sampling** (new)
   - Higher rate during deployment
   - Decay over time

**Privacy Filter**:
```python
class PIIFilter:
    PATTERNS = {
        "email": r"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
        "phone": r"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b",
        "ssn": r"\b\d{3}-\d{2}-\d{4}\b",
    }

    def filter(self, text: str) -> str:
        for pii_type, pattern in self.PATTERNS.items():
            text = re.sub(pattern, f"[{pii_type.upper()}]", text)
        return text
```

**Upload Strategy**:
- Batch every 60 seconds
- Gzip compression
- Retry with exponential backoff
- Offline queue

#### 5. Configuration Manager
**Purpose**: Manage deployment configuration

**Configuration File**:
```yaml
# /loopai-data/config/deployment.yaml
runtime:
  mode: edge  # edge, central, hybrid

cloud:
  api_url: https://api.loopai.io
  api_key: ${LOOPAI_API_KEY}
  signalr_enabled: true

execution:
  worker_count: 4
  timeout_ms: 1000

sampling:
  strategy: random
  rate: 0.05  # 5%

telemetry:
  enabled: true
  upload_interval_sec: 60
  batch_size: 1000
  privacy_mode: balanced  # strict, balanced, permissive

storage:
  data_dir: /loopai-data
  retention_days: 30
  max_size_gb: 100

privacy:
  pii_detection: true
  hash_inputs: false
  sample_only: true
```

---

## 📊 Data Models

### Core Models (from Phase 0-2)

```python
from pydantic import BaseModel
from enum import Enum
from datetime import datetime
from uuid import UUID

class TaskSpecification(BaseModel):
    id: UUID
    name: str
    description: str
    input_schema: dict
    output_schema: dict
    examples: list
    accuracy_target: float = 0.9
    latency_target_ms: int = 10
    sampling_rate: float = 0.1
    created_at: datetime
    updated_at: datetime

class ProgramArtifact(BaseModel):
    id: UUID
    task_id: UUID
    version: int
    language: str = "python"
    code: str
    synthesis_strategy: Enum  # rule, ml, hybrid, dsl
    confidence_score: float
    complexity_metrics: dict
    llm_provider: str
    llm_model: str
    generation_cost: float
    generation_time_sec: float
    status: Enum  # draft, validated, active, deprecated
    created_at: datetime

class ExecutionRecord(BaseModel):
    id: UUID
    program_id: UUID
    task_id: UUID
    input_data: dict
    output_data: dict
    latency_ms: float
    status: Enum  # success, error, timeout
    sampled_for_validation: bool = False
    executed_at: datetime

class ValidationRecord(BaseModel):
    id: UUID
    execution_id: UUID
    program_id: UUID
    oracle_output: dict
    oracle_cost: float
    oracle_latency_ms: float
    match: bool
    similarity_score: float
    comparison_method: Enum  # exact, semantic, fuzzy
    validated_at: datetime
```

### New Models (for Edge Runtime)

```python
class DeploymentConfig(BaseModel):
    deployment_id: UUID
    task_id: UUID
    program_version: int
    mode: Enum  # edge, central, hybrid
    privacy_mode: Enum  # strict, balanced, permissive
    sampling_config: dict
    telemetry_config: dict
    storage_config: dict

class TelemetryBatch(BaseModel):
    deployment_id: UUID
    task_id: UUID
    program_version: int
    timestamp: datetime
    execution_summaries: List[ExecutionSummary]
    sampled_validations: List[ValidationRecord]

class ExecutionSummary(BaseModel):
    """Privacy-aware execution summary"""
    execution_id: UUID
    input_hash: str  # SHA256 hash (privacy mode: strict)
    output: str      # Actual output (not sensitive)
    latency_ms: float
    status: str
    sampled: bool
```

---

## 🔄 Data Flow Scenarios

### Scenario 1: Central Execution (API Mode)

```python
# Customer code
import requests

response = requests.post(
    "https://api.loopai.io/v1/execute/task-abc",
    json={"input": {"text": "Buy now!"}},
    headers={"Authorization": "Bearer sk-..."}
)

result = response.json()
# {"output": "spam", "latency_ms": 5.2}
```

**Flow**:
1. Request → Cloud API Gateway
2. Load program from cache
3. Execute in cloud executor
4. Sampling decision (5%)
5. Return result immediately
6. If sampled, validate asynchronously

### Scenario 2: Edge Execution (Customer Infrastructure)

```python
# Customer code (in customer VPC)
import requests

response = requests.post(
    "http://localhost:8080/execute",  # Local edge runtime
    json={"input": {"text": "Buy now!"}}
)

result = response.json()
# {"output": "spam", "latency_ms": 3.8}
```

**Flow**:
1. Request → Edge Runtime (local)
2. Load program from local cache
3. Execute locally (&lt;5ms)
4. **Append to local JSONL file**
5. Sampling decision (5%)
6. Return result immediately
7. If sampled:
   - Validate against oracle (local or cloud)
   - Store validation result locally
   - Queue for telemetry upload (async, every 60s)

### Scenario 3: Artifact Update

```
Cloud Improvement Engine
    ↓ Detects 15% failure rate
    ↓ Regenerates program v3
    ↓ Stores in artifact repository
    ↓ Sends SignalR notification

Edge Runtime
    ↓ Receives notification
    ↓ Downloads program v3
    ↓ Stores in /loopai-data/artifacts/task-abc/v3/
    ↓ Starts A/B test: 10% → v3, 90% → v2
    ↓ Monitors metrics
    ↓ If v3 better: gradual rollout to 100%
    ↓ If v3 worse: rollback to v2
```

---

## 🚀 Deployment

### Docker Deployment

```bash
# Pull runtime image
docker pull loopai/runtime:latest

# Run edge runtime
docker run -d \
  -v /data/loopai:/loopai-data \
  -p 8080:8080 \
  -e LOOPAI_API_KEY=sk-... \
  -e LOOPAI_TASK_ID=task-abc \
  loopai/runtime:latest

# Execute request
curl -X POST http://localhost:8080/execute \
  -H "Content-Type: application/json" \
  -d '{"input": {"text": "Buy now!"}}'
```

### Kubernetes Deployment

```yaml
# loopai-runtime-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: loopai-runtime
spec:
  replicas: 3
  selector:
    matchLabels:
      app: loopai-runtime
  template:
    metadata:
      labels:
        app: loopai-runtime
    spec:
      containers:
      - name: runtime
        image: loopai/runtime:latest
        ports:
        - containerPort: 8080
        env:
        - name: LOOPAI_API_KEY
          valueFrom:
            secretKeyRef:
              name: loopai-secrets
              key: api-key
        volumeMounts:
        - name: data
          mountPath: /loopai-data
      volumes:
      - name: data
        persistentVolumeClaim:
          claimName: loopai-data-pvc
```

### Python Package

```bash
# Install runtime as Python package
pip install loopai-runtime

# Run runtime
loopai-runtime start \
  --task-id task-abc \
  --api-key sk-... \
  --data-dir /data/loopai
```

---

## 🔐 Security

### Code Execution Sandbox

**Python Sandboxing**:
- RestrictedPython for AST-level restrictions
- No file I/O, network access, subprocess
- Read-only file system in Docker
- Resource limits (CPU, memory)

**Allowed Operations**:
```python
# Allowed
x = input_data["text"].lower()
result = "spam" if "buy now" in x else "ham"
import re  # Allowlisted import
pattern = re.compile(r"\d+")

# Blocked
open("/etc/passwd")  # File I/O
import subprocess    # Dangerous import
eval(user_input)     # Dynamic execution
```

### Privacy & Compliance

**Data Sovereignty**:
- 100% customer data stays local
- Only sampled data sent to cloud (opt-in)
- GDPR compliant
- HIPAA ready

**Encryption**:
- TLS 1.3 for all network communication
- AES-256 for data at rest (optional)
- API key authentication

---

## 📈 Performance Characteristics

### Latency

| Operation | Target | Typical |
|-----------|--------|---------|
| Program execution (edge) | &lt;10ms p99 | 3-5ms p50 |
| Program execution (cloud) | &lt;100ms p99 | 50ms p50 |
| Artifact download | &lt;5s | 2-3s |
| Telemetry upload | &lt;1s | 500ms |

### Throughput

| Deployment | Throughput |
|-----------|------------|
| Single edge instance | 10K req/sec |
| Cloud API (10 instances) | 100K req/sec |

### Cost Savings

**Example**: 1M requests/day

| Approach | Cost/month |
|----------|------------|
| Direct LLM | $5,400 |
| Central execution | $2,300 |
| Edge deployment | $1,000 |
| **Savings** | **82%** |

---

## 🔮 Future Enhancements

### Phase 3-4 Roadmap

1. **Multi-language Support**
   - C# runtime
   - Java runtime
   - Go runtime

2. **Advanced Sampling**
   - Uncertainty-based sampling
   - Active learning
   - Adaptive rates

3. **Federated Learning**
   - Learn from multiple edge deployments
   - Privacy-preserving aggregation

4. **Enterprise Features**
   - Multi-tenancy
   - SSO integration
   - On-premises deployment

---

## 📚 Related Documentation

- **GETTING_STARTED.md**: Quick start guide
- **PHASE0_STATUS.md**: Phase 0 results (basic classification)
- **PHASE1_STATUS.md**: Phase 1 results (multi-class, sampling)
- **PHASE2_STATUS.md**: Phase 2 results (pattern recognition)
- **README.md**: Project overview

---

**Document Version**: 1.0.0
**Last Updated**: 2025-10-26
**Status**: Active Development
