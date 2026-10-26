using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

namespace Gum.Analyzers.Tests;

/// <summary>
/// The generator records which generated Gum element classes the game's own code uses, as
/// <c>[assembly: GumElementRoot("...")]</c>, so <c>gumcli pack --shake --roots-assembly</c> keeps them.
/// </summary>
public class GumElementRootGeneratorTests
{
    private const string GumTypes = @"
namespace Gum.DataTypes
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class GumElementAttribute : System.Attribute { public GumElementAttribute(string elementName) { } }
    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class GumElementRootAttribute : System.Attribute { public GumElementRootAttribute(string elementName) { } }
}
namespace Gum.Managers
{
    public class ObjectFinder
    {
        public static ObjectFinder Self { get; } = new ObjectFinder();
        public object GetElementSave(string name) => null;
        public object GetRequiredElementSave(string name) => null;
    }
}";

    // Generated code references every element class (registration, child instances), so it must not count.
    private const string GeneratedCode = @"
[Gum.DataTypes.GumElement(""Cards/CardGum"")] partial class CardGum { }
[Gum.DataTypes.GumElement(""Overlay"")] partial class Overlay { }
[Gum.DataTypes.GumElement(""Hud"")] partial class Hud { }
[Gum.DataTypes.GumElement(""Unused"")] partial class Unused { static void Register() { new Unused(); typeof(CardGum).ToString(); } }
class Holder<T> { }";

    private const string GameCode = @"
class Game
{
    void Start(object o, string dynamicName)
    {
        var card = new CardGum();
        var isOverlay = o is Overlay;
        var holder = new Holder<Hud>();
        Gum.Managers.ObjectFinder.Self.GetRequiredElementSave(""Screens/Title"");
        Gum.Managers.ObjectFinder.Self.GetElementSave(dynamicName);
    }
}";

    [Fact]
    public void Generator_roots_elements_the_game_code_uses_and_warns_on_a_dynamic_name()
    {
        (string generated, ImmutableArray<Diagnostic> diagnostics) = Run(
            (GumTypes, "GumTypes.cs"), (GeneratedCode, "Components/Cards/CardGum.Generated.cs"), (GameCode, "Game.cs"));

        generated.ShouldContain("[assembly: global::Gum.DataTypes.GumElementRoot(\"Cards/CardGum\")]");
        generated.ShouldContain("[assembly: global::Gum.DataTypes.GumElementRoot(\"Overlay\")]");
        generated.ShouldContain("[assembly: global::Gum.DataTypes.GumElementRoot(\"Hud\")]");
        generated.ShouldContain("[assembly: global::Gum.DataTypes.GumElementRoot(\"Screens/Title\")]");
        generated.ShouldNotContain("\"Unused\"");
        Diagnostic warning = diagnostics.ShouldHaveSingleItem();
        warning.Id.ShouldBe("GUM004");
        warning.Severity.ShouldBe(DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Generator_emits_nothing_when_the_runtime_has_no_GumElementRoot_attribute()
    {
        (string generated, ImmutableArray<Diagnostic> diagnostics) = Run(
            ("namespace Gum.DataTypes { public sealed class GumElementAttribute : System.Attribute { public GumElementAttribute(string n) { } } }", "Old.cs"),
            ("[Gum.DataTypes.GumElement(\"CardGum\")] partial class CardGum { } class Game { object c = new CardGum(); }", "Game.cs"));

        generated.ShouldBeEmpty();
        diagnostics.ShouldBeEmpty();
    }

    private static (string generated, ImmutableArray<Diagnostic> diagnostics) Run(params (string source, string path)[] files)
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Game",
            files.Select(file => CSharpSyntaxTree.ParseText(file.source, path: file.path)),
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new GumElementRootGenerator());
        driver = driver.RunGenerators(compilation);
        GeneratorRunResult result = driver.GetRunResult().Results.Single();
        string generated = string.Concat(result.GeneratedSources.Select(source => source.SourceText.ToString()));
        return (generated, result.Diagnostics);
    }
}
