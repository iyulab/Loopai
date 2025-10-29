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
┌─────────────────────────────────────┐
│     Consumer Applications           │ (Your End-User Apps)
│  (E-commerce, Email, Support, etc.) │
└──────────────┬──────────────────────┘
               │ Uses multiple Loop Apps
               ▼
┌─────────────────────────────────────┐
│         Loop Apps                   │ (Individual Task Instances)
│  spam-detector, sentiment-analyzer  │
│  email-categorizer, etc.            │
└──────────────┬──────────────────────┘
               │ Built on & Managed by
               ▼
┌─────────────────────────────────────┐
│     Loopai Framework                │ (Infrastructure Middleware)
├─────────────────────────────────────┤
│ Cloud Platform                      │ → Generate & Improve Programs
│ Edge Runtime                        │ → Execute & Store Data
│ Client SDKs                         │ → .NET, Python, TypeScript
│ CodeBeaker                          │ → Multi-Language Execution
└─────────────────────────────────────┘
```

**Loopai Framework**: Infrastructure middleware for program synthesis and execution
**Loop Apps**: Individual task instances (e.g., spam-detector-001, sentiment-analyzer-001)
**Consumer Apps**: Your applications that use multiple Loop Apps
**CodeBeaker**: Multi-language execution engine (Python, JavaScript, Go, C#)

> **New to Loopai?** Read [CONCEPTS.md](docs/CONCEPTS.md) for a clear explanation of Loop Apps and how they relate to the framework.

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

Each Loop App has its own execution endpoint. Here's how to execute programs:

**.NET**:
```csharp
builder.Services.AddLoopaiClient(options =>
{
    options.BaseUrl = "http://localhost:8080";
});

// Execute spam detector Loop App
var result = await _loopai.ExecuteAsync("spam-detector-001", new { text = "Buy now!" });
// Result: { "output": "spam", "latency_ms": 3.2 }
```

**Python**:
```python
from loopai import LoopaiClient

async with LoopaiClient("http://localhost:8080") as client:
    # Execute sentiment analyzer Loop App
    result = await client.execute("sentiment-analyzer-001",
                                  {"feedback": "Great product!"})
    # Result: { "output": "positive", "latency_ms": 4.1 }
```

**TypeScript**:
```typescript
import { LoopaiClient } from '@loopai/sdk';

const client = new LoopaiClient({ baseUrl: 'http://localhost:8080' });

// Execute email categorizer Loop App
const result = await client.execute({
  loopAppId: 'email-categorizer-001',
  input: { subject: 'Meeting tomorrow', body: '...' }
});
// Result: { output: 'inbox', latency_ms: 2.8 }
```

> **Note**: Loop App IDs follow the pattern `{task-name}-{instance}` (e.g., `spam-detector-001`). See [CONCEPTS.md](docs/CONCEPTS.md) for details.

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
