"""
Core data models for Loopai framework using Pydantic.
"""

from datetime import datetime
from enum import Enum
from typing import Any, Dict, List, Optional
from uuid import UUID, uuid4

from pydantic import BaseModel, Field


class SynthesisStrategy(str, Enum):
    """Program synthesis strategy types."""

    RULE = "rule"
    ML = "ml"
    HYBRID = "hybrid"
    DSL = "dsl"
    AUTO = "auto"


class ProgramStatus(str, Enum):
    """Program artifact status."""

    DRAFT = "draft"
    VALIDATED = "validated"
    ACTIVE = "active"
    DEPRECATED = "deprecated"
    FAILED = "failed"


class ExecutionStatus(str, Enum):
    """Execution status."""

    SUCCESS = "success"
    ERROR = "error"
    TIMEOUT = "timeout"


class ComparisonMethod(str, Enum):
    """Output comparison method."""

    EXACT = "exact"
    SEMANTIC = "semantic"
    FUZZY = "fuzzy"
    STRUCTURED = "structured"


class FailureType(str, Enum):
    """Validation failure categorization."""

    SYNTAX_ERROR = "syntax_error"
    LOGIC_ERROR = "logic_error"
    EDGE_CASE = "edge_case"
    PERFORMANCE = "performance"


class TaskSpecification(BaseModel):
    """Task specification model."""

    id: UUID = Field(default_factory=uuid4)
    name: str = Field(..., description="Task name")
    description: str = Field(..., description="Natural language task description")
    input_schema: Dict[str, Any] = Field(..., description="JSON schema for input")
    output_schema: Dict[str, Any] = Field(..., description="JSON schema for output")
    examples: List[Dict[str, Any]] = Field(
        default_factory=list, description="Example input-output pairs"
    )
    accuracy_target: float = Field(default=0.9, ge=0.0, le=1.0)
    latency_target_ms: int = Field(default=10, gt=0)
    sampling_rate: float = Field(default=0.1, ge=0.0, le=1.0)
    created_at: datetime = Field(default_factory=datetime.utcnow)
    updated_at: datetime = Field(default_factory=datetime.utcnow)

    class Config:
        json_schema_extra = {
            "example": {
                "name": "spam-detection",
                "description": "Classify emails as spam or not spam",
                "input_schema": {"type": "object", "properties": {"text": {"type": "string"}}},
                "output_schema": {"type": "string", "enum": ["spam", "not_spam"]},
                "examples": [
                    {"input": {"text": "Buy now!"}, "output": "spam"},
                    {"input": {"text": "Meeting at 2pm"}, "output": "not_spam"},
                ],
                "accuracy_target": 0.9,
                "latency_target_ms": 10,
                "sampling_rate": 0.1,
            }
        }


class ComplexityMetrics(BaseModel):
    """Program complexity metrics."""

    cyclomatic_complexity: Optional[int] = None
    lines_of_code: Optional[int] = None
    estimated_latency_ms: Optional[float] = None


class ProgramArtifact(BaseModel):
    """Generated program artifact model."""

    id: UUID = Field(default_factory=uuid4)
    task_id: UUID = Field(..., description="Reference to task specification")
    version: int = Field(default=1, ge=1)
    language: str = Field(default="python", description="Programming language")
    code: str = Field(..., description="Program source code")

    # Metadata
    synthesis_strategy: SynthesisStrategy = Field(default=SynthesisStrategy.AUTO)
    confidence_score: float = Field(default=0.5, ge=0.0, le=1.0)
    complexity_metrics: ComplexityMetrics = Field(default_factory=ComplexityMetrics)

    # Generation context
    llm_provider: str = Field(default="openai")
    llm_model: str = Field(default="gpt-4")
    generation_cost: float = Field(default=0.0, ge=0.0)
    generation_time_sec: float = Field(default=0.0, ge=0.0)

    # Status
    status: ProgramStatus = Field(default=ProgramStatus.DRAFT)
    deployment_percentage: float = Field(default=0.0, ge=0.0, le=100.0)

    created_at: datetime = Field(default_factory=datetime.utcnow)
    deployed_at: Optional[datetime] = None


class ExecutionRecord(BaseModel):
    """Program execution record model."""

    id: UUID = Field(default_factory=uuid4)
    program_id: UUID = Field(..., description="Reference to program artifact")
    task_id: UUID = Field(..., description="Reference to task specification")

    # Input/Output
    input_data: Dict[str, Any] = Field(..., alias="input")
    output_data: Optional[Dict[str, Any]] = Field(None, alias="output")

    # Execution details
    latency_ms: Optional[float] = None
    memory_usage_mb: Optional[float] = None
    status: ExecutionStatus = Field(default=ExecutionStatus.SUCCESS)
    error_message: Optional[str] = None

    # Sampling
    sampled_for_validation: bool = Field(default=False)
    validation_id: Optional[UUID] = None

    executed_at: datetime = Field(default_factory=datetime.utcnow)

    class Config:
        populate_by_name = True


class ValidationRecord(BaseModel):
    """Validation record against LLM oracle model."""

    id: UUID = Field(default_factory=uuid4)
    execution_id: UUID = Field(..., description="Reference to execution record")
    program_id: UUID = Field(..., description="Reference to program artifact")

    # Oracle response
    oracle_output: Dict[str, Any] = Field(...)
    oracle_provider: str = Field(default="openai")
    oracle_model: str = Field(default="gpt-4")
    oracle_cost: float = Field(default=0.0, ge=0.0)
    oracle_latency_ms: float = Field(default=0.0, ge=0.0)

    # Comparison result
    match: bool = Field(...)
    similarity_score: float = Field(default=0.0, ge=0.0, le=1.0)
    comparison_method: ComparisonMethod = Field(default=ComparisonMethod.EXACT)

    # Failure details (if not match)
    failure_type: Optional[FailureType] = None
    failure_category: Optional[str] = None
    failure_details: Optional[str] = None

    # Multi-tier validation
    tier1_passed: bool = Field(default=True, description="Syntax/type validation")
    tier2_passed: bool = Field(default=True, description="Unit tests")
    tier3_passed: bool = Field(default=False, description="Oracle validation")

    validated_at: datetime = Field(default_factory=datetime.utcnow)


class ImprovementAction(BaseModel):
    """Improvement action tracking model."""

    id: UUID = Field(default_factory=uuid4)
    program_id: UUID = Field(..., description="Reference to program artifact")
    task_id: UUID = Field(..., description="Reference to task specification")

    # Trigger
    trigger_type: str = Field(..., description="automatic, manual, or scheduled")
    trigger_reason: str = Field(...)
    failure_rate: float = Field(default=0.0, ge=0.0, le=1.0)
    failure_count: int = Field(default=0, ge=0)

    # Analysis
    failure_pattern: Optional[str] = None
    failure_examples: List[UUID] = Field(
        default_factory=list, description="References to ValidationRecord IDs"
    )
    improvement_strategy: SynthesisStrategy = Field(default=SynthesisStrategy.AUTO)

    # Result
    new_program_id: Optional[UUID] = None
    status: str = Field(default="pending")
    ab_test_result: Optional[Dict[str, Any]] = None

    # Human escalation
    escalated: bool = Field(default=False)
    escalation_ticket_id: Optional[str] = None
    escalation_reason: Optional[str] = None

    created_at: datetime = Field(default_factory=datetime.utcnow)
    completed_at: Optional[datetime] = None


class TestCase(BaseModel):
    """Test case from dataset."""

    id: int
    input: str
    expected_output: str
    difficulty: str = "trivial"
    keywords: Optional[List[str]] = None


class TestDataset(BaseModel):
    """Test dataset model."""

    metadata: Dict[str, Any]
    test_cases: List[TestCase]
