using System;
using System.Collections.Generic;

namespace Gum.Expressions;

/// <summary>
/// The names of the functions a variable reference expression may call (e.g. <c>Sin(Angle)</c>).
/// This lives in GumCommon, free of Roslyn, so code that only scans reference text (such as the
/// headless error checker) can tell a function name from a variable. The evaluator in
/// <c>Gum.Expressions</c> implements exactly these names.
/// </summary>
public static class ExpressionFunctionNames
{
    public static readonly IReadOnlyCollection<string> All = new[]
    {
        "Sin", "Cos", "Tan", "Sqrt", "Abs", "Floor", "Ceiling", "Round", "Min", "Max", "Clamp",
    };
}
