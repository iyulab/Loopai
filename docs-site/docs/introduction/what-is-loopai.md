---
title: What is Loopai?
sidebar_position: 1
description: Understanding the Loopai framework and its core value proposition
---

# What is Loopai?

Loopai is a **program synthesis and execution framework** - infrastructure middleware for building adaptive AI-powered applications with complete observability and data sovereignty.

## Framework Identity

- **Cloud API (C#/.NET 8)**: REST API for program generation, execution, and lifecycle management
- **Client SDKs**: .NET, Python, TypeScript with full async/await support
- **Edge Runtime (Deno)**: JavaScript/TypeScript/Python program execution with &lt;10ms latency
- **Framework Integration**: Webhook events, Prometheus metrics, OpenTelemetry tracing, Plugin system
- **Kubernetes-Ready**: Helm charts, health probes, horizontal autoscaling, security contexts

## Core Capability

Transform expensive LLM calls into self-improving programs that run anywhere with complete observability and data sovereignty.

## The Problem

Modern NLP applications rely on repeated LLM calls:

- **High Cost**: $0.002-0.03 per call = $2,000-30,000/month for 1M requests
- **High Latency**: 500-2000ms per call hurts user experience
- **No Data Sovereignty**: All data sent to cloud providers

## The Loopai Solution

**Build Once, Run Anywhere**:

- ✅ LLM generates program once ($0.20, 10s)
- ✅ Deploy to cloud OR customer infrastructure
- ✅ Execute locally (&lt;10ms, ~$0.00001/call)
- ✅ Store all data locally (JSONL logs)
- ✅ Sample 5% for validation
- ✅ Continuous improvement via cloud

**Results**:
- **Cost Reduction**: 82-97% vs direct LLM
- **Speed Improvement**: 50,000-100,000x faster
- **Data Sovereignty**: 100% data stays local

## Who Should Use Loopai?

Loopai is ideal for **developers building AI-powered applications** who need:

1. **Cost Efficiency**: Reduce LLM API costs by 80%+ through program synthesis
2. **Low Latency**: Sub-10ms execution for production workloads
3. **Data Control**: Keep sensitive data on your infrastructure
4. **Observability**: Full visibility into program execution and performance
5. **Framework Flexibility**: Multi-language SDK support (.NET, Python, TypeScript)

## When to Use Loopai

### ✅ Excellent Fit

**Text Classification**:
- Spam detection, content moderation
- Topic categorization, intent classification
- Language detection, sentiment analysis

**Pattern Recognition**:
- Email categorization
- Log parsing and classification
- Data validation

**Characteristics**:
- High volume (10K+ requests/day)
- Pattern-based logic
- Acceptable accuracy (85-95%)
- Low latency requirement (&lt;50ms)

### ⚠️ Not Recommended

- **Creative Generation**: Novel content, story writing
- **Complex Reasoning**: Multi-step inference, mathematical proofs
- **High-Stakes Decisions**: Medical diagnosis, legal advice (>98% accuracy required)

## Next Steps

- [Key Features](./key-features) - Explore Loopai's capabilities
- [Use Cases](./use-cases) - See real-world applications
- [Getting Started](../guides/getting-started) - Start building with Loopai
