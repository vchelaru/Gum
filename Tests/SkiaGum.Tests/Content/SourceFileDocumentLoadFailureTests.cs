using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using SkiaGum.Renderables;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SkiaGum.Tests.Content;

/// <summary>
/// An SvgRuntime or LottieAnimationRuntime SourceFile that cannot be loaded behaves like a Sprite's (#6903): with
/// <see cref="MissingFileBehavior.ThrowException"/> a FileNotFoundException naming the file, otherwise the document stays
/// null and PropertyAssignmentError is raised, whether the file is missing or exists but cannot be parsed. The failure is
/// never cached, so a file fixed on disk loads the next time.
/// </summary>
public class SourceFileDocumentLoadFailureTests : BaseTestClass
{
    private const string ValidSvg =
        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\"><rect width=\"10\" height=\"10\" fill=\"red\"/></svg>";
    private const string ValidLottie =
        "{\"v\":\"5.5.2\",\"fr\":30,\"ip\":0,\"op\":30,\"w\":10,\"h\":10,\"nm\":\"t\",\"ddd\":0,\"assets\":[],\"layers\":[]}";

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "GumSkiaDocFailure6903_" + Guid.NewGuid().ToString("N")) + "/";

    public SourceFileDocumentLoadFailureTests()
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

    // missing has no file, empty has zero bytes, broken holds text that is neither format,
    // wrongroot is well-formed XML / JSON that is not an SVG / Lottie document.
    public static TheoryData<string, string> FailingFiles => new()
    {
        { "svg", "missing.svg" }, { "svg", "broken.svg" }, { "svg", "empty.svg" }, { "svg", "wrongroot.svg" },
        { "lottie", "missing.json" }, { "lottie", "broken.json" }, { "lottie", "empty.json" }, { "lottie", "wrongroot.json" },
    };

    [Theory]
    [MemberData(nameof(FailingFiles))]
    public void SourceFile_FileFailsToLoad_ConsumeSilentlyLeavesNoDocumentAndRaisesError(string kind, string fileName)
    {
        string path = Prepare(fileName);

        GraphicalUiElement? runtime = null;
        List<string> errors = WithMissingFileBehavior(MissingFileBehavior.ConsumeSilently, () =>
        {
            runtime = Assign(kind, path);
        });

        HasDocument(runtime!).ShouldBeFalse();
        errors.Count.ShouldBe(1);
        errors[0].ShouldContain(fileName);
    }

    [Theory]
    [MemberData(nameof(FailingFiles))]
    public void SourceFile_FileFailsToLoad_ThrowExceptionThrowsFileNotFoundNamingFile(string kind, string fileName)
    {
        string path = Prepare(fileName);

        Exception thrown = null!;
        WithMissingFileBehavior(MissingFileBehavior.ThrowException, () =>
        {
            thrown = Should.Throw<Exception>(() => Assign(kind, path));
        });

        FindFileNotFound(thrown, fileName).ShouldNotBeNull();
    }

    [Theory]
    [InlineData("svg", "broken.svg", ValidSvg)]
    [InlineData("lottie", "broken.json", ValidLottie)]
    public void SourceFile_FileFixedAfterFailure_LoadsOnTheSameRuntime(string kind, string fileName, string fixedContent)
    {
        string path = Prepare(fileName);
        GraphicalUiElement runtime = kind == "svg" ? new SvgRuntime() : new LottieAnimationRuntime();
        WithMissingFileBehavior(MissingFileBehavior.ThrowException, () =>
        {
            Should.Throw<Exception>(() => SetSourceFile(runtime, path));
        });

        File.WriteAllText(path, fixedContent);
        SetSourceFile(runtime, path);

        HasDocument(runtime).ShouldBeTrue();
    }

    [Theory]
    [InlineData("svg", "broken.svg", ValidSvg)]
    [InlineData("lottie", "broken.json", ValidLottie)]
    public void SourceFile_FileFixedAfterSilentFailure_ReassigningSamePathLoads(string kind, string fileName, string fixedContent)
    {
        string path = Prepare(fileName);
        GraphicalUiElement runtime = kind == "svg" ? new SvgRuntime() : new LottieAnimationRuntime();
        WithMissingFileBehavior(MissingFileBehavior.ConsumeSilently, () => SetSourceFile(runtime, path));
        HasDocument(runtime).ShouldBeFalse();

        File.WriteAllText(path, fixedContent);
        SetSourceFile(runtime, path);

        HasDocument(runtime).ShouldBeTrue();
    }

    // How a project-loaded Svg or Lottie gets its file. A Sprite throws the FileNotFoundException itself, not a wrapper.
    [Theory]
    [InlineData("svg", "missing.svg")]
    [InlineData("svg", "broken.svg")]
    [InlineData("lottie", "missing.json")]
    [InlineData("lottie", "broken.json")]
    public void SetPropertySourceFile_FileFailsToLoad_ThrowsFileNotFoundNamingFileDirectly(string kind, string fileName)
    {
        string path = Prepare(fileName);
        GraphicalUiElement runtime = kind == "svg" ? new SvgRuntime() : new LottieAnimationRuntime();

        WithMissingFileBehavior(MissingFileBehavior.ThrowException, () =>
        {
            FileNotFoundException thrown = Should.Throw<FileNotFoundException>(() => runtime.SetProperty("SourceFile", path));
            thrown.Message.ShouldContain(fileName);
        });
    }

    [Fact]
    public void SourceFile_PlainElementWithVectorSprite_UnparseableSvgFollowsMissingFileBehavior()
    {
        string path = Prepare("broken.svg");
        GraphicalUiElement plain = new GraphicalUiElement();
        plain.SetContainedObject(new VectorSprite());

        List<string> errors = WithMissingFileBehavior(MissingFileBehavior.ConsumeSilently, () =>
        {
            plain.SetProperty("SourceFile", path);
        });
        errors.Count.ShouldBe(1);
        ((VectorSprite)plain.RenderableComponent).Texture.ShouldBeNull();

        WithMissingFileBehavior(MissingFileBehavior.ThrowException, () =>
        {
            Exception thrown = Should.Throw<Exception>(() => plain.SetProperty("SourceFile", path));
            FindFileNotFound(thrown, "broken.svg").ShouldNotBeNull();
        });
    }

    [Theory]
    [InlineData("svg", "ok.svg", ValidSvg)]
    [InlineData("lottie", "ok.json", ValidLottie)]
    public void SourceFile_ValidFile_LoadsDocumentAndRaisesNoError(string kind, string fileName, string content)
    {
        string path = _directory + fileName;
        File.WriteAllText(path, content);

        GraphicalUiElement? runtime = null;
        List<string> errors = WithMissingFileBehavior(MissingFileBehavior.ThrowException, () =>
        {
            runtime = Assign(kind, path);
        });

        HasDocument(runtime!).ShouldBeTrue();
        errors.ShouldBeEmpty();
    }

    private string Prepare(string fileName)
    {
        string path = _directory + fileName;
        if (fileName.StartsWith("broken"))
        {
            File.WriteAllText(path, "this is not a document");
        }
        else if (fileName.StartsWith("empty"))
        {
            File.WriteAllBytes(path, new byte[0]);
        }
        else if (fileName.StartsWith("wrongroot"))
        {
            File.WriteAllText(path, fileName.EndsWith(".svg") ? "<html></html>" : "{}");
        }
        return path;
    }

    private static GraphicalUiElement Assign(string kind, string path)
    {
        GraphicalUiElement runtime = kind == "svg" ? new SvgRuntime() : new LottieAnimationRuntime();
        SetSourceFile(runtime, path);
        return runtime;
    }

    private static void SetSourceFile(GraphicalUiElement runtime, string path)
    {
        if (runtime is SvgRuntime svg)
        {
            svg.SourceFile = path;
        }
        else
        {
            ((LottieAnimationRuntime)runtime).SourceFile = path;
        }
    }

    private static bool HasDocument(GraphicalUiElement runtime) =>
        runtime is SvgRuntime svg ? svg.Texture != null : ((LottieAnimationRuntime)runtime).Animation != null;

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
