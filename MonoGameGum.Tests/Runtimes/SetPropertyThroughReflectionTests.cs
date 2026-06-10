using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Covers the conversions <see cref="GraphicalUiElement.SetPropertyThroughReflection"/>
/// must handle for the data-driven (.gumx → ApplyState → SetProperty) path: enums are
/// serialized as their underlying int (or, in hand-written files, by name), and many
/// renderable properties are declared <c>Nullable&lt;T&gt;</c>. Before this was fixed,
/// every backend worked around the same two gaps individually (see issue #2924).
/// </summary>
public class SetPropertyThroughReflectionTests : BaseTestClass
{
    private enum SampleEnum
    {
        Zero = 0,
        One = 1,
        Two = 2,
    }

    /// <summary>
    /// A minimal <see cref="IRenderableIpso"/> exposing the property shapes the reflection
    /// setter must convert into: a plain enum, a nullable enum, a plain int, and a nullable float.
    /// </summary>
    private class ReflectionTargetRenderable : InvisibleRenderable
    {
        public SampleEnum EnumValue { get; set; }
        public SampleEnum? NullableEnumValue { get; set; }
        public int IntValue { get; set; }
        public float? NullableFloatValue { get; set; }
    }

    private static void SetProperty(ReflectionTargetRenderable renderable, string propertyName, object value) =>
        GraphicalUiElement.SetPropertyThroughReflection(renderable, new GraphicalUiElement(), propertyName, value);

    [Fact]
    public void SetProperty_EnumToNullableEnum_ShouldAssignValue()
    {
        // The #2923 case: .gumx serializes a Blend? variable as the non-nullable Blend enum.
        ReflectionTargetRenderable renderable = new ReflectionTargetRenderable();
        SetProperty(renderable, nameof(ReflectionTargetRenderable.NullableEnumValue), SampleEnum.Two);
        renderable.NullableEnumValue.ShouldBe(SampleEnum.Two);
    }

    [Fact]
    public void SetProperty_FloatToNullableFloat_ShouldWrapInNullable()
    {
        ReflectionTargetRenderable renderable = new ReflectionTargetRenderable();
        SetProperty(renderable, nameof(ReflectionTargetRenderable.NullableFloatValue), 8f);
        renderable.NullableFloatValue.ShouldBe(8f);
    }

    [Fact]
    public void SetProperty_IntOutOfEnumRange_ShouldNotThrowAndAssignRawValue()
    {
        // Enum.ToObject accepts any int — even undeclared members — so a stale enum int in
        // a .gumx assigns a raw underlying value rather than tearing down the screen load.
        ReflectionTargetRenderable renderable = new ReflectionTargetRenderable();
        Should.NotThrow(() => SetProperty(renderable, nameof(ReflectionTargetRenderable.EnumValue), 999));
        ((int)renderable.EnumValue).ShouldBe(999);
    }

    [Fact]
    public void SetProperty_IntToEnum_ShouldAssignEnumValue()
    {
        ReflectionTargetRenderable renderable = new ReflectionTargetRenderable();
        SetProperty(renderable, nameof(ReflectionTargetRenderable.EnumValue), 1);
        renderable.EnumValue.ShouldBe(SampleEnum.One);
    }

    [Fact]
    public void SetProperty_IntToNullableEnum_ShouldAssignEnumValue()
    {
        ReflectionTargetRenderable renderable = new ReflectionTargetRenderable();
        SetProperty(renderable, nameof(ReflectionTargetRenderable.NullableEnumValue), 2);
        renderable.NullableEnumValue.ShouldBe(SampleEnum.Two);
    }

    [Fact]
    public void SetProperty_PlainInt_ShouldRoundTrip()
    {
        ReflectionTargetRenderable renderable = new ReflectionTargetRenderable();
        SetProperty(renderable, nameof(ReflectionTargetRenderable.IntValue), 3);
        renderable.IntValue.ShouldBe(3);
    }

    [Fact]
    public void SetProperty_StringToEnum_ShouldBeCaseInsensitive()
    {
        ReflectionTargetRenderable renderable = new ReflectionTargetRenderable();
        SetProperty(renderable, nameof(ReflectionTargetRenderable.EnumValue), "two");
        renderable.EnumValue.ShouldBe(SampleEnum.Two);
    }

    [Fact]
    public void SetProperty_StringToEnum_ShouldParseEnumName()
    {
        ReflectionTargetRenderable renderable = new ReflectionTargetRenderable();
        SetProperty(renderable, nameof(ReflectionTargetRenderable.EnumValue), "One");
        renderable.EnumValue.ShouldBe(SampleEnum.One);
    }

    [Fact]
    public void SetProperty_StringToNullableEnum_ShouldParseEnumName()
    {
        ReflectionTargetRenderable renderable = new ReflectionTargetRenderable();
        SetProperty(renderable, nameof(ReflectionTargetRenderable.NullableEnumValue), "Two");
        renderable.NullableEnumValue.ShouldBe(SampleEnum.Two);
    }

    [Fact]
    public void SetProperty_UnknownProperty_ShouldNotThrow()
    {
        // A bad variable name in .gumx shouldn't abort loading.
        ReflectionTargetRenderable renderable = new ReflectionTargetRenderable();
        Should.NotThrow(() => SetProperty(renderable, "ThisPropertyDoesNotExist", 42));
    }

    [Fact]
    public void SetProperty_UnparseableStringToEnum_ShouldNotThrowAndLeaveDefault()
    {
        // A value that can't be converted (name not in the enum) is skipped rather than
        // throwing — one bad variable must not tear down the entire screen load.
        ReflectionTargetRenderable renderable = new ReflectionTargetRenderable();
        Should.NotThrow(() => SetProperty(renderable, nameof(ReflectionTargetRenderable.EnumValue), "NotAMember"));
        renderable.EnumValue.ShouldBe(SampleEnum.Zero);
    }
}
