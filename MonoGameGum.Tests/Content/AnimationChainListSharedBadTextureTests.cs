using Gum.Content.AnimationChain;
using Gum.Graphics.Animation;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Content;

/// <summary>
/// Many frames of an .achx often share one sprite sheet. When that texture fails to load, the load
/// raises one error per distinct texture, not one per frame (#6899), and the loader is asked for each
/// failing texture once.
/// </summary>
public class AnimationChainListSharedBadTextureTests : BaseTestClass
{
    [Fact]
    public void ToAnimationChainList_ManyFramesShareOneBadTexture_RaisesOneErrorAndLoadsOnce()
    {
        CountingFailingLoader loader = Install(new FileNotFoundException("not found", "sheet.png"));
        AnimationChainListSave save = CreateSave(new[] { "sheet.png", "sheet.png", "sheet.png", "sheet.png", "sheet.png" });

        AnimationChainList list = null!;
        List<string> errors = Capture(MissingFileBehavior.ConsumeSilently, () => list = save.ToAnimationChainList());

        errors.Count.ShouldBe(1);
        errors[0].ShouldContain("sheet.png");
        loader.LoadCount.ShouldBe(1);
        list[0].Count.ShouldBe(5);
        list[0].ShouldAllBe(frame => frame.Texture == null);
        list[0].ShouldAllBe(frame => frame.TextureName == "sheet.png");
    }

    [Fact]
    public void ToAnimationChainList_TwoBadTexturesInterleaved_RaisesOneErrorForEach()
    {
        Install(new FileNotFoundException("not found"));
        AnimationChainListSave save = CreateSave(new[] { "a.png", "b.png", "a.png", "b.png", "a.png" });

        List<string> errors = Capture(MissingFileBehavior.ConsumeSilently, () => save.ToAnimationChainList());

        errors.Count.ShouldBe(2);
        errors.Count(error => error.Contains("a.png")).ShouldBe(1);
        errors.Count(error => error.Contains("b.png")).ShouldBe(1);
    }

    [Fact]
    public void ToAnimationChainList_ChainsShareOneBadTexture_RaisesOneError()
    {
        Install(new FileNotFoundException("not found"));
        AnimationChainListSave save = CreateSave(new[] { "sheet.png", "sheet.png" }, new[] { "sheet.png" });

        List<string> errors = Capture(MissingFileBehavior.ConsumeSilently, () => save.ToAnimationChainList());

        errors.Count.ShouldBe(1);
    }

    [Fact]
    public void ToAnimationChainList_LoaderReturnsNull_RaisesOneErrorPerTexture()
    {
        CountingFailingLoader loader = Install(failure: null);
        AnimationChainListSave save = CreateSave(new[] { "sheet.png", "sheet.png", "sheet.png" });

        List<string> errors = Capture(MissingFileBehavior.ConsumeSilently, () => save.ToAnimationChainList());

        errors.Count.ShouldBe(1);
        errors[0].ShouldContain("sheet.png");
        loader.LoadCount.ShouldBe(1);
    }

    [Fact]
    public void ToAnimationChainList_BadTextureLoadedAgain_RaisesTheErrorAgain()
    {
        Install(new FileNotFoundException("not found"));
        AnimationChainListSave save = CreateSave(new[] { "sheet.png", "sheet.png" });

        List<string> errors = Capture(MissingFileBehavior.ConsumeSilently, () =>
        {
            save.ToAnimationChainList();
            save.ToAnimationChainList();
        });

        errors.Count.ShouldBe(2);
    }

    // The message is the same one a lone frame raises: the texture name, then the loader's exception once.
    [Fact]
    public void ToAnimationChainList_ManyFramesShareOneBadTexture_ErrorNamesTextureAndCarriesExceptionOnce()
    {
        Install(new FileNotFoundException("the loader said so", "sheet.png"));
        AnimationChainListSave save = CreateSave(new[] { "sheet.png", "sheet.png", "sheet.png" });

        List<string> errors = Capture(MissingFileBehavior.ConsumeSilently, () => save.ToAnimationChainList());

        errors[0].ShouldContain("sheet.png");
        errors[0].ShouldContain("the loader said so");
        errors[0].Split("the loader said so").Length.ShouldBe(2);
    }

    // Under ThrowException the first bad frame throws, so there is nothing to collapse.
    [Fact]
    public void ToAnimationChainList_ThrowException_ThrowsOnFirstBadFrame()
    {
        CountingFailingLoader loader = Install(new FileNotFoundException("not found"));
        AnimationChainListSave save = CreateSave(new[] { "sheet.png", "sheet.png" });

        List<string> errors = Capture(MissingFileBehavior.ThrowException, () =>
            Should.Throw<Exception>(() => save.ToAnimationChainList()));

        errors.ShouldBeEmpty();
        loader.LoadCount.ShouldBe(1);
    }

    // With throwError false the failing chain is recorded and the next chain still reports its own failure.
    [Fact]
    public void ToAnimationChainList_ThrowExceptionWithoutThrowError_RecordsAFailureForEachChain()
    {
        Install(new FileNotFoundException("not found"));
        AnimationChainListSave save = CreateSave(new[] { "sheet.png", "sheet.png" }, new[] { "sheet.png" });

        Capture(MissingFileBehavior.ThrowException, () => save.ToAnimationChainList(throwError: false));

        save.ToRuntimeErrors.Count.ShouldBe(2);
    }

    // A frame converted on its own is its own load.
    [Fact]
    public void ToAnimationFrame_BadTexture_RaisesOneErrorEachCall()
    {
        Install(new FileNotFoundException("not found"));
        AnimationFrameSave frame = CreateSave(new[] { "sheet.png" }).AnimationChains[0].Frames[0];

        List<string> errors = Capture(MissingFileBehavior.ConsumeSilently, () =>
        {
            frame.ToAnimationFrame(loadTexture: true);
            frame.ToAnimationFrame(loadTexture: true);
        });

        errors.Count.ShouldBe(2);
    }

    private static CountingFailingLoader Install(Exception? failure)
    {
        CountingFailingLoader loader = new(failure);
        LoaderManager.Self.ContentLoader = loader;
        return loader;
    }

    private static List<string> Capture(MissingFileBehavior behavior, Action action)
    {
        GraphicalUiElement.MissingFileBehavior = behavior;
        List<string> errors = new();
        Action<string> handler = errors.Add;
        CustomSetPropertyOnRenderable.PropertyAssignmentError += handler;
        try
        {
            action();
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= handler;
        }
        return errors;
    }

    private static AnimationChainListSave CreateSave(params string[][] chains)
    {
        AnimationChainListSave save = new() { FileRelativeTextures = false };
        int chainIndex = 0;
        foreach (string[] textureNames in chains)
        {
            AnimationChainSave chain = new() { Name = "Chain" + chainIndex++ };
            foreach (string textureName in textureNames)
            {
                chain.Frames.Add(new AnimationFrameSave
                {
                    TextureName = textureName,
                    FrameLength = 0.1f,
                    LeftCoordinate = 0,
                    RightCoordinate = 1,
                    TopCoordinate = 0,
                    BottomCoordinate = 1,
                });
            }
            save.AnimationChains.Add(chain);
        }
        return save;
    }

    private sealed class CountingFailingLoader : IContentLoader
    {
        private readonly Exception? _failure;

        public int LoadCount { get; private set; }

        public CountingFailingLoader(Exception? failure)
        {
            _failure = failure;
        }

        public T? LoadContent<T>(string contentName)
        {
            LoadCount++;
            if (_failure != null)
            {
                throw _failure;
            }
            return default;
        }

        public T? TryLoadContent<T>(string contentName) => default;
    }
}
