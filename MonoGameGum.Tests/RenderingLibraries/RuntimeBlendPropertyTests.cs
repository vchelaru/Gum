using Gum.GueDeriving;
using Gum.RenderingLibrary;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// The runtimes' <c>Blend</c> getter reads back what a code set or a project load (SetProperty) wrote,
/// including <see cref="Blend.Normal"/> when <see cref="Renderer.NormalBlendState"/> is not the default.
/// </summary>
public class RuntimeBlendPropertyTests : BaseTestClass
{
    public static TheoryData<string, Blend> RuntimeTypesAndBlends()
    {
        TheoryData<string, Blend> data = new();
        foreach (string type in new[] { nameof(ContainerRuntime), nameof(SpriteRuntime), nameof(NineSliceRuntime), nameof(ColoredRectangleRuntime), nameof(TextRuntime) })
        {
            foreach (Blend blend in System.Enum.GetValues<Blend>())
            {
                data.Add(type, blend);
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(RuntimeTypesAndBlends))]
    public void Blend_ReadsBackTheValueSetThroughSetProperty(string runtimeType, Blend blend)
    {
        GraphicalUiElement element = Create(runtimeType);

        element.SetProperty("Blend", blend);

        GetBlend(element).ShouldBe(blend);
    }

    [Theory]
    [MemberData(nameof(RuntimeTypesAndBlends))]
    public void Blend_ReadsBackTheValueSetThroughSetProperty_WhenNormalBlendStateIsCustom(string runtimeType, Blend blend)
    {
        WithNormalBlendState(CreateStraightAlphaOver(), () =>
        {
            GraphicalUiElement element = Create(runtimeType);

            element.SetProperty("Blend", blend);

            GetBlend(element).ShouldBe(blend);
        });
    }

    [Theory]
    [InlineData(nameof(ContainerRuntime))]
    [InlineData(nameof(SpriteRuntime))]
    [InlineData(nameof(NineSliceRuntime))]
    [InlineData(nameof(ColoredRectangleRuntime))]
    [InlineData(nameof(TextRuntime))]
    public void Blend_IsNormal_AfterSettingNormal_WhenUsePremultipliedAlpha(string runtimeType)
    {
        bool previous = Renderer.UsePremultipliedAlpha;
        try
        {
            Renderer.UsePremultipliedAlpha = true;
            GraphicalUiElement element = Create(runtimeType);

            SetBlend(element, Blend.Normal);

            GetBlend(element).ShouldBe(Blend.Normal);
        }
        finally
        {
            Renderer.UsePremultipliedAlpha = previous;
        }
    }

    [Theory]
    [InlineData(nameof(ContainerRuntime))]
    [InlineData(nameof(SpriteRuntime))]
    [InlineData(nameof(NineSliceRuntime))]
    [InlineData(nameof(ColoredRectangleRuntime))]
    [InlineData(nameof(TextRuntime))]
    public void Blend_IsNormal_AfterSettingNormal_WhenNormalBlendStateIsCustom(string runtimeType)
    {
        WithNormalBlendState(CreateStraightAlphaOver(), () =>
        {
            GraphicalUiElement element = Create(runtimeType);

            SetBlend(element, Blend.Normal);

            GetBlend(element).ShouldBe(Blend.Normal);
        });
    }

    [Theory]
    [InlineData(nameof(SpriteRuntime))]
    [InlineData(nameof(NineSliceRuntime))]
    [InlineData(nameof(ColoredRectangleRuntime))]
    [InlineData(nameof(TextRuntime))]
    public void BlendState_HoldsTheNormalBlendStateInstance_AfterSetPropertyNormal_WhenNormalBlendStateIsCustom(string runtimeType)
    {
        Gum.BlendState custom = CreateStraightAlphaOver();
        WithNormalBlendState(custom, () =>
        {
            GraphicalUiElement element = Create(runtimeType);

            element.SetProperty("Blend", Blend.Normal);

            ((IRenderableIpso)element.RenderableComponent).BlendState.ShouldBeSameAs(custom);
        });
    }

    internal static GraphicalUiElement Create(string runtimeType) => runtimeType switch
    {
        nameof(ContainerRuntime) => new ContainerRuntime(),
        nameof(SpriteRuntime) => new SpriteRuntime(),
        nameof(NineSliceRuntime) => new NineSliceRuntime(),
        nameof(ColoredRectangleRuntime) => new ColoredRectangleRuntime(),
        _ => new TextRuntime(),
    };

    private static Blend? GetBlend(GraphicalUiElement element) => element switch
    {
        ContainerRuntime c => c.Blend,
        SpriteRuntime s => s.Blend,
        NineSliceRuntime n => n.Blend,
        ColoredRectangleRuntime r => r.Blend,
        TextRuntime t => t.Blend,
        _ => throw new System.ArgumentException(element.GetType().Name),
    };

    private static void SetBlend(GraphicalUiElement element, Blend blend)
    {
        switch (element)
        {
            case ContainerRuntime c: c.Blend = blend; break;
            case SpriteRuntime s: s.Blend = blend; break;
            case NineSliceRuntime n: n.Blend = blend; break;
            case ColoredRectangleRuntime r: r.Blend = blend; break;
            case TextRuntime t: t.Blend = blend; break;
        }
    }

    internal static void WithNormalBlendState(Gum.BlendState normal, System.Action action)
    {
        Gum.BlendState previous = Renderer.NormalBlendState;
        try
        {
            Renderer.NormalBlendState = normal;
            action();
        }
        finally
        {
            Renderer.NormalBlendState = previous;
        }
    }

    internal static Gum.BlendState CreateStraightAlphaOver() => new Gum.BlendState
    {
        ColorSourceBlend = Gum.Blend.SourceAlpha,
        ColorDestinationBlend = Gum.Blend.InverseSourceAlpha,
        AlphaSourceBlend = Gum.Blend.One,
        AlphaDestinationBlend = Gum.Blend.InverseSourceAlpha,
    };
}
