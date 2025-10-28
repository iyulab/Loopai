---
title: What is Loopai?
sidebar_position: 1
description: Understanding the Loopai framework and its core value proposition
---

# What is Loopai?

Loopai is a **program synthesis and execution framework** that transforms natural language specifications into executable programs. It provides infrastructure for building adaptive AI-powered applications with complete observability and data sovereignty.

## Core Architecture

- **Cloud API (C#/.NET 8)**: REST API for program generation, execution, and lifecycle management
- **Client SDKs**: .NET, Python, TypeScript with full async/await support
- **Edge Runtime**: Program execution with low latency
- **Plugin System**: Extensible architecture for custom validation and event handling
- **Kubernetes-Ready**: Deployment-ready with Helm charts and health probes

## The Approach

Instead of making repeated calls to language models, Loopai:

1. Generates a program once using an LLM
2. Deploys the program to cloud or customer infrastructure
3. Executes the program locally with low latency
4. Stores execution data locally
5. Samples data for validation and improvement

This approach offers:
- **Cost Efficiency**: Reduce recurring API costs
- **Low Latency**: Local execution eliminates network calls
- **Data Privacy**: All execution data stays on your infrastructure

## Who Should Use Loopai?

Loopai is designed for **developers building AI-powered applications** who need:

1. **Cost Efficiency**: Reduce LLM API costs through program synthesis
2. **Low Latency**: Sub-millisecond execution for production workloads
3. **Data Control**: Keep sensitive data on your infrastructure
4. **Observability**: Full visibility into program execution
5. **Multi-Language Support**: SDK support for .NET, Python, TypeScript

## When to Use Loopai

### Good Fit

**Text Classification**:
- Spam detection, content moderation
- Topic categorization, intent classification
- Language detection, sentiment analysis

**Pattern Recognition**:
- Email categorization
- Log parsing and classification
- Data validation

**Characteristics**:
- High volume processing (10K+ requests/day)
- Pattern-based logic
- Acceptable accuracy (85-95%)
- Low latency requirements

### Not Recommended

- **Creative Generation**: Novel content creation, story writing
- **Complex Reasoning**: Multi-step inference, mathematical proofs
- **High-Stakes Decisions**: Applications requiring >98% accuracy (medical, legal)

## Next Steps

- [Key Features](./key-features) - Explore Loopai's capabilities
- [Use Cases](./use-cases) - See real-world applications
- [Getting Started](../guides/getting-started) - Start building with Loopai
