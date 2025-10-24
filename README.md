# Loopai

**Human-in-the-Loop AI Self-Improvement Framework for Cost-Efficient LLM Applications**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Python](https://img.shields.io/badge/python-3.9+-blue.svg)](https://www.python.org/downloads/)
[![.NET](https://img.shields.io/badge/.NET-8.0+-purple.svg)](https://dotnet.microsoft.com/)

---

## 🎯 What is Loopai?

Loopai is a framework that transforms expensive repeated LLM inference calls into lightweight, self-improving programs. Instead of calling an LLM for every request, Loopai generates a program once, validates it against LLM ground truth selectively, and automatically improves it when errors are detected.

### The Core Problem

Modern NLP applications often require repeated LLM inference for tasks like text classification, sentiment analysis, or log parsing. When processing millions of requests, the costs become prohibitive:

- **High Cost**: $0.002-0.03 per LLM call adds up to thousands monthly
- **High Latency**: 500-2000ms per LLM inference impacts user experience
- **Scalability Issues**: Peak loads require expensive infrastructure

### The Loopai Approach

**Generate Once, Execute Millions**: LLM generates an optimized program once (~5 seconds), then that program executes at near-zero cost (~5ms, <$0.00001 per call).

**Validate Selectively**: Sample 1-10% of executions and validate against LLM oracle, achieving cost reduction without sacrificing reliability.

**Improve Automatically**: When validation detects discrepancies, the system analyzes patterns and regenerates improved programs autonomously.

**Escalate When Needed**: Complex errors that require specification refinement are routed to developers with full context.

---

## 💡 Key Concepts

### Program Synthesis

Loopai uses LLMs to generate executable programs (not just responses) from natural language specifications. These programs can be:

- **Rule-based classifiers** with keyword matching and heuristics
- **Traditional ML models** (scikit-learn, lightweight neural networks)
- **Hybrid approaches** combining multiple strategies
- **Multi-tier implementations** (simple rules → ML models → complex logic)

The goal is to distill the LLM's knowledge into a deterministic, fast-executing artifact.

### Oracle-Based Validation

The LLM serves as the "ground truth" oracle. During runtime:

- A sampling strategy selects which executions to validate (1-10%)
- Selected inputs are sent to both the generated program and the LLM oracle
- Outputs are compared to detect discrepancies
- Validation results inform the improvement process and confidence metrics

This approach balances cost (expensive LLM validation) with quality assurance.

### Continuous Self-Improvement

When validation failure rate exceeds configured thresholds:

**Automatic Path**:
- Analyze failure patterns across validated samples
- Identify systematic errors vs random edge cases
- Regenerate improved program automatically
- Deploy with A/B testing (gradual rollout)

**Human Escalation Path**:
- Complex errors requiring specification refinement
- Ambiguous requirements discovered during operation
- Edge cases beyond program generation capabilities
- Routed to developers with full context and examples

### Cost-Accuracy Tradeoff

Loopai provides explicit control over the cost-accuracy tradeoff through configurable sampling rates:

- **Conservative** (10% sampling): ~90% cost reduction, minimal accuracy loss (<2%)
- **Balanced** (5% sampling): ~95% cost reduction, acceptable accuracy loss (<5%)
- **Aggressive** (1% sampling): ~99% cost reduction, suitable for non-critical tasks

The framework adapts sampling rates based on program confidence and historical validation success.

---

## 🏗️ Architecture Overview

```
┌────────────────────────────────────────────────────────────────┐
│                      Application Layer                          │
│   (Your NLP application using Loopai for cost optimization)    │
└──────────────────────────────┬─────────────────────────────────┘
                               │
                               ▼
┌────────────────────────────────────────────────────────────────┐
│                       Loopai Framework                          │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────┐      ┌──────────────┐      ┌──────────────┐│
│  │   Program    │─────▶│   Runtime    │─────▶│  Validation  ││
│  │  Generator   │      │   Executor   │      │    Engine    ││
│  └──────────────┘      └──────────────┘      └──────────────┘│
│         │                     │                       │        │
│         │                     │                       │        │
│         └─────────────────────┼───────────────────────┘        │
│                               ▼                                │
│                      ┌─────────────────┐                       │
│                      │  Improvement    │                       │
│                      │     Engine      │                       │
│                      └────────┬────────┘                       │
│                               │                                │
│                      ┌────────▼─────────┐                      │
│                      │      Human       │                      │
│                      │   Escalation     │                      │
│                      └──────────────────┘                      │
└────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

#### 1. Program Generator

**Role**: Synthesizes executable programs from natural language specifications

**Inputs**:
- Task description (natural language)
- Input/output schemas (type definitions)
- Example input-output pairs
- Performance requirements (latency, accuracy targets)

**Outputs**:
- Executable program in target language (Python, C#)
- Program metadata (complexity, estimated accuracy, confidence)
- Initial test results

**Generation Strategies**:
- Simple pattern matching and rule extraction
- Template-based synthesis for common tasks
- ML model generation (scikit-learn pipelines)
- Hybrid multi-tier approaches

#### 2. Runtime Executor

**Role**: Executes generated programs with intelligent sampling

**Responsibilities**:
- Execute programs with sub-10ms latency
- Implement sampling strategies (random, stratified, uncertainty-based)
- Route sampled data to validation engine
- Collect execution metrics (latency, success rate, error types)
- Handle program failures gracefully

**Sampling Strategies**:
- **Random Sampling**: Uniform probability across all executions
- **Stratified Sampling**: Ensure coverage across input distributions
- **Uncertainty Sampling**: Prioritize executions where program is uncertain
- **Adaptive Sampling**: Adjust rate based on validation success history

#### 3. Validation Engine

**Role**: Validates program outputs against LLM ground truth

**Process**:
1. Receive sampled inputs from runtime executor
2. Query LLM oracle for authoritative output
3. Compare program output vs oracle output
4. Log discrepancies with full context
5. Calculate validation metrics (success rate, error patterns)

**Multi-Tier Validation** (cost optimization):
- **Tier 1** (Free): Syntax, type checking, basic assertions
- **Tier 2** (Cheap): Unit tests, format validation
- **Tier 3** (Expensive): LLM oracle validation

Only 10-30% of samples reach expensive Tier 3.

#### 4. Improvement Engine

**Role**: Analyzes failures and orchestrates program improvement

**Automatic Improvement Path**:
- Aggregate validation failures over time window
- Identify systematic error patterns
- Generate improvement hypotheses
- Regenerate program with failure examples as additional training data
- A/B test new version (10% traffic initially)
- Gradual rollout based on success metrics

**Human Escalation Criteria**:
- Validation success rate < threshold (e.g., 85%)
- Repeated improvement attempts fail (e.g., 3+ iterations)
- Novel error patterns detected
- Specification ambiguity identified

**Escalation Process**:
- Package error context (failing examples, patterns, hypotheses)
- Create actionable ticket with reproduction steps
- Notify developers via configured channels
- Wait for specification refinement or manual fix
- Resume automatic improvement with updated specification

---

## 📊 Performance Characteristics

### Cost Reduction

| Sampling Rate | Cost Reduction | Use Case |
|--------------|----------------|----------|
| 10% | ~90% | High-stakes tasks (compliance, medical) |
| 5% | ~95% | Production workloads (recommended) |
| 1% | ~98% | High-volume, low-risk tasks |

**Break-even Point**: Typically profitable at ~1,000+ requests per task

### Latency Improvement

| Approach | Latency | Throughput |
|----------|---------|------------|
| Direct LLM | 500-2000ms | 0.5-2 req/sec |
| Loopai Program | 5-20ms | 50-200 req/sec |
| **Improvement** | **~100x faster** | **~100x higher** |

### Accuracy Retention

| Sampling Rate | Typical Accuracy Loss |
|--------------|----------------------|
| 10% | <2% |
| 5% | 2-5% |
| 1% | 5-10% |

Accuracy depends on task complexity and program quality. Simple classification tasks retain 95%+ accuracy even with 1% sampling.

---

## 🎯 Ideal Use Cases

### ✅ Excellent Fit

**Text Classification**
- Spam detection, content moderation
- Topic categorization, intent classification
- Language detection

**Sentiment Analysis**
- Product review analysis
- Customer feedback processing
- Social media monitoring

**Log Analysis**
- Error pattern extraction
- Structured data parsing
- Anomaly detection

**Data Validation**
- Email/phone validation
- Format checking (dates, IDs, addresses)
- Business rule validation

**Characteristics of Good Fits**:
- High volume (10K+ requests/day)
- Pattern-based logic
- Acceptable accuracy threshold (85-95%)
- Low latency requirement (<50ms)

### ⚠️ Not Recommended

**Creative Generation**
- Novel content creation
- Story writing, poetry
- Unique design or art

**Complex Reasoning**
- Multi-step logical inference
- Mathematical proofs
- Causal reasoning

**High-Stakes Decisions**
- Medical diagnosis (>98% accuracy required)
- Legal advice
- Financial fraud detection (strict audit requirements)

**Characteristics of Poor Fits**:
- Requires true creativity or novelty
- Needs deep contextual understanding
- High accuracy requirement (>98%)
- Regulatory constraints on automation

---

## 🔄 Operational Model

### Phase 1: Initial Deployment (Week 1-2)

**Objective**: Generate initial program, establish baseline

- Define task specification with examples
- Generate initial program
- Deploy with high sampling rate (20-30%)
- Collect validation data
- Measure baseline accuracy and cost

### Phase 2: Confidence Building (Month 1-2)

**Objective**: Reduce sampling rate as confidence grows

- Analyze validation results
- Identify and fix systematic errors
- Gradually reduce sampling rate (20% → 10% → 5%)
- Monitor accuracy retention
- Build labeled dataset for future improvements

### Phase 3: Autonomous Operation (Month 3+)

**Objective**: Minimal human intervention

- Low sampling rate (1-5%)
- Automatic improvement on error detection
- Human intervention only for escalations
- Continuous monitoring and alerting
- Periodic retraining with accumulated data

---

## 🛠️ Framework Scope

### What Loopai Provides

✅ Program generation from specifications  
✅ Runtime execution with sampling  
✅ LLM oracle validation  
✅ Automatic improvement orchestration  
✅ Human escalation workflows  
✅ Monitoring and metrics collection  
✅ A/B testing and gradual rollout  
✅ Multi-language support (Python, C#)

### What Loopai Does NOT Provide

❌ LLM infrastructure (use OpenAI, Anthropic, etc.)  
❌ Production hosting (deploy to your infrastructure)  
❌ Task-specific optimizations (domain expertise required)  
❌ Guaranteed accuracy (statistical validation only)  
❌ Real-time LLM responses (use direct LLM for that)

---

## 📚 Documentation Structure

- **[Getting Started](docs/getting-started.md)** - Installation and first program
- **[Architecture Guide](docs/architecture.md)** - Deep dive into components
- **[API Reference](docs/api-reference.md)** - Complete API documentation
- **[Configuration Guide](docs/configuration.md)** - Tuning and optimization
- **[Best Practices](docs/best-practices.md)** - Production deployment patterns
- **[Examples](examples/)** - Real-world implementation examples
- **[Research Paper](paper/loopai.pdf)** - Academic foundation and evaluation

---

## 🗺️ Roadmap

### v0.1 - Foundation (Current)
- Basic program generation (rule-based, simple ML)
- LLM oracle validation
- Manual improvement triggers
- Python support
- Single-node deployment

### v0.2 - Automation (Q2 2025)
- Autonomous self-improvement
- C# support
- Multi-LLM provider support (Anthropic, Gemini, Azure)
- Advanced sampling strategies (uncertainty, stratified)
- Monitoring dashboard

### v0.3 - Scale (Q3 2025)
- Distributed execution engine
- Custom DSL support for domain-specific tasks
- Local ML model generation (TF-IDF, small BERT)
- Advanced A/B testing framework
- Cost prediction and optimization tools

### v1.0 - Production (Q4 2025)
- Enterprise-grade reliability
- Multi-language support (Java, Go)
- Cloud-native deployment (K8s, serverless)
- Advanced security and compliance features
- SLA guarantees and support

---

## 🙏 Acknowledgments

Loopai builds upon foundational research in:

- **Program Synthesis**: Microsoft PROSE, AlphaCode, CodeLlama
- **Knowledge Distillation**: DistilBERT, TinyBERT, MiniLLM
- **Human-in-the-Loop AI**: InstructGPT RLHF, Self-Rewarding Language Models
- **Cost Optimization**: FrugalGPT, Prompt Caching, Model Cascading
- **Self-Improving Systems**: AlphaEvolve, Darwin Gödel Machine

See [RESEARCH.md](docs/RESEARCH.md) for comprehensive literature review.

---

## 📄 License

MIT License - see [LICENSE](LICENSE) for details

---

## 📧 Contact & Community

- **Issues**: [GitHub Issues](https://github.com/iyulab/loopai/issues)
- **Discussions**: [GitHub Discussions](https://github.com/iyulab/loopai/discussions)

---

**Loopai: From expensive LLM calls to efficient, self-improving programs.**