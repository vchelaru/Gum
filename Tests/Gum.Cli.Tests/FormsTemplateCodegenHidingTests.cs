using Gum.ProjectServices;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Gum.Cli.Tests;

/// <summary>
/// Generated Forms code must not hide members the real Forms base class already declares
/// (CS0108/CS0114). Code generation only knows the base classes through the hand-written
/// placeholders in FormsControlPlaceholders.cs, so these tests compare the generated output
/// against the real MonoGameGum types.
/// </summary>
public class FormsTemplateCodegenHidingTests : IDisposable
{
    private static readonly Regex ClassRegex = new Regex(
        @"^partial class \w+ : global::(?<base>Gum\.Forms\.Controls\.\w+)", RegexOptions.Multiline);

    // A class-level member declaration: four-space indent, "public", optional modifiers, type, name.
    private static readonly Regex MemberRegex = new Regex(
        @"^    public (?<modifiers>(?:(?:static|override|new|virtual|const|readonly) )*)(?<type>[\w\.<>\?\[\],:]+) (?<name>\w+)\s*(\{|;|=|$)",
        RegexOptions.Multiline);

    private readonly string _tempDirectory;

    public FormsTemplateCodegenHidingTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCliHidingTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Codegen_FormsTemplate_DoesNotHideMembersOfRealFormsBaseClass()
    {
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        new FormsTemplateCreator().Create(gumxPath);
        File.WriteAllText(Path.Combine(_tempDirectory, "MyGame.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup>" +
            "<PackageReference Include=\"MonoGame.Framework.DesktopGL\" Version=\"3.8.4\" />" +
            "</ItemGroup></Project>");

        CliTestHelper.Run("codegen", gumxPath).ExitCode.ShouldBe(0);

        const BindingFlags inherited = BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        Assembly formsAssembly = typeof(Gum.Forms.Controls.FrameworkElement).Assembly;
        List<string> hidden = new List<string>();
        int checkedClasses = 0;

        foreach (string path in Directory.GetFiles(_tempDirectory, "*.Generated.cs", SearchOption.AllDirectories))
        {
            string code = File.ReadAllText(path);
            Match classMatch = ClassRegex.Match(code);
            Type? baseType = classMatch.Success ? formsAssembly.GetType(classMatch.Groups["base"].Value) : null;
            if (baseType == null)
            {
                continue;
            }
            checkedClasses++;

            foreach (Match member in MemberRegex.Matches(code))
            {
                string modifiers = member.Groups["modifiers"].Value;
                string name = member.Groups["name"].Value;
                if (modifiers.Contains("override") || modifiers.Contains("new"))
                {
                    continue;
                }
                if (Array.Exists(baseType.GetMember(name, inherited), IsVisibleToDerivedClass))
                {
                    hidden.Add($"{Path.GetFileName(path)}: {name} hides {baseType.Name}.{name}");
                }
            }
        }

        checkedClasses.ShouldBeGreaterThan(10);
        hidden.ShouldBeEmpty();
    }

    // Private and internal members of the Forms assembly are invisible to the generated class,
    // so declaring a same-named member there hides nothing.
    private static bool IsVisibleToDerivedClass(MemberInfo member)
    {
        return member switch
        {
            FieldInfo field => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly,
            MethodBase method => method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly,
            PropertyInfo property => Array.Exists(property.GetAccessors(true), IsVisibleToDerivedClass),
            _ => true
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
