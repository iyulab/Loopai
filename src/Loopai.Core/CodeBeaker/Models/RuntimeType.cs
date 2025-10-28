namespace Loopai.Core.CodeBeaker.Models;

/// <summary>
/// CodeBeaker runtime types for execution environments.
/// </summary>
public enum RuntimeType
{
    /// <summary>
    /// Docker container-based runtime (default).
    /// Strong isolation, slower startup (~560ms), higher memory (~250MB).
    /// </summary>
    Docker,

    /// <summary>
    /// Deno process-based runtime for JavaScript/TypeScript.
    /// Fast startup (~80ms), low memory (~30MB), permission-based sandboxing.
    /// </summary>
    Deno,

    /// <summary>
    /// Bun process-based runtime for JavaScript/TypeScript.
    /// Ultra-fast startup (~50ms), very low memory (~25MB), best performance.
    /// </summary>
    Bun,

    /// <summary>
    /// Node.js process-based runtime for JavaScript.
    /// Good compatibility, moderate startup, medium memory.
    /// </summary>
    NodeJs,

    /// <summary>
    /// Python process-based runtime.
    /// Fast startup, low memory, native Python execution.
    /// </summary>
    Python
}
