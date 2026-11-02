using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Dialogs;
using GumFormsPlugin.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The screens a presenter shows in the beginner demo: New Project, Add Forms, the Code tab's setup
/// offer and settings (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter DemoBeatsScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class DemoBeatsScreenshotTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [SkippableFact]
    public void NewProjectDialog() => PrScreenshot.Run(() =>
    {
        ToolStartup.EnsureInitialized();
        NewProjectDialogViewModel viewModel = Services.GetRequiredService<NewProjectDialogViewModel>();
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel);
        window.Save("demo-new-project");
    });

    [SkippableFact]
    public void AddFormsDialog() => PrScreenshot.Run(() =>
    {
        ToolStartup.EnsureInitialized();
        AddFormsViewModel viewModel = Services.GetRequiredService<AddFormsViewModel>();
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel);
        window.Save("demo-add-forms");
    });

    [SkippableFact]
    public void CodeTabOffersSetup() => PrScreenshot.Run(() =>
    {
        using CodeTabHarness code = new CodeTabHarness(beforeLoad: folder => File.WriteAllText(Path.Combine(folder, "MyGame.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><RootNamespace>MyGame</RootNamespace></PropertyGroup><ItemGroup><PackageReference Include=\"MonoGame.Framework.DesktopGL\" Version=\"3.8.*\" /></ItemGroup></Project>"));
        ScreenSave screen = code.Project.AddScreen("MainMenu");
        code.Project.AddInstance(screen, "Title", "Text");
        code.Tree.SaveAll();
        code.Select(screen);
        PrScreenshot.SaveWindow(code.Input.Window, "demo-code-tab-setup-offer");
    });

    [SkippableFact]
    public void TreeAfterAddForms() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Dialogs.AnswerNext<AddFormsViewModel>(dialog => { dialog.IsIncludeDemoScreenGum = false; return true; });
        tree.PickMainMenu("Content", "Add Forms Components");
        tree.WaitUntil(() => Services.GetRequiredService<Gum.Managers.IProjectManager>().GumProjectSave != tree.Project.Project, TimeSpan.FromSeconds(60), "reload");
        PrScreenshot.SaveWindow(tree.Input.Window, "demo-tree-after-add-forms");
    });
}
