using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using AnalyzerVerifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Gum.Analyzers.ObsoleteShapeRuntimeAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Gum.Analyzers.Tests;

/// <summary>
/// Verifies that <c>GUM002</c> fires on references to the obsolete shape-runtime types
/// (<c>ColoredCircleRuntime</c>, <c>ColoredRectangleRuntime</c>, <c>RoundedRectangleRuntime</c>,
/// <c>SolidRectangleRuntime</c>) when they live in one of the eligible Gum runtime namespaces.
/// </summary>
public class ObsoleteShapeRuntimeAnalyzerTests
{
    [Fact]
    public async Task ColoredCircleRuntime_InGumGueDeriving_RaisesGum002()
    {
        string testCode = @"
using Gum.GueDeriving;

namespace TestProject
{
    class MyClass
    {
        void M()
        {
            var x = new {|GUM002:ColoredCircleRuntime|}();
        }
    }
}

namespace Gum.GueDeriving
{
    public class ColoredCircleRuntime { }
    public class CircleRuntime { }
}
";

        await AnalyzerVerifier.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task ColoredRectangleRuntime_InMonoGameGumGueDeriving_RaisesGum002()
    {
        // Covers shim references — MonoGameGum.GueDeriving.ColoredRectangleRuntime is the
        // compatibility shim that derives from the Gum.GueDeriving real obsolete type.
        string testCode = @"
using MonoGameGum.GueDeriving;

namespace TestProject
{
    class MyClass
    {
        void M()
        {
            var x = new {|GUM002:ColoredRectangleRuntime|}();
        }
    }
}

namespace MonoGameGum.GueDeriving
{
    public class ColoredRectangleRuntime { }
    public class RectangleRuntime { }
}
";

        await AnalyzerVerifier.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task RoundedRectangleRuntime_InSkiaGumGueDeriving_RaisesGum002()
    {
        string testCode = @"
using SkiaGum.GueDeriving;

namespace TestProject
{
    class MyClass
    {
        void M()
        {
            var x = new {|GUM002:RoundedRectangleRuntime|}();
        }
    }
}

namespace SkiaGum.GueDeriving
{
    public class RoundedRectangleRuntime { }
    public class RectangleRuntime { }
}
";

        await AnalyzerVerifier.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task SolidRectangleRuntime_InGumGueDeriving_RaisesGum002()
    {
        string testCode = @"
using Gum.GueDeriving;

namespace TestProject
{
    class MyClass
    {
        void M()
        {
            var x = new {|GUM002:SolidRectangleRuntime|}();
        }
    }
}

namespace Gum.GueDeriving
{
    public class SolidRectangleRuntime { }
    public class RectangleRuntime { }
}
";

        await AnalyzerVerifier.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task TypeWithSameName_InUnrelatedNamespace_NoDiagnostic()
    {
        // A user-defined class that happens to share a name with an obsolete runtime must NOT
        // fire GUM002 — the analyzer is scoped to the Gum runtime namespaces.
        string testCode = @"
using Unrelated.Things;

namespace TestProject
{
    class MyClass
    {
        void M()
        {
            var x = new ColoredCircleRuntime();
        }
    }
}

namespace Unrelated.Things
{
    public class ColoredCircleRuntime { }
}
";

        await AnalyzerVerifier.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task UnrelatedTypeName_NoDiagnostic()
    {
        string testCode = @"
namespace TestProject
{
    class MyClass { }
}
";

        await AnalyzerVerifier.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task MappingTable_CoversAllFourLegacyTypes()
    {
        Assert.True(ObsoleteShapeRuntimeMapping.ByOldTypeName.ContainsKey("ColoredCircleRuntime"));
        Assert.True(ObsoleteShapeRuntimeMapping.ByOldTypeName.ContainsKey("ColoredRectangleRuntime"));
        Assert.True(ObsoleteShapeRuntimeMapping.ByOldTypeName.ContainsKey("RoundedRectangleRuntime"));
        Assert.True(ObsoleteShapeRuntimeMapping.ByOldTypeName.ContainsKey("SolidRectangleRuntime"));

        // ColoredCircleRuntime.Color historically painted the outline — must map to StrokeColor,
        // not FillColor, or migration silently flips outlined rings into solid disks.
        var coloredCircle = ObsoleteShapeRuntimeMapping.ByOldTypeName["ColoredCircleRuntime"];
        Assert.Equal("CircleRuntime", coloredCircle.NewTypeName);
        Assert.Equal("Color", coloredCircle.OldPropertyName);
        Assert.Equal("StrokeColor", coloredCircle.NewPropertyName);

        var coloredRect = ObsoleteShapeRuntimeMapping.ByOldTypeName["ColoredRectangleRuntime"];
        Assert.Equal("RectangleRuntime", coloredRect.NewTypeName);
        Assert.Equal("FillColor", coloredRect.NewPropertyName);
    }
}
