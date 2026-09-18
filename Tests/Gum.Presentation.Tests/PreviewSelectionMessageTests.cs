using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Covers the selection-file format the tool writes and GumPreview reads
/// (<see cref="PreviewSelectionMessage"/>): serialization, parsing, and state lookup.
/// </summary>
public class PreviewSelectionMessageTests
{
    [Fact]
    public void Serialize_ThenParse_RoundTripsEveryField()
    {
        PreviewSelectionMessage message = new PreviewSelectionMessage("MainMenu")
        {
            CategoryName = "ButtonCategory",
            StateName = "Highlighted",
            SortByBatchKey = true,
            Activate = true,
        };

        PreviewSelectionMessage? parsed = PreviewSelectionMessage.TryParse(message.Serialize().Split('\n'));

        parsed.ShouldNotBeNull();
        parsed.ElementName.ShouldBe("MainMenu");
        parsed.CategoryName.ShouldBe("ButtonCategory");
        parsed.StateName.ShouldBe("Highlighted");
        parsed.SortByBatchKey.ShouldBeTrue();
        parsed.Activate.ShouldBeTrue();
    }

    [Fact]
    public void Serialize_WithOnlyAnElement_OmitsOptionalLines()
    {
        PreviewSelectionMessage.TryParse(new PreviewSelectionMessage("MainMenu").Serialize().Split('\n'))
            .ShouldNotBeNull()
            .ShouldSatisfyAllConditions(
                m => m.ElementName.ShouldBe("MainMenu"),
                m => m.CategoryName.ShouldBeNull(),
                m => m.StateName.ShouldBeNull(),
                m => m.SortByBatchKey.ShouldBeFalse(),
                m => m.Activate.ShouldBeFalse());
    }

    [Fact]
    public void TryParse_WithoutAnElementLine_ReturnsNull()
    {
        PreviewSelectionMessage.TryParse(new[] { "state=Highlighted", "end=true" }).ShouldBeNull();
    }

    [Fact]
    public void TryParse_WithoutTheEndLine_ReturnsNullBecauseTheFileIsStillBeingWritten()
    {
        PreviewSelectionMessage.TryParse(new[] { "element=MainMenu", "state=Highl" }).ShouldBeNull();
    }

    [Fact]
    public void HasSameSelection_IgnoresActivateAndOrderer()
    {
        PreviewSelectionMessage a = new PreviewSelectionMessage("MainMenu") { StateName = "Open", Activate = true, SortByBatchKey = true };
        PreviewSelectionMessage b = new PreviewSelectionMessage("MainMenu") { StateName = "Open" };
        PreviewSelectionMessage c = new PreviewSelectionMessage("MainMenu") { StateName = "Closed" };

        a.HasSameSelection(b).ShouldBeTrue();
        a.HasSameSelection(c).ShouldBeFalse();
    }

    [Fact]
    public void FindState_WithCategory_ReturnsTheCategorizedState()
    {
        ComponentSave component = new ComponentSave { Name = "Button" };
        StateSave highlighted = new StateSave { Name = "Highlighted" };
        component.Categories.Add(new StateSaveCategory { Name = "ButtonCategory", States = { highlighted } });
        component.States.Add(new StateSave { Name = "Highlighted" });

        new PreviewSelectionMessage("Button") { CategoryName = "ButtonCategory", StateName = "Highlighted" }
            .FindState(component).ShouldBeSameAs(highlighted);
    }

    [Fact]
    public void FindState_WithoutCategory_ReturnsTheUncategorizedState()
    {
        ComponentSave component = new ComponentSave { Name = "Button" };
        StateSave uncategorized = new StateSave { Name = "Big" };
        component.States.Add(uncategorized);
        component.Categories.Add(new StateSaveCategory { Name = "ButtonCategory", States = { new StateSave { Name = "Big" } } });

        new PreviewSelectionMessage("Button") { StateName = "Big" }.FindState(component).ShouldBeSameAs(uncategorized);
    }

    [Fact]
    public void FindState_WithNoStateName_ReturnsNull()
    {
        ComponentSave component = new ComponentSave { Name = "Button" };
        component.States.Add(new StateSave { Name = "Default" });

        new PreviewSelectionMessage("Button").FindState(component).ShouldBeNull();
    }
}
