using Gum.GueDeriving;
using Gum.Renderables;
using Gum.Wireframe;
using Raylib_cs;
using Shouldly;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// <see cref="GraphicalUiElement.ApplyPooledTextureFromPixelData"/> on raylib: the process-wide pool
/// hands a detached owner's texture to the next caller, which must only happen when the texture has
/// the size the caller's pixel data needs. Mirrors the Skia and XNA-family tests. Issue #7885.
/// </summary>
public class PooledPixelDataTextureTests : BaseTestClass
{
    [Fact]
    public void ApplyPooled_DetachedOwnerOfLargerSize_GivesTheNextOwnerATextureOfItsOwnSize()
    {
        ContainerRuntime root = new ContainerRuntime();
        ContainerRuntime largeOwner = Attach(root);
        ApplyPooled(new SpriteRuntime(), largeOwner, size: 9);
        largeOwner.RemoveFromRoot();

        Texture2D smallTexture = ApplyPooled(new SpriteRuntime(), Attach(root), size: 5);

        smallTexture.Width.ShouldBe(5);
        smallTexture.Height.ShouldBe(5);
    }

    [Fact]
    public void ApplyPooled_DetachedOwnerOfSmallerSize_GivesTheNextOwnerATextureOfItsOwnSize()
    {
        ContainerRuntime root = new ContainerRuntime();
        ContainerRuntime smallOwner = Attach(root);
        Texture2D smallTexture = ApplyPooled(new SpriteRuntime(), smallOwner, size: 6);
        smallOwner.RemoveFromRoot();

        Texture2D largeTexture = ApplyPooled(new SpriteRuntime(), Attach(root), size: 11);

        largeTexture.Width.ShouldBe(11);
        largeTexture.Height.ShouldBe(11);
        largeTexture.Id.ShouldNotBe(smallTexture.Id);
        smallTexture.Width.ShouldBe(6);
    }

    [Fact]
    public void ApplyPooled_DetachedOwnerOfSameSize_ReusesTheTexture()
    {
        ContainerRuntime root = new ContainerRuntime();
        ContainerRuntime firstOwner = Attach(root);
        Texture2D first = ApplyPooled(new SpriteRuntime(), firstOwner, size: 7);
        firstOwner.RemoveFromRoot();

        Texture2D second = ApplyPooled(new SpriteRuntime(), Attach(root), size: 7);

        second.Id.ShouldBe(first.Id);
    }

    private static ContainerRuntime Attach(ContainerRuntime root)
    {
        ContainerRuntime owner = new ContainerRuntime();
        root.AddChild(owner);
        return owner;
    }

    private static Texture2D ApplyPooled(SpriteRuntime target, GraphicalUiElement owner, int size)
    {
        byte[] rgba = new byte[size * size * 4];
        GraphicalUiElement.ApplyPooledTextureFromPixelData!(target, owner, rgba, size, size);
        return ((Sprite)target.RenderableComponent).Texture!.Value;
    }
}
