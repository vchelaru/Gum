using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.EditorTab;
using Shouldly;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// An edit that names a file already in the content cache must rebuild with a full reload, whatever the canvas
/// has built for the edited instance: disposing only that file's entry left cached animation lists holding the
/// disposed texture, which crashed the canvas on the next draw.
/// </summary>
public class EditedFileReloadTests : IDisposable
{
    private readonly string _oldRelativeDirectory = FileManager.RelativeDirectory;
    private readonly string _projectDirectory = Path.Combine(Path.GetTempPath(), "EditedFileReloadTests", Guid.NewGuid().ToString("N")) + Path.DirectorySeparatorChar;

    public EditedFileReloadTests()
    {
        FileManager.RelativeDirectory = _projectDirectory;
    }

    public void Dispose()
    {
        FileManager.RelativeDirectory = _oldRelativeDirectory;
    }

    private static VariableSave FileVariable() => new VariableSave { Name = "SourceFile", Type = "string", IsFile = true };

    [Fact]
    public void IsCachedFileEdit_FileVariableNamingACachedFile_IsTrue()
    {
        EditedFileReload.IsCachedFileEdit(FileVariable(), "Hero.png", key => true).ShouldBeTrue();
    }

    [Fact]
    public void IsCachedFileEdit_FileVariableNamingAFileNotCached_IsFalse()
    {
        EditedFileReload.IsCachedFileEdit(FileVariable(), "Hero.png", key => false).ShouldBeFalse();
    }

    [Fact]
    public void IsCachedFileEdit_VariableThatIsNotAFile_IsFalseWithoutLookingInTheCache()
    {
        VariableSave variable = new VariableSave { Name = "Text", Type = "string" };
        bool asked = false;

        EditedFileReload.IsCachedFileEdit(variable, "Hero.png", key => asked = true).ShouldBeFalse();

        asked.ShouldBeFalse();
    }

    [Fact]
    public void IsCachedFileEdit_NoVariableOrNoStringValue_IsFalse()
    {
        EditedFileReload.IsCachedFileEdit(null, "Hero.png", key => true).ShouldBeFalse();
        EditedFileReload.IsCachedFileEdit(FileVariable(), null, key => true).ShouldBeFalse();
        EditedFileReload.IsCachedFileEdit(FileVariable(), 5, key => true).ShouldBeFalse();
    }

    [Fact]
    public void IsCachedFileEdit_RelativePath_IsLookedUpAsTheAbsolutePathTheLoaderCaches()
    {
        string? looked = null;

        EditedFileReload.IsCachedFileEdit(FileVariable(), "../Art/Hero.png", key =>
        {
            looked = key;
            return false;
        });

        string expected = FileManager.RemoveDotDotSlash(FileManager.Standardize("../Art/Hero.png", preserveCase: true, makeAbsolute: true));
        looked.ShouldBe(expected);
        Path.IsPathRooted(looked).ShouldBeTrue();
    }

    [Fact]
    public void IsCachedFileEdit_LookupThatThrows_IsFalseAndDoesNotThrow()
    {
        EditedFileReload.IsCachedFileEdit(FileVariable(), "Hero.png", key => throw new ArgumentException("bad path")).ShouldBeFalse();
    }
}
