# Phase 3 Implementation Plan

**Objective**: Implement Edge Runtime with file system-based dataset management

**Status**: Planning
**Start Date**: 2025-10-26
**Target Completion**: 2-3 weeks

---

## 🎯 Phase 3 Scope

### Core Deliverables

1. **Dataset Manager** (File System Storage)
   - JSONL-based execution logging
   - Daily log rotation
   - Local analytics generation
   - Retention policy enforcement

2. **Configuration Manager**
   - YAML-based configuration
   - Environment variable support
   - Runtime configuration validation

3. **Artifact Cache**
   - Local artifact storage
   - Version management with symlinks
   - Artifact download (stub for v0.1)

4. **Edge Runtime API**
   - Simple REST API for execution
   - Execute endpoint with logging
   - Health check endpoint

5. **Docker Deployment**
   - Dockerfile for edge runtime
   - Volume mounting for /loopai-data
   - Environment configuration

### Out of Scope (for Phase 4)

- SignalR real-time communication
- Cloud telemetry upload
- A/B testing framework
- Privacy filters (PII detection)
- Multi-language support

---

## 📋 Implementation Phases

### Phase 3.1: Dataset Manager (Week 1)

**TDD Approach**:
1. Create `tests/test_phase3_dataset_manager.py`
2. Test file system structure creation
3. Test JSONL execution logging
4. Test daily log rotation
5. Test retention policy
6. Implement Dataset Manager

**Components**:
```python
src/loopai/runtime/
├── __init__.py
└── dataset_manager.py
    ├── DatasetManager
    ├── ExecutionLogger
    ├── ValidationLogger
    └── AnalyticsGenerator
```

**File System Structure**:
```
/loopai-data/
├── datasets/
│   └── {task_id}/
│       ├── executions/
│       │   └── YYYY-MM-DD.jsonl  (append-only)
│       ├── validations/
│       │   └── sampled-YYYY-MM-DD.jsonl
│       └── analytics/
│           └── daily-stats-YYYY-MM-DD.json
├── artifacts/
│   └── {task_id}/
│       ├── v1/
│       │   ├── program.py
│       │   └── metadata.json
│       ├── v2/
│       └── active -> v2/  (symlink)
└── config/
    └── deployment.yaml
```

**Tests**:
- ✅ Create directory structure
- ✅ Write execution record to JSONL
- ✅ Append multiple records
- ✅ Read execution logs
- ✅ Generate daily analytics
- ✅ Enforce retention policy (delete old logs)

### Phase 3.2: Configuration Manager (Week 1)

**TDD Approach**:
1. Create configuration tests
2. Test YAML loading
3. Test environment variable substitution
4. Test validation
5. Implement Configuration Manager

**Components**:
```python
src/loopai/runtime/
└── config_manager.py
    ├── ConfigurationManager
    ├── DeploymentConfig (Pydantic model)
    └── validate_config()
```

**Configuration Schema**:
```yaml
runtime:
  mode: edge  # edge, central, hybrid
  data_dir: /loopai-data

execution:
  worker_count: 4
  timeout_ms: 1000

sampling:
  strategy: random
  rate: 0.05

storage:
  retention_days: 7
  max_size_gb: 100
```

**Tests**:
- ✅ Load configuration from YAML
- ✅ Environment variable substitution
- ✅ Validation (required fields, types)
- ✅ Default values
- ✅ Invalid configuration errors

### Phase 3.3: Artifact Cache (Week 1-2)

**TDD Approach**:
1. Create artifact cache tests
2. Test artifact storage
3. Test version management
4. Test symlink to active version
5. Implement Artifact Cache

**Components**:
```python
src/loopai/runtime/
└── artifact_cache.py
    ├── ArtifactCache
    ├── store_artifact()
    ├── get_active_artifact()
    └── set_active_version()
```

**Tests**:
- ✅ Store artifact with version
- ✅ List artifact versions
- ✅ Get active artifact via symlink
- ✅ Update active version (change symlink)
- ✅ Load artifact metadata

### Phase 3.4: Edge Runtime API (Week 2)

**TDD Approach**:
1. Create API tests
2. Test /execute endpoint
3. Test /health endpoint
4. Test integration with dataset manager
5. Implement FastAPI runtime

**Components**:
```python
src/loopai/runtime/
├── api.py  (FastAPI app)
└── edge_runtime.py
    └── EdgeRuntime (orchestrator)
```

**Endpoints**:
```
POST /execute
  Body: {"input": {"text": "..."}}
  Response: {"output": "...", "latency_ms": 5.2}

GET /health
  Response: {"status": "healthy", "version": "0.1.0"}

GET /metrics
  Response: {"executions_today": 1000, "avg_latency_ms": 4.5}
```

**Tests**:
- ✅ Execute endpoint returns result
- ✅ Execution logged to JSONL
- ✅ Latency measured
- ✅ Health endpoint responds
- ✅ Metrics endpoint shows stats

### Phase 3.5: Docker Deployment (Week 2)

**Components**:
- `Dockerfile` (edge runtime image)
- `docker-compose.yml` (local testing)
- `.dockerignore`

**Dockerfile**:
```dockerfile
FROM python:3.9-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY src/ ./src/

VOLUME /loopai-data

EXPOSE 8080

CMD ["uvicorn", "loopai.runtime.api:app", "--host", "0.0.0.0", "--port", "8080"]
```

**Tests**:
- ✅ Docker build succeeds
- ✅ Container starts
- ✅ API accessible on port 8080
- ✅ Volume mounted correctly
- ✅ Logs written to volume

### Phase 3.6: Integration Testing (Week 3)

**End-to-End Tests**:
```python
tests/test_phase3_integration.py
```

**Scenarios**:
1. ✅ Generate program → Store artifact → Execute via API → Verify JSONL log
2. ✅ Multiple executions → Daily analytics generated
3. ✅ Retention policy → Old logs deleted
4. ✅ Docker deployment → Full workflow

---

## 🧪 TDD Test Structure

```
tests/
├── test_phase3_dataset_manager.py     (Week 1)
│   ├── test_create_directory_structure
│   ├── test_log_execution
│   ├── test_append_executions
│   ├── test_read_logs
│   ├── test_generate_analytics
│   └── test_retention_policy
│
├── test_phase3_config_manager.py      (Week 1)
│   ├── test_load_yaml_config
│   ├── test_env_var_substitution
│   ├── test_config_validation
│   ├── test_default_values
│   └── test_invalid_config_error
│
├── test_phase3_artifact_cache.py      (Week 1-2)
│   ├── test_store_artifact
│   ├── test_list_versions
│   ├── test_get_active_artifact
│   ├── test_update_active_version
│   └── test_load_metadata
│
├── test_phase3_edge_api.py            (Week 2)
│   ├── test_execute_endpoint
│   ├── test_execution_logged
│   ├── test_latency_measured
│   ├── test_health_endpoint
│   └── test_metrics_endpoint
│
└── test_phase3_integration.py         (Week 3)
    ├── test_full_workflow
    ├── test_daily_analytics
    ├── test_retention_enforcement
    └── test_docker_deployment
```

**Total**: ~25 tests (TDD approach)

---

## 📊 Success Criteria

### Functional Requirements

- [x] TDD approach (tests written first)
- [ ] Dataset Manager stores executions in JSONL
- [ ] Daily log rotation works
- [ ] Analytics generated daily
- [ ] Retention policy enforced
- [ ] Configuration loaded from YAML
- [ ] Artifact cache manages versions
- [ ] Active version symlink works
- [ ] REST API executes programs
- [ ] Executions logged automatically
- [ ] Docker deployment works
- [ ] Zero technical debt (no TODOs, proper docs, type hints)

### Performance Requirements

- [ ] Execution latency: <10ms p99 (same as Phase 2)
- [ ] JSONL append: <1ms
- [ ] Analytics generation: <100ms
- [ ] API response: <20ms p99

### Quality Requirements

- [ ] All tests pass
- [ ] Test coverage >80%
- [ ] No TODOs in code
- [ ] All functions have docstrings
- [ ] All functions have type hints
- [ ] Pydantic models for all data

---

## 🔧 Dependencies

**New Dependencies**:
```txt
# Runtime
fastapi==0.104.1
uvicorn==0.24.0
pyyaml==6.0.1

# Testing
httpx==0.25.0  # for FastAPI testing
pytest-asyncio==0.21.1
```

**Existing** (from Phase 0-2):
- pydantic
- pytest
- python-dotenv
- openai

---

## 📁 Project Structure After Phase 3

```
Loopai/
├── src/loopai/
│   ├── generator/          ✅ (Phase 0-1)
│   ├── executor/           ✅ (Phase 0)
│   ├── validator/          ✅ (Phase 1)
│   ├── sampler/            ✅ (Phase 1)
│   ├── runtime/            🆕 NEW (Phase 3)
│   │   ├── __init__.py
│   │   ├── dataset_manager.py
│   │   ├── config_manager.py
│   │   ├── artifact_cache.py
│   │   ├── edge_runtime.py
│   │   └── api.py
│   └── models.py           ✅ (extended)
│
├── tests/
│   ├── test_phase0.py      ✅
│   ├── test_phase1.py      ✅
│   ├── test_phase2.py      ✅
│   ├── test_phase3_dataset_manager.py   🆕
│   ├── test_phase3_config_manager.py    🆕
│   ├── test_phase3_artifact_cache.py    🆕
│   ├── test_phase3_edge_api.py          🆕
│   └── test_phase3_integration.py       🆕
│
├── scripts/
│   ├── run_phase2_benchmark.py   ✅
│   └── run_edge_runtime.py       🆕 NEW
│
├── Dockerfile                    🆕 NEW
├── docker-compose.yml            🆕 NEW
├── requirements.txt              📝 Updated
│
└── docs/
    ├── PHASE3_PLAN.md            🆕 This document
    └── PHASE3_STATUS.md          🆕 (created at end)
```

---

## 🚀 Getting Started

### Week 1: Dataset & Config Management

**Day 1-2**: Dataset Manager TDD
- Write all dataset manager tests
- Implement Dataset Manager
- Verify all tests pass

**Day 3-4**: Configuration Manager TDD
- Write all config manager tests
- Implement Configuration Manager
- Integration with Dataset Manager

**Day 5**: Artifact Cache TDD (start)
- Write artifact cache tests
- Begin implementation

### Week 2: Runtime & API

**Day 1-2**: Artifact Cache (complete)
- Complete implementation
- All tests passing

**Day 3-4**: Edge Runtime API
- Write API tests
- Implement FastAPI endpoints
- Integration testing

**Day 5**: Docker Deployment
- Create Dockerfile
- Test Docker build
- Verify volume mounting

### Week 3: Integration & Documentation

**Day 1-3**: End-to-End Testing
- Full workflow tests
- Performance validation
- Bug fixes

**Day 4-5**: Documentation
- Create PHASE3_STATUS.md
- Update README.md
- Update ARCHITECTURE.md if needed

---

## 🎯 Milestone Checkpoints

### Checkpoint 1 (End of Week 1)
- ✅ Dataset Manager complete with tests
- ✅ Configuration Manager complete with tests
- ✅ Artifact Cache ~50% complete

### Checkpoint 2 (End of Week 2)
- ✅ Artifact Cache complete
- ✅ Edge Runtime API complete
- ✅ Docker deployment working

### Checkpoint 3 (End of Week 3)
- ✅ All integration tests passing
- ✅ Performance targets met
- ✅ Documentation complete
- ✅ Phase 3 ready for review

---

## ⚠️ Known Challenges

1. **File System Performance**:
   - JSONL append performance with high throughput
   - Mitigation: Buffer writes, async I/O

2. **Symlink Support**:
   - Windows symlink permissions
   - Mitigation: Check OS, fallback to copy

3. **Docker Volumes**:
   - Permission issues with mounted volumes
   - Mitigation: Proper user/group configuration

4. **Retention Policy**:
   - Disk space management
   - Mitigation: Monitoring, alerts

---

## 📚 References

- **ARCHITECTURE.md**: Full architecture specification
- **Phase 2 Learnings**: TDD approach, zero technical debt
- **FastAPI Documentation**: https://fastapi.tiangolo.com/
- **Docker Best Practices**: https://docs.docker.com/develop/dev-best-practices/

---

**Next Step**: Create Phase 3 Dataset Manager tests and begin TDD implementation.
