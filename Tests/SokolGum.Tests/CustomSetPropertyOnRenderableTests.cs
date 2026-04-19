using Gum.GueDeriving;
using Gum.Renderables;
using RenderingLibrary.Graphics;
using Shouldly;

namespace SokolGum.Tests;

/// <summary>
/// Covers the two reflection-fallback bugs SokolGum caught mid-branch
/// that core Gum's <see cref="Gum.Wireframe.GraphicalUiElement.SetPropertyThroughReflection"/>
/// doesn't handle — <c>Convert.ChangeType</c> throws on int→enum and on
/// primitive→Nullable&lt;T&gt;. Both cases show up constantly in real
/// <c>.gumx</c> files (enum values serialized as int, nullable floats as
/// plain floats), so these regressions would bite every user.
/// </summary>
public class CustomSetPropertyOnRenderableTests : BaseTestClass
{
    [Fact]
    public void SetProperty_IntToEnum_ShouldAssignEnumValue()
    {
        var text = new TextRuntime();
        var renderable = (Text)text.RenderableComponent;
        // HorizontalAlignment.Center = 1 in the enum's backing int.
        CustomSetPropertyOnRenderable.SetPropertyOnRenderable(
            renderable, text, "HorizontalAlignment", 1);
        renderable.HorizontalAlignment.ShouldBe(HorizontalAlignment.Center);
    }

    [Fact]
    public void SetProperty_StringToEnum_ShouldParseEnumName()
    {
        var text = new TextRuntime();
        var renderable = (Text)text.RenderableComponent;
        CustomSetPropertyOnRenderable.SetPropertyOnRenderable(
            renderable, text, "HorizontalAlignment", "Right");
        renderable.HorizontalAlignment.ShouldBe(HorizontalAlignment.Right);
    }

    [Fact]
    public void SetProperty_StringToEnum_ShouldBeCaseInsensitive()
    {
        var text = new TextRuntime();
        var renderable = (Text)text.RenderableComponent;
        CustomSetPropertyOnRenderable.SetPropertyOnRenderable(
            renderable, text, "HorizontalAlignment", "center");
        renderable.HorizontalAlignment.ShouldBe(HorizontalAlignment.Center);
    }

    [Fact]
    public void SetProperty_FloatToNullableFloat_ShouldWrapInNullable()
    {
        // CustomFrameTextureCoordinateWidth is float?; .gumx stores the raw
        // value as float. This is the path Convert.ChangeType throws on.
        var nineSlice = new NineSliceRuntime();
        var renderable = (NineSlice)nineSlice.RenderableComponent;
        CustomSetPropertyOnRenderable.SetPropertyOnRenderable(
            renderable, nineSlice, "CustomFrameTextureCoordinateWidth", 8f);
        renderable.CustomFrameTextureCoordinateWidth.ShouldBe(8f);
    }

    [Fact]
    public void SetProperty_IntOutOfEnumRange_ShouldAssignRawIntValue()
    {
        // Enum.ToObject accepts any int — even values that aren't declared
        // enum members — so an out-of-range int silently assigns a raw
        // underlying value rather than throwing. Documenting this because
        // a .gumx with a stale enum int shouldn't tear down loading.
        var text = new TextRuntime();
        var renderable = (Text)text.RenderableComponent;
        Should.NotThrow(() =>
            CustomSetPropertyOnRenderable.SetPropertyOnRenderable(
                renderable, text, "HorizontalAlignment", 999));
        ((int)renderable.HorizontalAlignment).ShouldBe(999);
    }

    [Fact]
    public void SetProperty_UnknownProperty_ShouldFallThroughSilently()
    {
        // Unknown property names fall through to the core reflection helper,
        // which itself no-ops when the property doesn't exist. A single bad
        // variable name in .gumx shouldn't abort loading.
        var text = new TextRuntime();
        var renderable = (Text)text.RenderableComponent;
        Should.NotThrow(() =>
            CustomSetPropertyOnRenderable.SetPropertyOnRenderable(
                renderable, text, "ThisPropertyDoesntExist", 42));
    }

    [Fact]
    public void SetProperty_TextContent_ShouldAssignRawText()
    {
        // The ".gumx stores Text as variable named 'Text', not 'RawText'"
        // case: CustomSetPropertyOnRenderable translates the name.
        var text = new TextRuntime();
        var renderable = (Text)text.RenderableComponent;
        CustomSetPropertyOnRenderable.SetPropertyOnRenderable(
            renderable, text, "Text", "hello world");
        renderable.RawText.ShouldBe("hello world");
    }

    [Fact]
    public void SetProperty_OutlineThickness_ShouldAssignInt()
    {
        // Regression guard for #2537-era concerns — OutlineThickness is a
        // plain int property, should round-trip without any conversion.
        var text = new TextRuntime();
        var renderable = (Text)text.RenderableComponent;
        CustomSetPropertyOnRenderable.SetPropertyOnRenderable(
            renderable, text, "OutlineThickness", 3);
        renderable.OutlineThickness.ShouldBe(3);
    }
}
