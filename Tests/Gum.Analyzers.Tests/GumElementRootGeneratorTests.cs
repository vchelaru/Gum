using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
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
    public sealed class GumElementAttribute : System.Attribute { public GumElementAttribute(string elementName) { } public System.Type FormsType { get; set; } }
    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class GumElementRootAttribute : System.Attribute { public GumElementRootAttribute(string elementName) { } }
}
namespace Gum.Forms.Controls
{
    public class FrameworkElement { }
    public class Button : FrameworkElement { }
    public class CheckBox : FrameworkElement { }
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
    public void Generator_roots_elements_the_game_code_uses_and_warns_on_a_dynamic_name_when_the_game_shakes()
    {
        (string generated, ImmutableArray<Diagnostic> diagnostics) = Run(gumShake: "true",
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

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    public void Generator_does_not_warn_on_a_dynamic_name_when_the_game_does_not_shake(string? gumShake)
    {
        (string generated, ImmutableArray<Diagnostic> diagnostics) = Run(gumShake,
            (GumTypes, "GumTypes.cs"), (GeneratedCode, "Components/Cards/CardGum.Generated.cs"), (GameCode, "Game.cs"));

        generated.ShouldContain("[assembly: global::Gum.DataTypes.GumElementRoot(\"Cards/CardGum\")]");
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void Generator_roots_the_component_registered_for_a_Forms_type_the_game_code_creates()
    {
        const string generatedForms = @"
[Gum.DataTypes.GumElement(""Controls/ButtonStandard"", FormsType = typeof(Gum.Forms.Controls.Button))]
partial class ButtonStandard : Gum.Forms.Controls.Button { }
[Gum.DataTypes.GumElement(""Controls/CheckBox"", FormsType = typeof(Gum.Forms.Controls.CheckBox))]
partial class CheckBoxRuntime : Gum.Forms.Controls.CheckBox { }";
        const string gameCode = "class Game { object button = new Gum.Forms.Controls.Button(); }";

        (string generated, ImmutableArray<Diagnostic> _) = Run(gumShake: null,
            (GumTypes, "GumTypes.cs"), (generatedForms, "Components/Controls/Forms.Generated.cs"), (gameCode, "Game.cs"));

        generated.ShouldContain("[assembly: global::Gum.DataTypes.GumElementRoot(\"Controls/ButtonStandard\")]");
        generated.ShouldNotContain("Controls/CheckBox");
    }

    [Fact]
    public void Generator_emits_nothing_when_the_runtime_has_no_GumElementRoot_attribute()
    {
        (string generated, ImmutableArray<Diagnostic> diagnostics) = Run(gumShake: "true",
            ("namespace Gum.DataTypes { public sealed class GumElementAttribute : System.Attribute { public GumElementAttribute(string n) { } } }", "Old.cs"),
            ("[Gum.DataTypes.GumElement(\"CardGum\")] partial class CardGum { } class Game { object c = new CardGum(); }", "Game.cs"));

        generated.ShouldBeEmpty();
        diagnostics.ShouldBeEmpty();
    }

    private static (string generated, ImmutableArray<Diagnostic> diagnostics) Run(string? gumShake, params (string source, string path)[] files)
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Game",
            files.Select(file => CSharpSyntaxTree.ParseText(file.source, path: file.path)),
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Dictionary<string, string> globalOptions = new Dictionary<string, string>();
        if (gumShake != null)
        {
            globalOptions["build_property.GumShake"] = gumShake;
        }
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { new GumElementRootGenerator().AsSourceGenerator() },
            optionsProvider: new GlobalOptionsProvider(globalOptions));
        driver = driver.RunGenerators(compilation);
        GeneratorRunResult result = driver.GetRunResult().Results.Single();
        string generated = string.Concat(result.GeneratedSources.Select(source => source.SourceText.ToString()));
        return (generated, result.Diagnostics);
    }

    private sealed class GlobalOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _global;

        public GlobalOptionsProvider(Dictionary<string, string> values) => _global = new Options(values);

        public override AnalyzerConfigOptions GlobalOptions => _global;
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Options.Empty;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => Options.Empty;

        private sealed class Options : AnalyzerConfigOptions
        {
            public static readonly Options Empty = new Options(new Dictionary<string, string>());
            private readonly Dictionary<string, string> _values;

            public Options(Dictionary<string, string> values) => _values = values;

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) =>
                _values.TryGetValue(key, out value);
        }
    }
}
