using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.Wireframe;
using Raylib_cs;
using RaylibGum.Helpers;
using RaylibGum.Renderables;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using Xunit;

namespace RaylibGum.Tests.Runtimes;

// Covers the string-property-dispatch path (SetProperty -> CustomSetPropertyOnRenderable ->
// TrySetPropertyOnSprite / AssignSourceFileOnSprite) for cases that were previously commented
// out or missing entirely on raylib (issue #3615 "Sprite family" convergence with the
// MonoGame/KNI/FNA copy). These property names are set at runtime by the state/variable system
// (StateSave/VariableSave applied via GraphicalUiElement.SetProperty), not by direct C# property
// assignment, so a missing dispatch case here silently no-ops even though the equivalent direct
// C# property (e.g. SpriteRuntime.Blend) works fine.
public class SpriteSetPropertyTests : BaseTestClass
{
    [Fact]
    public void SetProperty_AlphaRedGreenBlue_ShouldForwardToContainedSprite()
    {
        SpriteRuntime sut = new();

        sut.SetProperty(nameof(SpriteRuntime.Alpha), 128);
        sut.SetProperty(nameof(SpriteRuntime.Red), 10);
        sut.SetProperty(nameof(SpriteRuntime.Green), 20);
        sut.SetProperty(nameof(SpriteRuntime.Blue), 30);

        sut.Alpha.ShouldBe(128);
        sut.Red.ShouldBe(10);
        sut.Green.ShouldBe(20);
        sut.Blue.ShouldBe(30);
    }

    [Fact]
    public void SetProperty_Blend_ShouldForwardToContainedSprite()
    {
        SpriteRuntime sut = new();

        sut.SetProperty("Blend", Gum.RenderingLibrary.Blend.Additive);

        sut.Blend.ShouldBe(Gum.RenderingLibrary.Blend.Additive);
    }

    [Fact]
    public void SetProperty_Color_ShouldForwardToContainedSprite()
    {
        SpriteRuntime sut = new();
        var drawingColor = System.Drawing.Color.FromArgb(40, 10, 20, 30);

        sut.SetProperty(nameof(SpriteRuntime.Color), drawingColor);

        ((Gum.Renderables.Sprite)sut.RenderableComponent).Color.ShouldBe(drawingColor.ToRaylib());
    }

    [Fact]
    public void SetProperty_Animate_ShouldForwardToContainedSprite()
    {
        SpriteRuntime sut = new();

        sut.SetProperty(nameof(SpriteRuntime.Animate), true);

        sut.Animate.ShouldBeTrue();
    }

    [Fact]
    public void SetProperty_CurrentChainName_ShouldUpdateTextureValues()
    {
        SpriteRuntime sut = new();

        // AnimationChains defaults to chain index 0 ("FirstChain"), so switching to
        // "SecondChain" via SetProperty is what actually exercises the dispatch case
        // (rather than the AnimationChains setter's own default-frame application).
        var firstChain = new AnimationChain { Name = "FirstChain" };
        firstChain.Add(new AnimationFrame
        {
            FrameLength = 1f,
            Texture = new Texture2D { Width = 10, Height = 10 },
            LeftCoordinate = 0f,
            RightCoordinate = 1f,
            TopCoordinate = 0f,
            BottomCoordinate = 1f,
        });
        var secondChain = new AnimationChain { Name = "SecondChain" };
        secondChain.Add(new AnimationFrame
        {
            FrameLength = 1f,
            Texture = new Texture2D { Width = 20, Height = 20 },
            LeftCoordinate = 0f,
            RightCoordinate = 1f,
            TopCoordinate = 0f,
            BottomCoordinate = 1f,
        });
        var chainList = new AnimationChainList();
        chainList.Add(firstChain);
        chainList.Add(secondChain);
        sut.AnimationChains = chainList;
        sut.TextureWidth.ShouldBe(10);

        sut.SetProperty(nameof(SpriteRuntime.CurrentChainName), "SecondChain");

        sut.CurrentChainName.ShouldBe("SecondChain");
        sut.TextureWidth.ShouldBe(20);
    }

    [Fact]
    public void SetProperty_RenderTargetTextureSource_CanAssignStringDirectReferenceAndNull()
    {
        var parent = new ContainerRuntime();
        var sut = new SpriteRuntime { Name = "TestSprite" };
        var renderTarget = new SpriteRuntime { Name = "RenderTarget" };

        parent.Children.Add(sut);
        parent.Children.Add(renderTarget);

        sut.SetProperty(nameof(SpriteRuntime.RenderTargetTextureSource), "RenderTarget");
        sut.RenderTargetTextureSource.ShouldNotBeNull();
        sut.RenderTargetTextureSource.ShouldBe(renderTarget);

        var directReference = new SpriteRuntime { Name = "Direct" };
        parent.Children.Add(directReference);

        sut.SetProperty(nameof(SpriteRuntime.RenderTargetTextureSource), directReference);
        sut.RenderTargetTextureSource.ShouldNotBeNull();
        sut.RenderTargetTextureSource.ShouldBe(directReference);

        sut.SetProperty(nameof(SpriteRuntime.RenderTargetTextureSource), null);
        sut.RenderTargetTextureSource.ShouldBeNull();
    }

    [Fact]
    public void SourceFileName_MissingFile_ShouldInvokePropertyAssignmentError_WhenNotThrowing()
    {
        var savedBehavior = GraphicalUiElement.MissingFileBehavior;
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;

        string? capturedMessage = null;
        void Handler(string message) => capturedMessage = message;
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Handler;

        try
        {
            SpriteRuntime sut = new();

            sut.SourceFileName = "ThisFileDoesNotExist_" + Guid.NewGuid().ToString("N") + ".png";

            capturedMessage.ShouldNotBeNull();
            sut.Texture.ShouldBeNull();
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= Handler;
            GraphicalUiElement.MissingFileBehavior = savedBehavior;
        }
    }
}
