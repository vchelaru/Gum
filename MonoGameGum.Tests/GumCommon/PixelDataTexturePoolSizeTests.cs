using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.GumCommon;

/// <summary>
/// Pins the size rule of the shared pooled-texture bookkeeping: a reclaimed texture is reused only when
/// it has the size the next caller needs, otherwise a new texture is created and the old entry stays
/// available for a caller of its own size. Issue #7885.
/// </summary>
public class PixelDataTexturePoolSizeTests : BaseTestClass
{
    private class SizedTexture
    {
        public SizedTexture(int size) => Size = size;
        public int Size { get; }
    }

    private static SizedTexture GetOrCreate(
        PixelDataTexturePool<SizedTexture> pool, GraphicalUiElement owner, int size, ref int createCount)
    {
        int created = 0;
        SizedTexture texture = pool.GetOrCreate(
            owner,
            existing => existing.Size == size,
            () =>
            {
                created++;
                return new SizedTexture(size);
            });
        createCount += created;
        return texture;
    }

    private static GraphicalUiElement AttachedOwner(ContainerRuntime root)
    {
        GraphicalUiElement owner = new GraphicalUiElement();
        root.AddChild(owner);
        return owner;
    }

    [Fact]
    public void GetOrCreate_DetachedOwnerOfSameSize_ReusesItsTexture()
    {
        ContainerRuntime root = new ContainerRuntime();
        PixelDataTexturePool<SizedTexture> pool = new PixelDataTexturePool<SizedTexture>();
        int createCount = 0;
        SizedTexture first = GetOrCreate(pool, new GraphicalUiElement(), 3, ref createCount);

        SizedTexture second = GetOrCreate(pool, AttachedOwner(root), 3, ref createCount);

        second.ShouldBeSameAs(first);
        createCount.ShouldBe(1);
    }

    [Fact]
    public void GetOrCreate_DetachedOwnerOfSmallerSize_CreatesANewTextureAndLeavesTheOldOneAlone()
    {
        ContainerRuntime root = new ContainerRuntime();
        PixelDataTexturePool<SizedTexture> pool = new PixelDataTexturePool<SizedTexture>();
        int createCount = 0;
        SizedTexture small = GetOrCreate(pool, new GraphicalUiElement(), 3, ref createCount);

        SizedTexture large = GetOrCreate(pool, AttachedOwner(root), 256, ref createCount);

        large.ShouldNotBeSameAs(small);
        large.Size.ShouldBe(256);
        small.Size.ShouldBe(3);
        createCount.ShouldBe(2);
    }

    [Fact]
    public void GetOrCreate_DetachedOwnerOfLargerSize_CreatesANewTexture()
    {
        ContainerRuntime root = new ContainerRuntime();
        PixelDataTexturePool<SizedTexture> pool = new PixelDataTexturePool<SizedTexture>();
        int createCount = 0;
        SizedTexture large = GetOrCreate(pool, new GraphicalUiElement(), 256, ref createCount);

        SizedTexture small = GetOrCreate(pool, AttachedOwner(root), 3, ref createCount);

        small.ShouldNotBeSameAs(large);
        small.Size.ShouldBe(3);
        createCount.ShouldBe(2);
    }

    [Fact]
    public void GetOrCreate_SkipsDetachedEntriesThatDoNotFit_AndReclaimsTheOneThatDoes()
    {
        ContainerRuntime root = new ContainerRuntime();
        PixelDataTexturePool<SizedTexture> pool = new PixelDataTexturePool<SizedTexture>();
        int createCount = 0;
        SizedTexture size3 = GetOrCreate(pool, new GraphicalUiElement(), 3, ref createCount);
        SizedTexture size9 = GetOrCreate(pool, new GraphicalUiElement(), 9, ref createCount);

        SizedTexture reclaimed9 = GetOrCreate(pool, AttachedOwner(root), 9, ref createCount);
        SizedTexture reclaimed3 = GetOrCreate(pool, AttachedOwner(root), 3, ref createCount);

        reclaimed9.ShouldBeSameAs(size9);
        reclaimed3.ShouldBeSameAs(size3);
        createCount.ShouldBe(2);
    }

    [Fact]
    public void GetOrCreate_SameOwnerAsksForANewSize_GetsAFittingTextureAndFreesTheOldEntry()
    {
        ContainerRuntime root = new ContainerRuntime();
        PixelDataTexturePool<SizedTexture> pool = new PixelDataTexturePool<SizedTexture>();
        int createCount = 0;
        GraphicalUiElement owner = AttachedOwner(root);
        SizedTexture small = GetOrCreate(pool, owner, 3, ref createCount);

        SizedTexture large = GetOrCreate(pool, owner, 9, ref createCount);
        SizedTexture sameOwnerAgain = GetOrCreate(pool, owner, 9, ref createCount);
        // The owner no longer holds the 3x3 entry, so another live owner can claim it.
        SizedTexture otherOwnerSmall = GetOrCreate(pool, AttachedOwner(root), 3, ref createCount);

        large.Size.ShouldBe(9);
        large.ShouldNotBeSameAs(small);
        sameOwnerAgain.ShouldBeSameAs(large);
        otherOwnerSmall.ShouldBeSameAs(small);
        createCount.ShouldBe(2);
    }

    [Fact]
    public void GetOrCreate_CreateThrowsOnASizeChange_OwnerKeepsItsTextureAndNobodyElseGetsIt()
    {
        ContainerRuntime root = new ContainerRuntime();
        PixelDataTexturePool<SizedTexture> pool = new PixelDataTexturePool<SizedTexture>();
        int createCount = 0;
        GraphicalUiElement owner = AttachedOwner(root);
        SizedTexture ownersTexture = GetOrCreate(pool, owner, 3, ref createCount);

        Should.Throw<InvalidOperationException>(() => pool.GetOrCreate(
            owner,
            existing => existing.Size == 9,
            () => throw new InvalidOperationException("allocation failed")));
        SizedTexture otherOwnersTexture = GetOrCreate(pool, AttachedOwner(root), 3, ref createCount);
        SizedTexture ownerAgain = GetOrCreate(pool, owner, 3, ref createCount);

        otherOwnersTexture.ShouldNotBeSameAs(ownersTexture);
        ownerAgain.ShouldBeSameAs(ownersTexture);
    }

    [Fact]
    public void GetOrCreate_LiveOwnerKeepsItsTexture_WhenAnotherOwnerNeedsADifferentSize()
    {
        ContainerRuntime root = new ContainerRuntime();
        PixelDataTexturePool<SizedTexture> pool = new PixelDataTexturePool<SizedTexture>();
        int createCount = 0;
        GraphicalUiElement liveOwner = AttachedOwner(root);
        SizedTexture liveTexture = GetOrCreate(pool, liveOwner, 3, ref createCount);

        GetOrCreate(pool, AttachedOwner(root), 9, ref createCount);
        SizedTexture liveAgain = GetOrCreate(pool, liveOwner, 3, ref createCount);

        liveAgain.ShouldBeSameAs(liveTexture);
        createCount.ShouldBe(2);
    }

    [Fact]
    public void GetOrCreate_RepeatedCreateAndDestroyCyclesOfOneSize_NeverGrowTheirPool()
    {
        ContainerRuntime root = new ContainerRuntime();
        PixelDataTexturePool<SizedTexture> pool = new PixelDataTexturePool<SizedTexture>();
        int createCount = 0;

        for (int cycle = 0; cycle < 25; cycle++)
        {
            GraphicalUiElement owner = AttachedOwner(root);
            GetOrCreate(pool, owner, 256, ref createCount);
            owner.RemoveFromRoot();
        }

        createCount.ShouldBe(1);
    }

    [Fact]
    public void GetOrCreate_RepeatedCyclesOfMixedSizes_GrowOnlyToOneTexturePerSize()
    {
        ContainerRuntime root = new ContainerRuntime();
        PixelDataTexturePool<SizedTexture> pool = new PixelDataTexturePool<SizedTexture>();
        int createCount = 0;
        int[] sizes = { 3, 256, 9 };

        for (int cycle = 0; cycle < 30; cycle++)
        {
            GraphicalUiElement owner = AttachedOwner(root);
            GetOrCreate(pool, owner, sizes[cycle % sizes.Length], ref createCount);
            owner.RemoveFromRoot();
        }

        createCount.ShouldBe(sizes.Length);
    }
}
