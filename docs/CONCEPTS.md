# Loopai Core Concepts

**Version**: 1.0.0
**Last Updated**: 2025-10-29
**Status**: Living Document

---

## 🎯 Overview

This document clarifies the core concepts and terminology used in the Loopai ecosystem. Understanding these concepts is essential for working with Loopai.

---

## 📚 Concept Hierarchy

```
┌─────────────────────────────────────────────────────────────┐
│                    CONSUMER APPLICATIONS                     │
│                  (Your End-User Applications)                │
│                                                               │
│  Examples:                                                   │
│  - E-commerce Platform                                       │
│  - Customer Support System                                   │
│  - Content Moderation Dashboard                             │
│  - Email Client                                              │
└────────────────────────┬─────────────────────────────────────┘
                         │
                         │ Uses multiple Loop Apps
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                       LOOP APPS                              │
│              (Individual Task Instances)                     │
│                                                               │
│  Each Loop App is an independent, specialized program:      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │ spam         │  │ sentiment    │  │ email        │     │
│  │  detector    │  │  analyzer    │  │  categorizer │     │
│  │              │  │              │  │              │     │
│  │ v1, v2, v3   │  │ v1, v2       │  │ v1           │     │
│  │ dataset/     │  │ dataset/     │  │ dataset/     │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
└────────────────────────┬─────────────────────────────────────┘
                         │
                         │ Built on & Managed by
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                   LOOPAI FRAMEWORK                           │
│              (Infrastructure Middleware)                     │
│                                                               │
│  ┌───────────────┐  ┌───────────────┐  ┌──────────────┐   │
│  │ Cloud         │  │ Edge          │  │ Client       │   │
│  │ Platform      │  │ Runtime       │  │ SDKs         │   │
│  │               │  │               │  │              │   │
│  │ • Generator   │  │ • Executor    │  │ • Python     │   │
│  │ • Repository  │  │ • Dataset Mgr │  │ • TypeScript │   │
│  │ • Improvement │  │ • Telemetry   │  │ • .NET       │   │
│  └───────────────┘  └───────────────┘  └──────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔑 Core Concepts

### 1. Loopai Framework

**Definition**: The infrastructure middleware that provides program synthesis, execution, and continuous improvement capabilities.

**Responsibilities**:
- **Program Generation**: Synthesize programs from specifications
- **Artifact Management**: Version control, storage, distribution
- **Execution Runtime**: Safe, fast program execution environment
- **Continuous Improvement**: Automated program refinement based on feedback
- **Telemetry & Analytics**: Collect, analyze, and act on execution data

**Analogy**: Loopai Framework is like **AWS** or **Kubernetes** - it's the infrastructure layer that applications are built on top of.

**Not Responsible For**:
- Business logic specific to individual tasks
- User-facing application features
- End-user authentication/authorization (handled by Consumer Apps)

---

### 2. Loop App

**Definition**: An individual, specialized program instance created for a specific task. Each Loop App is an independent entity with its own:
- Program versions (v1, v2, v3...)
- Dataset (execution history, validations)
- Configuration (sampling rate, privacy settings)
- Lifecycle (creation, improvement, deprecation)
- API endpoint

**Examples**:

```yaml
# Loop App 1: Email Spam Detection
loop_app_id: spam-detector-001
name: "Email Spam Detector"
task: "Classify emails as spam or ham"
input_schema:
  type: object
  properties:
    text: {type: string}
    sender: {type: string}
output_schema:
  type: string
  enum: [spam, ham]
current_version: 3
endpoint: /loop-apps/spam-detector-001/execute

# Loop App 2: Sentiment Analysis
loop_app_id: sentiment-analyzer-001
name: "Customer Sentiment Analyzer"
task: "Analyze customer feedback sentiment"
input_schema:
  type: object
  properties:
    feedback: {type: string}
output_schema:
  type: string
  enum: [positive, negative, neutral]
current_version: 2
endpoint: /loop-apps/sentiment-analyzer-001/execute

# Loop App 3: Email Categorization
loop_app_id: email-categorizer-001
name: "Email Category Router"
task: "Categorize emails into folders"
input_schema:
  type: object
  properties:
    subject: {type: string}
    body: {type: string}
output_schema:
  type: string
  enum: [inbox, urgent, newsletters, promotions]
current_version: 1
endpoint: /loop-apps/email-categorizer-001/execute
```

**Key Characteristics**:
- **Independence**: Each Loop App evolves independently
- **Versioning**: Multiple program versions coexist (v1, v2, v3...)
- **Data Isolation**: Separate datasets for each Loop App
- **Dedicated Endpoint**: Each has its own execution API
- **Lifecycle Management**: Can be created, improved, paused, archived

**Analogy**: A Loop App is like a **microservice** in a microservices architecture - it's a specialized, independently deployable unit.

**Storage Structure**:

**Phase 1 (Current - Full Isolation)**:
```
/loopai-data/
└── loop-apps/
    ├── spam-detector-001/
    │   ├── artifacts/
    │   │   ├── v1/program.py
    │   │   ├── v2/program.py
    │   │   ├── v3/program.py
    │   │   └── active -> v3/
    │   ├── datasets/
    │   │   ├── executions/2025-10-29.jsonl
    │   │   ├── validations/sampled.jsonl
    │   │   └── analytics/daily-stats.json
    │   └── config.yaml
    │
    ├── sentiment-analyzer-001/
    │   ├── artifacts/
    │   ├── datasets/
    │   └── config.yaml
    │
    └── email-categorizer-001/
        ├── artifacts/
        ├── datasets/
        └── config.yaml
```

**Phase 2+ (Hybrid - With Shared Artifacts)** - See [Advanced Architecture](#-advanced-architecture-optional) section:
```
/loopai-data/
├── shared/                          # Shared resources (optional)
│   ├── artifacts/
│   │   ├── models/
│   │   │   └── spam-classifier-base-v2/
│   │   └── vectorizers/
│   └── datasets/                    # Reference only (read-only)
│
└── loop-apps/                       # Individual Loop Apps
    ├── spam-detector-001/
    │   ├── artifacts/
    │   │   └── v1/
    │   │       ├── program.py
    │   │       └── dependencies.yaml  # References /shared/
    │   ├── datasets/                  # Always isolated
    │   └── config.yaml
    └── ...
```

---

### 3. Consumer Application

**Definition**: The end-user application that utilizes one or more Loop Apps to provide business value.

**Examples**:

#### Example 1: E-commerce Platform
```python
# Consumer App uses multiple Loop Apps
class EcommercePlatform:
    def __init__(self):
        self.spam_detector = LoopaiClient("spam-detector-001")
        self.sentiment_analyzer = LoopaiClient("sentiment-analyzer-001")
        self.product_categorizer = LoopaiClient("product-categorizer-001")

    def process_review(self, review):
        # Check if review is spam
        if self.spam_detector.execute({"text": review})["output"] == "spam":
            return {"status": "rejected", "reason": "spam"}

        # Analyze sentiment
        sentiment = self.sentiment_analyzer.execute({"text": review})["output"]

        # Categorize product
        category = self.product_categorizer.execute({"text": review})["output"]

        return {
            "status": "approved",
            "sentiment": sentiment,
            "category": category
        }
```

#### Example 2: Email Client
```python
# Consumer App orchestrates multiple Loop Apps
class EmailClient:
    def __init__(self):
        self.spam_detector = LoopaiClient("spam-detector-001")
        self.email_categorizer = LoopaiClient("email-categorizer-001")
        self.priority_scorer = LoopaiClient("priority-scorer-001")

    def process_incoming_email(self, email):
        # Filter spam
        if self.spam_detector.execute({"text": email.body})["output"] == "spam":
            return "move_to_spam"

        # Categorize
        category = self.email_categorizer.execute({
            "subject": email.subject,
            "body": email.body
        })["output"]

        # Score priority
        priority = self.priority_scorer.execute({"email": email})["output"]

        return {
            "folder": category,
            "priority": priority
        }
```

**Key Characteristics**:
- **Orchestration**: Combines multiple Loop Apps
- **Business Logic**: Implements domain-specific workflows
- **User Interface**: Provides end-user interaction layer
- **Data Ownership**: Owns user data and business data
- **Authentication**: Manages user auth and permissions

---

## 🔄 Relationship Between Concepts

### Flow of Execution

```
1. Consumer App receives user request
   ↓
2. Consumer App calls Loop App endpoint
   POST /loop-apps/spam-detector-001/execute
   ↓
3. Loopai Framework routes to appropriate Loop App
   ↓
4. Loop App executes its current program version
   ↓
5. Result returned to Consumer App
   ↓
6. Consumer App uses result in business logic
   ↓
7. Consumer App responds to user
```

### Data Flow

```
┌─────────────────────┐
│  Consumer App Data  │ (User profiles, business records)
└──────────┬──────────┘
           │ Sends task-specific input
           ▼
┌─────────────────────┐
│   Loop App Data     │ (Execution logs, validations, analytics)
└──────────┬──────────┘
           │ Telemetry (sampled, privacy-aware)
           ▼
┌─────────────────────┐
│ Framework Analytics │ (Aggregated patterns, improvement triggers)
└─────────────────────┘
```

### Lifecycle Management

```
Consumer App Lifecycle:
- Developed by your team
- Deployed to your infrastructure
- Maintained by your team

Loop App Lifecycle:
- Created via Loopai API or UI
- Generated by Loopai Framework
- Improved automatically by Loopai
- Versioned and managed by Loopai

Loopai Framework Lifecycle:
- Managed by Loopai team
- Updated without consumer app changes
- Backward compatible
```

---

## 🏗️ Advanced Architecture (Optional)

### Shared Artifacts Architecture

**Purpose**: Optimize storage and enable reusability when multiple Loop Apps share common resources.

**When to Use**:
- ✅ Multiple Loop Apps use identical foundation models (>50MB)
- ✅ Common preprocessing pipelines (tokenizers, vectorizers, embeddings)
- ✅ Organization-wide standard models
- ✅ Storage efficiency is critical

**When NOT to Use**:
- ❌ Loop App-specific customizations
- ❌ Rapid iteration and experimentation
- ❌ Strict isolation requirements
- ❌ Different versioning needs per Loop App

### Architecture: Hybrid Storage Model

```
/loopai-data/
├── shared/                          # Centralized shared resources
│   ├── artifacts/
│   │   ├── models/
│   │   │   └── spam-classifier-base-v2/
│   │   │       ├── model.pkl        # 100MB foundation model
│   │   │       ├── config.json
│   │   │       └── metadata.yaml
│   │   ├── vectorizers/
│   │   │   └── tfidf-english-v1.pkl
│   │   └── embeddings/
│   │       └── sentence-transformer-v1/
│   │
│   └── datasets/                    # Reference datasets (read-only)
│       └── common-spam-examples/
│           └── examples.jsonl       # 10K+ shared examples
│
└── loop-apps/
    ├── spam-detector-email/
    │   ├── artifacts/
    │   │   └── v1/
    │   │       ├── program.py
    │   │       ├── dependencies.yaml    # References /shared/
    │   │       └── email-patterns.json  # Local customization
    │   ├── datasets/                    # Isolated execution data
    │   │   ├── executions/
    │   │   └── validations/
    │   └── config.yaml
    │
    └── spam-detector-sms/
        └── artifacts/
            └── v1/
                ├── program.py
                ├── dependencies.yaml    # Same shared model
                └── sms-patterns.json    # Different customization
```

### Dependency Reference Mechanism

**dependencies.yaml Example**:
```yaml
# /loop-apps/spam-detector-email/artifacts/v1/dependencies.yaml

shared_artifacts:
  - type: model
    name: spam-classifier-base
    version: v2
    path: /shared/artifacts/models/spam-classifier-base-v2/
    hash: sha256:abc123def456...
    update_policy: pinned           # Never auto-update (stability)

  - type: vectorizer
    name: tfidf-english
    version: v1
    path: /shared/artifacts/vectorizers/tfidf-english-v1.pkl
    hash: sha256:789ghi012jkl...
    update_policy: pinned

local_artifacts:
  - type: custom-rules
    path: ./email-patterns.json
    description: Email-specific spam patterns
```

**Program Code Using Shared Artifacts**:
```python
# /loop-apps/spam-detector-email/artifacts/v1/program.py

from pathlib import Path
import pickle
import json

# Load shared artifacts (referenced, not copied)
SHARED_ROOT = Path("/loopai-data/shared/artifacts")
base_model = pickle.load(
    open(SHARED_ROOT / "models/spam-classifier-base-v2/model.pkl", "rb")
)
vectorizer = pickle.load(
    open(SHARED_ROOT / "vectorizers/tfidf-english-v1.pkl", "rb")
)

# Load local customization
LOCAL_ROOT = Path(__file__).parent
custom_rules = json.load(open(LOCAL_ROOT / "email-patterns.json"))

def execute(input_data):
    """
    Combines shared foundation model with local customization
    """
    # Use shared vectorizer
    features = vectorizer.transform([input_data["text"]])

    # Use shared base model
    base_prediction = base_model.predict_proba(features)[0]

    # Apply local email-specific rules
    if any(pattern in input_data["text"] for pattern in custom_rules["spam_keywords"]):
        return {"result": "spam", "confidence": 0.95, "reason": "custom_rule"}

    # Return base model prediction
    return {
        "result": "spam" if base_prediction[1] > 0.5 else "ham",
        "confidence": float(max(base_prediction)),
        "reason": "base_model"
    }
```

### Design Principles

1. **Execution Data Isolation**: Execution datasets are NEVER shared (privacy, security)
2. **Pinned Versioning**: Shared artifacts use explicit versions, never auto-update
3. **Hash Verification**: Integrity check for all shared artifact loads
4. **Fallback Strategy**: If shared artifact unavailable, fail gracefully with clear error
5. **Audit Trail**: Track which Loop Apps depend on which shared artifacts
6. **Opt-In**: Shared artifacts are optional; default is full isolation

### Implementation Phases

**Phase 1: MVP (Current)**
- ✅ Full isolation per Loop App
- ✅ Simple, predictable behavior
- ✅ No shared artifacts complexity
- ✅ Easy debugging and rollback

**Phase 2: Optimization (Future)**
- ✅ Introduce `/loopai-data/shared/` directory
- ✅ Implement dependency reference mechanism
- ✅ Storage efficiency gains (100MB × 1 vs 100MB × 10)
- ⚠️ Increased deployment complexity

**Phase 3: Advanced (Future)**
- ✅ Shared dataset references (training only)
- ✅ Federated learning support
- ✅ Auto-update policies with safety guards
- ⚠️ Enterprise-level complexity

### Example Use Cases

**Use Case 1: Multiple Spam Detectors**
```yaml
Scenario:
  - spam-detector-email (email text)
  - spam-detector-sms (SMS messages)
  - spam-detector-comments (forum comments)

Strategy:
  - All share: spam-classifier-base-v2 (100MB foundation model)
  - Each has: Custom patterns for their domain
  - Storage: 100MB shared + 3×5MB local = 115MB total
  - vs Full Isolation: 3×100MB = 300MB total

Savings: 62% storage reduction
```

**Use Case 2: Sentiment Analysis Family**
```yaml
Scenario:
  - sentiment-product-reviews
  - sentiment-customer-feedback
  - sentiment-social-media

Strategy:
  - All share: sentence-transformer embedding model (80MB)
  - All share: Common sentiment training set (50K examples)
  - Each has: Domain-specific fine-tuning data

Benefit: Consistent embeddings + transfer learning from shared data
```

### Trade-offs

**Advantages**:
- ✅ Storage efficiency (especially for large models)
- ✅ Consistency across Loop Apps (same base model version)
- ✅ Centralized management of foundation models
- ✅ Transfer learning from shared datasets
- ✅ Faster Loop App creation (reference existing artifacts)

**Disadvantages**:
- ⚠️ Increased complexity (dependency management)
- ⚠️ Reduced isolation (shared artifact update affects multiple Loop Apps)
- ⚠️ Deployment complexity (must deploy shared artifacts first)
- ⚠️ Versioning conflicts (if Loop Apps need different versions)
- ⚠️ Harder debugging (need to track shared dependencies)

### Best Practices

1. **Start Simple**: Begin with Phase 1 (full isolation), add shared artifacts only when needed
2. **Pin Versions**: Always pin shared artifact versions in dependencies.yaml
3. **Hash Verification**: Verify shared artifact integrity before loading
4. **Document Dependencies**: Maintain clear documentation of what shares what
5. **Monitor Usage**: Track which Loop Apps use which shared artifacts
6. **Test Isolation**: Ensure shared artifact failure doesn't cascade
7. **Privacy First**: Never share execution data, only models and reference datasets

---

## 🎨 Naming Conventions

### Loop App IDs
- **Format**: `{task-name}-{instance-number}`
- **Examples**:
  - `spam-detector-001`
  - `sentiment-analyzer-002`
  - `email-categorizer-prod`
  - `fraud-detector-v2`

### API Endpoints
- **Pattern**: `/loop-apps/{loop-app-id}/execute`
- **Examples**:
  - `POST /loop-apps/spam-detector-001/execute`
  - `POST /loop-apps/sentiment-analyzer-001/execute`

### File Paths
- **Pattern**: `/loopai-data/loop-apps/{loop-app-id}/...`
- **Examples**:
  - `/loopai-data/loop-apps/spam-detector-001/datasets/executions/2025-10-29.jsonl`
  - `/loopai-data/loop-apps/sentiment-analyzer-001/artifacts/v2/program.py`

---

## ❓ Common Questions

### Q: Is a Loop App the same as a Task?
**A**: Yes, historically we used "Task" to refer to what we now call "Loop App". We're standardizing on "Loop App" for clarity:
- **Task** = Abstract concept ("spam detection")
- **Loop App** = Concrete instance ("spam-detector-001")

### Q: Can I have multiple Loop Apps for the same task?
**A**: Yes! You might have:
- `spam-detector-prod` (production instance)
- `spam-detector-dev` (development instance)
- `spam-detector-experimental` (testing new approaches)

### Q: When should I create a new Loop App vs. a new Consumer App?
**A**:
- **New Loop App**: When you need a new specialized program for a specific task (e.g., adding sentiment analysis capability)
- **New Consumer App**: When you need a new user-facing application that uses existing Loop Apps (e.g., a mobile app version of your web app)

### Q: How does a Loop App relate to a Program Artifact?
**A**:
- **Loop App**: The logical entity (contains multiple versions)
- **Program Artifact**: A specific version (v1, v2, v3...)
- **Relationship**: Loop App → has many → Program Artifacts

```
spam-detector-001 (Loop App)
├── v1 (Program Artifact) - deprecated
├── v2 (Program Artifact) - active
└── v3 (Program Artifact) - testing
```

### Q: Can Loop Apps communicate with each other?
**A**: No, Loop Apps are independent. Communication happens at the Consumer App level:
```python
# Consumer App orchestrates
result1 = loop_app_1.execute(input)
result2 = loop_app_2.execute(result1)  # Sequential
```

### Q: How do I choose between Edge vs. Cloud execution?
**A**:
- **Cloud Execution**: Best for development, low-volume, or when you want zero infrastructure
- **Edge Execution**: Best for production, high-volume, low-latency, or privacy-sensitive data
- **Hybrid**: Develop in cloud, deploy to edge (recommended)

**Note**: This is a deployment choice at the Consumer App level, not per Loop App.

### Q: When should I use shared artifacts vs isolated artifacts?
**A**:
- **Start with Phase 1 (Isolated)**: Default for MVP, simple and predictable
- **Consider Phase 2 (Shared)** when:
  - Multiple Loop Apps use identical foundation models (>50MB)
  - Storage efficiency is critical (e.g., 10 Loop Apps × 100MB = 1GB waste)
  - You need consistent base models across Loop Apps
  - Organization has standardized models/embeddings

**Always keep isolated**:
- Execution datasets (privacy, security)
- Loop App-specific customizations
- Rapidly iterating or experimental artifacts

See [Advanced Architecture](#-advanced-architecture-optional) section for details.

---

## 📖 Related Documentation

- **ARCHITECTURE.md**: Technical architecture details
- **API.md**: API reference and endpoints
- **DEPLOYMENT.md**: Deployment guides for edge runtime
- **README.md**: Project overview and getting started

---

**Document Version**: 1.0.0
**Last Updated**: 2025-10-29
**Status**: Active - This is the authoritative definition of Loopai concepts
