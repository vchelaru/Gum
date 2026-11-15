using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Dialogs;
using Gum.DataTypes;
using Gum.Plugins.ImportPlugin.Services;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using ImportFromGumxPlugin.Services;
using ImportFromGumxPlugin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The Import from .gumx dialog's affirmative button is the default (Enter) while the user picks
/// items, and stops being the default once it has turned into a Close for an import that skipped items.
/// </summary>
public class ImportFromGumxDefaultButtonTests
{
    [AvaloniaFact]
    public void AffirmativeButton_IsDefaultWhilePicking_AndNotOnceItBecomesCloseAfterSkippedItems()
    {
        IServiceProvider services = TestAppBuilder.Services;
        ImportResult result = new ImportResult();
        result.AddSkipped("Neon/Icon", "texture missing");
        ImportFromGumxViewModel viewModel = new ImportFromGumxViewModel(
            new GumxSourceService(),
            new GumxDependencyResolver(),
            new SkippingImportService(result),
            services.GetRequiredService<IProjectState>(),
            services.GetRequiredService<IDialogService>(),
            services.GetRequiredService<Gum.Services.IDispatcher>(),
            new ImportVersionGate());
        string themeProject = Path.Combine(AppContext.BaseDirectory, "Content", "FormsThemes", "Neon", "GumProject.gumx");
        Task load = viewModel.UseFixedSourceAsync(new FixedImportSource(themeProject, "Add Forms: Neon", IncludeStandards: false, BeforeImport: null));
        DateTime giveUp = DateTime.UtcNow.AddSeconds(30);
        while (!load.IsCompleted && DateTime.UtcNow < giveUp)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }
        load.IsCompleted.ShouldBeTrue();
        IEnumerable<ImportTreeNodeViewModel> Walk(IEnumerable<ImportTreeNodeViewModel> nodes) =>
            nodes.SelectMany(node => Walk(node.Children).Prepend(node));
        Walk(viewModel.RootNodes).First(node => node.IsLeaf).InclusionState = InclusionState.Explicit;

        Control view = services.GetRequiredService<DialogViewRegistry>().CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Button affirmative = window.GetVisualDescendants().OfType<Button>().Single(button => button.Name == DialogWindow.AffirmativeButtonName);
        affirmative.Content.ShouldBe("Import");
        affirmative.IsDefault.ShouldBeTrue("Enter imports while the user is picking");

        viewModel.OnAffirmative();
        Dispatcher.UIThread.RunJobs();

        affirmative.Content.ShouldBe("Close");
        affirmative.IsDefault.ShouldBeFalse("Enter on a list item must not dismiss the skipped-items report");
        window.Close();
    }

    private sealed class SkippingImportService : IGumxImportService
    {
        private readonly ImportResult _result;

        public SkippingImportService(ImportResult result)
        {
            _result = result;
        }

        public ImportDestinationStatus GetDestinationStatus(GumProjectSave source, string destinationSubfolder) =>
            new ImportDestinationStatus(new HashSet<ImportItemKey>(), new HashSet<ImportItemKey>());

        public Task<ImportResult> ImportAsync(
            ImportSelections selections,
            GumProjectSave source,
            string sourceBase,
            string destinationSubfolder,
            ConflictResolution conflictResolution = ConflictResolution.Cancel,
            Action? beforeImport = null,
            bool replaceStandardAnimations = true) => Task.FromResult(_result);
    }
}
