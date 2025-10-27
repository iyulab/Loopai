---
slug: welcome
title: Welcome to Loopai Documentation
authors: [caveman, junhyung]
tags: [loopai, announcement, documentation]
---

# Welcome to Loopai Documentation

We're excited to announce the launch of the official Loopai documentation site!

<!-- truncate -->

## What is Loopai?

Loopai is a **Human-in-the-Loop AI Self-Improvement Framework** - infrastructure middleware for building adaptive AI-powered applications with complete observability and data sovereignty.

### Key Highlights

- 🚀 **Multi-Language SDKs**: .NET, Python, TypeScript
- ⚡ **High Performance**: &lt;10ms execution latency
- 💰 **Cost Efficient**: 82-97% cost reduction vs direct LLM
- 🔌 **Plugin System**: Extensible architecture
- 📊 **Production Ready**: 170+ tests passing

## Current Status: v0.3 Complete

We've successfully completed Phase 11, delivering:

- ✅ Three production-ready client SDKs
- ✅ 42 integration tests (100% passing)
- ✅ Cross-SDK compatibility verified
- ✅ Complete documentation

### What's New in v0.3

#### Multi-Language SDK Ecosystem

**[.NET SDK](/docs/sdks/dotnet)**: Production-ready with ASP.NET Core DI support
```csharp
builder.Services.AddLoopaiClient(options =>
{
    options.BaseUrl = "http://localhost:8080";
});
```

**[Python SDK](/docs/sdks/python)**: Full async/await with Pydantic v2
```python
async with LoopaiClient("http://localhost:8080") as client:
    result = await client.execute(task_id, input_data)
```

**[TypeScript SDK](/docs/sdks/typescript)**: Complete type safety
```typescript
const client = new LoopaiClient({ baseUrl: 'http://localhost:8080' });
const result = await client.execute({ taskId, input });
```

#### Integration Testing

- 14 tests per SDK covering all core functionality
- Cross-SDK compatibility verification
- CI/CD ready with GitHub Actions

## What's Next: v0.4

We're now working on infrastructure enhancements:

1. **SDK Feature Expansion**
   - Streaming API support
   - Go SDK implementation
   - Enhanced error handling

2. **Performance & Scalability**
   - Response caching layer
   - Connection pooling optimization
   - Performance benchmarking

3. **Developer Experience**
   - CLI tool for management
   - Interactive API playground
   - VS Code extension

## Get Started

Ready to try Loopai?

1. **[Install SDK](/docs/guides/getting-started)** - Choose .NET, Python, or TypeScript
2. **[Explore Examples](/docs/examples/spam-detection)** - See working implementations
3. **[Join Community](https://github.com/iyulab/Loopai/discussions)** - Ask questions and share feedback

## Documentation Structure

Our documentation is organized for easy navigation:

- **[Introduction](/docs/intro)** - What is Loopai and why use it
- **[Guides](/docs/guides/getting-started)** - Step-by-step tutorials
- **[SDKs](/docs/sdks/overview)** - Client SDK documentation
- **[API](/docs/api/overview)** - REST API reference
- **[Examples](/docs/examples/spam-detection)** - Real-world use cases

## Community & Support

- **GitHub**: [iyulab/Loopai](https://github.com/iyulab/Loopai)
- **Issues**: [Report bugs](https://github.com/iyulab/Loopai/issues)
- **Discussions**: [Ask questions](https://github.com/iyulab/Loopai/discussions)

## Stay Updated

Follow our blog for:
- Release announcements
- Technical deep dives
- Use case tutorials
- Performance updates

Thank you for your interest in Loopai! We're excited to see what you'll build with it.

---

*The Loopai Team*
