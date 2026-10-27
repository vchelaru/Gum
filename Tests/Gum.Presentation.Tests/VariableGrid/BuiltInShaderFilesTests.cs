using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.VariableGrid;

public class BuiltInShaderFilesTests : BaseTestClass
{
    private readonly string _root;
    private readonly string _builtInFolder;
    private readonly string _projectDirectory;
    private readonly Mock<IProjectState> _projectState = new Mock<IProjectState>();

    public BuiltInShaderFilesTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "GumBuiltInShaders_" + Guid.NewGuid().ToString("N"));
        _builtInFolder = Path.Combine(_root, "builtin");
        _projectDirectory = Path.Combine(_root, "project") + "/";
        Directory.CreateDirectory(_builtInFolder);
        Directory.CreateDirectory(_projectDirectory);
        _projectState.SetupGet(state => state.ProjectDirectory).Returns(_projectDirectory);
    }

    public override void Dispose()
    {
        base.Dispose();
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void GetOffered_ListsTheShippedShadersAlphabetically_IgnoringOtherFiles()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Sepia.slang"), "sepia");
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "blur");
        File.WriteAllText(Path.Combine(_builtInFolder, "Old.fx"), "old");
        File.WriteAllText(Path.Combine(_builtInFolder, "readme.txt"), "text");
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _builtInFolder);

        IReadOnlyList<string> offered = sut.GetOffered(Array.Empty<string>());

        offered.ShouldBe(new[] { "Blur.slang", "Old.fx", "Sepia.slang" });
    }

    [Fact]
    public void GetOffered_FolderIsMissing_OffersNothing()
    {
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, Path.Combine(_root, "nowhere"));

        sut.GetOffered(Array.Empty<string>()).ShouldBeEmpty();
    }

    [Fact]
    public void GetOffered_ProjectAlreadyHoldsACopy_LeavesThatShaderOut_WhateverTheSpelling()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "blur");
        File.WriteAllText(Path.Combine(_builtInFolder, "Sepia.slang"), "sepia");
        File.WriteAllText(Path.Combine(_builtInFolder, "Grayscale.slang"), "gray");
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _builtInFolder);

        IReadOnlyList<string> offered = sut.GetOffered(new[] { "./Shaders/Blur.slang", "shaders/sepia.slang", "Elsewhere/Grayscale.slang" });

        // A same-named file in another folder is a different file, so that shader is still offered.
        offered.ShouldBe(new[] { "Grayscale.slang" });
    }

    [Fact]
    public void CopyIntoProject_PutsTheShaderInTheShadersFolder_AndReturnsItsRelativePath()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "blur source");
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _builtInFolder);

        string? path = sut.CopyIntoProject("Blur.slang");

        path.ShouldBe("Shaders/Blur.slang");
        File.ReadAllText(Path.Combine(_projectDirectory, "Shaders", "Blur.slang")).ShouldBe("blur source");
    }

    [Fact]
    public void CopyIntoProject_ACopyIsAlreadyThere_KeepsTheUsersEdits()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "shipped");
        Directory.CreateDirectory(Path.Combine(_projectDirectory, "Shaders"));
        File.WriteAllText(Path.Combine(_projectDirectory, "Shaders", "Blur.slang"), "edited by the user");
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _builtInFolder);

        string? path = sut.CopyIntoProject("Blur.slang");

        path.ShouldBe("Shaders/Blur.slang");
        File.ReadAllText(Path.Combine(_projectDirectory, "Shaders", "Blur.slang")).ShouldBe("edited by the user");
    }

    [Fact]
    public void CopyIntoProject_ProjectIsNotSavedYet_ReturnsNull()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "blur");
        _projectState.SetupGet(state => state.ProjectDirectory).Returns((string?)null);
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _builtInFolder);

        sut.CopyIntoProject("Blur.slang").ShouldBeNull();
    }

    [Theory]
    [InlineData("Nope.slang")]
    [InlineData("../project/Project.gumx")]
    [InlineData("Sub/Blur.slang")]
    public void CopyIntoProject_NameThatIsNotAShippedShader_ReturnsNullAndCopiesNothing(string name)
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "blur");
        File.WriteAllText(_projectDirectory + "Project.gumx", "project");
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _builtInFolder);

        sut.CopyIntoProject(name).ShouldBeNull();
        Directory.Exists(Path.Combine(_projectDirectory, "Shaders")).ShouldBeFalse();
    }
}
