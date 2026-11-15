using Avalonia.Controls;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Shell;
using Gum.DataTypes;
using Gum.Managers;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Alignment tab with its Anchor and Dock subtext at every zoom level
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter AlignmentSubtextScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class AlignmentSubtextScreenshotTests
{
    [SkippableFact]
    public void AlignmentTab() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        InstanceSave panel = tree.Project.AddInstance(card, "Panel", "Container");
        tree.Click(tree.NodeFor(panel));

        AvaloniaTabManager tabs = (AvaloniaTabManager)TestAppBuilder.Services.GetRequiredService<ITabManager>();
        Control view = (Control)tabs.AllTabs.Single(tab => tab.Title == "Alignment").Content;
        using HeadlessWindowDriver alignment = new HeadlessWindowDriver(view, 500, 400, framesFolderName: "GumAlignmentSubtext");
        PrScreenshot.SaveWindowAtZoomLevels(alignment.Window, "alignment-tab");
    });
}
