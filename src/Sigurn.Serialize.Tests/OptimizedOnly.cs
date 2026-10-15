using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace Sigurn.Serialize.Tests;

// What a test of allocations measures holds only for optimized code: in a build without optimization the compiler makes the state
// machine of every async method a class, so every call sets one aside, finished at once or not. Such a test is skipped there, and says so.
static class OptimizedOnly
{
    public static string? SkipReason { get; } =
        typeof(Serializer).Assembly.GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled == true
            ? "Allocations are measured on optimized code only; Sigurn.Serialize is built without optimization."
            : null;
}

/// <summary>A fact that is run only where Sigurn.Serialize is built with optimization.</summary>
sealed class OptimizedOnlyFactAttribute : FactAttribute
{
    public OptimizedOnlyFactAttribute()
    {
        if (OptimizedOnly.SkipReason is { } reason)
            Skip = reason;
    }
}

/// <summary>A theory that is run only where Sigurn.Serialize is built with optimization.</summary>
sealed class OptimizedOnlyTheoryAttribute : TheoryAttribute
{
    public OptimizedOnlyTheoryAttribute()
    {
        if (OptimizedOnly.SkipReason is { } reason)
            Skip = reason;
    }
}
