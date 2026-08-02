using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.GumCommon;

/// <summary>
/// Pins the pooled-texture bookkeeping shared by every backend's PixelDataTextureApplier: an owner
/// keeps its own texture while it is in the visual tree, and an entry whose owner has detached is
/// recycled instead of growing the pool. Issue #4247.
/// </summary>
public class PixelDataTexturePoolTests : BaseTestClass
{
    [Fact]
    public void GetOrCreate_ReclaimsEntryWhoseOwnerLeftTheVisualTree()
    {
        object detachedOwnerTexture = new object();
        object newlyCreatedTexture = new object();
        ContainerRuntime root = new ContainerRuntime();
        GraphicalUiElement detachedOwner = new GraphicalUiElement();
        GraphicalUiElement newOwner = new GraphicalUiElement();
        root.AddChild(newOwner);
        PixelDataTexturePool<object> pool = new PixelDataTexturePool<object>();
        pool.GetOrCreate(detachedOwner, () => detachedOwnerTexture);

        object reclaimed = pool.GetOrCreate(newOwner, () => newlyCreatedTexture);

        reclaimed.ShouldBeSameAs(detachedOwnerTexture);
    }

    [Fact]
    public void GetOrCreate_SameOwner_ReturnsTheSameTexture()
    {
        object firstTexture = new object();
        object secondTexture = new object();
        ContainerRuntime root = new ContainerRuntime();
        GraphicalUiElement owner = new GraphicalUiElement();
        root.AddChild(owner);
        PixelDataTexturePool<object> pool = new PixelDataTexturePool<object>();
        pool.GetOrCreate(owner, () => firstTexture);

        object second = pool.GetOrCreate(owner, () => secondTexture);

        second.ShouldBeSameAs(firstTexture);
    }

    [Fact]
    public void GetOrCreate_SecondOwnerInVisualTree_CreatesItsOwnTexture()
    {
        object firstTexture = new object();
        object secondTexture = new object();
        ContainerRuntime root = new ContainerRuntime();
        GraphicalUiElement firstOwner = new GraphicalUiElement();
        GraphicalUiElement secondOwner = new GraphicalUiElement();
        root.AddChild(firstOwner);
        root.AddChild(secondOwner);
        PixelDataTexturePool<object> pool = new PixelDataTexturePool<object>();
        pool.GetOrCreate(firstOwner, () => firstTexture);

        object second = pool.GetOrCreate(secondOwner, () => secondTexture);

        second.ShouldBeSameAs(secondTexture);
    }
}
