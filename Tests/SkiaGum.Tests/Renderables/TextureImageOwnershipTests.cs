using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Sprite and NineSlice build an <see cref="SKImage"/> from their <c>Texture</c> bitmap. They own
/// that image and dispose it when it is replaced, but never dispose the bitmap (usually shared through
/// the content cache) or an image assigned directly through <c>Image</c> (#5199).
/// </summary>
public class TextureImageOwnershipTests
{
    public static TheoryData<string> RenderableNames => new() { nameof(Sprite), nameof(NineSlice) };

    private static RenderableShapeBase Create(string name) => name switch
    {
        nameof(Sprite) => new Sprite(),
        nameof(NineSlice) => new NineSlice(),
        _ => throw new ArgumentException(name),
    };

    private static void SetTexture(RenderableShapeBase renderable, SKBitmap? texture)
    {
        if (renderable is Sprite sprite) sprite.Texture = texture;
        else ((NineSlice)renderable).Texture = texture;
    }

    private static SKImage? GetImage(RenderableShapeBase renderable) =>
        renderable is Sprite sprite ? sprite.Image : ((NineSlice)renderable).Image;

    private static void SetImage(RenderableShapeBase renderable, SKImage? image)
    {
        if (renderable is Sprite sprite) sprite.Image = image;
        else ((NineSlice)renderable).Image = image;
    }

    private static bool IsDisposed(SKObject skObject) => skObject.Handle == IntPtr.Zero;

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Clone_ShouldKeepItsImage_WhenEitherSideChangesTexture(string name)
    {
        using SKBitmap first = new(4, 4);
        using SKBitmap second = new(4, 4);
        using SKBitmap third = new(4, 4);
        RenderableShapeBase original = Create(name);
        SetTexture(original, first);

        RenderableShapeBase clone = (RenderableShapeBase)original.Clone();
        SKImage cloneImage = GetImage(clone)!;
        SetTexture(original, second);
        bool cloneImageDisposedByOriginal = IsDisposed(cloneImage);
        SKImage originalImage = GetImage(original)!;
        SetTexture(clone, third);

        cloneImageDisposedByOriginal.ShouldBeFalse();
        IsDisposed(originalImage).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Dispose_ShouldDisposeTheOwnedImage_ButNotAnAssignedOne(string name)
    {
        using SKBitmap bitmap = new(4, 4);
        using SKImage assigned = SKImage.FromBitmap(bitmap);
        RenderableShapeBase owning = Create(name);
        SetTexture(owning, bitmap);
        SKImage owned = GetImage(owning)!;
        RenderableShapeBase notOwning = Create(name);
        SetImage(notOwning, assigned);

        owning.Dispose();
        notOwning.Dispose();

        IsDisposed(owned).ShouldBeTrue();
        IsDisposed(assigned).ShouldBeFalse();
        IsDisposed(bitmap).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Image_AssignedDirectly_ShouldReplaceTheOwnedImage_AndNotBeDisposedLater(string name)
    {
        using SKBitmap first = new(4, 4);
        using SKBitmap second = new(4, 4);
        using SKImage assigned = SKImage.FromBitmap(first);
        RenderableShapeBase renderable = Create(name);
        SetTexture(renderable, first);
        SKImage owned = GetImage(renderable)!;

        SetImage(renderable, owned);
        bool ownedDisposedByReassigningItself = IsDisposed(owned);
        SetImage(renderable, assigned);
        SetTexture(renderable, second);

        ownedDisposedByReassigningItself.ShouldBeFalse();
        IsDisposed(owned).ShouldBeTrue();
        IsDisposed(assigned).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Texture_Replaced_ShouldDisposeThePreviousImage_ButNotTheBitmap(string name)
    {
        using SKBitmap first = new(4, 4);
        using SKBitmap second = new(4, 4);
        RenderableShapeBase renderable = Create(name);
        SetTexture(renderable, first);
        SKImage firstImage = GetImage(renderable)!;
        SetTexture(renderable, first);
        SKImage sameBitmapImage = GetImage(renderable)!;

        SetTexture(renderable, second);
        SKImage secondImage = GetImage(renderable)!;
        SetTexture(renderable, null);

        IsDisposed(firstImage).ShouldBeTrue();
        IsDisposed(sameBitmapImage).ShouldBeTrue();
        IsDisposed(secondImage).ShouldBeTrue();
        IsDisposed(first).ShouldBeFalse();
        IsDisposed(second).ShouldBeFalse();
    }
}
