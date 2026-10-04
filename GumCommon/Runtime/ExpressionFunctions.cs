using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Expressions;

/// <summary>
/// The functions a variable reference expression may call, such as <c>Sin(Angle)</c> or
/// <c>Max(Width, 50)</c>. This is Gum's own small function set, not .NET's <see cref="Math"/>:
/// names are case sensitive with no <c>Math.</c> prefix, and angles are in degrees to match
/// Gum's <c>Rotation</c>. It lives in GumCommon, free of Roslyn, so both the evaluator in
/// <c>Gum.Expressions</c> and name-only scanners (the headless error checker) use this one table.
/// </summary>
public static class ExpressionFunctions
{
    private sealed class FunctionDefinition
    {
        public FunctionDefinition(int argumentCount, Func<object[], object?> invoke)
        {
            ArgumentCount = argumentCount;
            Invoke = invoke;
        }

        public int ArgumentCount { get; }
        public Func<object[], object?> Invoke { get; }
    }

    private const double DegreesToRadians = Math.PI / 180.0;

    private static readonly Dictionary<string, FunctionDefinition> Functions = new Dictionary<string, FunctionDefinition>
    {
        ["Sin"] = new FunctionDefinition(1, args => Trigonometric(args[0], Math.Sin)),
        ["Cos"] = new FunctionDefinition(1, args => Trigonometric(args[0], Math.Cos)),
        ["Tan"] = new FunctionDefinition(1, args => Trigonometric(args[0], Math.Tan)),
        ["Sqrt"] = new FunctionDefinition(1, args => KeepFloatness(args[0], Math.Sqrt(Convert.ToDouble(args[0])))),
        ["Abs"] = new FunctionDefinition(1, args => KeepType(args[0], Math.Abs)),
        ["Floor"] = new FunctionDefinition(1, args => KeepType(args[0], Math.Floor)),
        ["Ceiling"] = new FunctionDefinition(1, args => KeepType(args[0], Math.Ceiling)),
        ["Round"] = new FunctionDefinition(1, args => KeepType(args[0], d => Math.Round(d))),
        ["Min"] = new FunctionDefinition(2, args => Widened(args, (a, b) => a.CompareTo(b) <= 0 ? a : b)),
        ["Max"] = new FunctionDefinition(2, args => Widened(args, (a, b) => a.CompareTo(b) >= 0 ? a : b)),
        ["Clamp"] = new FunctionDefinition(3, Clamp),
    };

    /// <summary>The supported function names, sorted.</summary>
    public static IReadOnlyList<string> Names { get; } = Functions.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray();

    public static bool IsFunction(string name) => Functions.ContainsKey(name);

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
            if (result is float f && !float.IsFinite(f))
            {
                return null;
            }
            if (result is double d && !double.IsFinite(d))
            {
                return null;
            }
            return result;
        }
        catch (OverflowException)
        {
            // e.g. Abs(int.MinValue)
            return null;
        }
    }

    private static bool IsNumeric(object value) =>
        value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    // Sin/Cos/Tan take degrees. The result is rounded to 12 places so exact answers come out exact
    // (Sin(180) is 0, not 1.2e-16), which keeps comparisons like Sin(a) == 0 usable.
    private static object Trigonometric(object degrees, Func<double, double> function)
    {
        double result = Math.Round(function(Convert.ToDouble(degrees) * DegreesToRadians), 12);
        return KeepFloatness(degrees, result);
    }

    // A double input stays double; everything else (Gum variables are floats) yields a float.
    // Boxed branches: a bare `cond ? double : float` would promote the float to double.
    private static object KeepFloatness(object argument, double result) =>
        argument is double ? (object)result : (float)result;

    // Abs/Floor/Ceiling/Round: the result has the same type as the argument, so Abs of an int is an int.
    private static object KeepType(object argument, Func<double, double> function)
    {
        double result = function(Convert.ToDouble(argument));
        return Convert.ChangeType(result, argument.GetType());
    }

    private static Type GetWiderNumericType(Type type1, Type type2)
    {
        Type[] typeOrder =
        {
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal)
        };

        return typeOrder[Math.Max(Array.IndexOf(typeOrder, type1), Array.IndexOf(typeOrder, type2))];
    }

    private static object? Widened(object[] arguments, Func<IComparable, IComparable, IComparable> pick)
    {
        Type target = GetWiderNumericType(arguments[0].GetType(), arguments[1].GetType());
        return pick(
            (IComparable)Convert.ChangeType(arguments[0], target),
            (IComparable)Convert.ChangeType(arguments[1], target));
    }

    private static object? Clamp(object[] arguments)
    {
        Type target = arguments.Select(a => a.GetType()).Aggregate(GetWiderNumericType);
        IComparable value = (IComparable)Convert.ChangeType(arguments[0], target);
        IComparable min = (IComparable)Convert.ChangeType(arguments[1], target);
        IComparable max = (IComparable)Convert.ChangeType(arguments[2], target);

        if (min.CompareTo(max) > 0)
        {
            // A range with min above max has no valid answer, so the reference fails to evaluate.
            return null;
        }

        return value.CompareTo(min) < 0 ? min : value.CompareTo(max) > 0 ? max : value;
    }
}
