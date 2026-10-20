#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Gum.Tests.Shared;

/// <summary>
/// Checks that every back-compat shim in a legacy namespace records its earliest-removal dates in
/// its <see cref="ObsoleteAttribute"/> message, so a new shim cannot skip the retention policy.
/// Linked into each test project that references a runtime assembly holding shims.
/// </summary>
internal static class ShimRetention
{
    /// <summary>Text every shim message must contain.</summary>
    public static readonly string[] RequiredMessageText = { "May 2027", "November 2027" };

    // Namespaces that hold only [Obsolete] shims of types that moved to Gum.*.
    private static readonly string[] LegacyNamespacePrefixes =
    {
        "MonoGameGum",
        "MonoGameAndGum",
        "SkiaGum.GueDeriving",
        "RaylibGum",
    };

    /// <summary>
    /// The one shim that is never removed: Gum.GumService.Default returns it. Its message says so
    /// instead of carrying removal dates.
    /// </summary>
    private const string PermanentShimName = "GumService";

    /// <summary>
    /// Returns one line per shim in <paramref name="assembly"/> that is not obsolete or whose
    /// message lacks the retention dates. Empty when every shim complies.
    /// </summary>
    public static List<string> FindViolations(Assembly assembly)
    {
        List<string> violations = new List<string>();

        foreach (Type type in assembly.GetExportedTypes())
        {
            if (!IsInLegacyNamespace(type) || type.IsNested)
            {
                continue;
            }

            ObsoleteAttribute? typeObsolete = type.GetCustomAttribute<ObsoleteAttribute>(inherit: false);

            if (type.Name == PermanentShimName)
            {
                if (typeObsolete == null || !typeObsolete.Message!.Contains("permanent"))
                {
                    violations.Add($"{type.FullName}: the permanent shim must be [Obsolete] and say it is permanent");
                }
                continue;
            }

            if (typeObsolete != null)
            {
                CheckMessage(type.FullName!, typeObsolete, violations);
                continue;
            }

            // A static forwarder class that is not itself obsolete (extension-method hosts) must
            // have every public method obsolete.
            if (type.IsAbstract && type.IsSealed)
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    ObsoleteAttribute? methodObsolete = method.GetCustomAttribute<ObsoleteAttribute>(inherit: false);
                    if (methodObsolete == null)
                    {
                        violations.Add($"{type.FullName}.{method.Name}: not [Obsolete]");
                    }
                    else
                    {
                        CheckMessage($"{type.FullName}.{method.Name}", methodObsolete, violations);
                    }
                }
                continue;
            }

            violations.Add($"{type.FullName}: public type in a legacy namespace is not [Obsolete]");
        }

        return violations;
    }

    private static bool IsInLegacyNamespace(Type type)
    {
        string? ns = type.Namespace;
        return ns != null && LegacyNamespacePrefixes.Any(prefix => ns == prefix || ns.StartsWith(prefix + "."));
    }

    private static void CheckMessage(string name, ObsoleteAttribute attribute, List<string> violations)
    {
        string message = attribute.Message ?? "";
        foreach (string required in RequiredMessageText)
        {
            if (!message.Contains(required))
            {
                violations.Add($"{name}: [Obsolete] message lacks \"{required}\"");
            }
        }
    }
}
