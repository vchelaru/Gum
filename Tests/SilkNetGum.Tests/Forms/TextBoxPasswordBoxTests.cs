using Gum.Forms.Controls;
using Shouldly;

namespace SilkNetGum.Tests.Forms;

/// <summary>
/// End-to-end regression for issue #3653: <c>TextBoxBase.RefreshInternalVisualReferences</c> casts
/// the TextInstance child's RenderableComponent to <see cref="RenderingLibrary.Graphics.IFormsText"/>
/// and throws under <c>FULL_DIAGNOSTICS</c> (defined in SkiaGum's Debug/Release configs and in
/// MonoGameGum's, which TextBoxBase's shared source compiles through) if that cast fails. Before
/// SkiaGum's Text implemented <see cref="RenderingLibrary.Graphics.IFormsText"/>, constructing any
/// text-input control crashed here. Uses the real Silk-backed cursor from the assembly bootstrap
/// (unlike <c>SkiaGum.Tests.Forms.MenuPasswordBoxTests</c>, which uses the render-only
/// SkiaGum.Standalone GumService with no MainCursor, so it cannot construct these end-to-end).
/// </summary>
public class TextBoxPasswordBoxTests : BaseTestClass
{
    [Fact]
    public void TextBox_ConstructsWithVisual_OnSkia()
    {
        TextBox textBox = new();

        textBox.Visual.ShouldNotBeNull();
    }

    [Fact]
    public void PasswordBox_ConstructsWithVisual_OnSkia()
    {
        PasswordBox passwordBox = new();

        passwordBox.Visual.ShouldNotBeNull();
    }
}
