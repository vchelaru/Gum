using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SkiaGum.Tests.Content;

/// <summary>
/// A Sprite or NineSlice SourceFile that cannot be loaded behaves one way whether the file is missing
/// or exists but cannot be decoded (#6889), matching MonoGame/KNI/FNA and raylib: with
/// <see cref="MissingFileBehavior.ThrowException"/> a FileNotFoundException naming the file, otherwise
/// the texture stays null (Sprite also raises PropertyAssignmentError), and the failure is never cached.
/// </summary>
public class SourceFileLoadFailureTests : BaseTestClass
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "GumSkiaSourceFileFailure_" + Guid.NewGuid().ToString("N")) + "/";

    public SourceFileLoadFailureTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        LoaderManager.Self.ContentLoader = new ContentLoader();
        LoaderManager.Self.CacheTextures = true;
        Directory.CreateDirectory(_directory);
    }

    public override void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
        base.Dispose();
    }

    // missing.png has no file, empty.png has zero bytes, the others hold bytes no decoder reads.
    public static TheoryData<string> FailingFiles => new() { "missing.png", "broken.png", "broken.jpg", "empty.png" };

    [Theory]
    [MemberData(nameof(FailingFiles))]
    public void SpriteSourceFile_FileFailsToLoad_ConsumeSilentlyLeavesNoTextureAndRaisesError(string fileName)
    {
        string path = Prepare(fileName);

        SpriteRuntime? sprite = null;
        List<string> errors = WithMissingFileBehavior(MissingFileBehavior.ConsumeSilently, () =>
        {
            sprite = new SpriteRuntime { SourceFileName = path };
        });

        sprite!.Texture.ShouldBeNull();
        errors.Count.ShouldBe(1);
        errors[0].ShouldContain(fileName);
    }

    [Theory]
    [MemberData(nameof(FailingFiles))]
    public void SpriteSourceFile_FileFailsToLoad_ThrowExceptionThrowsFileNotFoundNamingFile(string fileName)
    {
        string path = Prepare(fileName);

        Exception thrown = null!;
        WithMissingFileBehavior(MissingFileBehavior.ThrowException, () =>
        {
            thrown = Should.Throw<Exception>(() => new SpriteRuntime { SourceFileName = path });
        });

        FindFileNotFound(thrown, fileName).ShouldNotBeNull();
    }

    [Theory]
    [MemberData(nameof(FailingFiles))]
    public void NineSliceSourceFile_FileFailsToLoad_ConsumeSilentlyLeavesNoTextureWithoutThrowing(string fileName)
    {
        string path = Prepare(fileName);

        NineSliceRuntime? nineSlice = null;
        WithMissingFileBehavior(MissingFileBehavior.ConsumeSilently, () =>
        {
            nineSlice = new NineSliceRuntime { SourceFileName = path };
        });

        nineSlice!.Texture.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(FailingFiles))]
    public void NineSliceSourceFile_FileFailsToLoad_ThrowExceptionThrowsFileNotFoundNamingFile(string fileName)
    {
        string path = Prepare(fileName);

        Exception thrown = null!;
        WithMissingFileBehavior(MissingFileBehavior.ThrowException, () =>
        {
            thrown = Should.Throw<Exception>(() => new NineSliceRuntime { SourceFileName = path });
        });

        FindFileNotFound(thrown, fileName).ShouldNotBeNull();
    }

    [Fact]
    public void LoadContent_UndecodableFileOnDisk_ThrowsAndLaterLoadOfFixedFileSucceeds()
    {
        string path = Prepare("broken.png");
        ContentLoader loader = new ContentLoader();

        FileNotFoundException thrown = Should.Throw<FileNotFoundException>(() => loader.LoadContent<SKBitmap>(path));
        thrown.Message.ShouldContain("broken.png");
        loader.TryLoadContent<SKBitmap>(path).ShouldBeNull();

        File.WriteAllBytes(path, CreatePng());
        SKBitmap? loaded = loader.LoadContent<SKBitmap>(path);

        loaded.ShouldNotBeNull();
        loaded.Width.ShouldBe(2);
    }

    [Fact]
    public void SpriteSourceFile_ValidFile_LoadsTextureAndRaisesNoError()
    {
        string path = _directory + "ok.png";
        File.WriteAllBytes(path, CreatePng());

        SpriteRuntime? sprite = null;
        List<string> errors = WithMissingFileBehavior(MissingFileBehavior.ThrowException, () =>
        {
            sprite = new SpriteRuntime { SourceFileName = path };
        });

        sprite!.Texture.ShouldNotBeNull();
        errors.ShouldBeEmpty();
    }

    private string Prepare(string fileName)
    {
        string path = _directory + fileName;
        if (fileName.StartsWith("broken"))
        {
            File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        }
        else if (fileName.StartsWith("empty"))
        {
            File.WriteAllBytes(path, new byte[0]);
        }
        return path;
    }

    private static byte[] CreatePng()
    {
        using SKBitmap source = new SKBitmap(2, 2);
        using SKImage image = SKImage.FromBitmap(source);
        using SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }

    private static Exception? FindFileNotFound(Exception? exception, string fileName)
    {
        while (exception != null)
        {
            if (exception is FileNotFoundException && exception.Message.Contains(fileName))
            {
                return exception;
            }
            exception = exception.InnerException;
        }
        return null;
    }

    private static List<string> WithMissingFileBehavior(MissingFileBehavior behavior, Action action)
    {
        MissingFileBehavior saved = GraphicalUiElement.MissingFileBehavior;
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
            GraphicalUiElement.MissingFileBehavior = saved;
        }
        return errors;
    }
}
