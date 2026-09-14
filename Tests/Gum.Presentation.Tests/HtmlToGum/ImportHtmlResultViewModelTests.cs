using HtmlToGumPlugin;
using Shouldly;

namespace Gum.Presentation.Tests.HtmlToGum;

/// <summary>The dialog after an HTML import: OK only, with the log behind a toggle.</summary>
public class ImportHtmlResultViewModelTests
{
    [Fact]
    public void ToggleDetails_ShowsAndHidesTheLog_WithTheCaptionFollowing()
    {
        ImportHtmlResultViewModel dialog = new ImportHtmlResultViewModel("Imported.", "log line");

        dialog.AffirmativeText.ShouldBe("OK");
        dialog.NegativeText.ShouldBeNull();
        dialog.HasDetails.ShouldBeTrue();
        dialog.IsDetailsVisible.ShouldBeFalse();
        dialog.DetailsButtonText.ShouldBe("Show details ▾");

        dialog.ToggleDetailsCommand.Execute(null);

        dialog.IsDetailsVisible.ShouldBeTrue();
        dialog.DetailsButtonText.ShouldBe("Hide details ▴");
    }

    [Fact]
    public void HasDetails_IsFalse_WithoutALog()
    {
        new ImportHtmlResultViewModel("Imported.", "  ").HasDetails.ShouldBeFalse();
    }
}
