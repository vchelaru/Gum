using Avalonia.Threading;
using Avalonia.Styling;
using Gum.DataTypes;
using Gum.Avalonia.Tests.Harness;
using Gum.Plugins.ImportPlugin.Services;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using ImportFromGumxPlugin.Services;
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
        // The project already has a Button that differs and a Label that is identical to the theme's.
        StubImportService importService = new StubImportService(new ImportDestinationStatus(
            new HashSet<ImportItemKey> { new ImportItemKey(ElementItemType.Component, "Neon/Controls/Button") },
            new HashSet<ImportItemKey> { new ImportItemKey(ElementItemType.Component, "Neon/Controls/Label") }));
        ImportFromGumxViewModel viewModel = new ImportFromGumxViewModel(
            new GumxSourceService(),
            new GumxDependencyResolver(),
            importService,
            services.GetRequiredService<IProjectState>(),
            services.GetRequiredService<IDialogService>(),
            services.GetRequiredService<Gum.Services.IDispatcher>(),
            new ImportVersionGate());
        string themeProject = Path.Combine(AppContext.BaseDirectory, "Content", "FormsThemes", "Neon", "GumProject.gumx");

        Task load = viewModel.UseFixedSourceAsync(
            new FixedImportSource(themeProject, "Add Forms: Neon", IncludeStandards: false, BeforeImport: null));
        DateTime giveUp = DateTime.UtcNow.AddSeconds(30);
        while (!load.IsCompleted && DateTime.UtcNow < giveUp)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }

        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, ThemeVariant.Dark);
        window.Save("add-forms-pick-what-to-import");
    });

    private sealed class StubImportService : IGumxImportService
    {
        private readonly ImportDestinationStatus _status;

        public StubImportService(ImportDestinationStatus status)
        {
            _status = status;
        }

        public ImportDestinationStatus GetDestinationStatus(GumProjectSave source, string destinationSubfolder) => _status;

        public Task<ImportResult> ImportAsync(
            ImportSelections selections,
            GumProjectSave source,
            string sourceBase,
            string destinationSubfolder,
            ConflictResolution conflictResolution = ConflictResolution.Cancel,
            Action? beforeImport = null,
            bool replaceStandardAnimations = true) => Task.FromResult(new ImportResult());
    }
}
