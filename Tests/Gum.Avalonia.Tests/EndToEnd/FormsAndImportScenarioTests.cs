using Avalonia.Headless.XUnit;
using Gum.Avalonia.Tests.Animations;
using Gum.DataTypes;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.Dialogs;
using Gum.Logic;
using Gum.Managers;
using Gum.Menus;
using Gum.Services.Dialogs;
using Gum.StateAnimation.SaveClasses;
using GumFormsPlugin.ViewModels;
using ImportFromGumxPlugin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on the work the neutral plugins and the staged Forms themes do: New Project
/// with Forms, Content > Add Forms Components, and Content > Import > .gumx. They need the test
/// output's Plugins and Content/FormsThemes folders, which the test project copies from the head's
/// output (Gum.Avalonia.Tests.csproj, StageHeadPlugins).
/// </summary>
[Trait("Category", "EndToEnd")]
public class FormsAndImportScenarioTests
{
    private static readonly TimeSpan AsyncWork = TimeSpan.FromSeconds(60);

    private static IServiceProvider Services => TestAppBuilder.Services;

    #region New project and Forms

    [AvaloniaFact]
    [Trait("Feature", "DLG-004")]
    [Trait("Feature", "FILE-001")]
    public void NewProject_WithFormsAndTheDemoScreen_ImportsTheThemesElements()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        GumProjectSave theme = StagedTheme("Standard");
        tree.Dialogs.AnswerNext<NewProjectDialogViewModel>(dialog =>
        {
            dialog.ThemeSelection.AvailableThemes.ShouldContain("Standard");
            dialog.IsIncludeFormsControls = true;
            dialog.IsIncludeDemoScreenGum = true;
            return true;
        });
        tree.Dialogs.AnswerNextSaveFile(tree.Project.ProjectFilePath);
        // The folder holds the project the harness started with.
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);

        tree.PickMainMenu("File", "New Project");
        tree.WaitUntil(() => tree.SelectedState.SelectedScreen?.Name == NewProjectLogic.StartingScreenName, AsyncWork, "the new project's starting screen");

        GumProjectSave project = Services.GetRequiredService<IProjectManager>().GumProjectSave.ShouldNotBeNull();
        new FilePath(project.FullFileName.ShouldNotBeNull()).ShouldBe(new FilePath(tree.Project.ProjectFilePath));
        theme.Components.Select(component => component.Name).ShouldBeSubsetOf(project.Components.Select(component => component.Name));
        project.Screens.Select(screen => screen.Name).ShouldContain("DemoScreenGum");
        project.Screens.Select(screen => screen.Name).ShouldContain(NewProjectLogic.StartingScreenName);
        tree.SaveAll();
        foreach (ComponentSave component in project.Components)
        {
            File.Exists(ElementFile(tree, "Components", component.Name, "gucx")).ShouldBeTrue($"{component.Name} was saved");
        }
        tree.RootNode("Components").Nodes.ShouldNotBeEmpty();

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DLG-021")]
    [Trait("Feature", "CONT-010")]
    [Trait("Feature", "COMBO-032")]
    [Trait("Feature", "EDIT-001")]
    public void AddForms_WithAChosenTheme_ImportsItsComponentsIntoTheProject_AndIsNotOfferedAgain()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        tree.SaveAll();
        tree.Click(tree.NodeFor(ComponentNamed(tree.Project.Project, "Card")));
        tree.Grid.TypeAndEnter("Width", "175");
        GumProjectSave theme = StagedTheme("Bubblegum");
        tree.Dialogs.AnswerNext<AddFormsViewModel>(dialog =>
        {
            dialog.ThemeSelection.SelectedTheme = "Bubblegum";
            dialog.IsIncludeDemoScreenGum = false;
            return true;
        });

        tree.PickMainMenu("Content", "Add Forms Components");
        GumProjectSave project = WaitForTheImportsReload(tree);

        List<string> componentNames = project.Components.Select(component => component.Name).ToList();
        componentNames.ShouldContain("Card");
        theme.Components.Select(component => component.Name).ShouldBeSubsetOf(componentNames);
        project.Screens.ShouldBeEmpty("no demo screen was asked for");
        tree.ChildTexts(tree.RootNode("Components")).ShouldContain("Card");
        MainMenuHeaders("Content").ShouldNotContain("Add Forms Components", "a project that has Forms is not offered them again");
        tree.Click(tree.NodeFor(ComponentNamed(project, "Card")));
        tree.Undo();
        VariableGridHarness.StoredValue(ComponentNamed(project, "Card"), "Width").ShouldBeNull("the edit made before adding Forms is still in Card's history");

        tree.AssertOracles();
    }

    #endregion

    #region Import .gumx

    [AvaloniaFact]
    [Trait("Feature", "DLG-022")]
    [Trait("Feature", "CONT-008")]
    public void ImportGumx_OneComponentIntoASubfolder_BringsWhatItDependsOn_UnderThatFolder()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        using TempThemeCopy source = new TempThemeCopy("Standard");
        tree.Dialogs.AnswerNextOpenFile(source.ProjectFile);
        tree.Dialogs.AnswerNext<ImportFromGumxViewModel>(dialog =>
        {
            dialog.BrowseCommand.Execute(null);
            tree.WaitUntil(() => dialog.IsPreviewLoaded, AsyncWork, "the .gumx preview");
            dialog.DestinationSubfolder = "Imported";
            Leaf(dialog, "Controls/ButtonStandard").IsChecked = true;
            // The preview adds what the pick needs on its next pass, before the user can press Import.
            tree.WaitUntil(() => Leaf(dialog, "ButtonBehavior").IsChecked == true, AsyncWork, "the preview to include the button's behavior");
            return true;
        });

        tree.PickMainMenu("Content", "Import", ".gumx…");
        GumProjectSave project = WaitForTheImportsReload(tree);

        List<string> componentNames = project.Components.Select(component => component.Name).ToList();
        componentNames.ShouldAllBe(name => name.StartsWith("Imported/", StringComparison.Ordinal));
        componentNames.ShouldContain("Imported/Styles", "the button's colors reference Styles");
        componentNames.ShouldNotContain("Imported/Controls/ListBox", "only what the button needs is imported");
        project.Behaviors.Select(behavior => behavior.Name).ShouldContain("ButtonBehavior");
        File.Exists(ElementFile(tree, "Components", "Imported/Controls/ButtonStandard", "gucx")).ShouldBeTrue();
        tree.FolderNode("Components", "Imported/Controls").Nodes.Select(node => node.Text).ShouldContain("ButtonStandard");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DLG-022")]
    [Trait("Feature", "COMBO-031")]
    public void ImportGumx_AComponentWhoseNameIsTaken_OverwriteAll_ReplacesTheExistingOne()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Controls/ButtonStandard");
        tree.SaveAll();
        using TempThemeCopy source = new TempThemeCopy("Standard");
        tree.Dialogs.AnswerNextOpenFile(source.ProjectFile);
        string? conflictMessage = null;
        tree.Dialogs.AnswerNext<ImportFromGumxViewModel>(dialog =>
        {
            dialog.BrowseCommand.Execute(null);
            tree.WaitUntil(() => dialog.IsPreviewLoaded, AsyncWork, "the .gumx preview");
            dialog.DestinationSubfolder = "";
            Leaf(dialog, "Controls/ButtonStandard").IsChecked = true;
            tree.WaitUntil(() => Leaf(dialog, "ButtonBehavior").IsChecked == true, AsyncWork, "the preview to include the button's behavior");
            return true;
        });
        tree.Dialogs.AnswerNext<ChoiceDialogViewModel>(dialog =>
        {
            conflictMessage = dialog.Message;
            dialog.SelectedValue = "Overwrite All";
            return true;
        });

        tree.PickMainMenu("Content", "Import", ".gumx…");
        GumProjectSave project = WaitForTheImportsReload(tree);

        conflictMessage.ShouldNotBeNull().ShouldContain("Controls/ButtonStandard");
        ComponentSave button = project.Components.Single(component => component.Name == "Controls/ButtonStandard");
        button.Instances.ShouldNotBeEmpty("the source's button replaced the empty one");
        ComponentSave saved = (ComponentSave)ElementReference.DeserializeElement<ComponentSave>(
            ElementFile(tree, "Components", "Controls/ButtonStandard", "gucx"), GumProjectSave.NativeVersion);
        saved.Instances.Select(instance => instance.Name).ShouldBe(button.Instances.Select(instance => instance.Name));

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "CONT-008")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void ImportGumx_AfterEditingAnExistingComponent_ThatEditCanStillBeUndoneAndRedone()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        tree.SaveAll();
        tree.Click(tree.NodeFor(ComponentNamed(tree.Project.Project, "Card")));
        tree.Grid.TypeAndEnter("Width", "175");
        using TempThemeCopy source = new TempThemeCopy("Standard");
        tree.Dialogs.AnswerNextOpenFile(source.ProjectFile);
        tree.Dialogs.AnswerNext<ImportFromGumxViewModel>(dialog =>
        {
            dialog.BrowseCommand.Execute(null);
            tree.WaitUntil(() => dialog.IsPreviewLoaded, AsyncWork, "the .gumx preview");
            dialog.DestinationSubfolder = "Imported";
            Leaf(dialog, "Controls/ButtonStandard").IsChecked = true;
            tree.WaitUntil(() => Leaf(dialog, "ButtonBehavior").IsChecked == true, AsyncWork, "the preview to include the button's behavior");
            return true;
        });

        tree.PickMainMenu("Content", "Import", ".gumx…");
        GumProjectSave project = WaitForTheImportsReload(tree);
        tree.Click(tree.NodeFor(ComponentNamed(project, "Card")));
        VariableGridHarness.StoredValue(ComponentNamed(project, "Card"), "Width").ShouldBe(175f);

        tree.Undo();
        VariableGridHarness.StoredValue(ComponentNamed(project, "Card"), "Width").ShouldBeNull("the edit made before the import is still in Card's history");

        tree.Redo();
        VariableGridHarness.StoredValue(ComponentNamed(project, "Card"), "Width").ShouldBe(175f);

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DLG-022")]
    [Trait("Feature", "CONT-008")]
    public void ImportGumx_AStandardWithAnimations_ReplacesTheTargetsAnimationsWhole()
    {
        // A Standard replaces the target's Standard whole (#5340), and its animations go with it.
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        string targetAnimations = Path.Combine(tree.Project.ProjectFolder, "Standards", "TextAnimations.ganx");
        SaveAnimations(targetAnimations, "TargetOnly");
        using TempThemeCopy source = new TempThemeCopy("Standard");
        SaveAnimations(Path.Combine(Path.GetDirectoryName(source.ProjectFile)!, "Standards", "TextAnimations.ganx"), "Pulse");
        tree.Dialogs.AnswerNextOpenFile(source.ProjectFile);
        tree.Dialogs.AnswerNext<ImportFromGumxViewModel>(dialog =>
        {
            dialog.BrowseCommand.Execute(null);
            tree.WaitUntil(() => dialog.IsPreviewLoaded, AsyncWork, "the .gumx preview");
            Leaf(dialog, "Text").IsChecked = true;
            return true;
        });

        tree.PickMainMenu("Content", "Import", ".gumx…");
        WaitForTheImportsReload(tree);

        ElementAnimationsSave.Load(targetAnimations).Animations.Select(animation => animation.Name)
            .ShouldBe(new[] { "Pulse" }, "the source's animations replace the target's, none kept");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "CONT-008")]
    [Trait("Feature", "EDIT-001")]
    public void ImportGumx_AStandardWhoseFileIsUnchangedButWhoseAnimationsAreNot_UndoKeepsTheImportedAnimations()
    {
        // The reload keeps an unchanged element's history (#5339), but the element file alone does
        // not say whether the import replaced its animations (#5345).
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        string targetAnimations = Path.Combine(editor.ProjectFolder, "Standards", "TextAnimations.ganx");
        SaveAnimations(targetAnimations, "Old");
        StandardElementSave text = editor.Project.StandardElements.Single(standard => standard.Name == "Text");
        editor.Select(text);
        editor.StartScenario();
        // The tab adds animations only to screens and components, but renames a Standard's.
        editor.RightClick(editor.RowFor(editor.AnimationList, editor.ViewModel.Animations.Single()));
        editor.Dialogs.AnswerNextUserString("Edited");
        editor.PickContextMenuItem("Rename Animation");
        ElementAnimationsSave.Load(targetAnimations).Animations.Select(animation => animation.Name).ShouldBe(new[] { "Edited" });
        using TempThemeCopy source = new TempThemeCopy("Standard");
        string sourceStandards = Path.Combine(Path.GetDirectoryName(source.ProjectFile)!, "Standards");
        File.Copy(Path.Combine(editor.ProjectFolder, "Standards", "Text." + GumProjectSave.StandardExtension),
            Path.Combine(sourceStandards, "Text." + GumProjectSave.StandardExtension), overwrite: true);
        SaveAnimations(Path.Combine(sourceStandards, "TextAnimations.ganx"), "Pulse");
        editor.Dialogs.AnswerNextOpenFile(source.ProjectFile);
        editor.Dialogs.AnswerNext<ImportFromGumxViewModel>(dialog =>
        {
            dialog.BrowseCommand.Execute(null);
            editor.WaitUntil(() => dialog.IsPreviewLoaded, AsyncWork).ShouldBeTrue("the .gumx preview loads");
            Leaf(dialog, "Text").IsChecked = true;
            return true;
        });

        PickMainMenu("Content", "Import", ".gumx…");
        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        editor.WaitUntil(() => projectManager.GumProjectSave != editor.Project, AsyncWork).ShouldBeTrue("the project reloads after the import");
        StandardElementSave reloadedText = projectManager.GumProjectSave!.StandardElements.Single(standard => standard.Name == "Text");
        editor.Select(reloadedText);
        ElementAnimationsSave.Load(targetAnimations).Animations.Select(animation => animation.Name).ShouldBe(new[] { "Pulse" });

        editor.Undo();

        ElementAnimationsSave.Load(targetAnimations).Animations.Select(animation => animation.Name)
            .ShouldBe(new[] { "Pulse" }, "undoing the edit made before the import must not write the pre-import animations back");
    }

    #endregion

    private static void PickMainMenu(params string[] path)
    {
        IEnumerable<MenuItemModel> items = Services.GetRequiredService<MenuModel>().TopLevelItems;
        MenuItemModel? item = null;
        foreach (string header in path)
        {
            item = items.Single(candidate => !candidate.IsSeparator && candidate.Header == header);
            items = item.Items;
        }
        item!.Invoke();
    }

    private static void SaveAnimations(string path, string animationName)
    {
        ElementAnimationsSave animations = new ElementAnimationsSave();
        animations.Animations.Add(new AnimationSave { Name = animationName });
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        animations.Save(path);
    }

    private static ComponentSave ComponentNamed(GumProjectSave project, string name) =>
        project.Components.Single(component => component.Name == name);

    /// <summary>The project of a Forms theme as the test output stages it.</summary>
    private static GumProjectSave StagedTheme(string name)
    {
        string gumx = Path.Combine(AppContext.BaseDirectory, "Content", "FormsThemes", name, "GumProject.gumx");
        return GumProjectSave.Load(gumx, out _) ?? throw new InvalidOperationException($"The staged {name} theme did not load from {gumx}.");
    }

    /// <summary>A staged Forms theme, a complete Gum project, copied to a temp folder to import from.</summary>
    private sealed class TempThemeCopy : IDisposable
    {
        private readonly string _folder;

        public TempThemeCopy(string themeName)
        {
            string from = Path.Combine(AppContext.BaseDirectory, "Content", "FormsThemes", themeName);
            _folder = Path.Combine(Path.GetTempPath(), "GumImportScenarios", Guid.NewGuid().ToString("N"));
            foreach (string file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(_folder, Path.GetRelativePath(from, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target);
            }
            // A theme's behavior references point into the repo's shared FormsBehaviors folder; staging
            // already copied each behavior into Behaviors/, where a user's own project keeps them.
            ProjectFile = Path.Combine(_folder, "GumProject.gumx");
            File.WriteAllText(ProjectFile, System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(ProjectFile), " SourcePath=\"[^\"]*\"", ""));
        }

        public string ProjectFile { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_folder, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // A file watcher may still hold it; the temp folder is cleaned up later.
            }
        }
    }

    private static ImportTreeNodeViewModel Leaf(ImportFromGumxViewModel dialog, string fullName)
    {
        IEnumerable<ImportTreeNodeViewModel> Walk(IEnumerable<ImportTreeNodeViewModel> nodes) =>
            nodes.SelectMany(node => Walk(node.Children).Prepend(node));
        return Walk(dialog.RootNodes).SingleOrDefault(node => node.IsLeaf && node.FullName == fullName)
            ?? throw new InvalidOperationException($"The import preview lists no {fullName}.");
    }

    /// <summary>
    /// Waits for the import a dialog started to end: both imports end by saving and reopening the
    /// project, which gives the tool a new project object. Returns that project.
    /// </summary>
    private static GumProjectSave WaitForTheImportsReload(ProjectTreeHarness tree)
    {
        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        tree.WaitUntil(() => projectManager.GumProjectSave != tree.Project.Project, AsyncWork, "the project to reload after the import");
        return projectManager.GumProjectSave.ShouldNotBeNull();
    }

    private static List<string> MainMenuHeaders(string topLevel) =>
        Services.GetRequiredService<MenuModel>().GetItem(topLevel)!.Items.Select(item => item.Header ?? "").ToList();

    private static string ElementFile(ProjectTreeHarness tree, string subfolder, string elementName, string extension) =>
        Path.Combine(tree.Project.ProjectFolder, subfolder, elementName.Replace('/', Path.DirectorySeparatorChar) + "." + extension);
}
