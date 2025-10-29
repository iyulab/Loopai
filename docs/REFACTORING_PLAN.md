# Task → Loop App Refactoring Plan

**Version**: 1.0.0
**Date**: 2025-10-29
**Status**: Approved - Ready for Execution
**Impact**: Breaking Change - Major Version Bump Required

---

## 🎯 Objective

Rename "Task" terminology to "Loop App" throughout the codebase to align with conceptual model defined in CONCEPTS.md.

**Current State**: Code uses "Task" (historical naming)
**Target State**: Code uses "Loop App" (new conceptual model)
**Scope**: ~91 files across API, Core, Client SDKs, Tests, and Documentation

---

## 📋 Principles

1. **No Ad-Hoc Changes**: Every change follows planned structure
2. **Incremental but Complete**: Each phase is fully functional and testable
3. **Backward Compatibility Window**: Provide deprecation period where possible
4. **Test-Driven**: Tests updated before/with implementation
5. **Documentation-First**: Already completed (CONCEPTS.md, README.md)

---

## 🏗️ Multi-Phase Architecture

### Phase 0: Foundation & Planning
**Duration**: 1-2 days
**Goal**: Establish refactoring infrastructure and safety nets

**Tasks**:
1. ✅ **Documentation Complete** (DONE)
   - CONCEPTS.md with Loop App definitions
   - README.md with Loop App examples
   - Advanced Architecture section

2. **Create Refactoring Infrastructure**
   - Create feature branch: `refactor/task-to-loop-app`
   - Backup current database schema
   - Document API endpoint migration path
   - Create migration guide for SDK users

3. **Establish Safety Nets**
   - Ensure all tests are passing
   - Create comprehensive integration test suite
   - Set up rollback procedures
   - Document breaking changes

**Deliverable**: Refactoring infrastructure ready, all tests green

---

### Phase 1: Core Domain Models
**Duration**: 2-3 days
**Goal**: Rename core domain models and interfaces (foundation layer)

**Rationale**: Start from the bottom (domain layer) and work up to API layer

**Tasks**:

#### 1.1 Core Models (src/Loopai.Core/Models/)
```
TaskSpecification.cs → LoopAppSpecification.cs
- Class: TaskSpecification → LoopAppSpecification
- Properties: Keep as-is (conceptually correct)
```

#### 1.2 Core Interfaces (src/Loopai.Core/Interfaces/)
```
ITaskService.cs → ILoopAppService.cs
- Interface: ITaskService → ILoopAppService
- Methods:
  - CreateTaskAsync → CreateLoopAppAsync
  - GetTaskAsync → GetLoopAppAsync
  - etc.
```

#### 1.3 Core Services (src/Loopai.Core/Services/)
```
TaskService.cs → LoopAppService.cs
- Class: TaskService → LoopAppService
- Implement ILoopAppService
```

#### 1.4 Data Entities (src/Loopai.CloudApi/Data/Entities/)
```
TaskEntity.cs → LoopAppEntity.cs (if exists)
- Update DbContext configuration
```

#### 1.5 Update Tests
```
tests/Loopai.Core.Tests/
- TaskServiceTests.cs → LoopAppServiceTests.cs
- Update all test cases
```

**Validation**: All Core layer tests pass

---

### Phase 2: Database Migration
**Duration**: 1-2 days
**Goal**: Migrate database schema to use "LoopApp" terminology

**Tasks**:

#### 2.1 Create Database Migration
```csharp
// Migration: RenameTaskToLoopApp

public partial class RenameTaskToLoopApp : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Rename tables
        migrationBuilder.RenameTable(
            name: "Tasks",
            newName: "LoopApps");

        // Rename columns
        migrationBuilder.RenameColumn(
            table: "ProgramArtifacts",
            name: "TaskId",
            newName: "LoopAppId");

        migrationBuilder.RenameColumn(
            table: "ExecutionRecords",
            name: "TaskId",
            newName: "LoopAppId");

        // Update indexes
        migrationBuilder.RenameIndex(
            table: "LoopApps",
            name: "IX_Tasks_Name",
            newName: "IX_LoopApps_Name");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reverse migration
    }
}
```

#### 2.2 Update DbContext
```csharp
public DbSet<LoopAppEntity> LoopApps { get; set; }  // was: Tasks
```

#### 2.3 Update Repositories
```
EfTaskRepository.cs → EfLoopAppRepository.cs
ITaskRepository.cs → ILoopAppRepository.cs
```

**Validation**:
- Migration runs successfully
- All data migrated
- Repository tests pass

---

### Phase 3: API Layer (Breaking Change)
**Duration**: 2-3 days
**Goal**: Update API endpoints to use Loop App terminology

**Tasks**:

#### 3.1 Rename Controller
```
src/Loopai.CloudApi/Controllers/TasksController.cs
→ src/Loopai.CloudApi/Controllers/LoopAppsController.cs

Changes:
- Route: [Route("api/v{version:apiVersion}/loop-apps")]
- Endpoints:
  - POST   /api/v1/loop-apps/{loopAppId}/execute
  - POST   /api/v1/loop-apps
  - GET    /api/v1/loop-apps/{loopAppId}
  - GET    /api/v1/loop-apps/{loopAppId}/artifacts
  - GET    /api/v1/loop-apps/{loopAppId}/artifacts/{version}
```

#### 3.2 Update DTOs
```
src/Loopai.CloudApi/DTOs/
- CreateTaskRequest.cs → CreateLoopAppRequest.cs
- TaskResponse.cs → LoopAppResponse.cs
- ExecuteRequest.cs:
  - Property: TaskId → LoopAppId
- ExecuteResponse.cs:
  - Property: TaskId → LoopAppId
```

#### 3.3 Update Validators
```
src/Loopai.CloudApi/Validators/
- Update FluentValidation rules
- Update error messages
```

#### 3.4 Update API Tests
```
tests/Loopai.CloudApi.Tests/Controllers/
- TasksControllerTests.cs → LoopAppsControllerTests.cs
- Update all integration tests
```

**Validation**: All API tests pass

---

### Phase 4: Client SDKs (Breaking Change)
**Duration**: 3-4 days
**Goal**: Update all client SDKs to use Loop App terminology

#### 4.1 .NET SDK (sdk/dotnet/)
```
src/Loopai.Client/LoopaiClient.cs:
- Methods:
  - ExecuteAsync(taskId, ...) → ExecuteAsync(loopAppId, ...)
  - BatchExecuteAsync(taskId, ...) → BatchExecuteAsync(loopAppId, ...)

src/Loopai.Client/Models/:
- BatchExecuteRequest.cs:
  - Property: TaskId → LoopAppId
- BatchExecuteResponse.cs:
  - Property: TaskId → LoopAppId
```

#### 4.2 Python SDK (sdk/python/)
```
loopai/client.py:
- Methods:
  - execute(task_id, ...) → execute(loop_app_id, ...)
  - batch_execute(task_id, ...) → batch_execute(loop_app_id, ...)
```

#### 4.3 TypeScript SDK (sdk/typescript/)
```
src/client.ts:
- Methods:
  - execute({ taskId, ... }) → execute({ loopAppId, ... })
  - batchExecute({ taskId, ... }) → batchExecute({ loopAppId, ... })
```

#### 4.4 Update SDK Tests
```
- tests/Loopai.Client.Tests/
- sdk/python/tests/
- sdk/typescript/tests/
- Update all test data
```

#### 4.5 Update SDK Examples
```
- examples/Loopai.Examples.AspNetCore/
- sdk/python/examples/
- Update all example code
```

**Validation**: All SDK tests pass

---

### Phase 5: Integration Tests & Cross-SDK Compatibility
**Duration**: 2 days
**Goal**: Ensure all SDKs work together after refactoring

**Tasks**:

#### 5.1 Update Integration Tests
```
tests/integration/compatibility/
- test-cross-sdk.py
- Update all test scenarios
```

#### 5.2 Update Integration Test README
```
tests/integration/compatibility/README.md
- Update examples to use Loop App terminology
```

#### 5.3 End-to-End Validation
```
tests/Loopai.Client.IntegrationTests/
- LoopaiClientIntegrationTests.cs
- Update all integration scenarios
```

**Validation**: Full integration test suite passes

---

### Phase 6: Documentation & Examples
**Duration**: 1-2 days
**Goal**: Update all remaining documentation

**Tasks**:

#### 6.1 Update Core Documentation
```
docs/
- ✅ CONCEPTS.md (DONE)
- ✅ README.md (DONE)
- ARCHITECTURE.md - Update diagrams and terminology
- DEPLOYMENT.md - Update deployment examples
- GETTING_STARTED.md - Update quick start guide
```

#### 6.2 Update API Documentation
```
src/Loopai.CloudApi/
- Update OpenAPI/Swagger documentation
- Update XML documentation comments
- Update README.md
```

#### 6.3 Update SDK Documentation
```
sdk/dotnet/README.md
sdk/python/README.md
sdk/typescript/README.md
- Update all code examples
```

#### 6.4 Update Examples
```
examples/Loopai.Examples.AspNetCore/
- Controllers/ClassificationController.cs
- Controllers/BatchController.cs
- Update README.md
```

**Validation**: Documentation review complete

---

### Phase 7: Migration Guide & Release
**Duration**: 1 day
**Goal**: Create migration guide and prepare release

**Tasks**:

#### 7.1 Create Migration Guide
```
docs/MIGRATION_V1_TO_V2.md
- Breaking changes summary
- Step-by-step migration instructions
- Code examples (before/after)
- FAQ
```

#### 7.2 Update CHANGELOG
```
CHANGELOG.md
- Document all breaking changes
- Provide migration path
```

#### 7.3 Version Bumps
```
- Loopai.Core: 0.x.x → 1.0.0
- Loopai.Client: 0.x.x → 1.0.0
- Loopai.CloudApi: 0.x.x → 1.0.0
- Python SDK: 0.x.x → 1.0.0
- TypeScript SDK: 0.x.x → 1.0.0
```

#### 7.4 Release Preparation
```
- Create release branch: release/v1.0.0
- Tag release: v1.0.0
- Generate release notes
- Update GitHub releases
```

**Validation**: Migration guide tested with real scenarios

---

## 📊 Impact Analysis

### Files to Update (Estimated)

| Layer | Files | Complexity |
|-------|-------|------------|
| Core Models & Interfaces | 10-15 | Medium |
| Database & Repositories | 8-12 | High |
| API Controllers & DTOs | 10-15 | Medium |
| .NET SDK | 6-10 | Medium |
| Python SDK | 4-6 | Low |
| TypeScript SDK | 4-6 | Low |
| Tests (Unit) | 20-30 | Medium |
| Tests (Integration) | 10-15 | Medium |
| Documentation | 15-20 | Low |
| Examples | 5-10 | Low |
| **Total** | **~91-140** | **Medium-High** |

### Breaking Changes

**API Endpoints**:
```diff
- POST /api/v1/tasks/{taskId}/execute
+ POST /api/v1/loop-apps/{loopAppId}/execute

- POST /api/v1/tasks
+ POST /api/v1/loop-apps

- GET /api/v1/tasks/{taskId}
+ GET /api/v1/loop-apps/{loopAppId}
```

**Database Schema**:
```diff
- Tables: Tasks
+ Tables: LoopApps

- Columns: TaskId
+ Columns: LoopAppId
```

**SDK Methods**:
```diff
# .NET
- client.ExecuteAsync(taskId, ...)
+ client.ExecuteAsync(loopAppId, ...)

# Python
- client.execute(task_id=...)
+ client.execute(loop_app_id=...)

# TypeScript
- client.execute({ taskId: ... })
+ client.execute({ loopAppId: ... })
```

---

## ⚠️ Risk Mitigation

### High Risks

1. **Database Migration Failure**
   - **Mitigation**: Test migration on staging first
   - **Rollback**: Keep Down() migration fully implemented
   - **Backup**: Full database backup before migration

2. **Breaking Client Applications**
   - **Mitigation**: Provide 2-week deprecation notice
   - **Support**: Maintain v0.x branch for critical fixes
   - **Documentation**: Clear migration guide

3. **Integration Test Failures**
   - **Mitigation**: Update tests incrementally with implementation
   - **Validation**: Run full test suite after each phase
   - **CI/CD**: All tests must pass before merging

### Medium Risks

1. **Inconsistent Naming**
   - **Mitigation**: Use consistent naming conventions document
   - **Review**: Code review checklist for naming consistency

2. **Documentation Lag**
   - **Mitigation**: Update docs in parallel with code
   - **Validation**: Documentation review before release

---

## ✅ Definition of Done

### Per Phase
- [ ] All code changes implemented
- [ ] All tests updated and passing
- [ ] Code review completed
- [ ] Documentation updated
- [ ] Phase validated in staging environment

### Overall Project
- [ ] All 7 phases complete
- [ ] All tests passing (Unit + Integration)
- [ ] Migration guide created and tested
- [ ] CHANGELOG updated
- [ ] Version bumps applied
- [ ] Release notes generated
- [ ] Tagged and released v1.0.0

---

## 📅 Timeline Estimate

| Phase | Duration | Dependencies |
|-------|----------|--------------|
| Phase 0: Foundation | 1-2 days | None |
| Phase 1: Core Models | 2-3 days | Phase 0 |
| Phase 2: Database | 1-2 days | Phase 1 |
| Phase 3: API Layer | 2-3 days | Phase 2 |
| Phase 4: Client SDKs | 3-4 days | Phase 3 |
| Phase 5: Integration Tests | 2 days | Phase 4 |
| Phase 6: Documentation | 1-2 days | Phase 5 |
| Phase 7: Release | 1 day | Phase 6 |
| **Total** | **13-19 days** | Sequential |

**Recommended**: 3-4 weeks with buffer for testing and validation

---

## 🎯 Next Steps

1. **Review and Approve This Plan**
   - Team review of refactoring strategy
   - Stakeholder approval for breaking changes
   - Schedule refactoring window

2. **Start Phase 0**
   - Create feature branch
   - Set up infrastructure
   - Ensure all tests are passing

3. **Execute Incrementally**
   - Complete one phase at a time
   - Validate each phase before proceeding
   - Merge to main only when fully complete

---

**Document Owner**: Engineering Team
**Last Updated**: 2025-10-29
**Status**: Ready for Execution
