using Avalonia.Controls;
using Gum.Avalonia.Panels;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins.Errors;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Errors tab listing GUM0008 rows that name where the reference lives, with a Fix button
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter CaseMismatchErrorsScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class CaseMismatchErrorsScreenshotTests
{
    [Fact]
    public void ErrorsTab() => PrScreenshot.Run(() =>
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumCaseMismatchShot");
        Directory.CreateDirectory(Path.Combine(fixture.ProjectFolder, "Art"));
        File.WriteAllText(Path.Combine(fixture.ProjectFolder, "Art", "UI_Sheet.png"), "");
        File.WriteAllText(Path.Combine(fixture.ProjectFolder, "Art", "pixel.png"), "");

        ComponentSave menu = fixture.AddComponent("Menu");
        InstanceSave logo = fixture.AddInstance(menu, "Logo", "Sprite");
        menu.DefaultState.SetValue("Logo.SourceFile", "Art/ui_sheet.png", "string");
        fixture.Project.SinglePixelTextureFile = "Art/Pixel.png";
        fixture.SaveAndReload();
        fixture.SelectedState.SelectedElement = fixture.Project.Components.Single(item => item.Name == "Menu");

        AvaloniaTabManager tabs = TestAppBuilder.Services.GetRequiredService<AvaloniaTabManager>();
        object content = tabs.AllTabs.Single(tab => tab.Title == "Errors").Content;
        AllErrorsViewModel viewModel = content as AllErrorsViewModel ?? (AllErrorsViewModel)((Control)content).DataContext!;
        AsyncWorkWait.Until(() => viewModel.Errors.Count(error => error.Code == "GUM0008") >= 2, TimeSpan.FromSeconds(10),
            "the GUM0008 rows", () => global::Avalonia.Threading.Dispatcher.UIThread.RunJobs(), fixture.Dialogs.Messages);

        ErrorsView view = new ErrorsView { DataContext = viewModel };
        using HeadlessWindowDriver window = new HeadlessWindowDriver(view, 900, 420, framesFolderName: "GumCaseMismatchShot");
        PrScreenshot.SaveWindowAtZoomLevels(window.Window, "errors-tab-gum0008");
    });
}
