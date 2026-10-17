using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using CodeOutputPlugin;
using Gum.Avalonia.Settings;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.SettingsWindow;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The Code generation page's view listens to a view model that lives as long as the plugin, so a
/// view in a closed window must stop listening or every Settings window ever opened rebuilds its
/// rows on each later change.
/// </summary>
public class CodeGenerationSettingsPageViewTests
{
    [AvaloniaFact]
    public void AViewOnScreen_RebuildsItsRows_WhenTheViewModelReloads()
    {
        CodeGenerationSettingsViewModel page = (CodeGenerationSettingsViewModel)SettingsPagesForTests.Page(SettingsPageIds.CodeGeneration).Content;
        CodeGenerationSettingsPageView view = new CodeGenerationSettingsPageView { DataContext = page };
        using HeadlessWindowDriver driver = new HeadlessWindowDriver(view, width: 600, height: 600, framesFolderName: "GumCodeGenerationSettingsPage");
        int rebuilds = 0;
        view.Grid.Categories.CollectionChanged += (_, _) => rebuilds++;

        page.Reload();

        rebuilds.ShouldBeGreaterThan(0);
    }

    [AvaloniaFact]
    public void AViewInAClosedWindow_NoLongerRebuildsItsRows_WhenTheViewModelReloads()
    {
        CodeGenerationSettingsViewModel page = (CodeGenerationSettingsViewModel)SettingsPagesForTests.Page(SettingsPageIds.CodeGeneration).Content;
        CodeGenerationSettingsPageView view = new CodeGenerationSettingsPageView { DataContext = page };
        int rebuilds = 0;
        using (HeadlessWindowDriver driver = new HeadlessWindowDriver(view, width: 600, height: 600, framesFolderName: "GumCodeGenerationSettingsPage"))
        {
            view.Grid.Categories.CollectionChanged += (_, _) => rebuilds++;
        }

        page.Reload();

        rebuilds.ShouldBe(0);
    }
}
