using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Expressions;

/// <summary>
/// The whitelisted functions callable from a variable reference expression, e.g. <c>Sin(Angle)</c>
/// or <c>Math.Max(Width, 50)</c>. This is deliberately a closed set, not a general call mechanism.
/// Names are case-sensitive and match <see cref="Math"/>; angles are in radians.
/// </summary>
internal static class ExpressionFunctions
{
    private sealed record FunctionDefinition(int ArgumentCount, Func<object[], object?> Invoke);

    private const string MathPrefix = "Math";

    private static readonly Dictionary<string, FunctionDefinition> Functions = new()
    {
        ["Sin"] = new(1, args => Transcendental(args[0], Math.Sin)),
        ["Cos"] = new(1, args => Transcendental(args[0], Math.Cos)),
        ["Tan"] = new(1, args => Transcendental(args[0], Math.Tan)),
        ["Sqrt"] = new(1, args => Transcendental(args[0], Math.Sqrt)),
        ["Abs"] = new(1, args => KeepType(args[0], Math.Abs)),
        ["Floor"] = new(1, args => KeepType(args[0], Math.Floor)),
        ["Ceiling"] = new(1, args => KeepType(args[0], Math.Ceiling)),
        ["Round"] = new(1, args => KeepType(args[0], d => Math.Round(d))),
        ["Min"] = new(2, args => Widened(args, (a, b) => a.CompareTo(b) <= 0 ? a : b)),
        ["Max"] = new(2, args => Widened(args, (a, b) => a.CompareTo(b) >= 0 ? a : b)),
        ["Clamp"] = new(3, args => Clamp(args)),
    };

    public static string SupportedNames => string.Join(", ", Functions.Keys.OrderBy(name => name));

    /// <summary>
    /// Resolves the callee of <paramref name="invocation"/> to a supported function name, accepting
    /// both <c>Max(a, b)</c> and <c>Math.Max(a, b)</c>. <paramref name="displayName"/> is the callee as
    /// the user wrote it, for error messages.
    /// </summary>
    public static bool TryGetFunctionName(InvocationExpressionSyntax invocation, out string name, out string displayName)
    {
        displayName = invocation.Expression.ToString();
        name = string.Empty;

        if (invocation.Expression is IdentifierNameSyntax identifier)
        {
            name = identifier.Identifier.ValueText;
        }
        else if (invocation.Expression is MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.ValueText: MathPrefix },
                Name: IdentifierNameSyntax memberName
            })
        {
            name = memberName.Identifier.ValueText;
        }
        else
        {
            return false;
        }

        return Functions.ContainsKey(name);
    }

    public static int GetArgumentCount(string name) => Functions[name].ArgumentCount;

    /// <summary>
    /// Invokes <paramref name="name"/> on already-evaluated arguments. Returns null when any argument
    /// is missing or non-numeric, or when the result is not a finite number (e.g. <c>Sqrt(-1)</c>),
    /// matching how division by zero is treated.
    /// </summary>
    public static object? Invoke(string name, object?[] arguments)
    {
        FunctionDefinition function = Functions[name];

        if (arguments.Length != function.ArgumentCount || arguments.Any(a => a == null || !IsNumeric(a)))
        {
            return null;
        }

        try
        {
            object? result = function.Invoke(arguments!);
            return result switch
            {
                float f when !float.IsFinite(f) => null,
                double d when !double.IsFinite(d) => null,
                _ => result
            };
        }
        catch (OverflowException)
        {
            // e.g. Abs(int.MinValue)
            return null;
        }
    }

    private static bool IsNumeric(object value) =>
        value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    // Sin/Cos/Tan/Sqrt: a double input stays double, everything else (Gum variables are floats) yields a float.
    private static object Transcendental(object argument, Func<double, double> function)
    {
        double result = function(Convert.ToDouble(argument));
        // Boxed branches: a bare `cond ? double : float` would promote the float to double.
        return argument is double ? (object)result : (float)result;
    }

    // Abs/Floor/Ceiling/Round: the result has the same type as the argument, so Abs of an int is an int.
    private static object KeepType(object argument, Func<double, double> function)
    {
        double result = function(Convert.ToDouble(argument));
        return Convert.ChangeType(result, argument.GetType());
    }

    private static object? Widened(object[] arguments, Func<IComparable, IComparable, IComparable> pick)
    {
        Type target = EvaluatedSyntax.GetWiderNumericType(arguments[0].GetType(), arguments[1].GetType());
        return pick(
            (IComparable)Convert.ChangeType(arguments[0], target),
            (IComparable)Convert.ChangeType(arguments[1], target));
    }

    private static object? Clamp(object[] arguments)
    {
        Type target = arguments.Select(a => a.GetType()).Aggregate(EvaluatedSyntax.GetWiderNumericType);
        IComparable value = (IComparable)Convert.ChangeType(arguments[0], target);
        IComparable min = (IComparable)Convert.ChangeType(arguments[1], target);
        IComparable max = (IComparable)Convert.ChangeType(arguments[2], target);

        if (min.CompareTo(max) > 0)
        {
            // Math.Clamp throws for this; a reference should just fail to evaluate.
            return null;
        }

        return value.CompareTo(min) < 0 ? min : value.CompareTo(max) > 0 ? max : value;
    }
}
