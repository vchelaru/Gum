using CodeOutputPlugin;
using Gum.ProjectServices.CodeGeneration;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Tests for <see cref="CodeFileLocationChange"/>, which decides whether a code settings edit moves
/// where code files belong, and describes it for the migration prompt (issue #5846).
/// </summary>
public class CodeFileLocationChangeTests
{
    [Fact]
    public void Describe_NamesEachSettingThatMovesFiles_FromOldToNew()
    {
        CodeOutputProjectSettings before = new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGame, CodeProjectRoot = "../Old/", GeneratedCodeFolder = "" };
        CodeOutputProjectSettings after = new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGameForms, CodeProjectRoot = "../New/", GeneratedCodeFolder = "Generated" };

        string? description = new CodeFileLocationChange().Describe(before, after);

        description.ShouldBe(
            "Output Library from MonoGame (deprecated) to Gum Forms (recommended), " +
            "Code Project Root from ../Old/ to ../New/, " +
            "Generated Code Folder from (none) to Generated");
    }

    [Fact]
    public void Describe_ReturnsNull_WhenNoSettingThatMovesFilesChanged()
    {
        // The namespace changes what is inside the files, not where they are.
        CodeOutputProjectSettings before = new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGameForms, RootNamespace = "Old" };
        CodeOutputProjectSettings after = new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGameForms, RootNamespace = "New" };

        new CodeFileLocationChange().Describe(before, after).ShouldBeNull();
    }
}
