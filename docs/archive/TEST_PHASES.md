# Loopai Test-Driven Development Phases

**Progressive Test-Driven Development Strategy: From Simple to Complex**

---

## 📋 Document Purpose

This document defines a test-driven, incremental development strategy for Loopai. Each phase includes concrete test datasets, success criteria, and evaluation metrics. We progress from simple, rule-based problems to complex, ML-based solutions.

**Development Philosophy**:
- Start with the simplest possible problem
- Validate core thesis with minimal complexity
- Add complexity only when previous phase succeeds
- Each phase has objective, measurable success criteria

**Target Audience**: Engineering team, QA team, project managers

**Version**: 1.0
**Last Updated**: 2025-10-25
**Status**: Development Planning

---

## 🎯 Phase Progression Overview

| Phase | Focus | Complexity | Synthesis Strategy | Target Accuracy | Duration |
|-------|-------|------------|-------------------|-----------------|----------|
| **Phase 0** | Proof of Concept | Trivial | Hard-coded rules | 100% | 1-2 weeks |
| **Phase 1** | Basic Classification | Simple | Keyword matching | 80-85% | 2-3 weeks |
| **Phase 2** | Pattern Recognition | Moderate | Regex + rules | 85-90% | 3-4 weeks |
| **Phase 3** | ML-Based Solutions | Complex | Traditional ML | 88-93% | 4-5 weeks |
| **Phase 4** | Hybrid Systems | Very Complex | Rules + ML | 92-96% | 5-6 weeks |
| **Phase 5** | Production Ready | Real-world | Adaptive | 90-95% | 6-8 weeks |

**Total Timeline**: ~5-6 months from Phase 0 to Phase 5 completion

---

## Phase 0: Proof of Concept (Weeks 1-2)

### 🎯 Phase Objective

Validate the core Loopai thesis with the simplest possible problem: deterministic text classification that can be solved with hard-coded rules. Prove that:
1. Program generation works end-to-end
2. Program execution is faster than LLM inference
3. Validation against LLM oracle detects errors
4. Basic cost savings achieved (even on trivial task)

### 📊 Test Dataset: Binary Sentiment (Trivial)

**Task**: Classify text as "positive" or "negative"

**Dataset Size**: 100 samples (50 positive, 50 negative)

**Characteristics**: Extremely obvious sentiment with strong indicator words

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": "This is amazing! I love it!",
      "expected_output": "positive",
      "difficulty": "trivial",
      "keywords": ["amazing", "love"]
    },
    {
      "input": "Terrible experience, very disappointing.",
      "expected_output": "negative",
      "difficulty": "trivial",
      "keywords": ["terrible", "disappointing"]
    },
    {
      "input": "Best product ever! Highly recommend.",
      "expected_output": "positive",
      "difficulty": "trivial",
      "keywords": ["best", "recommend"]
    },
    {
      "input": "Worst purchase I've made. Complete waste.",
      "expected_output": "negative",
      "difficulty": "trivial",
      "keywords": ["worst", "waste"]
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase0_binary_sentiment_trivial.json`

### 📈 Evaluation Metrics

**Accuracy Metrics**:
- **Program Accuracy**: 100% (deterministic rules should be perfect)
- **LLM Oracle Accuracy**: 100% (baseline ground truth)
- **Agreement Rate**: 100% (program matches oracle)

**Performance Metrics**:
- **Program Latency**: <5ms p99
- **LLM Oracle Latency**: 500-2000ms (typical GPT-4 response)
- **Speedup**: >100x
- **Cost per Execution**: <$0.00001 (program) vs $0.002 (LLM)

**Cost Metrics**:
- **Program Generation Cost**: ~$0.05 (one-time)
- **100 Executions Cost**: $0.001 (program) vs $0.20 (LLM direct)
- **Break-even Point**: ~25 executions
- **Cost Reduction**: 99.5% after 100 executions

### ✅ Success Criteria

**Phase 0 Complete When**:
- [ ] Program generator produces valid Python code from task specification
- [ ] Generated program achieves 100% accuracy on test dataset
- [ ] Program execution <10ms p99 latency
- [ ] LLM oracle validation correctly identifies 0 errors (100% agreement)
- [ ] Cost reduction >99% demonstrated on 100+ executions
- [ ] End-to-end workflow executes without manual intervention

### 🔧 Implementation Focus

**Components to Implement**:
1. Task specification parser (minimal)
2. LLM program generator (simple prompt engineering)
3. Program executor (basic Python exec with timeout)
4. LLM oracle interface (OpenAI API call)
5. Comparison engine (exact string match)

**Not Implemented Yet**:
- Sampling strategies (validate 100% for now)
- Improvement engine (no failures expected)
- Multi-tier validation (all go to oracle)
- Advanced security (basic sandboxing only)

---

## Phase 1: Basic Classification (Weeks 3-5)

### 🎯 Phase Objective

Solve simple text classification problems using keyword-based and rule-based approaches. Handle cases with slight ambiguity and introduce sampling-based validation.

### 📊 Test Datasets

#### 1.1 Spam Detection

**Task**: Classify emails as "spam" or "not_spam"

**Dataset Size**: 500 samples (250 spam, 250 legitimate)

**Characteristics**: Clear spam indicators (keywords, patterns)

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": "Congratulations! You won $1,000,000! Click here to claim.",
      "expected_output": "spam",
      "difficulty": "easy",
      "indicators": ["won", "claim", "urgent", "money"]
    },
    {
      "input": "Hi John, attached is the quarterly report we discussed.",
      "expected_output": "not_spam",
      "difficulty": "easy",
      "indicators": ["professional", "specific_reference"]
    },
    {
      "input": "LIMITED TIME OFFER!!! Buy now and get 90% discount!!!",
      "expected_output": "spam",
      "difficulty": "easy",
      "indicators": ["urgency", "excessive_punctuation", "discount"]
    },
    {
      "input": "Meeting reminder: Project sync tomorrow at 2pm in conference room B.",
      "expected_output": "not_spam",
      "difficulty": "easy",
      "indicators": ["calendar_event", "specific_details"]
    },
    {
      "input": "Verify your account immediately or it will be suspended.",
      "expected_output": "spam",
      "difficulty": "medium",
      "indicators": ["urgency", "threat", "vague"]
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase1_spam_detection.json`

**Challenge Cases** (10% of dataset):
- Legitimate marketing emails that look like spam
- Phishing emails with professional tone
- Newsletters with promotional content

#### 1.2 Language Detection

**Task**: Identify language of text input

**Dataset Size**: 300 samples (50 each: English, Spanish, French, German, Japanese, Chinese)

**Characteristics**: Short text snippets with clear language-specific characters

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": "Hello, how are you today?",
      "expected_output": "english",
      "difficulty": "easy"
    },
    {
      "input": "Hola, ¿cómo estás hoy?",
      "expected_output": "spanish",
      "difficulty": "easy"
    },
    {
      "input": "Bonjour, comment allez-vous aujourd'hui?",
      "expected_output": "french",
      "difficulty": "easy"
    },
    {
      "input": "こんにちは、今日はどうですか？",
      "expected_output": "japanese",
      "difficulty": "easy"
    },
    {
      "input": "The café serves great croissants.",
      "expected_output": "english",
      "difficulty": "medium",
      "note": "Contains French words but English structure"
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase1_language_detection.json`

#### 1.3 Sentiment Analysis (Simple)

**Task**: Classify text as "positive", "negative", or "neutral"

**Dataset Size**: 600 samples (200 each class)

**Characteristics**: Clear sentiment with common expressions

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": "The food was delicious and the service was excellent!",
      "expected_output": "positive",
      "difficulty": "easy"
    },
    {
      "input": "Poor quality product, not worth the money.",
      "expected_output": "negative",
      "difficulty": "easy"
    },
    {
      "input": "The item arrived on time as described.",
      "expected_output": "neutral",
      "difficulty": "medium"
    },
    {
      "input": "I expected more for the price, but it's okay.",
      "expected_output": "neutral",
      "difficulty": "medium",
      "note": "Mixed sentiment should be classified as neutral"
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase1_sentiment_simple.json`

### 📈 Evaluation Metrics

**Accuracy Metrics** (per dataset):
- **Target Accuracy**: 80-85% (program vs oracle)
- **Baseline (Random)**: 33-50% (depending on classes)
- **LLM Oracle Accuracy**: 95-98% (GPT-4 baseline)

**Sampling Strategy**:
- **Phase 1a** (Weeks 3-4): 30% sampling rate (build confidence)
- **Phase 1b** (Week 5): 10% sampling rate (reduce cost)

**Performance Metrics**:
- **Program Latency**: <10ms p99
- **Throughput**: >1000 req/sec on single instance
- **Memory Usage**: <100MB per executor

**Cost Metrics**:
- **Cost Reduction**: 90%+ with 10% sampling
- **Break-even Point**: <100 executions per task

### ✅ Success Criteria

**Phase 1 Complete When**:
- [ ] All three datasets achieve 80%+ accuracy
- [ ] Sampling strategy implemented (random sampling)
- [ ] Validation failure detection works correctly
- [ ] Generated programs execute in <10ms p99
- [ ] Cost reduction >90% demonstrated
- [ ] Zero critical errors in 1000+ test executions

### 🔧 Implementation Focus

**New Components**:
1. Random sampling strategy (10-30% validation rate)
2. Keyword extraction and rule generation
3. Multi-class classification support
4. Validation failure logging
5. Basic metrics collection (accuracy, latency, cost)

**Key Enhancements**:
- Prompt engineering for rule-based generation
- Support for 2-6 class classification
- Handle UTF-8 and multilingual text

---

## Phase 2: Pattern Recognition (Weeks 6-9)

### 🎯 Phase Objective

Handle problems requiring pattern recognition, regular expressions, and structured data extraction. Introduce more complex logic beyond simple keyword matching.

### 📊 Test Datasets

#### 2.1 Email Validation

**Task**: Validate if text is a properly formatted email address

**Dataset Size**: 400 samples (200 valid, 200 invalid)

**Characteristics**: Requires regex or formal validation logic

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": "user@example.com",
      "expected_output": "valid",
      "difficulty": "easy"
    },
    {
      "input": "user.name+tag@domain.co.uk",
      "expected_output": "valid",
      "difficulty": "medium"
    },
    {
      "input": "invalid.email@",
      "expected_output": "invalid",
      "difficulty": "easy",
      "reason": "missing_domain"
    },
    {
      "input": "@example.com",
      "expected_output": "invalid",
      "difficulty": "easy",
      "reason": "missing_local_part"
    },
    {
      "input": "user@domain_with_underscore.com",
      "expected_output": "valid",
      "difficulty": "medium",
      "note": "Underscores allowed in domain names"
    },
    {
      "input": "user..name@example.com",
      "expected_output": "invalid",
      "difficulty": "hard",
      "reason": "consecutive_dots"
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase2_email_validation.json`

#### 2.2 Log Parsing & Error Detection

**Task**: Parse log lines and classify as "ERROR", "WARNING", "INFO", or "DEBUG"

**Dataset Size**: 800 samples (200 each class)

**Characteristics**: Structured text with consistent patterns

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": "[2025-01-15 10:30:45] ERROR: Database connection failed",
      "expected_output": "ERROR",
      "difficulty": "easy"
    },
    {
      "input": "[2025-01-15 10:30:46] WARNING: High memory usage detected (85%)",
      "expected_output": "WARNING",
      "difficulty": "easy"
    },
    {
      "input": "[2025-01-15 10:30:47] INFO: User login successful - user_id: 12345",
      "expected_output": "INFO",
      "difficulty": "easy"
    },
    {
      "input": "2025-01-15 10:30:48 | DEBUG | Request processing started",
      "expected_output": "DEBUG",
      "difficulty": "medium",
      "note": "Different date format"
    },
    {
      "input": "Application crashed unexpectedly at 10:30:49",
      "expected_output": "ERROR",
      "difficulty": "medium",
      "note": "No explicit ERROR tag"
    },
    {
      "input": "10:30:50 - Possible security breach detected",
      "expected_output": "ERROR",
      "difficulty": "hard",
      "note": "Implicit severity from content"
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase2_log_parsing.json`

#### 2.3 Phone Number Validation

**Task**: Validate and normalize phone numbers (US format)

**Dataset Size**: 500 samples (300 valid, 200 invalid)

**Characteristics**: Multiple valid formats, requires normalization

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": "(555) 123-4567",
      "expected_output": "valid",
      "normalized": "+15551234567",
      "difficulty": "easy"
    },
    {
      "input": "555-123-4567",
      "expected_output": "valid",
      "normalized": "+15551234567",
      "difficulty": "easy"
    },
    {
      "input": "5551234567",
      "expected_output": "valid",
      "normalized": "+15551234567",
      "difficulty": "easy"
    },
    {
      "input": "+1 (555) 123-4567",
      "expected_output": "valid",
      "normalized": "+15551234567",
      "difficulty": "medium"
    },
    {
      "input": "123-4567",
      "expected_output": "invalid",
      "difficulty": "easy",
      "reason": "too_short"
    },
    {
      "input": "(555) 123-456",
      "expected_output": "invalid",
      "difficulty": "medium",
      "reason": "incomplete"
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase2_phone_validation.json`

#### 2.4 Date Parsing & Normalization

**Task**: Parse various date formats and normalize to ISO 8601

**Dataset Size**: 600 samples (500 valid, 100 invalid)

**Characteristics**: Multiple international formats

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": "2025-01-15",
      "expected_output": "2025-01-15",
      "difficulty": "easy",
      "format": "ISO8601"
    },
    {
      "input": "01/15/2025",
      "expected_output": "2025-01-15",
      "difficulty": "easy",
      "format": "US"
    },
    {
      "input": "15/01/2025",
      "expected_output": "2025-01-15",
      "difficulty": "medium",
      "format": "EU"
    },
    {
      "input": "January 15, 2025",
      "expected_output": "2025-01-15",
      "difficulty": "medium",
      "format": "long_form"
    },
    {
      "input": "15-Jan-2025",
      "expected_output": "2025-01-15",
      "difficulty": "medium",
      "format": "short_month"
    },
    {
      "input": "32/01/2025",
      "expected_output": "invalid",
      "difficulty": "easy",
      "reason": "invalid_day"
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase2_date_parsing.json`

### 📈 Evaluation Metrics

**Accuracy Metrics**:
- **Target Accuracy**: 85-90% (program vs oracle)
- **Regex Pattern Correctness**: 95%+ for valid patterns
- **Edge Case Handling**: 70%+ on hard difficulty cases

**Sampling Strategy**:
- **Phase 2a** (Weeks 6-7): 20% sampling (pattern validation)
- **Phase 2b** (Weeks 8-9): 10% sampling (confidence building)

**Performance Metrics**:
- **Program Latency**: <8ms p99 (regex operations)
- **Throughput**: >2000 req/sec
- **Pattern Compilation**: <1ms (cached)

**Cost Metrics**:
- **Cost Reduction**: 92%+ with 10% sampling
- **Oracle Cost**: $0.002 per validation (GPT-4)
- **Program Cost**: <$0.00005 per execution

### ✅ Success Criteria

**Phase 2 Complete When**:
- [ ] All four datasets achieve 85%+ accuracy
- [ ] Regex-based programs generated correctly
- [ ] Edge cases handled gracefully (invalid formats)
- [ ] Normalization logic works correctly (phone, date)
- [ ] Sampling rate reduced to 10% without accuracy loss
- [ ] Zero security issues in generated regex patterns

### 🔧 Implementation Focus

**New Components**:
1. Regex pattern generation from examples
2. Format normalization logic
3. Structured data extraction
4. Edge case handling (invalid inputs)

**Key Enhancements**:
- LLM prompt engineering for pattern recognition
- Validation for regex safety (no ReDoS vulnerabilities)
- Support for multiple input formats

---

## Phase 3: ML-Based Solutions (Weeks 10-14)

### 🎯 Phase Objective

Tackle problems requiring traditional machine learning models (scikit-learn). Handle cases where rule-based approaches are insufficient due to semantic complexity or subtle patterns.

### 📊 Test Datasets

#### 3.1 News Topic Classification

**Task**: Classify news articles into categories

**Categories**: Politics, Technology, Sports, Business, Entertainment, Science (6 classes)

**Dataset Size**: 1200 samples (200 per category)

**Characteristics**: Requires semantic understanding, not just keywords

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": "The Federal Reserve announced interest rate changes affecting the economy.",
      "expected_output": "business",
      "difficulty": "easy"
    },
    {
      "input": "New AI model achieves breakthrough in natural language understanding.",
      "expected_output": "technology",
      "difficulty": "easy"
    },
    {
      "input": "Scientists discover potential cure for rare genetic disease.",
      "expected_output": "science",
      "difficulty": "medium",
      "note": "Could be confused with technology"
    },
    {
      "input": "The quarterback led his team to victory in overtime.",
      "expected_output": "sports",
      "difficulty": "easy"
    },
    {
      "input": "Government debates new regulations for tech companies.",
      "expected_output": "politics",
      "difficulty": "hard",
      "note": "Overlaps with business and technology"
    },
    {
      "input": "Box office numbers show strong performance for latest blockbuster.",
      "expected_output": "entertainment",
      "difficulty": "medium"
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase3_news_classification.json`

**Dataset Source**: Sample from AG News or BBC News datasets

#### 3.2 Intent Classification (Customer Support)

**Task**: Classify customer queries into intent categories

**Categories**: account_issue, billing_question, technical_support, product_inquiry, complaint, feedback (6 classes)

**Dataset Size**: 1200 samples (200 per category)

**Characteristics**: Natural language variety, similar phrasing for different intents

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": "I can't log into my account, it says my password is wrong.",
      "expected_output": "account_issue",
      "difficulty": "easy"
    },
    {
      "input": "Why was I charged twice this month?",
      "expected_output": "billing_question",
      "difficulty": "easy"
    },
    {
      "input": "The app keeps crashing when I try to upload photos.",
      "expected_output": "technical_support",
      "difficulty": "easy"
    },
    {
      "input": "Do you have this product available in blue?",
      "expected_output": "product_inquiry",
      "difficulty": "easy"
    },
    {
      "input": "I've been waiting for my order for two weeks, this is unacceptable.",
      "expected_output": "complaint",
      "difficulty": "medium",
      "note": "Emotional tone indicates complaint"
    },
    {
      "input": "Great service, but would love to see more payment options.",
      "expected_output": "feedback",
      "difficulty": "medium",
      "note": "Mixed sentiment"
    },
    {
      "input": "How do I cancel my subscription and get a refund?",
      "expected_output": "billing_question",
      "difficulty": "hard",
      "note": "Could be account_issue or complaint"
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase3_intent_classification.json`

#### 3.3 Product Review Rating Prediction

**Task**: Predict star rating (1-5) from review text

**Dataset Size**: 1000 samples (200 per rating)

**Characteristics**: Nuanced sentiment, sarcasm, mixed reviews

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": "Absolutely perfect! Exceeded all my expectations.",
      "expected_output": 5,
      "difficulty": "easy"
    },
    {
      "input": "Decent product for the price, does what it says.",
      "expected_output": 3,
      "difficulty": "easy"
    },
    {
      "input": "Worst purchase ever, broke after one day.",
      "expected_output": 1,
      "difficulty": "easy"
    },
    {
      "input": "Good but not great. Battery life could be better.",
      "expected_output": 4,
      "difficulty": "medium",
      "note": "Subtle negative in mostly positive"
    },
    {
      "input": "I wanted to love this, but it just didn't work for me.",
      "expected_output": 2,
      "difficulty": "hard",
      "note": "Indirect expression of dissatisfaction"
    },
    {
      "input": "It's okay, I guess. Nothing special but not terrible.",
      "expected_output": 3,
      "difficulty": "medium"
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase3_review_rating.json`

**Dataset Source**: Amazon or Yelp review subsets

#### 3.4 Toxic Comment Detection

**Task**: Classify comments as "toxic" or "not_toxic"

**Dataset Size**: 1000 samples (300 toxic, 700 not toxic)

**Characteristics**: Subtle toxicity, context-dependent offense

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": "This is a great discussion, thanks for sharing your perspective.",
      "expected_output": "not_toxic",
      "difficulty": "easy"
    },
    {
      "input": "You're an idiot if you believe that.",
      "expected_output": "toxic",
      "difficulty": "easy",
      "type": "direct_insult"
    },
    {
      "input": "I disagree with your point, but I respect your opinion.",
      "expected_output": "not_toxic",
      "difficulty": "easy"
    },
    {
      "input": "People like you are what's wrong with society.",
      "expected_output": "toxic",
      "difficulty": "medium",
      "type": "indirect_attack"
    },
    {
      "input": "This is garbage content, waste of time.",
      "expected_output": "toxic",
      "difficulty": "medium",
      "type": "aggressive_criticism"
    },
    {
      "input": "I find this disappointing and poorly researched.",
      "expected_output": "not_toxic",
      "difficulty": "hard",
      "note": "Harsh criticism but not toxic"
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase3_toxic_detection.json`

**Dataset Source**: Kaggle Toxic Comment Classification Challenge (filtered)

### 📈 Evaluation Metrics

**Accuracy Metrics**:
- **Target Accuracy**: 88-93% (program vs oracle)
- **Baseline (Majority Class)**: 16-70% depending on task
- **LLM Oracle Accuracy**: 96-98% (GPT-4 baseline)

**ML Model Metrics**:
- **F1 Score**: >0.85 (macro-averaged across classes)
- **Precision**: >0.87 (minimize false positives)
- **Recall**: >0.85 (minimize false negatives)
- **Confusion Matrix**: Analyze misclassification patterns

**Sampling Strategy**:
- **Phase 3a** (Weeks 10-12): 15% sampling (model validation)
- **Phase 3b** (Weeks 13-14): 8% sampling (production-like)

**Performance Metrics**:
- **Model Training Time**: <30 seconds on 1000 samples
- **Inference Latency**: <15ms p99 (TF-IDF + classifier)
- **Model Size**: <50MB (serialized)

**Cost Metrics**:
- **Cost Reduction**: 93%+ with 8% sampling
- **Model Training Cost**: $0.10-0.50 per task (one-time)
- **Total Cost**: Lower than direct LLM after 500+ executions

### ✅ Success Criteria

**Phase 3 Complete When**:
- [ ] All four datasets achieve 88%+ accuracy
- [ ] ML models (LogisticRegression, RandomForest, SVM) generated correctly
- [ ] Training pipeline works end-to-end (TF-IDF → model → serialization)
- [ ] Model inference <20ms p99 latency
- [ ] F1 scores >0.85 for all tasks
- [ ] Cost reduction >93% with 8% sampling
- [ ] Generated models are deterministic (same input → same output)

### 🔧 Implementation Focus

**New Components**:
1. Feature extraction (TF-IDF, CountVectorizer)
2. ML model training (scikit-learn pipelines)
3. Model serialization and loading (pickle/joblib)
4. Hyperparameter selection logic
5. Model performance evaluation

**Key Enhancements**:
- LLM prompt engineering for ML model generation
- Training data preparation from examples
- Cross-validation for model selection
- Model versioning and tracking

---

## Phase 4: Hybrid Systems (Weeks 15-20)

### 🎯 Phase Objective

Combine rule-based and ML-based approaches for optimal performance. Handle complex, multi-faceted problems that benefit from hybrid strategies. Implement intelligent routing between strategies.

### 📊 Test Datasets

#### 4.1 Email Routing (Hybrid)

**Task**: Route customer emails to correct department with confidence score

**Departments**: sales, support, billing, technical, general (5 classes)

**Dataset Size**: 1500 samples (300 per department)

**Characteristics**: Requires both keyword rules and semantic understanding

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": "I want to purchase 10 licenses for my company.",
      "expected_output": {
        "department": "sales",
        "confidence": "high"
      },
      "difficulty": "easy",
      "strategy": "rule",
      "note": "Clear keywords: purchase, licenses"
    },
    {
      "input": "My account was charged incorrectly, please review invoice #12345.",
      "expected_output": {
        "department": "billing",
        "confidence": "high"
      },
      "difficulty": "easy",
      "strategy": "rule",
      "note": "Keywords: charged, invoice"
    },
    {
      "input": "The API returns 500 errors when I authenticate.",
      "expected_output": {
        "department": "technical",
        "confidence": "high"
      },
      "difficulty": "medium",
      "strategy": "ml",
      "note": "Technical jargon requires ML"
    },
    {
      "input": "Can you help me understand how the pricing works?",
      "expected_output": {
        "department": "sales",
        "confidence": "medium"
      },
      "difficulty": "medium",
      "strategy": "hybrid",
      "note": "Could be sales or support"
    },
    {
      "input": "I love your product and wanted to share some thoughts.",
      "expected_output": {
        "department": "general",
        "confidence": "medium"
      },
      "difficulty": "hard",
      "strategy": "ml",
      "note": "Positive feedback, no clear department"
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase4_email_routing.json`

**Hybrid Strategy**:
1. **Rule Layer**: Check for explicit keywords (purchase, invoice, error, etc.)
2. **ML Layer**: If no rule matches or confidence low, use classifier
3. **Confidence Scoring**: Combine rule confidence + ML probability

#### 4.2 Content Moderation (Multi-Label)

**Task**: Detect multiple violation types in content (multi-label classification)

**Labels**: spam, hate_speech, violence, sexual_content, misinformation, none (not mutually exclusive)

**Dataset Size**: 1200 samples (including multi-label cases)

**Characteristics**: Content can have multiple violations simultaneously

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": "Buy cheap medications online now! Limited time offer!",
      "expected_output": ["spam"],
      "difficulty": "easy",
      "strategy": "rule"
    },
    {
      "input": "Great article, thanks for sharing!",
      "expected_output": ["none"],
      "difficulty": "easy",
      "strategy": "rule"
    },
    {
      "input": "I hate people from [ethnic group], they should all leave.",
      "expected_output": ["hate_speech"],
      "difficulty": "medium",
      "strategy": "ml",
      "note": "Requires context understanding"
    },
    {
      "input": "Click here for free money!!! Everyone from [group] is stupid!",
      "expected_output": ["spam", "hate_speech"],
      "difficulty": "hard",
      "strategy": "hybrid",
      "note": "Multiple violations"
    },
    {
      "input": "This pandemic cure works 100%! Big pharma doesn't want you to know!",
      "expected_output": ["misinformation", "spam"],
      "difficulty": "hard",
      "strategy": "ml",
      "note": "Subtle misinformation"
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase4_content_moderation.json`

**Hybrid Strategy**:
1. **Rule Layer**: Detect obvious spam patterns (excessive caps, URLs, etc.)
2. **ML Layer**: Semantic analysis for hate speech, misinformation
3. **Cascading**: Apply rules first (fast), ML only if needed

#### 4.3 Smart Form Validation

**Task**: Validate form fields with context-aware rules

**Fields**: email, phone, address, credit_card, ssn, custom

**Dataset Size**: 1000 samples (various field types)

**Characteristics**: Different validation rules per field type + ML for ambiguous cases

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": {
        "field_type": "email",
        "value": "user@example.com"
      },
      "expected_output": {
        "valid": true,
        "normalized": "user@example.com"
      },
      "difficulty": "easy",
      "strategy": "rule"
    },
    {
      "input": {
        "field_type": "phone",
        "value": "(555) 123-4567"
      },
      "expected_output": {
        "valid": true,
        "normalized": "+15551234567"
      },
      "difficulty": "easy",
      "strategy": "rule"
    },
    {
      "input": {
        "field_type": "address",
        "value": "123 Main St, Apt 4B, New York, NY 10001"
      },
      "expected_output": {
        "valid": true,
        "components": {
          "street": "123 Main St",
          "unit": "Apt 4B",
          "city": "New York",
          "state": "NY",
          "zip": "10001"
        }
      },
      "difficulty": "medium",
      "strategy": "hybrid",
      "note": "Regex + ML for parsing"
    },
    {
      "input": {
        "field_type": "custom",
        "value": "John Doe (CEO)",
        "context": "Expecting a person's name with optional title"
      },
      "expected_output": {
        "valid": true,
        "name": "John Doe",
        "title": "CEO"
      },
      "difficulty": "hard",
      "strategy": "ml",
      "note": "Requires understanding context"
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase4_form_validation.json`

#### 4.4 Hierarchical Text Classification

**Task**: Classify documents into hierarchical categories

**Hierarchy**:
```
Technology
  ├── Software
  │   ├── Mobile Apps
  │   └── Web Development
  └── Hardware
      ├── Computers
      └── Networking

Business
  ├── Finance
  └── Marketing
```

**Dataset Size**: 1600 samples (100 per leaf category)

**Characteristics**: Requires multi-level classification (top-level → sub-category)

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": "New iPhone app released with innovative AR features.",
      "expected_output": {
        "level1": "technology",
        "level2": "software",
        "level3": "mobile_apps"
      },
      "difficulty": "easy",
      "strategy": "hybrid"
    },
    {
      "input": "Company announces Q3 earnings, stock price surges.",
      "expected_output": {
        "level1": "business",
        "level2": "finance",
        "level3": null
      },
      "difficulty": "medium",
      "strategy": "ml",
      "note": "Only two levels detected"
    },
    {
      "input": "5G router technology enables faster home internet speeds.",
      "expected_output": {
        "level1": "technology",
        "level2": "hardware",
        "level3": "networking"
      },
      "difficulty": "medium",
      "strategy": "hybrid"
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase4_hierarchical_classification.json`

**Hybrid Strategy**:
1. **Level 1 (Top)**: Rule-based keywords (fast, high confidence)
2. **Level 2-3**: ML classifier per parent category
3. **Confidence Thresholding**: Stop at level where confidence drops

### 📈 Evaluation Metrics

**Accuracy Metrics**:
- **Target Accuracy**: 92-96% (program vs oracle)
- **Strategy Selection Accuracy**: 90%+ (rule vs ML vs hybrid)
- **Multi-Label F1**: >0.88 (for content moderation)
- **Hierarchical Accuracy**: 85%+ exact match, 95%+ level-1 match

**Hybrid Performance**:
- **Rule Coverage**: 40-60% of cases handled by rules alone
- **ML Fallback Rate**: 30-50% require ML
- **Hybrid Cases**: 10-20% benefit from combining both

**Sampling Strategy**:
- **Phase 4a** (Weeks 15-17): 12% sampling (hybrid validation)
- **Phase 4b** (Weeks 18-20): 6% sampling (production-like)

**Performance Metrics**:
- **Rule Path Latency**: <5ms (fast path)
- **ML Path Latency**: <15ms (fallback)
- **Hybrid Path Latency**: <20ms (combined)
- **Average Latency**: <10ms (weighted by strategy usage)

**Cost Metrics**:
- **Cost Reduction**: 95%+ with 6% sampling
- **Rule Execution Cost**: Negligible (<$0.000001)
- **ML Execution Cost**: <$0.00005 per inference
- **Blended Cost**: Significantly lower than pure ML

### ✅ Success Criteria

**Phase 4 Complete When**:
- [ ] All four datasets achieve 92%+ accuracy
- [ ] Hybrid strategy routing works correctly
- [ ] Multi-label classification implemented and validated
- [ ] Hierarchical classification achieves 85%+ exact match
- [ ] Rule coverage 40-60% (demonstrating efficiency)
- [ ] Cost reduction >95% with 6% sampling
- [ ] Confidence scoring accurately reflects prediction quality

### 🔧 Implementation Focus

**New Components**:
1. Strategy router (decide rule vs ML vs hybrid)
2. Multi-label classification support
3. Hierarchical classification logic
4. Confidence scoring and calibration
5. Cascading validation (rules → ML)

**Key Enhancements**:
- LLM generates both rule-based and ML-based solutions
- Intelligent strategy selection based on task characteristics
- Confidence-based fallback mechanisms
- Multi-objective optimization (accuracy + latency + cost)

---

## Phase 5: Production Ready (Weeks 21-28)

### 🎯 Phase Objective

Validate Loopai on real-world production scenarios with all features enabled: autonomous improvement, A/B testing, gradual rollout, human escalation. Achieve production-grade reliability and cost efficiency.

### 📊 Test Datasets

#### 5.1 E-commerce Product Categorization

**Task**: Categorize products into taxonomy (real e-commerce catalog)

**Categories**: 100+ categories in 3-level hierarchy

**Dataset Size**: 5000 samples (real product descriptions)

**Characteristics**: Real-world noise, inconsistent formatting, ambiguous cases

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": {
        "title": "Sony WH-1000XM5 Wireless Noise Cancelling Headphones",
        "description": "Industry-leading noise cancellation, 30-hour battery life, premium sound quality"
      },
      "expected_output": {
        "level1": "electronics",
        "level2": "audio",
        "level3": "headphones"
      },
      "difficulty": "easy"
    },
    {
      "input": {
        "title": "Organic Cotton Blend T-Shirt - Unisex",
        "description": "Comfortable everyday wear, eco-friendly materials"
      },
      "expected_output": {
        "level1": "clothing",
        "level2": "tops",
        "level3": "t-shirts"
      },
      "difficulty": "medium",
      "note": "Unisex could be men's or women's"
    },
    {
      "input": {
        "title": "Multi-tool camping knife with LED flashlight",
        "description": "Stainless steel blade, 12 functions including can opener, screwdriver, flashlight"
      },
      "expected_output": {
        "level1": "outdoor",
        "level2": "camping",
        "level3": "tools"
      },
      "difficulty": "hard",
      "note": "Could be tools, camping, or survival gear"
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase5_ecommerce_categorization.json`

**Real-World Challenges**:
- Inconsistent product descriptions
- Missing information
- Multi-category products
- New product types not in training data

#### 5.2 Customer Support Ticket Routing

**Task**: Route support tickets to correct team with priority

**Teams**: tier1_support, tier2_technical, billing, sales, escalation (5 teams)

**Priority**: low, medium, high, urgent (4 levels)

**Dataset Size**: 3000 samples (real anonymized support tickets)

**Characteristics**: Natural language, spelling errors, emotional content

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": {
        "subject": "Cannot access my account",
        "body": "I've tried resetting my password multiple times but still can't log in. Please help ASAP!",
        "customer_tier": "premium"
      },
      "expected_output": {
        "team": "tier2_technical",
        "priority": "high",
        "estimated_response_time": "1 hour"
      },
      "difficulty": "medium"
    },
    {
      "input": {
        "subject": "Inquiry about enterprise pricing",
        "body": "We're interested in purchasing 500+ licenses. Can you provide a quote?"
      },
      "expected_output": {
        "team": "sales",
        "priority": "medium",
        "estimated_response_time": "4 hours"
      },
      "difficulty": "easy"
    },
    {
      "input": {
        "subject": "URGENT - service down for entire organization",
        "body": "Our production system has been down for 2 hours affecting 10,000 users. This is critical!!!"
      },
      "expected_output": {
        "team": "escalation",
        "priority": "urgent",
        "estimated_response_time": "immediate"
      },
      "difficulty": "easy",
      "note": "Clear urgency signals"
    },
    {
      "input": {
        "subject": "question",
        "body": "how do i chang my email adress"
      },
      "expected_output": {
        "team": "tier1_support",
        "priority": "low",
        "estimated_response_time": "24 hours"
      },
      "difficulty": "medium",
      "note": "Typos and informal language"
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase5_support_routing.json`

#### 5.3 Content Recommendation Tags

**Task**: Generate relevant tags for content (multi-label, open vocabulary)

**Dataset Size**: 2000 samples (blog posts, articles)

**Characteristics**: Open-ended tags, subjective relevance, emerging topics

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": {
        "title": "Introduction to Machine Learning with Python",
        "content": "This tutorial covers the basics of machine learning using scikit-learn library..."
      },
      "expected_output": {
        "tags": ["machine-learning", "python", "tutorial", "beginner", "scikit-learn"],
        "primary_tag": "machine-learning"
      },
      "difficulty": "easy"
    },
    {
      "input": {
        "title": "Sustainable Practices in Modern Agriculture",
        "content": "Exploring how technology and traditional methods combine to create eco-friendly farming..."
      },
      "expected_output": {
        "tags": ["agriculture", "sustainability", "technology", "environment", "farming"],
        "primary_tag": "sustainability"
      },
      "difficulty": "medium",
      "note": "Multiple valid primary tags"
    },
    {
      "input": {
        "title": "The Future of Remote Work Post-Pandemic",
        "content": "Analysis of hybrid work models, digital collaboration tools, and workplace culture changes..."
      },
      "expected_output": {
        "tags": ["remote-work", "future-of-work", "hybrid", "workplace", "technology", "covid-19"],
        "primary_tag": "future-of-work"
      },
      "difficulty": "hard",
      "note": "Emerging topic, subjective tags"
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase5_content_tagging.json`

#### 5.4 Production Log Analysis (Real-Time)

**Task**: Analyze production logs in real-time, detect anomalies and extract insights

**Dataset Size**: 10,000 log lines (1 day of production logs)

**Characteristics**: High volume, time-series, pattern changes over time

**Sample Data**:
```json
{
  "test_cases": [
    {
      "input": "[2025-01-15 10:30:45.123] INFO [api-gateway] Request processed successfully - user_id: 12345, endpoint: /api/users, latency: 45ms",
      "expected_output": {
        "severity": "info",
        "component": "api-gateway",
        "action": "request_processed",
        "metrics": {
          "latency_ms": 45,
          "endpoint": "/api/users"
        },
        "anomaly": false
      },
      "difficulty": "easy"
    },
    {
      "input": "[2025-01-15 10:30:50.456] ERROR [database] Connection pool exhausted - active: 100, max: 100, waiting: 50",
      "expected_output": {
        "severity": "error",
        "component": "database",
        "action": "connection_pool_exhausted",
        "metrics": {
          "active_connections": 100,
          "max_connections": 100,
          "waiting_requests": 50
        },
        "anomaly": true,
        "alert": "critical"
      },
      "difficulty": "medium"
    },
    {
      "input": "[2025-01-15 10:31:00.789] WARN [cache] High memory usage detected - used: 7.8GB, available: 8GB, eviction_rate: 150/sec",
      "expected_output": {
        "severity": "warning",
        "component": "cache",
        "action": "high_memory_usage",
        "metrics": {
          "memory_used_gb": 7.8,
          "memory_available_gb": 8,
          "eviction_rate_per_sec": 150
        },
        "anomaly": true,
        "alert": "warning"
      },
      "difficulty": "medium"
    },
    {
      "input": "[2025-01-15 10:31:15.234] INFO [auth] Unusual login pattern - user_id: 67890, locations: ['US', 'CN', 'US'], time_span: 5 minutes",
      "expected_output": {
        "severity": "info",
        "component": "auth",
        "action": "unusual_login_pattern",
        "security_concern": true,
        "anomaly": true,
        "alert": "security"
      },
      "difficulty": "hard",
      "note": "Implicit security concern"
    }
  ]
}
```

**Full Dataset**: `tests/datasets/phase5_log_analysis.json`

### 📈 Evaluation Metrics

**Accuracy Metrics**:
- **Target Accuracy**: 90-95% (real-world performance)
- **Production Baseline**: Direct LLM inference at 97-99%
- **Acceptable Accuracy Gap**: <5% vs LLM baseline

**Production Metrics**:
- **Uptime**: 99.9% (SLA target)
- **Latency**: p50 <10ms, p99 <50ms
- **Throughput**: 10K+ req/sec per task
- **Error Rate**: <0.1% (excluding validation failures)

**Autonomous Improvement**:
- **Auto-Improvement Success Rate**: 70%+ (improvements actually improve)
- **Improvement Cycle Time**: <30 minutes (detection → deployment)
- **Human Escalation Rate**: <10% (90% handled autonomously)

**Cost Metrics**:
- **Cost Reduction**: 95%+ vs direct LLM
- **Sampling Rate**: 3-5% (adaptive based on confidence)
- **Total Cost**: <$0.0001 per execution (including validation)

**A/B Testing**:
- **Deployment Safety**: 0 regressions in gradual rollout
- **Rollback Time**: <5 minutes when needed
- **Statistical Significance**: Achieved in <1000 samples

### ✅ Success Criteria

**Phase 5 Complete When**:
- [ ] All four production datasets achieve 90%+ accuracy
- [ ] System runs autonomously for 7+ days without intervention
- [ ] Autonomous improvement successfully improves programs 70%+ of time
- [ ] A/B testing and gradual rollout work correctly
- [ ] Human escalation triggers appropriately (<10% of improvements)
- [ ] 99.9% uptime achieved over 30 days
- [ ] Cost reduction >95% sustained
- [ ] Real-world production deployment validated

### 🔧 Implementation Focus

**New Components**:
1. Autonomous improvement orchestration (full workflow)
2. A/B testing framework (traffic splitting, metrics comparison)
3. Gradual rollout automation (10% → 50% → 100%)
4. Automatic rollback on regression
5. Human escalation system (ticket creation, notification)
6. Production monitoring dashboard (real-time metrics)

**Production Hardening**:
- Error handling and recovery
- Rate limiting and quota management
- Security hardening (input validation, sandboxing)
- Comprehensive logging and tracing
- Performance optimization (caching, batching)
- Disaster recovery procedures

---

## 📊 Cross-Phase Success Tracking

### Overall Metrics Dashboard

| Phase | Accuracy Target | Latency Target | Cost Reduction | Status |
|-------|----------------|----------------|----------------|--------|
| Phase 0 | 100% | <5ms | 99%+ | ⏳ Pending |
| Phase 1 | 80-85% | <10ms | 90%+ | ⏳ Pending |
| Phase 2 | 85-90% | <8ms | 92%+ | ⏳ Pending |
| Phase 3 | 88-93% | <15ms | 93%+ | ⏳ Pending |
| Phase 4 | 92-96% | <10ms | 95%+ | ⏳ Pending |
| Phase 5 | 90-95% | <10ms | 95%+ | ⏳ Pending |

### Dataset Inventory

**Total Test Cases**: ~20,000 samples across all phases

**Storage Location**: `tests/datasets/`

**Format**: JSON Lines (JSONL) for easy streaming

**Version Control**: All datasets in git, immutable once phase starts

**Data Quality**:
- Manual review: 10% of each dataset
- LLM validation: 100% (GPT-4 as oracle)
- Inter-annotator agreement: >90% on ambiguous cases

### Continuous Evaluation

**Daily**:
- Run all test suites for completed phases
- Track accuracy, latency, cost metrics
- Detect regressions immediately

**Weekly**:
- Comprehensive benchmark report
- Compare against LLM baseline
- Analyze failure patterns

**Monthly**:
- Dataset refresh (add new edge cases)
- Benchmark against latest LLM models
- Performance optimization review

---

## 🚀 Getting Started with Test Phases

### For Engineers

**Week 1**: Phase 0 Development
```bash
# 1. Create Phase 0 test dataset
python scripts/create_dataset.py --phase 0 --output tests/datasets/phase0_binary_sentiment_trivial.json

# 2. Implement minimal components
# - Task specification parser
# - LLM program generator (basic)
# - Program executor (simple exec)
# - LLM oracle (OpenAI API)
# - Comparison engine (string equality)

# 3. Run Phase 0 tests
pytest tests/test_phase0.py -v

# 4. Validate success criteria
python scripts/validate_phase.py --phase 0
```

**Week 2**: Phase 0 Validation
```bash
# 1. Run 100+ executions
python scripts/run_benchmark.py --phase 0 --samples 100

# 2. Generate cost report
python scripts/cost_analysis.py --phase 0

# 3. Submit Phase 0 completion
python scripts/submit_phase.py --phase 0 --results results/phase0_report.json
```

### For QA Team

**Test Execution**:
```bash
# Run specific phase tests
pytest tests/test_phase1.py -v --dataset tests/datasets/phase1_spam_detection.json

# Run all completed phases
pytest tests/ -k "phase0 or phase1" -v

# Generate test report
pytest tests/ --html=reports/test_report.html
```

**Dataset Validation**:
```bash
# Validate dataset format
python scripts/validate_dataset.py --input tests/datasets/phase1_spam_detection.json

# Check label distribution
python scripts/dataset_stats.py --input tests/datasets/phase1_spam_detection.json

# Generate oracle labels (GPT-4)
python scripts/generate_oracle_labels.py --input tests/datasets/phase1_spam_detection.json
```

### For Project Managers

**Phase Status Dashboard**: `http://localhost:3000/phases`

**Weekly Reports**: Auto-generated every Monday

**Success Criteria Checklist**: Available in project wiki

---

## 📝 Document Maintenance

**Ownership**: Engineering Lead, QA Lead

**Review Cadence**: After each phase completion

**Dataset Updates**: Add 50-100 samples per phase after completion (continuous improvement)

**Change Process**: RFC for dataset changes, approval required

---

**Last Updated**: 2025-10-25
**Version**: 1.0
**Status**: Ready for Phase 0 implementation

---

**End of Test Phases Document**
