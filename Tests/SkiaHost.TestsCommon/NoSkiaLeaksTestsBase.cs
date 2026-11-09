using System.Reflection;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace SkiaHostTests;

/// <summary>
/// An interactive Skia host's public <c>GumService</c> exposes no SkiaSharp type: a game only calls
/// <c>GumUI.Initialize(...)</c>, <c>Update</c>, <c>Draw()</c> and the <c>Use...</c> methods, and the canvas
/// plumbing stays internal to the host (#7788). A host's test project derives from this class and supplies
/// nothing, except a host that deliberately keeps a member public lists it in <see cref="AllowedLeaks"/>.
/// Not covered: protected members (a game does not see them unless it subclasses the service), and
/// public types other than the service itself and its nested types.
/// </summary>
public abstract class NoSkiaLeaksTestsBase
{
    /// <summary>
    /// Members this host keeps public on purpose, as <c>DeclaringType.Member</c> for a property or event or
    /// <c>DeclaringType.Method(ParameterType,...)</c> for a method. Each entry needs a reason in the
    /// derived class; the list shrinks as the members are replaced.
    /// </summary>
    protected virtual IReadOnlyCollection<string> AllowedLeaks => Array.Empty<string>();

    [Fact]
    public void PublicSurface_ExposesNoSkiaType()
    {
        Type serviceType = IGumService.Default!.GetType();

        List<string> leaks = new List<string>();
        foreach (MemberInfo member in serviceType.GetMembers(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy))
        {
            if (AllowedLeaks.Contains(Signature(member)))
            {
                continue;
            }

            foreach (Type type in TypesExposedBy(member))
            {
                if (IsSkiaType(type))
                {
                    leaks.Add($"{member.DeclaringType!.Name}.{member.Name} exposes {type.FullName}");
                    break;
                }
            }
        }

        leaks.ShouldBeEmpty(string.Join(Environment.NewLine, leaks));
    }

    [Fact]
    public void NestedPublicTypes_ExposeNoSkiaType()
    {
        Type serviceType = IGumService.Default!.GetType();

        List<string> leaks = serviceType.GetNestedTypes(BindingFlags.Public)
            .Where(nested => TypesExposedBy(nested).Any(IsSkiaType))
            .Select(nested => nested.FullName!)
            .ToList();

        leaks.ShouldBeEmpty(string.Join(Environment.NewLine, leaks));
    }

    // "DeclaringType.Member" for a property or event (its accessors included), and
    // "DeclaringType.Method(ParameterType,...)" for a method, so an allowed entry exempts exactly one member.
    private static string Signature(MemberInfo member)
    {
        string owner = member.DeclaringType!.Name;
        if (member is MethodInfo method)
        {
            if (method.IsSpecialName)
            {
                foreach (string prefix in new[] { "add_", "remove_", "get_", "set_" })
                {
                    if (method.Name.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        return $"{owner}.{method.Name.Substring(prefix.Length)}";
                    }
                }
            }
            return $"{owner}.{method.Name}({string.Join(",", method.GetParameters().Select(p => p.ParameterType.Name))})";
        }
        return $"{owner}.{member.Name}";
    }

    private static bool IsSkiaType(Type type)
    {
        string name = type.Namespace ?? string.Empty;
        return name == "SkiaSharp" || name.StartsWith("SkiaSharp.", StringComparison.Ordinal)
            || name == "SkiaGum.Helpers" && (type.Name.StartsWith("SkiaTargetHolder") || type.Name == "ISkiaHostTarget");
    }

    private static IEnumerable<Type> TypesExposedBy(MemberInfo member)
    {
        IEnumerable<Type> declared = member switch
        {
            MethodBase method => method.GetParameters().Select(p => p.ParameterType)
                .Concat(method is MethodInfo info ? new[] { info.ReturnType } : Array.Empty<Type>()),
            PropertyInfo property => new[] { property.PropertyType }
                .Concat(property.GetIndexParameters().Select(p => p.ParameterType)),
            EventInfo evt => new[] { evt.EventHandlerType! },
            FieldInfo field => new[] { field.FieldType },
            Type nested => nested.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .SelectMany(TypesExposedBy),
            _ => Array.Empty<Type>(),
        };

        return declared.SelectMany(Unwrap);
    }

    // Element, by-ref and generic-argument types count: Action<SKCanvas> exposes SKCanvas.
    private static IEnumerable<Type> Unwrap(Type type)
    {
        yield return type;
        if (type.HasElementType)
        {
            foreach (Type inner in Unwrap(type.GetElementType()!))
            {
                yield return inner;
            }
        }
        if (type.IsGenericType)
        {
            foreach (Type argument in type.GetGenericArguments().SelectMany(Unwrap))
            {
                yield return argument;
            }
        }
    }
}
