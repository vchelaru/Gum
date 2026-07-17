using System.Collections.Generic;
using Gum.Plugins.Behaviors;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for CheckListBehaviorItem, relocated out of Gum.csproj into
/// the headless Gum.Presentation assembly (ADR-0005, #3754) as a clean leaf VM with no injected
/// interfaces.
/// </summary>
public class CheckListBehaviorItemTests
{
    [Fact]
    public void DisplayText_AppendsMissingSuffix_WhenOrphaned()
    {
        CheckListBehaviorItem item = new() { Name = "MyBehavior", IsOrphaned = true };

        item.DisplayText.ShouldBe("MyBehavior (missing)");
    }

    [Fact]
    public void DisplayText_ReturnsName_WhenNotOrphaned()
    {
        CheckListBehaviorItem item = new() { Name = "MyBehavior", IsOrphaned = false };

        item.DisplayText.ShouldBe("MyBehavior");
    }

    [Fact]
    public void IsChecked_Set_RaisesPropertyChanged()
    {
        CheckListBehaviorItem item = new();
        List<string?> changedProperties = new();
        item.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        item.IsChecked = true;

        item.IsChecked.ShouldBeTrue();
        changedProperties.ShouldContain(nameof(CheckListBehaviorItem.IsChecked));
    }

    [Fact]
    public void IsOrphaned_Set_RaisesPropertyChanged_AndDisplayText()
    {
        CheckListBehaviorItem item = new() { Name = "MyBehavior" };
        List<string?> changedProperties = new();
        item.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        item.IsOrphaned = true;

        changedProperties.ShouldContain(nameof(CheckListBehaviorItem.IsOrphaned));
        changedProperties.ShouldContain(nameof(CheckListBehaviorItem.DisplayText));
    }

    [Fact]
    public void Name_Set_RaisesPropertyChanged_AndDisplayText()
    {
        CheckListBehaviorItem item = new();
        List<string?> changedProperties = new();
        item.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        item.Name = "MyBehavior";

        item.Name.ShouldBe("MyBehavior");
        changedProperties.ShouldContain(nameof(CheckListBehaviorItem.Name));
        changedProperties.ShouldContain(nameof(CheckListBehaviorItem.DisplayText));
    }
}
