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
    public async Task CodeFix_AddsGumInputUsing_KeepsMonoGameGumInput_ForPartialMove()
    {
        // MonoGameGum.Input is a PARTIAL move (Cursor stays, Keyboard moved to Gum.Input),
        // so the fix must ADD `using Gum.Input;` while keeping `using MonoGameGum.Input;`.
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
using MonoGameGum.Input;
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
}
