using Gum.Content.AnimationChain;
using Gum.Graphics.Animation;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace MonoGameGum.Tests.Content;

/// <summary>
/// Whatever a runtime's content loader throws for a frame texture it cannot load (a missing file, an
/// image no decoder reads), <see cref="AnimationFrameExtensionMethods.ToAnimationFrame(AnimationFrameSave, bool)"/>
/// handles it the same way (#6890). The loaders throw different types: the raylib loader a
/// FileNotFoundException for an undecodable image and an IOException for a missing file, the MonoGame
/// family an InvalidOperationException for an undecodable one, Skia an ArgumentException for a missing one.
/// </summary>
public class AnimationFrameTextureLoadFailureTests : BaseTestClass
{
    public static TheoryData<string> LoaderFailures => new()
    {
        "FileNotFoundException",
        "DirectoryNotFoundException",
        "IOException wrapping a missing file",
        "InvalidOperationException",
        "ArgumentException",
    };

    [Theory]
    [MemberData(nameof(LoaderFailures))]
    public void ToAnimationFrame_LoaderThrows_ConsumeSilentlyLeavesFrameWithoutTexture(string failure)
    {
        LoaderManager.Self.ContentLoader = new ThrowingContentLoader(CreateFailure(failure));
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;

        AnimationFrame frame = CreateSave("frame.png").ToAnimationFrame(loadTexture: true);

        frame.Texture.ShouldBeNull();
        frame.TextureName.ShouldBe("frame.png");
    }

    [Theory]
    [MemberData(nameof(LoaderFailures))]
    public void ToAnimationFrame_LoaderThrows_ThrowExceptionThrowsFileNotFoundNamingTexture(string failure)
    {
        Exception loaderFailure = CreateFailure(failure);
        LoaderManager.Self.ContentLoader = new ThrowingContentLoader(loaderFailure);
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;

        FileNotFoundException thrown = Should.Throw<FileNotFoundException>(() =>
            CreateSave("frame.png").ToAnimationFrame(loadTexture: true));

        thrown.Message.ShouldContain("frame.png");
        thrown.InnerException.ShouldBeSameAs(loaderFailure);
    }

    // The loader returns null for a missing file on desktop MonoGame, and AnimationFrame does not check
    // for it, so a missing frame texture there ignores ThrowException (#6896).
    [Fact]
    public void ToAnimationFrame_LoaderReturnsNull_ThrowExceptionDoesNotThrow()
    {
        LoaderManager.Self.ContentLoader = new ThrowingContentLoader(failure: null);
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;

        AnimationFrame frame = CreateSave("frame.png").ToAnimationFrame(loadTexture: true);

        frame.Texture.ShouldBeNull();
    }

    [Fact]
    public void ToAnimationFrame_LoadTextureFalse_DoesNotCallLoader()
    {
        ThrowingContentLoader loader = new(new InvalidOperationException());
        LoaderManager.Self.ContentLoader = loader;
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;

        CreateSave("frame.png").ToAnimationFrame(loadTexture: false);

        loader.LoadCount.ShouldBe(0);
    }

    private static Exception CreateFailure(string failure) => failure switch
    {
        "FileNotFoundException" => new FileNotFoundException("not found", "frame.png"),
        "DirectoryNotFoundException" => new DirectoryNotFoundException("no directory"),
        "IOException wrapping a missing file" => new IOException("Could not get the stream", new FileNotFoundException("not found")),
        "InvalidOperationException" => new InvalidOperationException("unknown image type"),
        _ => new ArgumentException("no such resource"),
    };

    private static AnimationFrameSave CreateSave(string textureName) => new()
    {
        TextureName = textureName,
        FrameLength = 0.1f,
        LeftCoordinate = 0,
        RightCoordinate = 1,
        TopCoordinate = 0,
        BottomCoordinate = 1,
    };

    private sealed class ThrowingContentLoader : IContentLoader
    {
        private readonly Exception? _failure;

        public int LoadCount { get; private set; }

        public ThrowingContentLoader(Exception? failure)
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
