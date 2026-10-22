using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// A renderable's Width and Height treat NaN and infinity the same (#6477). Under FULL_DIAGNOSTICS each class rejects
/// the same values on both axes; without it neither axis rejects anything. Before, InvisibleRenderable (the base of
/// the Raylib renderables) rejected a Height of +infinity but accepted it as a Width, and Sprite rejected a NaN
/// Height but accepted it as a Width.
/// </summary>
public class RenderableSizeNonFiniteTests
{
    public static TheoryData<float> NonFiniteValues() => new()
    {
        float.PositiveInfinity,
        float.NegativeInfinity,
        float.NaN,
    };

    private static bool Throws(Action assign)
    {
        try
        {
            assign();
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }

    [Theory]
    [MemberData(nameof(NonFiniteValues))]
    public void InvisibleRenderable_WidthAndHeight_AcceptOrRejectTheSameValues(float value)
    {
        InvisibleRenderable renderable = new InvisibleRenderable();

        bool widthThrows = Throws(() => renderable.Width = value);
        bool heightThrows = Throws(() => renderable.Height = value);

        widthThrows.ShouldBe(heightThrows,
            $"because Width and Height are siblings and must treat {value} alike");
    }

    [Theory]
    [MemberData(nameof(NonFiniteValues))]
    public void Sprite_WidthAndHeight_AcceptOrRejectTheSameValues(float value)
    {
        Sprite sprite = new Sprite((Texture2D?)null);

        bool widthThrows = Throws(() => sprite.Width = value);
        bool heightThrows = Throws(() => sprite.Height = value);

        widthThrows.ShouldBe(heightThrows,
            $"because Width and Height are siblings and must treat {value} alike");
    }

    [Fact]
    public void InvisibleRenderable_PositiveInfinity_IsRejectedOnBothAxesOnlyWithDiagnostics()
    {
        InvisibleRenderable renderable = new InvisibleRenderable();

#if FULL_DIAGNOSTICS
        Should.Throw<ArgumentException>(() => renderable.Width = float.PositiveInfinity);
        Should.Throw<ArgumentException>(() => renderable.Height = float.PositiveInfinity);
        renderable.Width.ShouldBe(0f, "because a rejected value is not stored");
        renderable.Height.ShouldBe(0f, "because a rejected value is not stored");
#else
        renderable.Width = float.PositiveInfinity;
        renderable.Height = float.PositiveInfinity;
        renderable.Width.ShouldBe(float.PositiveInfinity);
        renderable.Height.ShouldBe(float.PositiveInfinity);
#endif
    }

    [Fact]
    public void InvisibleRenderable_NaNAndNegativeInfinity_AreAcceptedOnBothAxes()
    {
        InvisibleRenderable renderable = new InvisibleRenderable();

        renderable.Width = float.NaN;
        renderable.Height = float.NaN;
        float.IsNaN(renderable.Width).ShouldBeTrue();
        float.IsNaN(renderable.Height).ShouldBeTrue();

        renderable.Width = float.NegativeInfinity;
        renderable.Height = float.NegativeInfinity;
        renderable.Width.ShouldBe(float.NegativeInfinity);
        renderable.Height.ShouldBe(float.NegativeInfinity);
    }

    [Fact]
    public void Sprite_NaN_IsRejectedOnBothAxesOnlyWithDiagnostics()
    {
        Sprite sprite = new Sprite((Texture2D?)null);

#if FULL_DIAGNOSTICS
        Should.Throw<InvalidOperationException>(() => sprite.Width = float.NaN);
        Should.Throw<InvalidOperationException>(() => sprite.Height = float.NaN);
        sprite.Width.ShouldBe(0f, "because a rejected value is not stored");
        sprite.Height.ShouldBe(0f, "because a rejected value is not stored");
#else
        sprite.Width = float.NaN;
        sprite.Height = float.NaN;
        float.IsNaN(sprite.Width).ShouldBeTrue();
        float.IsNaN(sprite.Height).ShouldBeTrue();
#endif
    }

    [Fact]
    public void Sprite_Infinity_IsAcceptedOnBothAxes()
    {
        Sprite sprite = new Sprite((Texture2D?)null);

        sprite.Width = float.PositiveInfinity;
        sprite.Height = float.PositiveInfinity;
        sprite.Width.ShouldBe(float.PositiveInfinity);
        sprite.Height.ShouldBe(float.PositiveInfinity);
    }

    [Fact]
    public void FiniteSizes_AreStoredUnchangedOnBothAxes()
    {
        Random random = new Random(6477);
        InvisibleRenderable renderable = new InvisibleRenderable();
        Sprite sprite = new Sprite((Texture2D?)null);

        for (int i = 0; i < 300; i++)
        {
            float width = (float)(random.NextDouble() * 4000 - 2000);
            float height = (float)(random.NextDouble() * 4000 - 2000);

            renderable.Width = width;
            renderable.Height = height;
            sprite.Width = width;
            sprite.Height = height;

            renderable.Width.ShouldBe(width);
            renderable.Height.ShouldBe(height);
            sprite.Width.ShouldBe(width);
            sprite.Height.ShouldBe(height);
        }
    }
}
