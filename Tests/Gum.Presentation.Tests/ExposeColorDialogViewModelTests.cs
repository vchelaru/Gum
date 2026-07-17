using System;
using System.Collections.Generic;
using Gum.Dialogs;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for ExposeColorDialogViewModel, relocated out of Gum.csproj
/// into the headless Gum.Presentation assembly (ADR-0005, #3754) - migrated verbatim from
/// GumToolUnitTests alongside the view model move.
/// </summary>
public class ExposeColorDialogViewModelTests
{
    private static readonly string[] RgbRoots = { "Red", "Green", "Blue" };

    private static ExposeColorDialogViewModel Make(string baseName, IReadOnlyList<string> roots,
        Func<string, string?>? validate = null)
    {
        return new ExposeColorDialogViewModel(baseName, roots, validate ?? (_ => null));
    }

    [Fact]
    public void CanExecuteAffirmative_ShouldBeFalse_WhenANameIsInvalid()
    {
        ExposeColorDialogViewModel vm = Make("MyColor", RgbRoots, _ => "nope");
        vm.CanExecuteAffirmative().ShouldBeFalse();
    }

    [Fact]
    public void CanExecuteAffirmative_ShouldBeTrue_WhenAllNamesValid()
    {
        ExposeColorDialogViewModel vm = Make("MyColor", RgbRoots);
        vm.CanExecuteAffirmative().ShouldBeTrue();
    }

    [Fact]
    public void Error_ShouldReturnFirstFailure_WhenANameInvalid()
    {
        ExposeColorDialogViewModel vm = Make("MyColor", RgbRoots,
            name => name == "MyColorGreen" ? "bad green" : null);
        vm.Error.ShouldBe("bad green");
    }

    [Fact]
    public void ExposedNames_ShouldAppendEachRootToBase()
    {
        ExposeColorDialogViewModel vm = Make("MyColor", RgbRoots);
        vm.ExposedNames.ShouldBe(new[] { "MyColorRed", "MyColorGreen", "MyColorBlue" });
    }

    [Fact]
    public void ExposedNames_ShouldBeRawRootNames_WhenBaseNameEmpty()
    {
        ExposeColorDialogViewModel vm = Make("", new[] { "FillRed", "FillGreen", "FillBlue" });
        vm.ExposedNames.ShouldBe(new[] { "FillRed", "FillGreen", "FillBlue" });
    }

    [Fact]
    public void ExposedNames_ShouldUpdate_WhenBaseNameChanges()
    {
        ExposeColorDialogViewModel vm = Make("MyColor", RgbRoots);
        vm.BaseName = "Other";
        vm.ExposedNames.ShouldBe(new[] { "OtherRed", "OtherGreen", "OtherBlue" });
    }
}
