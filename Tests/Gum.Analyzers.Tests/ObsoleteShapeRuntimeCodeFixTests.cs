using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using CodeFixVerifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    Gum.Analyzers.ObsoleteShapeRuntimeAnalyzer,
    Gum.Analyzers.ObsoleteShapeRuntimeCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Gum.Analyzers.Tests;

/// <summary>
/// Verifies that the GUM002 code fix rewrites obsolete shape-runtime types and renames
/// <c>Color</c> accesses on rewritten instances to <c>FillColor</c> / <c>StrokeColor</c>.
/// </summary>
public class ObsoleteShapeRuntimeCodeFixTests
{
    [Fact]
    public async Task ColoredCircleRuntime_RewritesTypeAndColorToStrokeColor()
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
            x.Color = 0;
        }
    }
}

namespace Gum.GueDeriving
{
    public class ColoredCircleRuntime
    {
        public int Color { get; set; }
    }
    public class CircleRuntime
    {
        public int StrokeColor { get; set; }
        public int FillColor { get; set; }
    }
}
";

        string fixedCode = @"
using Gum.GueDeriving;

namespace TestProject
{
    class MyClass
    {
        void M()
        {
            var x = new CircleRuntime();
            x.StrokeColor = 0;
        }
    }
}

namespace Gum.GueDeriving
{
    public class ColoredCircleRuntime
    {
        public int Color { get; set; }
    }
    public class CircleRuntime
    {
        public int StrokeColor { get; set; }
        public int FillColor { get; set; }
    }
}
";

        await CodeFixVerifier.VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task ColoredRectangleRuntime_RewritesTypeAndColorToFillColor()
    {
        string testCode = @"
using Gum.GueDeriving;

namespace TestProject
{
    class MyClass
    {
        void M()
        {
            var x = new {|GUM002:ColoredRectangleRuntime|}();
            x.Color = 0;
        }
    }
}

namespace Gum.GueDeriving
{
    public class ColoredRectangleRuntime
    {
        public int Color { get; set; }
    }
    public class RectangleRuntime
    {
        public int FillColor { get; set; }
    }
}
";

        string fixedCode = @"
using Gum.GueDeriving;

namespace TestProject
{
    class MyClass
    {
        void M()
        {
            var x = new RectangleRuntime();
            x.FillColor = 0;
        }
    }
}

namespace Gum.GueDeriving
{
    public class ColoredRectangleRuntime
    {
        public int Color { get; set; }
    }
    public class RectangleRuntime
    {
        public int FillColor { get; set; }
    }
}
";

        await CodeFixVerifier.VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task RoundedRectangleRuntime_RewritesTypePreservesCornerRadius()
    {
        string testCode = @"
using Gum.GueDeriving;

namespace TestProject
{
    class MyClass
    {
        void M()
        {
            var x = new {|GUM002:RoundedRectangleRuntime|}();
            x.CornerRadius = 5f;
        }
    }
}

namespace Gum.GueDeriving
{
    public class RoundedRectangleRuntime
    {
        public float CornerRadius { get; set; }
    }
    public class RectangleRuntime
    {
        public float CornerRadius { get; set; }
    }
}
";

        string fixedCode = @"
using Gum.GueDeriving;

namespace TestProject
{
    class MyClass
    {
        void M()
        {
            var x = new RectangleRuntime();
            x.CornerRadius = 5f;
        }
    }
}

namespace Gum.GueDeriving
{
    public class RoundedRectangleRuntime
    {
        public float CornerRadius { get; set; }
    }
    public class RectangleRuntime
    {
        public float CornerRadius { get; set; }
    }
}
";

        await CodeFixVerifier.VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task SolidRectangleRuntime_RewritesTypeAndColorToFillColor()
    {
        string testCode = @"
using SkiaGum.GueDeriving;

namespace TestProject
{
    class MyClass
    {
        void M()
        {
            var x = new {|GUM002:SolidRectangleRuntime|}();
            x.Color = 0;
        }
    }
}

namespace SkiaGum.GueDeriving
{
    public class SolidRectangleRuntime
    {
        public int Color { get; set; }
    }
    public class RectangleRuntime
    {
        public int FillColor { get; set; }
    }
}
";

        string fixedCode = @"
using SkiaGum.GueDeriving;

namespace TestProject
{
    class MyClass
    {
        void M()
        {
            var x = new RectangleRuntime();
            x.FillColor = 0;
        }
    }
}

namespace SkiaGum.GueDeriving
{
    public class SolidRectangleRuntime
    {
        public int Color { get; set; }
    }
    public class RectangleRuntime
    {
        public int FillColor { get; set; }
    }
}
";

        await CodeFixVerifier.VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Fact]
    public async Task ColorAccess_OnUnrelatedType_NotRewritten()
    {
        // Defensive: 'Color' is a common property name. The fix must not touch '.Color' accesses
        // on receivers that aren't one of the obsolete shape-runtime types.
        string testCode = @"
using Gum.GueDeriving;

namespace TestProject
{
    class Other { public int Color { get; set; } }

    class MyClass
    {
        void M()
        {
            var x = new {|GUM002:ColoredRectangleRuntime|}();
            x.Color = 0;
            var other = new Other();
            other.Color = 1;
        }
    }
}

namespace Gum.GueDeriving
{
    public class ColoredRectangleRuntime
    {
        public int Color { get; set; }
    }
    public class RectangleRuntime
    {
        public int FillColor { get; set; }
    }
}
";

        string fixedCode = @"
using Gum.GueDeriving;

namespace TestProject
{
    class Other { public int Color { get; set; } }

    class MyClass
    {
        void M()
        {
            var x = new RectangleRuntime();
            x.FillColor = 0;
            var other = new Other();
            other.Color = 1;
        }
    }
}

namespace Gum.GueDeriving
{
    public class ColoredRectangleRuntime
    {
        public int Color { get; set; }
    }
    public class RectangleRuntime
    {
        public int FillColor { get; set; }
    }
}
";

        await CodeFixVerifier.VerifyCodeFixAsync(testCode, fixedCode);
    }
}
