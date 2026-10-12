using Avalonia.Threading;
using Avalonia.Styling;
using Gum.Avalonia.Tests.Harness;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Commands;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.ToolStates;
using ImportFromGumxPlugin;
using ImportFromGumxPlugin.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Import from .gumx dialog opened by Add Forms on a theme ("Pick what to import"), for a PR's
/// screenshot table (<c>Tools/pr-screenshots.ps1 -Filter ImportFromGumxFixedSourceScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ImportFromGumxFixedSourceScreenshotTests
{
    [SkippableFact]
    public void PickWhatToImport() => PrScreenshot.Run(() =>
    {
        IServiceProvider services = TestAppBuilder.Services;
        ImportFromGumxViewModel viewModel = new ImportFromGumxLogic(
            services.GetRequiredService<IProjectState>(),
            services.GetRequiredService<IImportLogic>(),
            services.GetRequiredService<IFileCommands>(),
            services.GetRequiredService<IDialogService>(),
            services.GetRequiredService<Gum.Services.IDispatcher>()).CreateImportViewModel();
        string themeProject = Path.Combine(AppContext.BaseDirectory, "Content", "FormsThemes", "Neon", "GumProject.gumx");

        Task load = viewModel.UseFixedSourceAsync(
            new FixedImportSource(themeProject, "Add Forms: Neon", IncludeStandards: false, BeforeProjectSave: null));
        DateTime giveUp = DateTime.UtcNow.AddSeconds(30);
        while (!load.IsCompleted && DateTime.UtcNow < giveUp)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }

        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, ThemeVariant.Dark);
        window.Save("add-forms-pick-what-to-import");
    });
}
