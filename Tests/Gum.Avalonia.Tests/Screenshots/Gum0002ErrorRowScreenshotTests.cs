using Avalonia.Controls;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Errors tab listing a GUM0002 row, at every zoom level, for a PR's before/after images
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter Gum0002ErrorRowScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class Gum0002ErrorRowScreenshotTests
{
    [SkippableFact]
    public void ErrorsTabWithGum0002Row() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        InstanceSave leader = tree.Project.AddInstance(card, "Leader", "Container");
        InstanceSave label = tree.Project.AddInstance(card, "Label", "Container");
        tree.Click(tree.NodeFor(leader));
        tree.Grid.TypeAndEnter("Width", "120");
        tree.Click(tree.NodeFor(label));
        tree.Grid.TypeLinesAndApply("VariableReferences", "Width = Leader.Width");
        // A stale value, as a hand-edited or older file holds it.
        card.DefaultState.Variables.Single(variable => variable.Name == "Label.Width").Value = 50f;
        tree.Project.SaveAndReload();
        ComponentSave reopened = tree.Project.Project.Components.Single(component => component.Name == "Card");
        tree.Click(tree.NodeFor(reopened));

        AvaloniaPluginTab errorsTab = ((AvaloniaTabManager)TestAppBuilder.Services.GetRequiredService<ITabManager>())
            .AllTabs.Single(tab => tab.Title == "Errors");
        using HeadlessWindowDriver errorsWindow = new HeadlessWindowDriver(
            (Control)errorsTab.Content, 1100, 160, framesFolderName: "GumGum0002Screenshot");

        PrScreenshot.SaveWindowAtZoomLevels(errorsWindow.Window, "gum0002-errors-row");
    });
}
