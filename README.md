# Loopai

Human-in-the-Loop AI Self-Improvement Framework

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)

---

## What is Loopai?

Loopai is a **program synthesis and execution framework** that transforms natural language specifications into executable programs. It provides infrastructure for building adaptive AI-powered applications with multi-language support and complete data sovereignty.

### Core Concept

Instead of making repeated calls to language models, Loopai generates programs once and executes them locally. This approach offers:

- **Cost Efficiency**: Programs run locally instead of calling remote LLM APIs
- **Low Latency**: Local execution eliminates network round-trips
- **Data Privacy**: All execution data stays on your infrastructure
- **Continuous Improvement**: Programs can be refined based on collected data

### Architecture Overview

```
Cloud Platform (Generate & Improve)
    ↓
Edge Runtime (Execute & Store)
    ↓
Client SDKs (.NET, Python, TypeScript)
    ↓
Local Filesystem (Execution logs)
```

**Cloud Platform**: C#/.NET API for program generation and management
**Client SDKs**: Multi-language clients (.NET, Python, TypeScript)
**Edge Runtime**: Program executor with dataset management
**CodeBeaker**: Multi-language execution engine (Python, JavaScript, Go, C#)

---

## Quick Start

### Install Client SDK

**.NET**:
```bash
dotnet add package Loopai.Client
```

**Python**:
```bash
pip install loopai
```

**TypeScript**:
```bash
npm install @loopai/sdk
```

### Start API Server

```bash
cd src/Loopai.CloudApi
dotnet run
```

### Basic Usage

**.NET**:
```csharp
builder.Services.AddLoopaiClient(options =>
{
    options.BaseUrl = "http://localhost:8080";
});

// Use in controller
var result = await _loopai.ExecuteAsync(taskId, new { text = "sample" });
```

**Python**:
```python
from loopai import LoopaiClient

async with LoopaiClient("http://localhost:8080") as client:
    result = await client.execute(task_id, input_data)
```

**TypeScript**:
```typescript
import { LoopaiClient } from '@loopai/sdk';

const client = new LoopaiClient({ baseUrl: 'http://localhost:8080' });
const result = await client.execute({ taskId, input });
```

---

## Key Features

### Multi-Language SDK Support

Production-ready client SDKs for .NET, Python, and TypeScript with:
- Async/await support
- Automatic retry with exponential backoff
- Batch operations with concurrency control
- Type safety and comprehensive error handling

### Plugin System

Extensible architecture for custom validation, sampling, and event handling:

```csharp
public class MyValidatorPlugin : IValidatorPlugin
{
    public string Name => "my-validator";

    public Task<ValidationResult> ValidateAsync(
        ExecutionRecord execution,
        ValidationContext context,
        CancellationToken ct)
    {
        // Custom validation logic
    }
}

registry.Register<IValidatorPlugin>(new MyValidatorPlugin());
```

Plugin types:
- **Validators**: Custom execution result validation
- **Samplers**: Custom sampling strategies
- **Webhook Handlers**: Event-driven integrations

### Batch Operations

Efficient bulk processing with concurrency control:

```csharp
var request = new BatchExecuteRequest
{
    TaskId = taskId,
    Items = items,
    MaxConcurrency = 10
};

var response = await client.BatchExecuteAsync(request);
```

### Multi-Language Program Execution

CodeBeaker provides isolated execution environments for multiple languages:
- Python
- JavaScript (Deno runtime)
- Go
- C#

Programs execute in sandboxed environments with resource limits and timeout control.

---

## Use Cases

### Good Fit

- **Text Classification**: Spam detection, content moderation, topic categorization
- **Pattern Recognition**: Email categorization, log parsing, data validation
- **High Volume Processing**: Applications with 10K+ requests/day
- **Low Latency Requirements**: Tasks requiring <50ms response time

### Not Recommended

- **Creative Generation**: Novel content creation, story writing
- **Complex Reasoning**: Multi-step inference, mathematical proofs
- **High-Stakes Decisions**: Applications requiring >98% accuracy (medical, legal)

---

## Project Structure

```
Loopai/
├── sdk/
│   ├── dotnet/              # .NET Client SDK
│   ├── python/              # Python Client SDK
│   └── typescript/          # TypeScript SDK
├── src/
│   ├── Loopai.CloudApi/     # C# Cloud API
│   ├── Loopai.Core/         # Core library
│   └── Loopai.Client/       # .NET SDK
├── tests/
│   ├── integration/         # SDK integration tests
│   └── Loopai.Core.Tests/   # Unit tests
├── docs/
│   ├── ARCHITECTURE.md      # Architecture overview
│   ├── GETTING_STARTED.md   # Quick start guide
│   ├── DEPLOYMENT.md        # Deployment guide
│   └── PLUGIN_DEVELOPMENT_GUIDE.md
└── README.md
```

---

## Documentation

### Getting Started
- [Installation Guide](docs/GETTING_STARTED.md)
- [Development Guide](docs/DEVELOPMENT.md)
- [Deployment Guide](docs/DEPLOYMENT.md)

### SDK Documentation
- [.NET SDK](sdk/dotnet/README.md)
- [Python SDK](sdk/python/README.md)
- [TypeScript SDK](sdk/typescript/README.md)

### Architecture
- [Architecture Overview](docs/ARCHITECTURE.md)
- [Plugin Development](docs/PLUGIN_DEVELOPMENT_GUIDE.md)

---

## Development

```bash
# Clone repository
git clone https://github.com/iyulab/loopai.git
cd loopai

# Start API server
cd src/Loopai.CloudApi
dotnet run

# Run tests
dotnet test
```

---

## Contributing

Contributions are welcome! Areas for contribution:
- SDK enhancements and new language support
- Custom plugins (validators, samplers, webhooks)
- Documentation and examples
- Testing and performance benchmarks

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

## License

MIT License - see [LICENSE](LICENSE) for details.

---

## Contact

- **GitHub Issues**: [github.com/iyulab/loopai/issues](https://github.com/iyulab/loopai/issues)
- **Discussions**: [github.com/iyulab/loopai/discussions](https://github.com/iyulab/loopai/discussions)

---

Built on research in program synthesis, knowledge distillation, and human-in-the-loop AI.
