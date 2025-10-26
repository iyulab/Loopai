namespace Loopai.Core.Models;

/// <summary>
/// Program synthesis strategy types.
/// </summary>
public enum SynthesisStrategy
{
    Rule,
    ML,
    Hybrid,
    DSL,
    Auto
}

/// <summary>
/// Program artifact status.
/// </summary>
public enum ProgramStatus
{
    Draft,
    Validated,
    Active,
    Deprecated,
    Failed
}

/// <summary>
/// Execution status.
/// </summary>
public enum ExecutionStatus
{
    Success,
    Error,
    Timeout
}

/// <summary>
/// Output comparison method.
/// </summary>
public enum ComparisonMethod
{
    Exact,
    Semantic,
    Fuzzy,
    Structured
}

/// <summary>
/// Validation failure categorization.
/// </summary>
public enum FailureType
{
    SyntaxError,
    LogicError,
    EdgeCase,
    Performance
}
