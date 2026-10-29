using System.Text;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using Shouldly;
using ToolsUtilities;

namespace Gum.Presentation.Tests.VariableGrid;

public class BuiltInShaderFilesTests : BaseTestClass
{
    private readonly string _root;
    private readonly string _builtInFolder;
    private readonly string _projectDirectory;
    private readonly Mock<IProjectState> _projectState = new Mock<IProjectState>();
    private readonly Mock<IDialogService> _dialogService = new Mock<IDialogService>();
    private readonly Mock<IOutputManager> _outputManager = new Mock<IOutputManager>();

    public BuiltInShaderFilesTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "GumBuiltInShaders_" + Guid.NewGuid().ToString("N"));
        _builtInFolder = Path.Combine(_root, "builtin");
        _projectDirectory = Path.Combine(_root, "project") + "/";
        Directory.CreateDirectory(_builtInFolder);
        Directory.CreateDirectory(_projectDirectory);
        _projectState.SetupGet(state => state.ProjectDirectory).Returns(_projectDirectory);
        // A test that sets up no answer gets a dialog the user closes, as an unset mock used to answer "not yes".
        AnswerChoice(null);
    }

    private readonly List<ChoiceDialogViewModel> _shown = new List<ChoiceDialogViewModel>();

    // IDialogService.Show has an out parameter, which Moq can only populate through a callback
    // delegate whose signature mirrors the method.
    private delegate void ShowChoiceCallback(Action<ChoiceDialogViewModel>? initializer, out ChoiceDialogViewModel viewModel);

    private const string ReplaceChoice = "Replace with built-in";
    private const string KeepChoice = "Keep my copy";

    /// <summary>Answers the next choice dialog with the option text <paramref name="pick"/>, or closes it (null).</summary>
    private void AnswerChoice(string? pick)
    {
        _dialogService
            .Setup(dialogs => dialogs.Show(It.IsAny<Action<ChoiceDialogViewModel>?>(), out It.Ref<ChoiceDialogViewModel>.IsAny))
            .Callback(new ShowChoiceCallback((Action<ChoiceDialogViewModel>? initializer, out ChoiceDialogViewModel viewModel) =>
            {
                viewModel = new ChoiceDialogViewModel();
                initializer?.Invoke(viewModel);
                _shown.Add(viewModel);
                if (pick == null)
                {
                    viewModel.NegativeCommand.Execute(null);
                }
                else
                {
                    viewModel.SelectedValue = pick;
                    viewModel.AffirmativeCommand.Execute(null);
                }
            }))
            .Returns(pick != null);
    }

    private void AnswerPrompt(MessageDialogResult answer) =>
        AnswerChoice(answer switch
        {
            MessageDialogResult.Affirmative => ReplaceChoice,
            MessageDialogResult.Negative => KeepChoice,
            _ => null,
        });

    private void VerifyAskedTimes(Times times) =>
        _dialogService.Verify(
            dialogs => dialogs.Show(It.IsAny<Action<ChoiceDialogViewModel>?>(), out It.Ref<ChoiceDialogViewModel>.IsAny),
            times);

    // Also catches the old yes/no message box, so a "no prompt" test cannot pass just because the
    // prompt moved to another dialog.
    private void VerifyNothingAsked()
    {
        VerifyAskedTimes(Times.Never());
        _dialogService.Verify(
            dialogs => dialogs.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()),
            Times.Never());
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
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);

        IReadOnlyList<string> offered = sut.GetOffered(Array.Empty<string>());

        offered.ShouldBe(new[] { "Blur.slang", "Old.fx", "Sepia.slang" });
    }

    [Fact]
    public void GetOffered_FolderIsMissing_OffersNothing()
    {
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, Path.Combine(_root, "nowhere"));

        sut.GetOffered(Array.Empty<string>()).ShouldBeEmpty();
    }

    [Fact]
    public void GetOffered_ProjectAlreadyHoldsACopy_LeavesThatShaderOut_WhateverTheSpelling()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "blur");
        File.WriteAllText(Path.Combine(_builtInFolder, "Sepia.slang"), "sepia");
        File.WriteAllText(Path.Combine(_builtInFolder, "Grayscale.slang"), "gray");
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);

        IReadOnlyList<string> offered = sut.GetOffered(new[] { "./Shaders/Blur.slang", "shaders/sepia.slang", "Elsewhere/Grayscale.slang" });

        // A same-named file in another folder is a different file, so that shader is still offered.
        offered.ShouldBe(new[] { "Grayscale.slang" });
    }

    [Fact]
    public void CopyIntoProject_PutsTheShaderInTheShadersFolder_AndReturnsItsRelativePath()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "blur source");
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);

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
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);

        string? path = sut.CopyIntoProject("Blur.slang");

        path.ShouldBe("Shaders/Blur.slang");
        File.ReadAllText(Path.Combine(_projectDirectory, "Shaders", "Blur.slang")).ShouldBe("edited by the user");
    }

    [Fact]
    public void CopyIntoProject_NoCopyYet_CopiesWithoutAsking()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "shipped");
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);

        sut.CopyIntoProject("Blur.slang");

        File.ReadAllText(Path.Combine(_projectDirectory, "Shaders", "Blur.slang")).ShouldBe("shipped");
        VerifyAskedTimes(Times.Never());
    }

    [Fact]
    public void CopyIntoProject_CopyMatchesTheShippedFile_SaysNothing()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "shipped");
        Directory.CreateDirectory(Path.Combine(_projectDirectory, "Shaders"));
        File.WriteAllText(Path.Combine(_projectDirectory, "Shaders", "Blur.slang"), "shipped");
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);

        string? path = sut.CopyIntoProject("Blur.slang");

        path.ShouldBe("Shaders/Blur.slang");
        VerifyAskedTimes(Times.Never());
    }

    [Fact]
    public void CopyIntoProject_CopyDiffers_AnsweringYesReplacesItWithTheShippedFile()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "shipped");
        Directory.CreateDirectory(Path.Combine(_projectDirectory, "Shaders"));
        File.WriteAllText(Path.Combine(_projectDirectory, "Shaders", "Blur.slang"), "old copy");
        AnswerPrompt(MessageDialogResult.Affirmative);
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);

        string? path = sut.CopyIntoProject("Blur.slang");

        path.ShouldBe("Shaders/Blur.slang");
        File.ReadAllText(Path.Combine(_projectDirectory, "Shaders", "Blur.slang")).ShouldBe("shipped");
        VerifyAskedTimes(Times.Once());
        _shown.Single().Message.ShouldContain("Shaders/Blur.slang");
    }

    [Fact]
    public void CopyIntoProject_ReplacingADifferingCopy_TellsTheWatcherToIgnoreTheWrite()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "shipped");
        Directory.CreateDirectory(Path.Combine(_projectDirectory, "Shaders"));
        string copyPath = Path.Combine(_projectDirectory, "Shaders", "Blur.slang");
        File.WriteAllText(copyPath, "old copy");
        AnswerPrompt(MessageDialogResult.Affirmative);
        Mock<IFileWatchManager> watch = new Mock<IFileWatchManager>();
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder, new Lazy<IFileWatchManager>(() => watch.Object));

        sut.CopyIntoProject("Blur.slang");

        watch.Verify(w => w.IgnoreNextChangeUntil(new FilePath(Path.GetFullPath(copyPath)), It.IsAny<DateTime?>()), Times.Once);
    }

    [Fact]
    public void CopyIntoProject_KeepingADifferingCopy_DoesNotTouchTheWatcher()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "shipped");
        Directory.CreateDirectory(Path.Combine(_projectDirectory, "Shaders"));
        File.WriteAllText(Path.Combine(_projectDirectory, "Shaders", "Blur.slang"), "old copy");
        AnswerPrompt(MessageDialogResult.Negative);
        Mock<IFileWatchManager> watch = new Mock<IFileWatchManager>();
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder, new Lazy<IFileWatchManager>(() => watch.Object));

        sut.CopyIntoProject("Blur.slang");

        watch.Verify(w => w.IgnoreNextChangeUntil(It.IsAny<FilePath>(), It.IsAny<DateTime?>()), Times.Never);
    }

    [Theory]
    [InlineData(MessageDialogResult.Negative)]
    [InlineData(MessageDialogResult.Canceled)]
    public void CopyIntoProject_CopyDiffers_AnsweringNoKeepsTheProjectsCopyAndStillReturnsIt(MessageDialogResult answer)
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "shipped");
        Directory.CreateDirectory(Path.Combine(_projectDirectory, "Shaders"));
        File.WriteAllText(Path.Combine(_projectDirectory, "Shaders", "Blur.slang"), "old copy");
        AnswerPrompt(answer);
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);

        string? path = sut.CopyIntoProject("Blur.slang");

        path.ShouldBe("Shaders/Blur.slang");
        File.ReadAllText(Path.Combine(_projectDirectory, "Shaders", "Blur.slang")).ShouldBe("old copy");
        VerifyAskedTimes(Times.Once());
    }

    [Fact]
    public void CopyIntoProject_CopyIsReadOnly_AnsweringYes_KeepsItReportsTheFailureAndStillReturnsThePath()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "shipped");
        string copy = Path.Combine(_projectDirectory, "Shaders", "Blur.slang");
        Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
        File.WriteAllText(copy, "old copy");
        File.SetAttributes(copy, FileAttributes.ReadOnly);
        try
        {
            AnswerPrompt(MessageDialogResult.Affirmative);
            BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);

            string? path = sut.CopyIntoProject("Blur.slang");

            path.ShouldBe("Shaders/Blur.slang");
            File.ReadAllText(copy).ShouldBe("old copy");
            _outputManager.Verify(output => output.AddError(It.Is<string>(message => message.Contains("Shaders/Blur.slang"))), Times.Once());
        }
        finally
        {
            File.SetAttributes(copy, FileAttributes.Normal);
        }
    }

    [Fact]
    public void CopyIntoProject_CopyCannotBeRead_DoesNotAskOrReplaceReportsTheFailureAndStillReturnsThePath()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "shipped");
        string copy = Path.Combine(_projectDirectory, "Shaders", "Blur.slang");
        Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
        File.WriteAllText(copy, "old copy");
        AnswerPrompt(MessageDialogResult.Affirmative);
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);
        string? path;

        // Another process holding the file open exclusively.
        using (new FileStream(copy, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            path = sut.CopyIntoProject("Blur.slang");
        }

        path.ShouldBe("Shaders/Blur.slang");
        File.ReadAllText(copy).ShouldBe("old copy");
        VerifyAskedTimes(Times.Never());
        _outputManager.Verify(output => output.AddError(It.Is<string>(message => message.Contains("Shaders/Blur.slang"))), Times.Once());
    }

    [Fact]
    public void CopyIntoProject_PickedAgainAfterTheShippedFileChanged_AsksThenReplaces()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Outline.slang"), "version 1");
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);
        sut.CopyIntoProject("Outline.slang");

        // The assignment is removed, but the file stays. Picking it again with nothing new shipped is quiet.
        sut.CopyIntoProject("Outline.slang");
        VerifyAskedTimes(Times.Never());

        // A newer Gum ships a different Outline.
        File.WriteAllText(Path.Combine(_builtInFolder, "Outline.slang"), "version 2");
        AnswerPrompt(MessageDialogResult.Affirmative);
        sut.CopyIntoProject("Outline.slang");

        VerifyAskedTimes(Times.Once());
        File.ReadAllText(Path.Combine(_projectDirectory, "Shaders", "Outline.slang")).ShouldBe("version 2");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CopyIntoProject_CopyDiffersOnlyInLineEndings_SaysNothingAndLeavesTheCopyAlone(bool copyHasCrLf)
    {
        byte[] lf = Encoding.UTF8.GetBytes("float a;\nfloat b;\n");
        byte[] crlf = Encoding.UTF8.GetBytes("float a;\r\nfloat b;\r\n");
        File.WriteAllBytes(Path.Combine(_builtInFolder, "Blur.slang"), copyHasCrLf ? lf : crlf);
        string copy = Path.Combine(_projectDirectory, "Shaders", "Blur.slang");
        Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
        File.WriteAllBytes(copy, copyHasCrLf ? crlf : lf);
        AnswerChoice(ReplaceChoice);
        _dialogService
            .Setup(dialogs => dialogs.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()))
            .Returns(MessageDialogResult.Affirmative);
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);

        string? path = sut.CopyIntoProject("Blur.slang");

        path.ShouldBe("Shaders/Blur.slang");
        VerifyNothingAsked();
        File.ReadAllBytes(copy).ShouldBe(copyHasCrLf ? crlf : lf);
    }

    [Fact]
    public void CopyIntoProject_CopyDiffersInContentAndLineEndings_StillAsks()
    {
        File.WriteAllBytes(Path.Combine(_builtInFolder, "Blur.slang"), Encoding.UTF8.GetBytes("float a;\nfloat b;\n"));
        string copy = Path.Combine(_projectDirectory, "Shaders", "Blur.slang");
        Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
        File.WriteAllBytes(copy, Encoding.UTF8.GetBytes("float a;\r\nfloat c;\r\n"));
        AnswerChoice(KeepChoice);
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);

        sut.CopyIntoProject("Blur.slang");

        VerifyAskedTimes(Times.Once());
    }

    [Fact]
    public void CopyIntoProject_CopyDiffers_AnsweringReplaceCopiesTheShippedFilesBytesUnchanged()
    {
        byte[] shipped = Encoding.UTF8.GetBytes("float a;\r\nfloat b;\r\n");
        File.WriteAllBytes(Path.Combine(_builtInFolder, "Blur.slang"), shipped);
        string copy = Path.Combine(_projectDirectory, "Shaders", "Blur.slang");
        Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
        File.WriteAllBytes(copy, Encoding.UTF8.GetBytes("float a;\nfloat c;\n"));
        AnswerChoice(ReplaceChoice);
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);

        string? path = sut.CopyIntoProject("Blur.slang");

        path.ShouldBe("Shaders/Blur.slang");
        File.ReadAllBytes(copy).ShouldBe(shipped);
    }

    [Fact]
    public void CopyIntoProject_CopyDiffers_AnsweringKeepLeavesTheFileAndStillReturnsThePath()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "shipped");
        string copy = Path.Combine(_projectDirectory, "Shaders", "Blur.slang");
        Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
        File.WriteAllText(copy, "my edits");
        AnswerChoice(KeepChoice);
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);

        string? path = sut.CopyIntoProject("Blur.slang");

        path.ShouldBe("Shaders/Blur.slang");
        File.ReadAllText(copy).ShouldBe("my edits");
        _outputManager.Verify(output => output.AddError(It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public void CopyIntoProject_CopyDiffers_ClosingTheDialogKeepsTheCopyAndStillReturnsThePath()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "shipped");
        string copy = Path.Combine(_projectDirectory, "Shaders", "Blur.slang");
        Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
        File.WriteAllText(copy, "my edits");
        AnswerChoice(null);
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);

        string? path = sut.CopyIntoProject("Blur.slang");

        path.ShouldBe("Shaders/Blur.slang");
        File.ReadAllText(copy).ShouldBe("my edits");
        _outputManager.Verify(output => output.AddError(It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public void CopyIntoProject_CopyDiffers_OffersExactlyReplaceOrKeep_KeepFirstSoEnterNeverOverwrites_AndCanBeClosed()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "shipped");
        Directory.CreateDirectory(Path.Combine(_projectDirectory, "Shaders"));
        File.WriteAllText(Path.Combine(_projectDirectory, "Shaders", "Blur.slang"), "my edits");
        AnswerChoice(KeepChoice);
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);

        sut.CopyIntoProject("Blur.slang");

        ChoiceDialogViewModel dialog = _shown.Single();
        dialog.OptionValues.ShouldBe(new[] { KeepChoice, ReplaceChoice });
        dialog.CanCancel.ShouldBeTrue();
    }

    [Fact]
    public void CopyIntoProject_CopyDiffers_TheMessageSaysWhatEachChoiceDoes()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "shipped");
        Directory.CreateDirectory(Path.Combine(_projectDirectory, "Shaders"));
        File.WriteAllText(Path.Combine(_projectDirectory, "Shaders", "Blur.slang"), "my edits");
        AnswerChoice(KeepChoice);
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);

        sut.CopyIntoProject("Blur.slang");

        ChoiceDialogViewModel dialog = _shown.Single();
        dialog.Title.ShouldBe("Built-in shader differs");
        dialog.Message.ShouldBe(
            "Your project's Shaders/Blur.slang differs from the built-in Blur.slang.\n\n" +
            "Replace with built-in: overwrites your file with the built-in one.\n" +
            "Keep my copy: uses your file as it is.\n\n" +
            "The shader is assigned either way.");
    }

    [Fact]
    public void CopyIntoProject_ProjectIsNotSavedYet_ReturnsNull()
    {
        File.WriteAllText(Path.Combine(_builtInFolder, "Blur.slang"), "blur");
        _projectState.SetupGet(state => state.ProjectDirectory).Returns((string?)null);
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);

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
        BuiltInShaderFiles sut = new BuiltInShaderFiles(_projectState.Object, _dialogService.Object, _outputManager.Object, _builtInFolder);

        sut.CopyIntoProject(name).ShouldBeNull();
        Directory.Exists(Path.Combine(_projectDirectory, "Shaders")).ShouldBeFalse();
    }
}
