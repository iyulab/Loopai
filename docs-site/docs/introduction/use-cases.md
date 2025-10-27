---
title: Use Cases
sidebar_position: 3
description: Real-world applications and use cases for Loopai
---

# Use Cases

Loopai excels at pattern-based AI tasks with high volume and low latency requirements.

## ✅ Ideal Use Cases

### Text Classification

Transform repetitive LLM classification tasks into fast, cost-effective programs.

#### Spam Detection
```python
# Before: $0.002 per email, 500ms latency
# After: $0.00001 per email, &lt;1ms latency

async with LoopaiClient("http://api.loopai.io") as client:
    result = await client.execute(
        task_id="spam-classifier",
        input_data={"text": "Buy now! Limited offer!"}
    )
    # Output: {"category": "spam", "confidence": 0.95}
```

**Benefits**:
- 98% cost reduction
- 500x faster execution
- Data stays local

#### Content Moderation
- Toxic content detection
- Policy violation identification
- Community guideline enforcement

**Performance**: 10K messages/second, &lt;5ms latency

#### Topic Categorization
- News article classification
- Support ticket routing
- Document organization

**Accuracy**: 90-95% on domain-specific data

### Pattern Recognition

#### Email Classification
```typescript
const client = new LoopaiClient({
  baseUrl: 'http://localhost:8080',
});

const result = await client.execute({
  taskId: 'email-classifier',
  input: {
    subject: 'RE: Project Update',
    body: '...'
  },
});
// Output: { category: 'work', priority: 'high' }
```

**Use Cases**:
- Inbox organization
- Priority detection
- Auto-responder triggers

#### Log Parsing
- Error classification
- Alert categorization
- Anomaly detection

**Performance**: 1M logs/hour, &lt;2ms per log

#### Data Validation
- Format verification
- Business rule checking
- Quality scoring

## 📊 Cost Analysis

### Example: 1M requests/day spam detection

| Approach | Monthly Cost | vs Loopai |
|----------|-------------|-----------|
| Direct LLM (GPT-4) | $5,400 | - |
| Loopai Central | $2,300 | 57% savings |
| Loopai Edge | $1,000 | **82% savings** |

### ROI Calculation

**Scenario**: E-commerce site with 100K emails/day

**Before Loopai**:
- Cost: $60/day ($0.0006/email)
- Latency: 500ms average
- Monthly: $1,800

**After Loopai**:
- Cost: $3/day ($0.00003/email)
- Latency: &lt;1ms average
- Monthly: $90

**Savings**: $1,710/month (95% reduction)

## 🎯 Best Practices

### High Volume Use Cases

**Characteristics**:
- 10K+ requests/day
- Consistent patterns
- Acceptable accuracy (85-95%)

**Examples**:
- Email filtering
- Content moderation
- Log analysis

**Benefits**:
- Massive cost savings
- Ultra-low latency
- Data sovereignty

### Pattern-Based Tasks

**Characteristics**:
- Repeatable logic
- Clear classification criteria
- Domain-specific vocabulary

**Examples**:
- Support ticket routing
- Document classification
- Intent detection

**Benefits**:
- High accuracy
- Fast execution
- Easy validation

### Low Latency Requirements

**Characteristics**:
- Real-time processing
- User-facing applications
- &lt;50ms required

**Examples**:
- Chat moderation
- Live content filtering
- Interactive apps

**Benefits**:
- Sub-10ms execution
- Predictable performance
- No API rate limits

## ⚠️ When NOT to Use Loopai

### Creative Generation
- Novel content creation
- Story writing
- Unique responses

**Why**: Loopai synthesizes programs for repetitive patterns, not creative generation.

### Complex Reasoning
- Multi-step inference
- Mathematical proofs
- Causal reasoning

**Why**: Best suited for direct LLM calls with full context.

### High-Stakes Decisions
- Medical diagnosis
- Legal advice
- Financial predictions

**Why**: Requires >98% accuracy and full LLM reasoning capability.

## 🚀 Getting Started

Ready to use Loopai for your use case?

1. [Install SDK](../guides/getting-started) - Choose .NET, Python, or TypeScript
2. [Architecture Guide](../guides/architecture) - Understand the system
3. [Examples](../examples/spam-detection) - See working implementations

## 💡 Need Help?

- Check [Examples](../examples/spam-detection) for working code
- Review [API Documentation](../api/overview) for details
- Join [Discussions](https://github.com/iyulab/loopai/discussions) for support
