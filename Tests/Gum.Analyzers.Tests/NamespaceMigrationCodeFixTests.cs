using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using CodeFixVerifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    Gum.Analyzers.NamespaceMigrationAnalyzer,
    Gum.Analyzers.NamespaceMigrationCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Gum.Analyzers.Tests;

/// <summary>
/// Verifies that <c>GUM001</c> fires on <c>using</c> directives that import old runtime namespaces
/// (<c>MonoGameGum.GueDeriving</c>, <c>SkiaGum.GueDeriving</c>) and that the code fix rewrites them
/// to the unified <c>Gum.GueDeriving</c> namespace.
/// </summary>
public class NamespaceMigrationCodeFixTests
{
    [Fact]
    public async Task MappingTable_ContainsRuntimeMigrations()
    {
        // Smoke test: at least one migration from each old namespace should be present
        // after Phase 2 lands.
        Assert.False(NamespaceMigrationMapping.Migrations.IsEmpty,
            "Migration table should contain runtime namespace migrations after Phase 2.");

        Assert.True(NamespaceMigrationMapping.ByOldNamespace.ContainsKey("MonoGameGum.GueDeriving"),
            "MonoGameGum.GueDeriving should be a registered old namespace.");
        Assert.True(NamespaceMigrationMapping.ByOldNamespace.ContainsKey("SkiaGum.GueDeriving"),
            "SkiaGum.GueDeriving should be a registered old namespace.");
    }

    [Fact]
    public async Task UsingMonoGameGumGueDeriving_RaisesGum001()
    {
        string testCode = @"
{|GUM001:using MonoGameGum.GueDeriving;|}

namespace TestProject
{
    class MyClass { }
}

namespace MonoGameGum.GueDeriving
{
    public class SpriteRuntime { }
}
";

        await CodeFixVerifier.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task UsingSkiaGumGueDeriving_RaisesGum001()
    {
        string testCode = @"
{|GUM001:using SkiaGum.GueDeriving;|}

namespace TestProject
{
    class MyClass { }
}

namespace SkiaGum.GueDeriving
{
    public class ArcRuntime { }
}
";

        await CodeFixVerifier.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task CodeFix_RewritesMonoGameGumGueDerivingUsing()
    {
        string testCode = @"
{|GUM001:using MonoGameGum.GueDeriving;|}

namespace TestProject
{
    class MyClass { }
}

namespace MonoGameGum.GueDeriving
{
    public class SpriteRuntime { }
}

namespace Gum.GueDeriving
{
    public class SpriteRuntime { }
}
";

        string fixedCode = @"
using Gum.GueDeriving;

namespace TestProject
{
    class MyClass { }
}

namespace MonoGameGum.GueDeriving
{
    public class SpriteRuntime { }
}

namespace Gum.GueDeriving
{
    public class SpriteRuntime { }
}
";

        await CodeFixVerifier.VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task UsingMonoGameGumInput_RaisesGum001()
    {
        string testCode = @"
{|GUM001:using MonoGameGum.Input;|}

namespace TestProject
{
    class MyClass { }
}

namespace MonoGameGum.Input
{
    public class Cursor { }
}
";

        await CodeFixVerifier.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task CodeFix_RewritesMonoGameGumInputUsing()
    {
        string testCode = @"
{|GUM001:using MonoGameGum.Input;|}

namespace TestProject
{
    class MyClass { }
}

namespace MonoGameGum.Input
{
    public class Cursor { }
}

namespace Gum.Input
{
    public class Keyboard { }
}
";

        string fixedCode = @"
using Gum.Input;

namespace TestProject
{
    class MyClass { }
}

namespace MonoGameGum.Input
{
    public class Cursor { }
}

namespace Gum.Input
{
    public class Keyboard { }
}
";

        await CodeFixVerifier.VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task CodeFix_RemovesMonoGameGumInputUsing_WhenGumInputAlreadyImported()
    {
        // The earlier GUM001 fix left both usings in place; with Cursor now in Gum.Input the
        // stale one makes `Cursor` ambiguous, so it is flagged and removed.
        string testCode = @"
{|GUM001:using MonoGameGum.Input;|}
using Gum.Input;

namespace TestProject
{
    class MyClass { }
}

namespace MonoGameGum.Input
{
    public class Cursor { }
}

namespace Gum.Input
{
    public class Keyboard { }
}
";

        string fixedCode = @"using Gum.Input;

namespace TestProject
{
    class MyClass { }
}

namespace MonoGameGum.Input
{
    public class Cursor { }
}

namespace Gum.Input
{
    public class Keyboard { }
}
";

        await CodeFixVerifier.VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Theory]
    [InlineData("Cursor")]
    [InlineData("CursorExtensions")]
    public void MappingTable_MapsCursorTypesToGumInput(string typeName)
    {
        Assert.Contains(
            NamespaceMigrationMapping.ByOldNamespace["MonoGameGum.Input"],
            m => m.TypeName == typeName && m.NewNamespace == "Gum.Input");
    }

    [Fact]
    public async Task CodeFix_RewritesSkiaGumGueDerivingUsing()
    {
        string testCode = @"
{|GUM001:using SkiaGum.GueDeriving;|}

namespace TestProject
{
    class MyClass { }
}

namespace SkiaGum.GueDeriving
{
    public class ArcRuntime { }
}

namespace Gum.GueDeriving
{
    public class ArcRuntime { }
}
";

        string fixedCode = @"
using Gum.GueDeriving;

namespace TestProject
{
    class MyClass { }
}

namespace SkiaGum.GueDeriving
{
    public class ArcRuntime { }
}

namespace Gum.GueDeriving
{
    public class ArcRuntime { }
}
";

        await CodeFixVerifier.VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Theory]
    [InlineData("MonoGameGum", "Gum", "GumService")]
    [InlineData("MonoGameGum", "Gum", "GraphicalUiElementExtensionMethods")]
    [InlineData("MonoGameGum", "Gum", "ElementSaveExtensionMethods")]
    [InlineData("MonoGameGum.Renderables", "Gum.Renderables", "RenderableCreator")]
    [InlineData("MonoGameGum.Renderables", "Gum.Renderables", "DefaultFilledRectangleRenderable")]
    [InlineData("MonoGameGum.ExtensionMethods", "Gum.ExtensionMethods", "IReadOnlyListExtensionMethods")]
    [InlineData("MonoGameAndGum.Renderables", "Gum.Renderables", "ShapeRenderer")]
    [InlineData("MonoGameAndGum.Renderables", "Gum.Renderables", "RenderableShapeBase")]
    [InlineData("MonoGameAndGum.Renderables", "Gum.Renderables", "Circle")]
    [InlineData("MonoGameAndGum.Renderables", "Gum.Renderables", "RoundedRectangle")]
    [InlineData("MonoGameAndGum.Content", "Gum.Content", "ShapeSvgLoader")]
    public void MappingTable_MapsRemainingLegacyNamespaces(string oldNamespace, string newNamespace, string typeName)
    {
        Assert.True(NamespaceMigrationMapping.ByOldNamespace.TryGetValue(oldNamespace, out var migrations));
        Assert.Contains(migrations, m => m.TypeName == typeName && m.NewNamespace == newNamespace);
    }

    [Fact]
    public async Task UsingMonoGameAndGumRenderables_RaisesGum001()
    {
        string testCode = @"
{|GUM001:using MonoGameAndGum.Renderables;|}

namespace TestProject
{
    class MyClass { }
}

namespace MonoGameAndGum.Renderables
{
    public class ShapeRenderer { }
}
";

        await CodeFixVerifier.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task CodeFix_RewritesMonoGameAndGumRenderablesUsing()
    {
        string testCode = @"
{|GUM001:using MonoGameAndGum.Renderables;|}

namespace TestProject
{
    class MyClass { }
}

namespace MonoGameAndGum.Renderables
{
    public class ShapeRenderer { }
}

namespace Gum.Renderables
{
    public class ShapeRenderer { }
}
";

        string fixedCode = @"
using Gum.Renderables;

namespace TestProject
{
    class MyClass { }
}

namespace MonoGameAndGum.Renderables
{
    public class ShapeRenderer { }
}

namespace Gum.Renderables
{
    public class ShapeRenderer { }
}
";

        await CodeFixVerifier.VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task UsingMonoGameGum_RaisesGum001()
    {
        // The GumService shim in this namespace is permanent, but the namespace itself is still
        // the legacy home: `using Gum;` resolves GumService to the real type, and
        // GumService.Default still returns the same singleton.
        string testCode = @"
{|GUM001:using MonoGameGum;|}

namespace TestProject
{
    class MyClass { }
}

namespace MonoGameGum
{
    public class GumService { }
}
";

        await CodeFixVerifier.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task CodeFix_RewritesMonoGameGumUsing()
    {
        string testCode = @"
{|GUM001:using MonoGameGum;|}

namespace TestProject
{
    class MyClass { }
}

namespace MonoGameGum
{
    public class GumService { }
}

namespace Gum
{
    public class GumService { }
}
";

        string fixedCode = @"
using Gum;

namespace TestProject
{
    class MyClass { }
}

namespace MonoGameGum
{
    public class GumService { }
}

namespace Gum
{
    public class GumService { }
}
";

        await CodeFixVerifier.VerifyCodeFixAsync(testCode, fixedCode);
    }
}
