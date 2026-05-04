using Gum.Forms;
using Gum.Forms.Controls;
using RenderingLibrary;
using Shouldly;

namespace RaylibGum.Tests.Forms;

/// <summary>
/// Regression coverage for Raylib-side TextBox bugs. Specifically guards against the NRE
/// in <c>TextBoxBase.UpdateSelectionStartEnds</c> that fires when a multi-line / wrapped
/// TextBox tries to render its selection but <c>selectionTemplate</c> was never assigned
/// because the underlying renderable did not implement <see cref="System.ICloneable"/>.
///
/// Uses V3 default visuals because (a) V3's <c>SelectionInstance</c> is a NineSlice — the
/// renderable that exposed the missing <c>ICloneable</c> contract — and (b) raylib's V2
/// default templates don't register TextBox at all (gated <c>#if XNALIKE || FRB</c>),
/// so <c>new TextBox()</c> would otherwise produce a null Visual.
/// </summary>
public class TextBoxTests : BaseTestClass
{
    public TextBoxTests()
    {
        // Layered on top of TestAssemblyInitialize's V2 setup. TryAdd-style behavior in
        // InitializeDefaults means V2 registrations stick; only TextBox (unregistered in
        // V2 for raylib) and V3-specific Styling.ActiveStyle get added.
        FormsUtilities.InitializeDefaults(SystemManagers.Default, DefaultVisualsVersion.V3);
    }

    public override void Dispose()
    {
        FrameworkElement.DefaultFormsTemplates.Remove(typeof(TextBox));
        base.Dispose();
        TestAssemblyInitialize.ApplyDefaultTestState();
    }

    [Fact]
    public void SelectAll_DoesNotThrow_WhenAcceptsReturnIsTrue()
    {
        var textBox = new TextBox();
        textBox.Visual.ShouldNotBeNull();
        textBox.AcceptsReturn = true;
        textBox.Text = "line one\nline two";

        Should.NotThrow(() => textBox.SelectAll());
    }

    [Fact]
    public void SelectAll_DoesNotThrow_WhenTextWrappingIsWrap()
    {
        var textBox = new TextBox();
        textBox.Visual.ShouldNotBeNull();
        textBox.TextWrapping = TextWrapping.Wrap;
        textBox.Text = "some text that may wrap depending on width";

        Should.NotThrow(() => textBox.SelectAll());
    }
}
