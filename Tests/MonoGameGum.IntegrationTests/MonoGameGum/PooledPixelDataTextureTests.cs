using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// <see cref="GraphicalUiElement.ApplyPooledTextureFromPixelData"/> on the XNA family (MonoGame, KNI,
/// FNA share this applier): the process-wide pool hands a detached owner's texture to the next caller,
/// which must only happen when the texture has the size the caller's pixel data needs. Mirrors the
/// Skia and raylib tests. Issue #7885.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class PooledPixelDataTextureTests : BaseTestClass
{
    [Fact]
    public void ApplyPooled_DetachedOwnerOfLargerSize_GivesTheNextOwnerATextureOfItsOwnSize()
    {
        using GumServiceGame game = StartGame();
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
        using GumServiceGame game = StartGame();
        ContainerRuntime root = new ContainerRuntime();
        ContainerRuntime smallOwner = Attach(root);
        Texture2D smallTexture = ApplyPooled(new SpriteRuntime(), smallOwner, size: 6);
        smallOwner.RemoveFromRoot();

        Texture2D largeTexture = ApplyPooled(new SpriteRuntime(), Attach(root), size: 11);

        largeTexture.Width.ShouldBe(11);
        largeTexture.Height.ShouldBe(11);
        largeTexture.ShouldNotBeSameAs(smallTexture);
        smallTexture.Width.ShouldBe(6);
    }

    [Fact]
    public void ApplyPooled_DetachedOwnerOfSameSize_ReusesTheTexture()
    {
        using GumServiceGame game = StartGame();
        ContainerRuntime root = new ContainerRuntime();
        ContainerRuntime firstOwner = Attach(root);
        Texture2D first = ApplyPooled(new SpriteRuntime(), firstOwner, size: 7);
        firstOwner.RemoveFromRoot();

        Texture2D second = ApplyPooled(new SpriteRuntime(), Attach(root), size: 7);

        second.ShouldBeSameAs(first);
    }

    [Fact]
    public void ApplyPooled_DataOfTheWrongLength_ThrowsTheSharedMessageAndLeavesTheSpriteAlone()
    {
        using GumServiceGame game = StartGame();
        ContainerRuntime root = new ContainerRuntime();
        SpriteRuntime target = new SpriteRuntime();

        ArgumentException exception = Should.Throw<ArgumentException>(() =>
            GraphicalUiElement.ApplyPooledTextureFromPixelData!(target, Attach(root), new byte[8], 6, 6));

        exception.Message.ShouldContain("6x6");
        ((global::RenderingLibrary.Graphics.Sprite)target.RenderableComponent).Texture.ShouldBeNull();
    }

    [Fact]
    public void ApplyPooled_BadDataWhenAnOwnerChangesSize_KeepsTheOwnersTextureFromOtherOwners()
    {
        using GumServiceGame game = StartGame();
        ContainerRuntime root = new ContainerRuntime();
        ContainerRuntime ownerA = Attach(root);
        SpriteRuntime targetA = new SpriteRuntime();
        Texture2D textureA = ApplyPooled(targetA, ownerA, size: 6);

        Should.Throw<ArgumentException>(() =>
            GraphicalUiElement.ApplyPooledTextureFromPixelData!(targetA, ownerA, new byte[8], 9, 9));
        Texture2D textureB = ApplyPooled(new SpriteRuntime(), Attach(root), size: 6);

        textureB.ShouldNotBeSameAs(textureA);
        ((global::RenderingLibrary.Graphics.Sprite)targetA.RenderableComponent).Texture.ShouldBeSameAs(textureA);
    }

    private static GumServiceGame StartGame()
    {
        GumServiceGame game = new GumServiceGame();
        game.RunOneFrame();
        return game;
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
        return ((global::RenderingLibrary.Graphics.Sprite)target.RenderableComponent).Texture!;
    }

    private class GumServiceGame : SharedGameLease
    {
        protected override void OnInitialize()
        {
            GumService.Default.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);
        }

        protected override void OnDispose()
        {
            if (GumService.Default.IsInitialized)
            {
                GumService.Default.Uninitialize();
            }
        }
    }
}
