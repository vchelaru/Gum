using Gum.ProjectServices.CodeGeneration;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Gum.Cli.Tests;

/// <summary>
/// Code generation sees the Forms controls only through the placeholders in
/// FormsControlPlaceholders.cs. A behavior mapped to a Forms type with no placeholder, or a
/// placeholder missing a member the real control declares, makes an exposed variable generate
/// a property that hides the inherited one (CS0108, #7515).
/// </summary>
public class FormsControlPlaceholderTests
{
    // The names the Forms template components expose from their children.
    private static readonly string[] ExposableNames =
    {
        "Text", "Header", "Placeholder", "Password", "PasswordChar", "MaxLettersToShow", "MaxNumberOfLines",
    };

    [Fact]
    public void BehaviorGumFormsTypes_ExposableMemberOfRealControl_IsMirroredOnPlaceholder()
    {
        FieldInfo field = typeof(CodeGenerator).GetField("BehaviorGumFormsTypes",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        Dictionary<string, string> behaviorTypes = (Dictionary<string, string>)field.GetValue(null)!;
        Assembly formsAssembly = typeof(Gum.Forms.Controls.FrameworkElement).Assembly;
        Assembly placeholderAssembly = typeof(CodeGenerator).Assembly;
        List<string> gaps = new List<string>();

        foreach (string qualifiedName in behaviorTypes.Values.Distinct())
        {
            string typeName = qualifiedName.Substring("global::".Length);
            Type realType = formsAssembly.GetType(typeName)!;
            realType.ShouldNotBeNull(typeName);
            Type? placeholder = placeholderAssembly.GetType(typeName);

            foreach (string name in ExposableNames)
            {
                PropertyInfo? real = realType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(item => item.Name == name);
                if (real == null)
                {
                    continue;
                }

                PropertyInfo? mirrored = placeholder?.GetProperty(name);
                if (mirrored == null)
                {
                    gaps.Add($"{realType.Name}.{name} has no placeholder property");
                }
                else if (IsOverridable(mirrored) != IsOverridable(real))
                {
                    gaps.Add($"{realType.Name}.{name} virtual-ness differs from its placeholder");
                }
            }
        }

        gaps.ShouldBeEmpty();
    }

    private static bool IsOverridable(PropertyInfo property)
    {
        MethodInfo? setter = property.GetSetMethod();
        return setter?.IsVirtual == true && !setter.IsFinal;
    }
}
