# Phase 4.1 Work Items: C# Cloud API Implementation

**Phase**: 4.1 - C# Cloud API
**Duration**: 4-6 weeks
**Team**: 2 C# developers + 1 Python developer
**Status**: 🟢 IN PROGRESS (Sprint 1, Week 2)
**Last Updated**: 2025-10-26

---

## 🎯 Phase 4.1 Overview

Implement high-performance C# Cloud API using ASP.NET Core with 100K+ req/sec target throughput.

**Key Deliverables**:
- REST API with Swagger documentation
- Program Generator Service integration
- Artifact Repository with versioning
- Caching layer (Redis)
- Comprehensive testing (unit, integration, load)
- Complete documentation

---

## 📋 Sprint 1 (Weeks 1-2): Project Setup & Core API

### Work Item 1.1: Project Infrastructure Setup
**Priority**: P0 (Critical)
**Assignee**: C# Dev 1
**Estimated Hours**: 24h (3 days)
**Dependencies**: None

**Tasks**:
- [x] ✅ Create Loopai.CloudApi .NET 8.0 solution
  - ✅ Create `src/Loopai.CloudApi/` project (Web API template)
  - ✅ Create `src/Loopai.Core/` project (Class Library)
  - ✅ Create `tests/Loopai.CloudApi.Tests/` project (xUnit)
  - ✅ Configure solution file with all projects

- [x] ✅ Setup dependency injection and logging
  - ✅ Configure `Program.cs` with DI container
  - ✅ Add Serilog for structured logging
  - ✅ Configure logging levels (Development vs Production)
  - ⏳ Add correlation ID middleware (pending)

- [ ] CI/CD pipeline configuration
  - Create `.github/workflows/dotnet-ci.yml`
  - Configure build, test, and publish steps
  - Add code coverage reporting (Coverlet)
  - Setup automatic Docker image build

- [ ] Development environment setup
  - Create `docker-compose.yml` for local development
  - Add Redis service for caching
  - Add PostgreSQL service for metadata
  - Create `.devcontainer/` configuration for VS Code

- [ ] Code quality tools
  - Add StyleCop.Analyzers for code style
  - Configure .editorconfig
  - Add SonarQube analysis (optional)
  - Setup pre-commit hooks

**Acceptance Criteria**:
- [x] ✅ Solution builds without errors on Windows and Linux
- [x] ✅ Unit test project runs successfully (65 tests passing)
- [ ] ⏳ CI pipeline executes on PR creation (pending)
- [ ] ⏳ Docker Compose brings up development environment (pending)
- [ ] ⏳ Code style checks pass (pending)

**Deliverables**:
- `src/Loopai.CloudApi/Loopai.CloudApi.csproj`
- `src/Loopai.CloudApi/Program.cs`
- `src/Loopai.Core/Loopai.Core.csproj`
- `tests/Loopai.CloudApi.Tests/Loopai.CloudApi.Tests.csproj`
- `.github/workflows/dotnet-ci.yml`
- `docker-compose.yml`
- `.devcontainer/devcontainer.json`

---

### Work Item 1.2: Data Models and DTOs
**Priority**: P0 (Critical)
**Assignee**: C# Dev 2
**Estimated Hours**: 16h (2 days)
**Dependencies**: 1.1 (Project setup complete)

**Tasks**:
- [x] ✅ Create core domain models (record types)
  - ✅ `ProgramArtifact` model (matches Python Pydantic model)
  - ✅ `ExecutionRecord` model
  - ✅ `TaskSpecification` model
  - ✅ `ComplexityMetrics` model
  - ✅ All enums (SynthesisStrategy, ProgramStatus, ExecutionStatus, etc.)
  - ✅ Add XML documentation comments

- [x] ✅ Create API DTOs
  - ✅ `ExecuteRequest` DTO
  - ✅ `ExecuteResponse` DTO
  - ✅ `CreateTaskRequest` DTO
  - ✅ `TaskResponse` DTO
  - ✅ `ErrorResponse` DTO

- [x] ✅ Add validation with FluentValidation
  - ✅ `ExecuteRequestValidator`
  - ✅ `CreateTaskRequestValidator`
  - ✅ Custom validation rules (task ID format, ranges, etc.)
  - ✅ Error message customization

- [x] ✅ Configure JSON serialization
  - ✅ System.Text.Json settings
  - ✅ snake_case naming policy (Python compatibility)
  - ✅ Enum string conversion
  - ✅ Null handling configuration

- [x] ✅ Unit tests for models and validation
  - ✅ 44 validation rule tests (CreateTaskRequestValidator)
  - ✅ 14 validation rule tests (ExecuteRequestValidator)
  - ✅ 8 JSON serialization tests
  - ✅ Edge case handling tests
  - ✅ 65 total tests passing

**Acceptance Criteria**:
- [x] ✅ All models match Python equivalents (verified by JSON comparison)
- [x] ✅ Validation rules enforce business logic correctly
- [x] ✅ JSON serialization works for all models (snake_case compatible)
- [x] ✅ High code coverage on validation logic
- [x] ✅ All unit tests passing (65/65)

**Deliverables**:
- ✅ `src/Loopai.Core/Models/Enums.cs`
- ✅ `src/Loopai.Core/Models/ComplexityMetrics.cs`
- ✅ `src/Loopai.Core/Models/TaskSpecification.cs`
- ✅ `src/Loopai.Core/Models/ProgramArtifact.cs`
- ✅ `src/Loopai.Core/Models/ExecutionRecord.cs`
- ✅ `src/Loopai.CloudApi/DTOs/ExecuteRequest.cs`
- ✅ `src/Loopai.CloudApi/DTOs/ExecuteResponse.cs`
- ✅ `src/Loopai.CloudApi/DTOs/CreateTaskRequest.cs`
- ✅ `src/Loopai.CloudApi/DTOs/TaskResponse.cs`
- ✅ `src/Loopai.CloudApi/DTOs/ErrorResponse.cs`
- ✅ `src/Loopai.CloudApi/Validators/ExecuteRequestValidator.cs`
- ✅ `src/Loopai.CloudApi/Validators/CreateTaskRequestValidator.cs`
- ✅ `tests/Loopai.CloudApi.Tests/DTOs/JsonSerializationTests.cs`
- ✅ `tests/Loopai.CloudApi.Tests/Validators/ExecuteRequestValidatorTests.cs`
- ✅ `tests/Loopai.CloudApi.Tests/Validators/CreateTaskRequestValidatorTests.cs`
- ✅ `src/Loopai.CloudApi/README.md` (comprehensive documentation)

---

### Work Item 1.3: REST API Controllers and Endpoints
**Priority**: P0 (Critical)
**Assignee**: C# Dev 1
**Estimated Hours**: 40h (5 days)
**Dependencies**: 1.2 (Models complete)

**Tasks**:
- [ ] Create TasksController
  - `POST /api/v1/tasks/{taskId}/execute` endpoint
  - `GET /api/v1/tasks/{taskId}/artifacts` endpoint
  - `GET /api/v1/tasks/{taskId}/artifacts/{version}` endpoint
  - `POST /api/v1/tasks` endpoint
  - Request validation using FluentValidation
  - Response formatting with consistent structure

- [ ] Create HealthController
  - `GET /api/v1/health` endpoint (basic health check)
  - `GET /api/v1/health/ready` endpoint (readiness probe)
  - `GET /api/v1/health/live` endpoint (liveness probe)
  - Add dependency health checks (Redis, Database)

- [ ] Create MetricsController
  - `GET /api/v1/metrics` endpoint (Prometheus format)
  - Custom metrics (request count, latency)
  - Business metrics (artifact count, execution count)

- [ ] Add global exception handling
  - `ExceptionMiddleware` for consistent error responses
  - Handle common exceptions (ValidationException, NotFoundException)
  - Log all exceptions with correlation ID
  - Return appropriate HTTP status codes

- [ ] Configure Swagger/OpenAPI
  - Add Swashbuckle.AspNetCore
  - XML documentation integration
  - Example request/response bodies
  - API versioning support (v1, v2)

- [ ] Add API versioning
  - URL-based versioning (/api/v1/)
  - Version-specific controllers
  - Deprecation headers for old versions

- [ ] Controller unit tests
  - Test all endpoints with valid input
  - Test validation error handling
  - Test exception handling
  - Test response formatting

**Acceptance Criteria**:
- [ ] All endpoints return correct HTTP status codes
- [ ] Request validation works (400 for invalid input)
- [ ] Error responses follow consistent format
- [ ] Swagger UI accessible at `/swagger`
- [ ] Health checks work for all dependencies
- [ ] Metrics exported in Prometheus format
- [ ] 90%+ code coverage on controllers

**Deliverables**:
- `src/Loopai.CloudApi/Controllers/TasksController.cs`
- `src/Loopai.CloudApi/Controllers/HealthController.cs`
- `src/Loopai.CloudApi/Controllers/MetricsController.cs`
- `src/Loopai.CloudApi/Middleware/ExceptionMiddleware.cs`
- `src/Loopai.CloudApi/appsettings.json`
- `tests/Loopai.CloudApi.Tests/Controllers/TasksControllerTests.cs`
- `tests/Loopai.CloudApi.Tests/Controllers/HealthControllerTests.cs`

---

## 📋 Sprint 2 (Weeks 3-4): Business Logic & Storage

### Work Item 2.1: Artifact Repository Implementation
**Priority**: P0 (Critical)
**Assignee**: C# Dev 2
**Estimated Hours**: 32h (4 days)
**Dependencies**: 1.2 (Models complete)

**Tasks**:
- [ ] Define repository interfaces
  - `IArtifactRepository` interface
  - CRUD operations (Create, Read, Update, Delete)
  - Version management methods
  - Active version tracking

- [ ] Implement FileSystemArtifactRepository
  - Directory structure creation
  - Artifact serialization (JSON)
  - Version file management
  - Active version symlink/reference
  - Concurrent access handling (file locking)

- [ ] Implement BlobStorageArtifactRepository (Azure)
  - Azure Blob Storage client integration
  - Blob naming conventions
  - Version metadata in blob properties
  - Active version tracking in separate blob
  - Retry logic with Polly

- [ ] Add repository configuration
  - Configuration section in appsettings.json
  - Repository type selection (FileSystem vs Blob)
  - Connection string configuration
  - Path configuration

- [ ] Repository unit tests
  - CRUD operation tests
  - Version management tests
  - Concurrent access tests
  - Edge case handling (missing files, etc.)
  - Mock tests for both implementations

**Acceptance Criteria**:
- [ ] Repository pattern abstraction works correctly
- [ ] FileSystem implementation functional
- [ ] BlobStorage implementation functional
- [ ] Version history maintained correctly
- [ ] Concurrent access safe (no race conditions)
- [ ] 95%+ code coverage on repository logic

**Deliverables**:
- `src/Loopai.Core/Repositories/IArtifactRepository.cs`
- `src/Loopai.Core/Repositories/FileSystemArtifactRepository.cs`
- `src/Loopai.Core/Repositories/BlobStorageArtifactRepository.cs`
- `src/Loopai.Core/Configuration/ArtifactRepositoryOptions.cs`
- `tests/Loopai.Core.Tests/Repositories/FileSystemArtifactRepositoryTests.cs`
- `tests/Loopai.Core.Tests/Repositories/BlobStorageArtifactRepositoryTests.cs`

---

### Work Item 2.2: Program Generator Service
**Priority**: P0 (Critical)
**Assignee**: C# Dev 1
**Estimated Hours**: 40h (5 days)
**Dependencies**: 1.2 (Models complete), Python Generator API available

**Tasks**:
- [ ] Define service interface
  - `IProgramGeneratorService` interface
  - `GenerateProgramAsync` method
  - `ValidateProgramAsync` method (optional)
  - Request/response models

- [ ] Create HTTP client for Python Generator
  - `ProgramGeneratorHttpClient` using IHttpClientFactory
  - Base URL configuration
  - Authentication (API key)
  - Request serialization
  - Response deserialization

- [ ] Implement ProgramGeneratorService
  - Call Python Generator API
  - Handle HTTP errors gracefully
  - Retry logic with exponential backoff (Polly)
  - Circuit breaker pattern (prevent cascading failures)
  - Timeout configuration (default 30s)

- [ ] Add resilience patterns with Polly
  - Retry policy (3 retries with backoff)
  - Circuit breaker (open after 5 consecutive failures)
  - Timeout policy (30 seconds)
  - Fallback policy (cache or error response)

- [ ] Add metrics and logging
  - Request count metrics
  - Latency metrics (histogram)
  - Error rate metrics
  - Detailed error logging with context

- [ ] Service unit tests
  - Successful generation tests
  - Error handling tests
  - Timeout tests
  - Retry tests
  - Circuit breaker tests

**Acceptance Criteria**:
- [ ] Successfully calls Python Generator API
- [ ] Handles timeouts gracefully (no hanging requests)
- [ ] Circuit breaker prevents cascading failures
- [ ] Retry logic works correctly (exponential backoff)
- [ ] All exceptions logged with context
- [ ] Metrics tracked for all calls
- [ ] 90%+ code coverage

**Deliverables**:
- `src/Loopai.Core/Services/IProgramGeneratorService.cs`
- `src/Loopai.Core/Services/ProgramGeneratorService.cs`
- `src/Loopai.Core/Services/ProgramGeneratorHttpClient.cs`
- `src/Loopai.Core/Configuration/ProgramGeneratorOptions.cs`
- `tests/Loopai.Core.Tests/Services/ProgramGeneratorServiceTests.cs`

---

### Work Item 2.3: Caching Layer Implementation
**Priority**: P1 (High)
**Assignee**: C# Dev 2
**Estimated Hours**: 24h (3 days)
**Dependencies**: 2.1 (Repository complete)

**Tasks**:
- [ ] Define caching interface
  - `IArtifactCacheService` interface
  - Get, Set, Remove methods
  - Cache key strategy

- [ ] Implement RedisCacheService
  - StackExchange.Redis client
  - Connection multiplexer configuration
  - Serialization (JSON or MessagePack)
  - TTL configuration (default 1 hour)
  - Cache invalidation logic

- [ ] Implement InMemoryCacheService (development)
  - IMemoryCache integration
  - LRU eviction policy
  - Size limits
  - Same interface as Redis

- [ ] Add cache configuration
  - Redis connection string
  - TTL settings
  - Cache size limits
  - Eviction policies

- [ ] Implement cache warming
  - Load frequently accessed artifacts on startup
  - Background refresh before TTL expiry
  - Metrics on cache warming effectiveness

- [ ] Cache metrics
  - Hit/miss ratio
  - Eviction count
  - Average latency
  - Size metrics

- [ ] Caching unit tests
  - Get/Set/Remove tests
  - TTL expiration tests
  - Eviction tests
  - Concurrent access tests

**Acceptance Criteria**:
- [ ] Artifact retrieval uses cache first
- [ ] Cache TTL configurable (default 1 hour)
- [ ] Cache invalidation works on artifact update
- [ ] 10x latency improvement for cached artifacts vs repository
- [ ] Cache metrics tracked correctly
- [ ] 90%+ code coverage

**Deliverables**:
- `src/Loopai.Core/Services/IArtifactCacheService.cs`
- `src/Loopai.Core/Services/RedisCacheService.cs`
- `src/Loopai.Core/Services/InMemoryCacheService.cs`
- `src/Loopai.Core/Configuration/CacheOptions.cs`
- `tests/Loopai.Core.Tests/Services/CacheServiceTests.cs`

---

## 📋 Sprint 3 (Weeks 5-6): Testing & Documentation

### Work Item 3.1: Integration Testing
**Priority**: P0 (Critical)
**Assignee**: C# Dev 1 + C# Dev 2
**Estimated Hours**: 40h (5 days)
**Dependencies**: All Sprint 1 & 2 work items complete

**Tasks**:
- [ ] Setup WebApplicationFactory
  - Create test server with in-memory dependencies
  - Configure test database (SQLite in-memory)
  - Configure test cache (in-memory)
  - Seed test data

- [ ] End-to-end API tests
  - Test POST /api/v1/tasks (create task)
  - Test POST /api/v1/tasks/{id}/execute (execute)
  - Test GET /api/v1/tasks/{id}/artifacts (list)
  - Test GET /api/v1/tasks/{id}/artifacts/{version} (get version)
  - Test error scenarios (404, 400, 500)

- [ ] Python Generator integration tests
  - Test successful program generation
  - Test generator timeout handling
  - Test generator error handling
  - Test circuit breaker activation

- [ ] Database integration tests
  - Test artifact persistence
  - Test version management
  - Test concurrent access
  - Test transaction rollback

- [ ] Cache integration tests
  - Test cache hit/miss
  - Test cache invalidation
  - Test TTL expiration

- [ ] Health check integration tests
  - Test all dependencies healthy
  - Test dependency failures
  - Test partial health (some dependencies down)

**Acceptance Criteria**:
- [ ] 100+ integration tests covering all endpoints
- [ ] All happy path scenarios tested
- [ ] All error scenarios tested
- [ ] Tests run in isolated environment (no external dependencies)
- [ ] All tests pass in CI/CD
- [ ] Test execution time <5 minutes

**Deliverables**:
- `tests/Loopai.CloudApi.IntegrationTests/WebApplicationFactorySetup.cs`
- `tests/Loopai.CloudApi.IntegrationTests/TasksControllerIntegrationTests.cs`
- `tests/Loopai.CloudApi.IntegrationTests/GeneratorIntegrationTests.cs`
- `tests/Loopai.CloudApi.IntegrationTests/HealthCheckIntegrationTests.cs`

---

### Work Item 3.2: Load Testing and Performance
**Priority**: P0 (Critical)
**Assignee**: C# Dev 2 + Python Dev
**Estimated Hours**: 40h (5 days)
**Dependencies**: 3.1 (Integration tests complete)

**Tasks**:
- [ ] Setup load testing framework
  - NBomber for .NET load testing
  - k6 for HTTP load testing
  - Test environment setup (Docker Compose)
  - Monitoring during load tests (Prometheus + Grafana)

- [ ] Create load test scenarios
  - Scenario 1: Constant load (10K req/sec)
  - Scenario 2: Ramp-up test (0 → 100K req/sec over 10 min)
  - Scenario 3: Spike test (sudden 10x load)
  - Scenario 4: Soak test (sustained 50K req/sec for 1 hour)

- [ ] Execute load tests
  - Run on 4-core server
  - Monitor CPU, memory, network
  - Collect latency percentiles (p50, p95, p99)
  - Identify bottlenecks

- [ ] Performance benchmarks
  - BenchmarkDotNet micro-benchmarks
  - Serialization performance
  - Cache performance
  - Repository performance

- [ ] Optimization based on results
  - Fix identified bottlenecks
  - Tune thread pool settings
  - Optimize database queries
  - Optimize cache strategies

- [ ] Document results
  - Performance report
  - Benchmark comparison (before/after)
  - Recommendations for production

**Acceptance Criteria**:
- [ ] 100,000+ req/sec sustained throughput achieved
- [ ] p50 latency <10ms
- [ ] p99 latency <20ms
- [ ] Memory usage <100MB per instance
- [ ] CPU usage <80% at max load
- [ ] No memory leaks in soak test
- [ ] Performance report published

**Deliverables**:
- `tests/Loopai.CloudApi.LoadTests/NBomberLoadTests.cs`
- `tests/Loopai.CloudApi.LoadTests/k6-load-test.js`
- `tests/Loopai.CloudApi.Benchmarks/SerializationBenchmarks.cs`
- `tests/Loopai.CloudApi.Benchmarks/CacheBenchmarks.cs`
- `docs/PHASE4.1_PERFORMANCE_REPORT.md`

---

### Work Item 3.3: Documentation
**Priority**: P1 (High)
**Assignee**: C# Dev 1 + Python Dev
**Estimated Hours**: 24h (3 days)
**Dependencies**: 3.1, 3.2 (All testing complete)

**Tasks**:
- [ ] API documentation
  - Swagger/OpenAPI spec generation
  - Example request/response bodies
  - Authentication guide
  - Error code reference

- [ ] Developer guide
  - Getting started guide
  - Local development setup
  - Running tests
  - Debugging tips

- [ ] Deployment guide
  - Docker deployment instructions
  - Kubernetes deployment instructions
  - Configuration reference
  - Environment variables

- [ ] Architecture documentation
  - Component diagram
  - Sequence diagrams (key flows)
  - Data flow diagrams
  - Integration points with Python

- [ ] Performance tuning guide
  - Configuration recommendations
  - Caching strategies
  - Database optimization
  - Scaling recommendations

- [ ] Troubleshooting guide
  - Common issues and solutions
  - Log analysis guide
  - Health check interpretation
  - Performance debugging

**Acceptance Criteria**:
- [ ] Complete API reference published
- [ ] Example code for all endpoints works
- [ ] Deployment tested on dev environment
- [ ] Documentation reviewed by team
- [ ] All diagrams created and embedded

**Deliverables**:
- `docs/CLOUD_API.md`
- `docs/CLOUD_API_DEVELOPER_GUIDE.md`
- `docs/CLOUD_API_DEPLOYMENT.md`
- `docs/CLOUD_API_PERFORMANCE_TUNING.md`
- `docs/diagrams/cloud-api-architecture.png`
- `docs/diagrams/cloud-api-sequence.png`
- `examples/csharp/CloudApiClient.cs`
- `examples/csharp/Program.cs`

---

## 📊 Phase 4.1 Completion Checklist

### Code Quality
- [ ] All code passes linting (StyleCop)
- [ ] Code coverage >90%
- [ ] All unit tests passing (100+ tests)
- [ ] All integration tests passing (100+ tests)
- [ ] No compiler warnings
- [ ] No SonarQube critical issues

### Performance
- [ ] Load test: 100K+ req/sec sustained
- [ ] Latency: p50 <10ms, p99 <20ms
- [ ] Memory: <100MB per instance
- [ ] CPU: <80% at max load
- [ ] Cache hit rate: >80% after warm-up

### Security
- [ ] Zero critical/high vulnerabilities (Snyk scan)
- [ ] Authentication working (API key)
- [ ] Input validation comprehensive
- [ ] HTTPS enforced
- [ ] Secrets not in source code

### Operational
- [ ] Docker image builds successfully (<200MB)
- [ ] Health checks functional (liveness, readiness)
- [ ] Metrics exported (Prometheus format)
- [ ] Logging structured (JSON format)
- [ ] CI/CD pipeline complete

### Documentation
- [ ] API documentation complete (Swagger)
- [ ] Developer guide complete
- [ ] Deployment guide complete
- [ ] Performance report published
- [ ] Architecture diagrams created

---

## 🚀 Deployment Readiness

### Phase 4.1 Deployment Artifacts

**Docker Images**:
- `loopai/cloud-api:4.1.0` (production image)
- `loopai/cloud-api:4.1.0-dev` (development image with debugging)

**Kubernetes Manifests**:
- `k8s/cloud-api-deployment.yaml`
- `k8s/cloud-api-service.yaml`
- `k8s/cloud-api-configmap.yaml`
- `k8s/cloud-api-secret.yaml`
- `k8s/cloud-api-hpa.yaml` (Horizontal Pod Autoscaler)

**Configuration**:
- `appsettings.Production.json`
- `appsettings.Development.json`
- `.env.example`

---

## 📈 Success Metrics

**Phase 4.1 is complete when**:

✅ **Performance**: 100K+ req/sec, <20ms p99 latency
✅ **Quality**: 90%+ coverage, 0 critical bugs, 200+ tests passing
✅ **Security**: 0 critical/high vulnerabilities
✅ **Integration**: Python Generator working, end-to-end tests passing
✅ **Documentation**: Complete API docs, deployment guide, performance report
✅ **Operational**: Docker deployment working, health checks operational

---

**Next Phase**: Phase 4.2 - Python-C# Integration

---

**Document Version**: 1.0
**Last Updated**: 2025-10-26
**Status**: ✅ Ready for development kickoff
