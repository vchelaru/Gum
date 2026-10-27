using Gum.Content;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Issue #6917 - an SvgRuntime SourceFile that is missing or cannot be parsed follows
// GraphicalUiElement.MissingFileBehavior like a Sprite image does: ThrowException throws a
// FileNotFoundException naming the file, otherwise nothing is drawn and PropertyAssignmentError is raised.
public class SvgSourceFileLoadFailureTests : RegistrationIsolatedTest
{
    private const string ValidSvg =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 100">
          <rect x="0" y="0" width="200" height="100" fill="#ff0000" />
        </svg>
        """;

    private readonly MissingFileBehavior _previousBehavior = GraphicalUiElement.MissingFileBehavior;
    private readonly List<string> _errors = new();
    private readonly List<string> _tempFiles = new();

    public SvgSourceFileLoadFailureTests()
    {
        CustomSetPropertyOnRenderable.PropertyAssignmentError += _errors.Add;
    }

    public override void Dispose()
    {
        CustomSetPropertyOnRenderable.PropertyAssignmentError -= _errors.Add;
        GraphicalUiElement.MissingFileBehavior = _previousBehavior;
        LoaderManager.Self.DisposeAndClear();
        foreach (string file in _tempFiles)
        {
            File.Delete(file);
        }
        base.Dispose();
    }

    private string NewPath()
    {
        string path = Path.Combine(Path.GetTempPath(), $"gum-svg-6917-{Guid.NewGuid():N}.svg");
        _tempFiles.Add(path);
        return path;
    }

    private string WriteBadFile(string kind)
    {
        string path = NewPath();
        switch (kind)
        {
            case "corrupt":
                File.WriteAllText(path, "this is <<< not an svg at all");
                break;
            case "empty":
                File.WriteAllBytes(path, Array.Empty<byte>());
                break;
            case "html":
                File.WriteAllText(path, "<html><body><p>not an svg</p></body></html>");
                break;
        }
        return path;
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("corrupt")]
    [InlineData("empty")]
    [InlineData("html")]
    public void SourceFile_BadFile_ThrowException_ThrowsFileNotFoundNamingFileAndFormat(string kind)
    {
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;
        string path = kind == "missing" ? NewPath() : WriteBadFile(kind);
        SvgRuntime runtime = new SvgRuntime();

        FileNotFoundException exception =
            Should.Throw<FileNotFoundException>(() => runtime.SourceFile = path);

        exception.Message.ShouldContain(Path.GetFileName(path));
        exception.Message.ShouldContain("SVG");
        _errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("corrupt")]
    [InlineData("empty")]
    [InlineData("html")]
    public void SourceFile_BadFile_ConsumeSilently_LeavesEmptyAndRaisesPropertyAssignmentError(string kind)
    {
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;
        string path = kind == "missing" ? NewPath() : WriteBadFile(kind);
        SvgRuntime runtime = new SvgRuntime();

        runtime.SourceFile = path;

        runtime.Texture.ShouldBeNull();
        _errors.Count.ShouldBe(1);
        _errors[0].ShouldContain(Path.GetFileName(path));
    }

    [Fact]
    public void SourceFile_FailedThenFixedOnDisk_LoadsOnNextAssignmentOfSamePath()
    {
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;
        string path = WriteBadFile("corrupt");
        SvgRuntime runtime = new SvgRuntime();
        Should.Throw<FileNotFoundException>(() => runtime.SourceFile = path);

        File.WriteAllText(path, ValidSvg);
        runtime.SourceFile = path;

        runtime.Texture.ShouldNotBeNull();
    }

    [Fact]
    public void Load_FailedThenFixedOnDisk_IsNotCachedAsNull()
    {
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;
        string path = WriteBadFile("corrupt");
        ShapeSvgLoader.Load(path).ShouldBeNull();

        File.WriteAllText(path, ValidSvg);

        ShapeSvgLoader.Load(path).ShouldNotBeNull();
    }
}
