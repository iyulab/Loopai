# Getting Started with Loopai Development

**Quick start guide for developers working on Loopai**

---

## 🚀 Development Setup

### Prerequisites

- Python 3.9 or higher
- pip or poetry for package management
- Git for version control
- OpenAI API key (for LLM integration)

### 1. Clone and Setup

```bash
# Clone the repository
git clone https://github.com/iyulab/loopai.git
cd loopai

# Create virtual environment
python -m venv venv

# Activate virtual environment
# On Windows:
venv\Scripts\activate
# On macOS/Linux:
source venv/bin/activate

# Install dependencies
pip install -r requirements.txt

# Install in development mode
pip install -e .
```

### 2. Configure Environment

Create a `.env` file in the project root:

```bash
# Copy the example file
cp .env.example .env

# Edit .env and add your keys
```

The `.env` file should contain:

```env
# OpenAI Configuration (required)
OPENAI_API_KEY=your_openai_api_key_here
OPENAI_MODEL=gpt-4

# Available models:
# - gpt-4 (recommended for accuracy)
# - gpt-4-turbo-preview (faster, cheaper)
# - gpt-3.5-turbo (cheapest, lower accuracy)

# Optional: For Phase 2+
# ANTHROPIC_API_KEY=your_anthropic_api_key_here

# Configuration
LOOPAI_LOG_LEVEL=INFO
LOOPAI_ENV=development
```

### 3. Verify Installation

```bash
# Run tests to verify setup
pytest tests/test_phase0.py -v

# Expected output: 4 passing tests (dataset validation)
# Note: Implementation tests are skipped initially
```

---

## 📋 Phase 0 Development Workflow

### Current Status

**Phase 0: Proof of Concept** - In Progress

**Completed**:
- ✅ Project structure created
- ✅ Core data models defined
- ✅ Phase 0 test dataset (100 samples)
- ✅ Phase 0 test suite skeleton

**Next Steps**:
- [ ] Implement program generator (minimal)
- [ ] Implement program executor
- [ ] Implement LLM oracle interface
- [ ] Implement comparison engine
- [ ] Run full Phase 0 validation

### Development Tasks

#### Task 1: Implement Program Generator

**Goal**: Generate a simple Python program that classifies sentiment using hard-coded rules

**File**: `src/loopai/generator/program_generator.py`

**Approach**:
```python
# Pseudo-code for Phase 0 generator
def generate_program(task_spec):
    # For Phase 0: Generate rule-based sentiment classifier
    # Use LLM to generate code with keywords like:
    # "Write a Python function that classifies text as 'positive' or 'negative'
    #  based on keyword matching. Positive keywords: amazing, love, best, great...
    #  Negative keywords: terrible, worst, awful, bad..."

    # Return ProgramArtifact with generated code
    pass
```

**Test Command**:
```bash
pytest tests/test_phase0.py::TestPhase0ProgramGeneration -v
```

#### Task 2: Implement Program Executor

**Goal**: Execute generated Python programs safely with timeout

**File**: `src/loopai/executor/program_executor.py`

**Approach**:
```python
# Pseudo-code for Phase 0 executor
def execute_program(program_code, input_data):
    # Compile program
    # Execute with timeout (10ms target)
    # Capture output
    # Return ExecutionRecord
    pass
```

**Test Command**:
```bash
pytest tests/test_phase0.py::TestPhase0Execution -v
```

#### Task 3: Implement LLM Oracle Interface

**Goal**: Query LLM (GPT-4) for ground truth output

**File**: `src/loopai/validator/llm_oracle.py`

**Approach**:
```python
# Pseudo-code for LLM oracle
def query_oracle(task_spec, input_data):
    # Build prompt: "Classify this text as positive or negative: {input}"
    # Call OpenAI API
    # Parse response
    # Return oracle output with cost and latency
    pass
```

**Test Command**:
```bash
pytest tests/test_phase0.py::TestPhase0Validation -v
```

#### Task 4: Implement Comparison Engine

**Goal**: Compare program output vs oracle output

**File**: `src/loopai/validator/comparison_engine.py`

**Approach**:
```python
# Pseudo-code for comparison
def compare_outputs(program_output, oracle_output, method="exact"):
    # For Phase 0: Simple string equality
    # Return ValidationRecord with match result
    pass
```

---

## 🧪 Testing Strategy

### Run Specific Phase Tests

```bash
# Run only Phase 0 tests
pytest -m phase0 -v

# Run only implemented tests (skip placeholders)
pytest -m phase0 -v -k "not skip"

# Run with coverage
pytest -m phase0 --cov=loopai --cov-report=html
```

### Test-Driven Development Flow

1. **Write test first** (already done for Phase 0)
2. **Run test** - should fail initially
3. **Implement minimal code** to pass test
4. **Run test again** - should pass
5. **Refactor** if needed
6. **Repeat** for next test

### Phase 0 Success Criteria Checklist

Run this after implementation:

```bash
# Full Phase 0 validation
pytest tests/test_phase0.py -v

# Success criteria:
# ✓ Dataset validation: 4/4 tests pass
# ✓ Program generation: 3/3 tests pass
# ✓ Execution: 3/3 tests pass
# ✓ Validation: 3/3 tests pass
# ✓ Metrics: 3/3 tests pass
# ✓ Total: 16/16 tests pass
```

---

## 📊 Phase 0 Validation Script

After implementing all components, run the full validation:

```bash
# Run Phase 0 benchmark
python scripts/run_phase0_benchmark.py

# Expected output:
# ==========================================
# Phase 0 Benchmark Results
# ==========================================
# Accuracy: 100.0% (100/100 correct)
# Average Latency: 3.5ms (p99: 8.2ms)
# LLM Oracle Latency: 1250ms average
# Speedup: 357x
#
# Cost Analysis:
# - Program Generation: $0.05 (one-time)
# - 100 Executions: $0.001 (program)
# - 100 LLM Calls: $0.20 (direct inference)
# - Cost Reduction: 99.5%
# - Break-even: 25 executions
#
# ✅ Phase 0 SUCCESS - All criteria met
# ==========================================
```

---

## 🔄 Development Workflow

### Daily Development

```bash
# 1. Pull latest changes
git pull origin main

# 2. Create feature branch
git checkout -b feature/phase0-generator

# 3. Make changes and test frequently
pytest tests/test_phase0.py -v

# 4. Run linting and formatting
black src/ tests/
ruff check src/ tests/

# 5. Commit changes
git add .
git commit -m "feat: implement program generator for Phase 0"

# 6. Push and create PR
git push origin feature/phase0-generator
```

### Code Quality Checks

```bash
# Format code
black src/ tests/

# Lint code
ruff check src/ tests/ --fix

# Type checking
mypy src/loopai

# Run all checks before committing
black src/ tests/ && ruff check src/ tests/ && mypy src/loopai && pytest
```

---

## 📚 Key Files Reference

### Source Code Structure

```
src/loopai/
├── __init__.py                     # Package initialization
├── models.py                       # Core data models (Pydantic)
├── generator/
│   ├── __init__.py
│   └── program_generator.py       # LLM program generation
├── executor/
│   ├── __init__.py
│   └── program_executor.py        # Program execution engine
├── validator/
│   ├── __init__.py
│   ├── llm_oracle.py              # LLM oracle interface
│   └── comparison_engine.py       # Output comparison logic
└── orchestrator/
    ├── __init__.py
    └── improvement_orchestrator.py # (Phase 2+)
```

### Test Structure

```
tests/
├── datasets/
│   └── phase0_binary_sentiment_trivial.json
├── unit/
│   ├── test_models.py
│   ├── test_generator.py
│   └── test_executor.py
├── integration/
│   └── test_end_to_end.py
└── test_phase0.py                  # Phase 0 integration tests
```

---

## 🐛 Troubleshooting

### Common Issues

**Issue**: `ImportError: No module named 'loopai'`
```bash
# Solution: Install in development mode
pip install -e .
```

**Issue**: `OpenAI API key not found`
```bash
# Solution: Create .env file with OPENAI_API_KEY
echo "OPENAI_API_KEY=your_key_here" > .env
```

**Issue**: Tests fail with "fixture not found"
```bash
# Solution: Run pytest from project root
cd /path/to/loopai
pytest tests/test_phase0.py
```

### Getting Help

- Check documentation: `docs/`
- Review architecture: `docs/architecture.md`
- Check test phases: `docs/TEST_PHASES.md`
- Open an issue: GitHub Issues

---

## 🎯 Next Steps After Phase 0

Once Phase 0 is complete:

1. **Review Phase 0 results** - Document learnings and metrics
2. **Plan Phase 1** - Basic classification tasks
3. **Create Phase 1 datasets** - Spam detection, language detection, sentiment
4. **Implement sampling** - Random sampling (10-30% rate)
5. **Begin Phase 1 development**

---

## 📖 Additional Resources

- [Architecture Guide](architecture.md)
- [Test Phases](TEST_PHASES.md)
- [Development Tasks](TASKS.md)
- [Research Background](research.md)

---

**Last Updated**: 2025-10-25
**Status**: Phase 0 - In Development
