using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Gum.Avalonia.Panels;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.Errors;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// The Errors tab's Fix button on GUM0008 rows (#8364): clicking it rewrites the element's file variable,
/// the single pixel texture and the localization entry to the on-disk spelling, the rows clear, the element
/// change is one undo step, and the saved files differ from the mismatched ones only on those lines.
/// </summary>
[Trait("Category", "EndToEnd")]
public class CaseMismatchFixScenarioTests
{
    [AvaloniaFact]
    public void ClickingFix_RewritesElementSinglePixelAndLocalizationReferences_ClearsRows_AndElementFixUndoes()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumCaseFix8368");
        Directory.CreateDirectory(Path.Combine(fixture.ProjectFolder, "Art"));
        Directory.CreateDirectory(Path.Combine(fixture.ProjectFolder, "Loc"));
        File.WriteAllText(Path.Combine(fixture.ProjectFolder, "Art", "UI_Sheet.png"), "");
        File.WriteAllText(Path.Combine(fixture.ProjectFolder, "Art", "pixel.png"), "");
        File.WriteAllText(Path.Combine(fixture.ProjectFolder, "Loc", "Strings.csv"), "Key,English\nA,Hi\n");

        ComponentSave menu = fixture.AddComponent("Menu");
        fixture.AddInstance(menu, "Logo", "Sprite");
        fixture.AddInstance(menu, "Other", "Sprite");
        menu.DefaultState!.SetValue("Logo.SourceFile", "Art/ui_sheet.png", "string");
        menu.DefaultState!.SetValue("Other.SourceFile", "Art/UI_Sheet.png", "string");
        fixture.Project.SinglePixelTextureFile = "Art/Pixel.png";
        fixture.Project.LocalizationFiles.Add("Loc/strings.csv");
        fixture.SaveAndReload();
        ComponentSave reloaded = fixture.Project.Components.Single(item => item.Name == "Menu");
        fixture.SelectedState.SelectedElement = reloaded;

        string gumx = fixture.ProjectFilePath;
        string gucx = Path.Combine(fixture.ProjectFolder, "Components", "Menu.gucx");
        string[] gumxBefore = File.ReadAllLines(gumx);
        string[] gucxBefore = File.ReadAllLines(gucx);

        AvaloniaTabManager tabs = TestAppBuilder.Services.GetRequiredService<AvaloniaTabManager>();
        object content = tabs.AllTabs.Single(tab => tab.Title == "Errors").Content;
        AllErrorsViewModel viewModel = content as AllErrorsViewModel ?? (AllErrorsViewModel)((Control)content).DataContext!;
        Wait(() => viewModel.Errors.Count(e => e.Code == "GUM0008") >= 3, "three GUM0008 rows", fixture);
        string[] messages = viewModel.Errors.Where(e => e.Code == "GUM0008").Select(e => e.Message).ToArray();

        ErrorsView view = new ErrorsView { DataContext = viewModel };
        using HeadlessWindowDriver window = new HeadlessWindowDriver(view, 900, 420, framesFolderName: "GumCaseFix8368");
        window.Layout();

        ClickFix(window, view, viewModel, "Logo");
        Wait(() => viewModel.Errors.Count(e => e.Code == "GUM0008") == 2, "element row cleared", fixture);
        reloaded.DefaultState!.GetValue("Logo.SourceFile").ShouldBe("Art/UI_Sheet.png");
        File.ReadAllText(gucx).ShouldContain("Art/UI_Sheet.png");
        string[] gucxAfter = File.ReadAllLines(gucx);
        gucxAfter.Except(gucxBefore).Count().ShouldBe(0, "only an existing line value changed: " + string.Join("|", gucxAfter.Except(gucxBefore)));

        window.Layout();
        ClickFix(window, view, viewModel, "single pixel");
        Wait(() => viewModel.Errors.Count(e => e.Code == "GUM0008") == 1, "single pixel row cleared", fixture);
        fixture.Project.SinglePixelTextureFile.ShouldBe("Art/pixel.png");

        ClickFix(window, view, viewModel, "localization");
        Wait(() => viewModel.Errors.Count(e => e.Code == "GUM0008") == 0, "localization row cleared", fixture);
        fixture.Project.LocalizationFiles.ShouldBe(new[] { "Loc/Strings.csv" });

        string[] gumxAfter = File.ReadAllLines(gumx);
        gumxAfter.Length.ShouldBe(gumxBefore.Length);
        gumxBefore.Except(gumxAfter).Count().ShouldBe(3, "only the single pixel line and the two serialized localization entries changed");

        fixture.UndoManager.PerformUndo();
        reloaded.DefaultState!.GetValue("Logo.SourceFile").ShouldBe("Art/ui_sheet.png");
    }

    private static void ClickFix(HeadlessWindowDriver window, ErrorsView view, AllErrorsViewModel viewModel, string rowText)
    {
        WaitUntilRowsSettle(window, viewModel);
        Button fix = view.GetVisualDescendants().OfType<Button>()
            .Where(b => (b.Content as string) == "Fix" && b.DataContext is ErrorViewModel vm && vm.Message.Contains(rowText))
            .Single();
        window.Click(fix);
    }

    // A fix queues a project pass that swaps in new row objects when it lands (MainErrorsPlugin.ApplyProjectErrors),
    // detaching the buttons a test already found. Waiting until the rows stop changing makes the next click land on a live one.
    private static void WaitUntilRowsSettle(HeadlessWindowDriver window, AllErrorsViewModel viewModel)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(10);
        DateTime quietSince = DateTime.UtcNow;
        ErrorViewModel[] last = viewModel.Errors.ToArray();
        while (DateTime.UtcNow - quietSince < TimeSpan.FromMilliseconds(500))
        {
            DateTime.UtcNow.CompareTo(deadline).ShouldBeLessThan(0, "the Errors rows never settled");
            global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            Thread.Sleep(25);
            ErrorViewModel[] now = viewModel.Errors.ToArray();
            if (!now.SequenceEqual(last))
            {
                last = now;
                quietSince = DateTime.UtcNow;
            }
        }
        window.Layout();
    }

    private static void Wait(Func<bool> condition, string what, ToolProjectFixture fixture) =>
        AsyncWorkWait.Until(condition, TimeSpan.FromSeconds(10), what,
            () => global::Avalonia.Threading.Dispatcher.UIThread.RunJobs(), fixture.Dialogs.Messages);
}
