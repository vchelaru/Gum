using Gum.GueDeriving;
using MonoGameGum.Tests;
using Shouldly;
using Xunit;

// IMPORTANT: this test lives in the Gum.Wireframe.Tests namespace (NOT MonoGameGum.Tests.*)
// and intentionally does NOT import `Gum.Forms.Controls` or `MonoGameGum`. That restriction is
// the whole point of issue #3226: it proves `gue.AddChild(formsControl)` / `gue.RemoveChild(...)`
// resolve through the new INSTANCE methods on GraphicalUiElement alone — with neither the
// `Gum.Forms.Controls.FrameworkElementExt.AddChild` extension nor the legacy `MonoGameGum`
// (GumServiceCompat) shim in scope. The Button below is therefore referenced fully-qualified.
//
// Do NOT "tidy" this into the MonoGameGum.Tests.Forms namespace or add `using Gum.Forms.Controls;`:
// the MonoGameGum namespace is an *enclosing* namespace of MonoGameGum.Tests.*, so the compat
// AddChild extension would silently come back into scope and these tests would pass even if the
// instance methods were deleted — defeating the regression they exist to catch.
namespace Gum.Wireframe.Tests;

public class GraphicalUiElementFormsInstanceTests : BaseTestClass
{
    [Fact]
    public void AddChild_ShouldParentFormsControlVisualUnderVisual()
    {
        ContainerRuntime parent = new ContainerRuntime();
        Gum.Forms.Controls.Button child = new Gum.Forms.Controls.Button();

        parent.AddChild(child);

        parent.Children.ShouldContain(child.Visual);
    }

    [Fact]
    public void RemoveChild_ShouldUnparentFormsControlVisualFromVisual()
    {
        ContainerRuntime parent = new ContainerRuntime();
        Gum.Forms.Controls.Button child = new Gum.Forms.Controls.Button();
        parent.AddChild(child);

        parent.RemoveChild(child);

        parent.Children.ShouldNotContain(child.Visual);
    }
}
