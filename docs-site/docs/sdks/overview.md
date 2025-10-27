---
title: SDK Overview
sidebar_position: 1
description: Overview of Loopai client SDKs and their capabilities
---

# SDK Overview

Loopai provides production-ready client SDKs for .NET, Python, and TypeScript with consistent APIs and modern development patterns.

## Available SDKs

### .NET SDK

**Package**: `Loopai.Client` (NuGet)
**Target**: .NET 8.0+
**License**: MIT

```bash
dotnet add package Loopai.Client
```

**Features**:
- ASP.NET Core dependency injection
- Polly v8 retry policies
- IHttpClientFactory integration
- Comprehensive logging

[View .NET SDK Documentation](./dotnet) →

### Python SDK

**Package**: `loopai` (PyPI)
**Requires**: Python 3.9+
**License**: MIT

```bash
pip install loopai
```

**Features**:
- Full async/await support
- Pydantic v2 models
- Context manager support
- Type hints throughout

[View Python SDK Documentation](./python) →

### TypeScript SDK

**Package**: `@loopai/sdk` (npm)
**Requires**: Node.js 16+
**License**: MIT

```bash
npm install @loopai/sdk
```

**Features**:
- Full TypeScript types
- Promise-based async/await
- Node.js and browser support
- Tree-shakable modules

[View TypeScript SDK Documentation](./typescript) →

## Common Features

All SDKs share these capabilities:

### ✅ Core Operations

| Operation | Description | All SDKs |
|-----------|-------------|----------|
| Task Creation | Create AI-powered tasks | ✅ |
| Task Execution | Execute tasks with input | ✅ |
| Batch Operations | Bulk processing | ✅ |
| Task Management | CRUD operations | ✅ |
| Error Handling | Comprehensive exceptions | ✅ |

### ✅ Advanced Features

| Feature | Description | .NET | Python | TypeScript |
|---------|-------------|------|--------|------------|
| Async/Await | Modern async patterns | ✅ | ✅ | ✅ |
| Automatic Retry | Exponential backoff | ✅ | ✅ | ✅ |
| Type Safety | Strong typing | ✅ | ✅ | ✅ |
| Batch Concurrency | Concurrent execution | ✅ | ✅ | ✅ |
| DI Integration | Dependency injection | ✅ | ❌ | ❌ |
| Context Manager | Resource management | ❌ | ✅ | ❌ |
| Browser Support | Browser compatibility | ❌ | ❌ | ✅ |

### ✅ Quality Standards

| Metric | Target | Status |
|--------|--------|--------|
| Test Coverage | >90% | ✅ 92-94% |
| Integration Tests | 14 per SDK | ✅ 42 total |
| Cross-SDK Compatibility | 100% | ✅ Verified |
| Documentation | Complete | ✅ All SDKs |

## Quick Start Comparison

### .NET
```csharp
// Dependency Injection
builder.Services.AddLoopaiClient(options =>
{
    options.BaseUrl = "http://localhost:8080";
});

// Usage
var result = await _loopai.ExecuteAsync(taskId, input);
```

### Python
```python
# Context Manager
async with LoopaiClient("http://localhost:8080") as client:
    result = await client.execute(task_id, input_data)
```

### TypeScript
```typescript
// Promise-based
const client = new LoopaiClient({ baseUrl: 'http://localhost:8080' });
const result = await client.execute({ taskId, input });
```

## Performance

All SDKs deliver consistent performance:

| SDK | Avg Response Time | Throughput (req/s) | Memory Usage |
|-----|------------------|-------------------|--------------|
| .NET | 45.2ms | 34.6 | 62 MB avg |
| Python | 43.8ms | 35.1 | 52 MB avg |
| TypeScript | 44.5ms | 34.2 | 58 MB avg |

## Installation Guides

### .NET

```bash
# NuGet Package Manager
Install-Package Loopai.Client

# .NET CLI
dotnet add package Loopai.Client

# Package Reference
<PackageReference Include="Loopai.Client" Version="0.1.0" />
```

### Python

```bash
# pip
pip install loopai

# poetry
poetry add loopai

# requirements.txt
loopai==0.1.0
```

### TypeScript

```bash
# npm
npm install @loopai/sdk

# yarn
yarn add @loopai/sdk

# pnpm
pnpm add @loopai/sdk
```

## Examples

Each SDK includes comprehensive examples:

- **Basic Usage**: Task creation and execution
- **Batch Processing**: Bulk operations with concurrency
- **Error Handling**: Exception handling patterns

Browse SDK-specific examples:
- [.NET Examples](./dotnet#examples)
- [Python Examples](./python#examples)
- [TypeScript Examples](./typescript#examples)

## SDK Roadmap

### Coming Soon

- **Go SDK** (Phase 12.2)
  - Native Go client
  - Context support
  - goroutine-safe

- **Streaming API** (Phase 12.1)
  - Real-time results
  - Server-Sent Events
  - Available in all SDKs

### Under Consideration

- Java SDK
- Ruby SDK
- Rust SDK
- PHP SDK

## Support

- **Documentation**: SDK-specific guides
- **Examples**: Working code samples
- **GitHub**: [Issues](https://github.com/iyulab/loopai/issues) and [Discussions](https://github.com/iyulab/loopai/discussions)
- **Integration Tests**: [Test Results](https://github.com/iyulab/loopai/tree/main/tests/integration)

## Next Steps

Choose your SDK and start building:

1. [.NET SDK Guide](./dotnet) - For .NET applications
2. [Python SDK Guide](./python) - For Python applications
3. [TypeScript SDK Guide](./typescript) - For JavaScript/TypeScript applications
