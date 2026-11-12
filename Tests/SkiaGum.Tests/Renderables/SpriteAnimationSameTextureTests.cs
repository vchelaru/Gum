using Gum.Graphics.Animation;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;
using Rectangle = System.Drawing.Rectangle;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A spritesheet animation applies the same bitmap on every frame, so changing frames must not rebuild the
/// sprite's <see cref="SKImage"/> (#7943). Assigning <c>Texture</c> again by hand still rebuilds it.
/// </summary>
public class SpriteAnimationSameTextureTests
{
    private static Sprite CreateAnimatedSprite(SKBitmap sheet)
    {
        AnimationChain chain = new() { Name = "Walk" };
        for (int i = 0; i < 3; i++)
        {
            chain.Add(new AnimationFrame
            {
                FrameLength = 1,
                Texture = sheet,
                LeftCoordinate = i / 3f,
                RightCoordinate = (i + 1) / 3f,
                BottomCoordinate = 1,
            });
        }
        AnimationChainList chains = new() { chain };
        Sprite sprite = new();
        sprite.AnimationLogic.AnimationChains = chains;
        sprite.AnimationLogic.CurrentChainName = "Walk";
        sprite.AnimationLogic.Animate = true;
        return sprite;
    }

    [Fact]
    public void AnimateSelf_FramesShareOneBitmap_KeepsTheSameImage()
    {
        using SKBitmap sheet = new(30, 10);
        Sprite sprite = CreateAnimatedSprite(sheet);
        sprite.AnimateSelf(0.1);
        SKImage? image = sprite.Image;
        Rectangle? firstSource = sprite.SourceRectangle;

        sprite.AnimateSelf(1);
        sprite.AnimateSelf(1);

        sprite.SourceRectangle.ShouldNotBe(firstSource);
        sprite.Image.ShouldBeSameAs(image);
        image!.Handle.ShouldNotBe(IntPtr.Zero);
    }

    [Fact]
    public void AnimateSelf_ImageAssignedOverTexture_IsReplacedByTheFrameTexture()
    {
        using SKBitmap sheet = new(30, 10);
        using SKBitmap other = new(30, 10);
        Sprite sprite = CreateAnimatedSprite(sheet);
        sprite.Texture = sheet;
        using SKImage mine = SKImage.FromBitmap(other);
        sprite.Image = mine;

        sprite.AnimateSelf(1);

        sprite.Image.ShouldNotBeSameAs(mine);
        sprite.Texture.ShouldBeSameAs(sheet);
    }

    [Fact]
    public void Texture_AssignedAgain_StillRebuildsTheImage()
    {
        using SKBitmap bitmap = new(4, 4);
        Sprite sprite = new() { Texture = bitmap };
        SKImage? first = sprite.Image;

        sprite.Texture = bitmap;

        sprite.Image.ShouldNotBeSameAs(first);
    }
}
