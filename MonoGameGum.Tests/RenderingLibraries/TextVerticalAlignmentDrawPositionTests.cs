using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.RenderingLibraries;

public class TextVerticalAlignmentDrawPositionTests : BaseTestClass
{
    [Theory]
    [InlineData(VerticalAlignment.Top, "Line1")]
    [InlineData(VerticalAlignment.Center, "Line1")]
    [InlineData(VerticalAlignment.Bottom, "Line1")]
    [InlineData(VerticalAlignment.Top, "Line1\nLine2\nLine3")]
    [InlineData(VerticalAlignment.Center, "Line1\nLine2\nLine3")]
    [InlineData(VerticalAlignment.Bottom, "Line1\nLine2\nLine3")]
    public void UpdateIpsoForRendering_Unrotated_ShouldPlaceLinesAtAlignmentOffset(VerticalAlignment verticalAlignment, string rawText)
    {
        Text text = new Text();
        text.X = 10;
        text.Y = 20;
        text.Width = 200;
        text.Height = 101;
        text.VerticalAlignment = verticalAlignment;
        text.RawText = rawText;

        var temp = text.UpdateIpsoForRenderingForTests();

        text.WrappedTextHeight.ShouldBeGreaterThan(0);
        float expectedOffset = verticalAlignment switch
        {
            VerticalAlignment.Center => (text.EffectiveHeight - text.WrappedTextHeight) / 2.0f,
            VerticalAlignment.Bottom => text.EffectiveHeight - text.WrappedTextHeight,
            _ => 0,
        };
        temp.GetAbsoluteTop().ShouldBe(text.GetAbsoluteTop() + expectedOffset);
        temp.GetAbsoluteLeft().ShouldBe(text.GetAbsoluteLeft());
    }
}
