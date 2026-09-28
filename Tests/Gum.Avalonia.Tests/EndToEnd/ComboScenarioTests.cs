using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using ConvertToJsonPlugin;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Animations;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
using Gum.Managers;
using Gum.Plugins.Behaviors;
using Gum.ProjectServices;
using Gum.ProjectServices.FontGeneration;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios for the COMBO items: long workflows that chain features the way a user
/// does (build a component with states, inherit and instance it, rebase, delete, rename, move,
/// generate code), driven through the Project tree and the other tabs with real gestures. Each
/// checks the model and the saved files after its steps, undoes every element back to the files
/// it started from, redoes back to the finished files, and ends with the shared oracles.
/// </summary>
[Trait("Category", "EndToEnd")]
public class ComboScenarioTests
{
    #region Inheritance

    [AvaloniaFact]
    [Trait("Feature", "COMBO-011")]
    [Trait("Feature", "COMBO-017")]
    [Trait("Feature", "COMBO-022")]
    public void ABaseComponentWithStates_InheritedAndInstanced_SurvivesAStateDeleteARebaseAndItsOwnDelete()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Panel");
        tree.Project.AddComponent("Button");
        tree.Project.AddComponent("IconButton");
        tree.Project.AddScreen("Title");
        tree.SaveAll();
        ProjectFileSnapshot empty = tree.SnapshotFiles();

        // Button: a Label and a Looks category whose Pressed state widens it.
        AddObject(tree, Component(tree, "Button"), "Text", "Label");
        StateSaveCategory looks = tree.Project.AddCategory(Component(tree, "Button"), "Looks");
        tree.Project.AddState(Component(tree, "Button"), looks, "Pressed");
        tree.Click(tree.NodeFor(Component(tree, "Button").Instances.Single()));
        VariableGridHarness grid = tree.Grid;
        grid.TypeAndEnter("Width", "80");
        StatesTabHarness states = tree.States;
        states.Click(states.ItemFor("Looks", "Pressed"));
        grid.TypeAndEnter("Width", "120");

        // IconButton inherits Button and gives the inherited Label its own text.
        tree.Click(tree.NodeFor(Component(tree, "IconButton")));
        grid.PickComboItem("BaseType", "Button");
        tree.ChildTexts(tree.NodeFor(Component(tree, "IconButton"))).ShouldBe(new[] { "Label" });
        tree.Click(tree.NodeFor(Component(tree, "IconButton").Instances.Single()));
        grid.TypeAndLeave("Text", "Icon");

        // Title uses an IconButton, dragged onto it from the tree, in its Pressed look.
        tree.Drag(tree.NodeFor(Component(tree, "IconButton")), tree.NodeFor(Screen(tree, "Title")));
        Screen(tree, "Title").Instances.ShouldHaveSingleItem().BaseType.ShouldBe("IconButton");
        tree.Click(tree.NodeFor(Screen(tree, "Title").Instances.Single()));
        tree.Dialogs.AnswerNextUserString("OkButton");
        tree.Press(Key.F2, PhysicalKey.F2);
        tree.Click(tree.NodeFor(Screen(tree, "Title").Instances.Single()));
        grid.PickComboItem("LooksState", "Pressed");
        VariableGridHarness.StoredValue(grid.ReadSaved(Screen(tree, "Title")), "OkButton.LooksState").ShouldBe("Pressed");
        ProjectFileSnapshot built = tree.SnapshotFiles();

        // Deleting Pressed from Button clears the state Title's instance used, and one undo in
        // Button brings both back.
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        states.RightClick(states.ItemFor("Looks", "Pressed"));
        states.PickMenu("Delete [Pressed]");
        states.Shown().ShouldBe(new[] { "Looks: " });
        VariableGridHarness.StoredValue(grid.ReadSaved(Screen(tree, "Title")), "OkButton.LooksState").ShouldBeNull();
        tree.Undo();
        states.Shown().ShouldBe(new[] { "Looks: Pressed" });
        VariableGridHarness.StoredValue(Screen(tree, "Title"), "OkButton.LooksState").ShouldBe("Pressed");
        tree.SnapshotFiles().ShouldMatch(built, "undoing the state delete should restore Button and the Title that used the state");

        // Rebasing IconButton onto Panel drops the inherited Label; undo puts it back.
        tree.Click(tree.NodeFor(Component(tree, "IconButton")));
        grid.PickComboItem("BaseType", "Panel");
        Component(tree, "IconButton").BaseType.ShouldBe("Panel");
        tree.ChildTexts(tree.NodeFor(Component(tree, "IconButton"))).ShouldBeEmpty();
        grid.ReadSaved(Component(tree, "IconButton")).BaseType.ShouldBe("Panel");
        tree.Undo();
        Component(tree, "IconButton").BaseType.ShouldBe("Button");
        tree.ChildTexts(tree.NodeFor(Component(tree, "IconButton"))).ShouldBe(new[] { "Label" });
        tree.SnapshotFiles().ShouldMatch(built, "undoing the rebase should restore IconButton");
        tree.Redo();
        Component(tree, "IconButton").BaseType.ShouldBe("Panel");
        ProjectFileSnapshot finished = tree.SnapshotFiles();

        List<(string Element, int Steps)> undone = UndoEverything(tree, Component(tree, "IconButton"), Screen(tree, "Title"), Component(tree, "Button"));
        tree.SnapshotFiles().ShouldMatch(empty, "undoing every element should restore the files it started from");
        RedoEverything(tree, undone);
        tree.SnapshotFiles().ShouldMatch(finished, "redoing every element should restore the finished files");

        // IconButton derives from Button again, and then Button is deleted. Deleting an element
        // records no undo, so this comes after the undo and redo checks.
        tree.Click(tree.NodeFor(Component(tree, "IconButton")));
        grid.PickComboItem("BaseType", "Button");
        string? deleteMessage = null;
        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(dialog => { deleteMessage = dialog.Message; return true; });
        tree.RightClick(tree.NodeFor(Component(tree, "Button")));
        tree.PickMenu("Delete");
        deleteMessage.ShouldNotBeNull().ShouldContain("IconButton");
        tree.Project.Project.Components.Select(component => component.Name).ShouldBe(new[] { "Panel", "IconButton" }, ignoreOrder: true);
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "Button.gucx")).ShouldBeFalse();
        Should.Throw<ShouldAssertException>(() => ProjectOracles.AssertCheckClean(tree.Project.ProjectFilePath))
            .Message.ShouldContain("IconButton");

        // The user points IconButton at a base that exists again; the project checks clean.
        tree.Click(tree.NodeFor(Component(tree, "IconButton")));
        grid.PickComboItem("BaseType", "Container");
        tree.AssertOracles();
    }

    #endregion

    #region Laying out a screen

    [AvaloniaFact]
    [Trait("Feature", "COMBO-023")]
    [Trait("Feature", "COMBO-024")]
    [Trait("Feature", "COMBO-038")]
    public void LayingOutAScreen_WithMultiSelectEditsPastedValuesAndADragIntoAComponent_UndoesBackInEachElement()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        tree.Project.AddScreen("Menu");
        tree.SaveAll();
        ProjectFileSnapshot empty = tree.SnapshotFiles();

        AddObject(tree, Screen(tree, "Menu"), "Container", "Box");
        AddObject(tree, Screen(tree, "Menu"), "Text", "Label");
        AddObject(tree, Screen(tree, "Menu"), "Sprite", "Icon");
        AddObject(tree, Component(tree, "Card"), "Container", "Frame");
        VariableGridHarness grid = tree.Grid;
        tree.Click(tree.NodeFor(Instance(Screen(tree, "Menu"), "Label")));
        grid.PickComboItem("Parent", "Box");
        tree.ChildTexts(tree.NodeFor(Instance(Screen(tree, "Menu"), "Box"))).ShouldBe(new[] { "Label" });

        // A Text and a Sprite selected together show the rows of both types. A row they share
        // sets both in one undo step; a row only one of them has sets only that one.
        tree.Click(tree.NodeFor(Instance(Screen(tree, "Menu"), "Label")));
        tree.Click(tree.NodeFor(Instance(Screen(tree, "Menu"), "Icon")), RawInputModifiers.Control);
        tree.SelectedState.SelectedInstances.Select(instance => instance.Name).ShouldBe(new[] { "Label", "Icon" }, ignoreOrder: true);
        grid.ShownMemberNames().ShouldContain("Label.Text");
        grid.ShownMemberNames().ShouldContain("Icon.SourceFile");
        grid.TypeAndEnter("Label.Y", "14");
        grid.TypeAndLeave("Label.Text", "Play");
        ScreenSave savedMenu = grid.ReadSaved(Screen(tree, "Menu"));
        VariableGridHarness.StoredValue(savedMenu, "Label.Y").ShouldBe(14f);
        VariableGridHarness.StoredValue(savedMenu, "Icon.Y").ShouldBe(14f);
        VariableGridHarness.StoredValue(savedMenu, "Label.Text").ShouldBe("Play");
        savedMenu.GetDefaultStateOrThrow().Variables.ShouldNotContain(variable => variable.Name == "Icon.Text");
        ProjectFileSnapshot multiEdited = tree.SnapshotFiles();
        tree.Undo();
        VariableGridHarness.StoredValue(Screen(tree, "Menu"), "Label.Text").ShouldBeNull();
        VariableGridHarness.StoredValue(Screen(tree, "Menu"), "Icon.Y").ShouldBe(14f);
        tree.Undo();
        VariableGridHarness.StoredValue(Screen(tree, "Menu"), "Label.Y").ShouldBeNull();
        VariableGridHarness.StoredValue(Screen(tree, "Menu"), "Icon.Y").ShouldBeNull();
        tree.Redo();
        tree.Redo();
        tree.SnapshotFiles().ShouldMatch(multiEdited, "redoing the multi-select edit should set both again");

        // Box's position is copied and pasted onto the Text and the Sprite at once.
        tree.Click(tree.NodeFor(Instance(Screen(tree, "Menu"), "Box")));
        grid.TypeAndEnter("X", "10");
        grid.TypeAndEnter("Y", "20");
        grid.PickCategoryMenuItem("Position", "Copy Values");
        tree.Click(tree.NodeFor(Instance(Screen(tree, "Menu"), "Label")));
        tree.Click(tree.NodeFor(Instance(Screen(tree, "Menu"), "Icon")), RawInputModifiers.Control);
        grid.PickCategoryMenuItem("Position", "Paste Values");
        savedMenu = grid.ReadSaved(Screen(tree, "Menu"));
        foreach (string name in new[] { "Label", "Icon" })
        {
            VariableGridHarness.StoredValue(savedMenu, $"{name}.X").ShouldBe(10f, name);
            VariableGridHarness.StoredValue(savedMenu, $"{name}.Y").ShouldBe(20f, name);
        }
        VariableGridHarness.StoredValue(savedMenu, "Label.Parent").ShouldBe("Box", "Paste Values takes the position values, not the parent");
        tree.Undo();
        VariableGridHarness.StoredValue(Screen(tree, "Menu"), "Label.X").ShouldBeNull();
        VariableGridHarness.StoredValue(Screen(tree, "Menu"), "Icon.Y").ShouldBe(14f);
        tree.Redo();
        VariableGridHarness.StoredValue(Screen(tree, "Menu"), "Icon.X").ShouldBe(10f);

        // Dragging Icon alone from Menu onto Card's Frame puts a copy inside Frame.
        tree.Click(tree.NodeFor(Instance(Screen(tree, "Menu"), "Icon")));
        tree.Drag(tree.NodeFor(Instance(Screen(tree, "Menu"), "Icon")), tree.NodeFor(Instance(Component(tree, "Card"), "Frame")));
        ComponentSave card = Component(tree, "Card");
        card.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Frame", "Icon" });
        Screen(tree, "Menu").Instances.Select(instance => instance.Name).ShouldBe(new[] { "Box", "Label", "Icon" }, "a drag into another element copies");
        VariableGridHarness.StoredValue(card, "Icon.Parent").ShouldBe("Frame");
        tree.ChildTexts(tree.NodeFor(Instance(card, "Frame"))).ShouldBe(new[] { "Icon" });
        VariableGridHarness.StoredValue(grid.ReadSaved(card), "Icon.Parent").ShouldBe("Frame");
        ProjectFileSnapshot finished = tree.SnapshotFiles();

        tree.Click(tree.NodeFor(card));
        tree.Undo();
        Component(tree, "Card").Instances.Select(instance => instance.Name).ShouldBe(new[] { "Frame" });
        tree.ChildTexts(tree.NodeFor(Component(tree, "Card"))).ShouldBe(new[] { "Frame" });
        tree.Redo();
        tree.SnapshotFiles().ShouldMatch(finished, "redoing the drop should restore the files");

        List<(string Element, int Steps)> undone = UndoEverything(tree, Component(tree, "Card"), Screen(tree, "Menu"));
        tree.SnapshotFiles().ShouldMatch(empty, "undoing every element should restore the files it started from");
        RedoEverything(tree, undone);
        tree.SnapshotFiles().ShouldMatch(finished, "redoing every element should restore the finished files");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "COMBO-026")]
    [Trait("Feature", "COMBO-039")]
    public void PromotingAParentInstanceToAComponent_KeepsItsPlaceAndItsChildsReferencedValue_AndTheHistorySkipsADeletedElement()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        tree.Project.AddScreen("Menu");
        tree.SaveAll();
        AddObject(tree, Screen(tree, "Menu"), "Container", "Box");
        AddObject(tree, Screen(tree, "Menu"), "Text", "Label");
        AddObject(tree, Screen(tree, "Menu"), "Text", "Caption");
        VariableGridHarness grid = tree.Grid;
        tree.Click(tree.NodeFor(Instance(Screen(tree, "Menu"), "Label")));
        grid.PickComboItem("Parent", "Box");
        tree.Click(tree.NodeFor(Instance(Screen(tree, "Menu"), "Box")));
        grid.TypeAndEnter("Width", "150");
        grid.TypeAndEnter("X", "30");
        // Label, inside Box, follows Box's width.
        tree.Click(tree.NodeFor(Instance(Screen(tree, "Menu"), "Label")));
        grid.TypeLinesAndApply("VariableReferences", "Width = Box.Width");
        VariableGridHarness.StoredValue(Screen(tree, "Menu"), "Label.Width").ShouldBe(150f);
        string menuFile = Path.Combine(tree.Project.ProjectFolder, "Screens", "Menu.gusx");
        string menuBeforePromotion = File.ReadAllText(menuFile);

        tree.Dialogs.AnswerNext<CreateComponentDialogViewModel>(dialog => { dialog.IsCheckboxChecked = true; return true; });
        tree.RightClick(tree.NodeFor(Instance(Screen(tree, "Menu"), "Box")));
        tree.PickMenu("Create Component");

        ComponentSave boxComponent = Component(tree, "BoxComponent");
        boxComponent.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Label" });
        // The reference to the promoted instance becomes its value; Box's size moves to the
        // component and its position stays on the replacement, which keeps Box's place.
        VariableGridHarness.StoredValue(boxComponent, "Label.Width").ShouldBe(150f);
        VariableGridHarness.StoredValue(boxComponent, "Width").ShouldBe(150f);
        Screen(tree, "Menu").Instances.Select(instance => instance.Name).ShouldBe(new[] { "Box", "Caption" });
        tree.ChildTexts(tree.NodeFor(Screen(tree, "Menu"))).ShouldBe(new[] { "Box", "Caption" });
        Instance(Screen(tree, "Menu"), "Box").BaseType.ShouldBe("BoxComponent");
        VariableGridHarness.StoredValue(grid.ReadSaved(Screen(tree, "Menu")), "Box.X").ShouldBe(30f);
        ProjectOracles.AssertCheckClean(tree.Project.ProjectFilePath);

        // The replacement is one undo step in Menu; the new component stays (adding an element
        // records no undo).
        tree.Click(tree.NodeFor(Screen(tree, "Menu")));
        tree.Undo();
        File.ReadAllText(menuFile).ShouldBe(menuBeforePromotion);
        tree.ChildTexts(tree.NodeFor(Screen(tree, "Menu"))).ShouldBe(new[] { "Box", "Caption" });
        tree.ChildTexts(tree.NodeFor(Instance(Screen(tree, "Menu"), "Box"))).ShouldBe(new[] { "Label" });
        tree.Redo();
        Instance(Screen(tree, "Menu"), "Box").BaseType.ShouldBe("BoxComponent");
        Screen(tree, "Menu").Instances.Select(instance => instance.Name).ShouldBe(new[] { "Box", "Caption" });

        // Visit Card, then BoxComponent, then delete Card: stepping back through the selection
        // history passes over the deleted element.
        tree.Click(tree.NodeFor(Component(tree, "Card")));
        tree.Click(tree.NodeFor(boxComponent));
        tree.Click(tree.NodeFor(Screen(tree, "Menu")));
        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(_ => true);
        tree.RightClick(tree.NodeFor(Component(tree, "Card")));
        tree.PickMenu("Delete");
        tree.Project.Project.Components.Select(component => component.Name).ShouldBe(new[] { "BoxComponent" });
        tree.Click(tree.NodeFor(Screen(tree, "Menu")));
        tree.Press(Key.Left, PhysicalKey.ArrowLeft, RawInputModifiers.Alt);
        tree.SelectedState.SelectedElement.ShouldBeSameAs(boxComponent);
        tree.Press(Key.Left, PhysicalKey.ArrowLeft, RawInputModifiers.Alt);
        tree.SelectedState.SelectedElement.ShouldNotBeNull().Name.ShouldNotBe("Card");
        tree.Press(Key.Right, PhysicalKey.ArrowRight, RawInputModifiers.Alt);
        tree.SelectedState.SelectedElement.ShouldNotBeNull().Name.ShouldNotBe("Card");

        tree.AssertOracles();
    }

    [AvaloniaFact(Skip = "#5377: Create Component leaves a reference from outside the promoted subtree pointing at an instance the element no longer has")]
    [Trait("Feature", "COMBO-026")]
    public void PromotingAParentInstanceToAComponent_LeavesNoReferenceToAnInstanceThatMovedIntoIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddScreen("Menu");
        tree.SaveAll();
        AddObject(tree, Screen(tree, "Menu"), "Container", "Box");
        AddObject(tree, Screen(tree, "Menu"), "Text", "Label");
        AddObject(tree, Screen(tree, "Menu"), "Text", "Caption");
        VariableGridHarness grid = tree.Grid;
        tree.Click(tree.NodeFor(Instance(Screen(tree, "Menu"), "Label")));
        grid.PickComboItem("Parent", "Box");
        grid.TypeAndEnter("X", "5");
        tree.Click(tree.NodeFor(Instance(Screen(tree, "Menu"), "Caption")));
        grid.TypeLinesAndApply("VariableReferences", "X = Label.X");
        VariableGridHarness.StoredValue(Screen(tree, "Menu"), "Caption.X").ShouldBe(5f);

        tree.Dialogs.AnswerNext<CreateComponentDialogViewModel>(dialog => { dialog.IsCheckboxChecked = true; return true; });
        tree.RightClick(tree.NodeFor(Instance(Screen(tree, "Menu"), "Box")));
        tree.PickMenu("Create Component");

        // Label now lives inside BoxComponent, so Menu's reference to it can never apply.
        VariableGridHarness.StoredValue(Screen(tree, "Menu"), "Caption.X").ShouldBe(5f);
        ProjectOracles.AssertCheckClean(tree.Project.ProjectFilePath);
        tree.AssertOracles();
    }

    #endregion

    #region Fonts

    [AvaloniaFact]
    [Trait("Feature", "COMBO-034")]
    public void AFontChangeInTheTool_IsWhatGumcliFontsGeneratesFromTheSavedProject_AndItsUndoIsToo()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Button");
        tree.Project.AddScreen("Title");
        AddObject(tree, Component(tree, "Button"), "Text", "Label");
        tree.Drag(tree.NodeFor(Component(tree, "Button")), tree.NodeFor(Screen(tree, "Title")));
        List<string> before = FontsGumcliWouldGenerate(tree.Project.ProjectFilePath);
        before.ShouldNotContain(path => path.Contains("37"));

        // A bigger, bold Label, saved as the tool saves each edit.
        tree.Click(tree.NodeFor(Instance(Component(tree, "Button"), "Label")));
        VariableGridHarness grid = tree.Grid;
        grid.TypeAndEnter("FontSize", "37");
        grid.Input.Click(grid.Editor<AvaloniaDataUi.Controls.CheckBoxDisplay>("IsBold").CheckBox);
        grid.Settle();
        VariableGridHarness.StoredValue(grid.ReadSaved(Component(tree, "Button")), "Label.FontSize").ShouldBe(37);
        VariableGridHarness.StoredValue(grid.ReadSaved(Component(tree, "Button")), "Label.IsBold").ShouldBe(true);

        List<string> after = FontsGumcliWouldGenerate(tree.Project.ProjectFilePath);
        after.ShouldContain(path => path.Contains("37") && path.Contains("Bold"), customMessage: string.Join(", ", after));
        after.ShouldAllBe(path => path.StartsWith(Path.Combine(tree.Project.ProjectFolder, "FontCache"), StringComparison.OrdinalIgnoreCase));

        tree.Undo();
        tree.Undo();
        FontsGumcliWouldGenerate(tree.Project.ProjectFilePath).ShouldBe(before, ignoreOrder: true);
        tree.Redo();
        tree.Redo();
        FontsGumcliWouldGenerate(tree.Project.ProjectFilePath).ShouldBe(after, ignoreOrder: true);

        tree.AssertOracles();
    }

    /// <summary>
    /// The .fnt files <c>gumcli fonts</c> would generate for the project saved at
    /// <paramref name="projectPath"/>: its load and font pass (<c>FontsCommand</c>), with a generator
    /// that records each request instead of running a font tool. The tool's own project stays loaded.
    /// </summary>
    private static List<string> FontsGumcliWouldGenerate(string projectPath)
    {
        GumProjectSave? toolProject = ObjectFinder.Self.GumProjectSave;
        string relativeDirectory = FileManager.RelativeDirectory;
        try
        {
            ProjectLoadResult loadResult = new ProjectLoader().Load(projectPath);
            loadResult.Success.ShouldBeTrue(loadResult.ErrorMessage);
            string projectDirectory = Path.GetDirectoryName(projectPath)! + Path.DirectorySeparatorChar;
            ObjectFinder.Self.GumProjectSave = loadResult.Project;
            FileManager.RelativeDirectory = projectDirectory;
            NoOpFontFileGenerator recorder = new NoOpFontFileGenerator();
            Task generation = new HeadlessFontGenerationService(recorder).CreateAllMissingFontFiles(loadResult.Project!, projectDirectory);
            while (!generation.IsCompleted)
            {
                Thread.Sleep(10);
                global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            }
            generation.GetAwaiter().GetResult();
            return recorder.RequestedFntPaths().Select(path => Path.GetFullPath(path)).Distinct().ToList();
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = toolProject;
            FileManager.RelativeDirectory = relativeDirectory;
        }
    }

    #endregion

    #region Project format

    [AvaloniaFact]
    [Trait("Feature", "COMBO-035")]
    public void AProjectConvertedToJson_ReopensAsJson_AndAFullEditCycleSavesJsonAndUndoesBack()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "Label", "Text");
        StateSaveCategory looks = tree.Project.AddCategory(button, "Looks");
        tree.Project.AddState(button, looks, "Pressed");
        ScreenSave title = tree.Project.AddScreen("Title");
        tree.Project.AddInstance(title, "OkButton", "Button");
        string components = Path.Combine(tree.Project.ProjectFolder, "Components");
        File.WriteAllText(Path.Combine(components, "ButtonAnimations.ganx"), AnimationFileWithKeyframe("Press", "Looks/Pressed"));
        tree.Click(tree.NodeFor(Instance(Screen(tree, "Title"), "OkButton")));
        VariableGridHarness grid = tree.Grid;
        grid.PickComboItem("LooksState", "Pressed");
        tree.SaveAll();

        tree.Dialogs.AnswerNext<ConvertToJsonDialogViewModel>(dialog => { dialog.ShouldRecycleXmlFiles = false; return true; });
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.PickMainMenu("Content", "Convert to JSON…");
        string jsonProject = Path.ChangeExtension(tree.Project.ProjectFilePath, GumProjectSave.ProjectJsonExtension);
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();
        tree.WaitUntil(() => projectManager.GumProjectSave?.FullFileName is { } open && new FilePath(open) == new FilePath(jsonProject),
            TimeSpan.FromSeconds(30), "the JSON project to open");
        tree.Project.FollowProjectFile(jsonProject);
        tree.TreeManager.RefreshUi();
        tree.Dialogs.Messages.Last().ShouldContain("Now editing");

        // What the XML held arrives in the JSON files the tool now edits.
        File.Exists(Path.Combine(components, "Button.gucj")).ShouldBeTrue();
        File.ReadAllText(Path.Combine(components, "ButtonAnimations.ganj")).ShouldContain("Looks/Pressed");
        VariableGridHarness.StoredValue(Screen(tree, "Title"), "OkButton.LooksState").ShouldBe("Pressed");
        Component(tree, "Button").Categories.Single().States.Single().Name.ShouldBe("Pressed");
        tree.SaveAll();
        ProjectFileSnapshot converted = tree.SnapshotFiles();

        // A full edit cycle on the JSON project: add, set, rename across elements.
        AddObject(tree, Component(tree, "Button"), "Sprite", "Icon");
        tree.Click(tree.NodeFor(Instance(Component(tree, "Button"), "Icon")));
        grid.TypeAndEnter("X", "12");
        tree.Click(tree.NodeFor(Instance(Screen(tree, "Title"), "OkButton")));
        grid.TypeAndEnter("X", "5");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        tree.Dialogs.AnswerNext<RenameElementDialogViewModel>(dialog => { dialog.Value = "PrimaryButton"; return true; });
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Press(Key.F2, PhysicalKey.F2);

        File.Exists(Path.Combine(components, "Button.gucj")).ShouldBeFalse();
        string primaryButton = File.ReadAllText(Path.Combine(components, "PrimaryButton.gucj"));
        primaryButton.ShouldContain("Icon.X");
        File.ReadAllText(Path.Combine(components, "PrimaryButtonAnimations.ganj")).ShouldContain("Looks/Pressed");
        File.ReadAllText(Path.Combine(tree.Project.ProjectFolder, "Screens", "Title.gusj")).ShouldContain("PrimaryButton");
        File.ReadAllText(Path.Combine(components, "Button.gucx")).ShouldNotContain("Icon", customMessage: "the XML files left behind are not edited");
        ProjectFileSnapshot finished = tree.SnapshotFiles();

        List<(string Element, int Steps)> undone = UndoEverything(tree, Screen(tree, "Title"), Component(tree, "PrimaryButton"));
        tree.SnapshotFiles().ShouldMatch(converted, "undoing every element should restore the converted files");
        RedoEverything(tree, undone);
        tree.SnapshotFiles().ShouldMatch(finished, "redoing every element should restore the finished files");

        tree.AssertOracles();
        new FilePath(projectManager.GumProjectSave!.FullFileName!).ShouldBe(new FilePath(jsonProject));
    }

    #endregion

    #region Changes from outside the tool

    [AvaloniaFact]
    [Trait("Feature", "COMBO-030")]
    public void AnElementFileEditedOutsideTheTool_WhileTheElementHasEdits_IsReloadedAndLaterUndoKeepsTheOutsideEdit()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Button");
        AddObject(tree, Component(tree, "Button"), "Text", "Label");
        VariableGridHarness grid = tree.Grid;
        tree.Click(tree.NodeFor(Instance(Component(tree, "Button"), "Label")));
        grid.TypeAndEnter("Width", "80");
        string buttonFile = Path.Combine(tree.Project.ProjectFolder, "Components", "Button.gucx");
        FileChangeReactionLogic fileChanges = TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>();
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();

        // Another program (a text editor, a merge) changes Label's width to 95 while the tool has
        // an edit to Button it has not saved yet.
        bool autoSave = projectManager.AutoSave;
        try
        {
            projectManager.AutoSave = false;
            grid.TypeAndEnter("X", "7");
            File.ReadAllText(buttonFile).ShouldNotContain("Label.X");
            File.WriteAllText(buttonFile, File.ReadAllText(buttonFile).Replace(">80</Value>", ">95</Value>"));
            fileChanges.ReactToFileChanged(new FilePath(buttonFile));
            tree.ThrowIfCrashed();
        }
        finally
        {
            projectManager.AutoSave = autoSave;
        }

        // The file on disk is what the tool shows after the reload. The unsaved X is dropped
        // without asking (#5379).
        ComponentSave button = Component(tree, "Button");
        VariableGridHarness.StoredValue(button, "Label.Width").ShouldBe(95f);
        tree.SelectedState.SelectedElement.ShouldBeSameAs(button);
        tree.ChildTexts(tree.NodeFor(button)).ShouldBe(new[] { "Label" });

        // The next edit is saved over the outside change without losing it, and undoing that
        // edit does not bring back what was there before the outside change.
        tree.Click(tree.NodeFor(Instance(button, "Label")));
        grid.FieldText("Width").ShouldBe("95");
        grid.TypeAndEnter("Height", "33");
        VariableGridHarness.StoredValue(grid.ReadSaved(button), "Label.Width").ShouldBe(95f);
        VariableGridHarness.StoredValue(grid.ReadSaved(button), "Label.Height").ShouldBe(33f);
        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Label.Height").ShouldBeNull();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Label.Width").ShouldBe(95f);
        VariableGridHarness.StoredValue(grid.ReadSaved(Component(tree, "Button")), "Label.Width").ShouldBe(95f);
        tree.Redo();
        VariableGridHarness.StoredValue(grid.ReadSaved(Component(tree, "Button")), "Label.Height").ShouldBe(33f);

        tree.AssertOracles();
    }

    #endregion

    #region Animations

    [AvaloniaFact]
    [Trait("Feature", "COMBO-029")]
    public void AnAnimation_PlaysAStatesNewValues_AfterTheStateIsEditedInTheVariablesTab_AndAfterTheEditIsUndone()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        // Pressed sets X to 0 and Released to 100.
        ComponentSave button = editor.AddComponent("Button", "Looks", "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe("Looks/Pressed");
        editor.AddStateKeyframe("Looks/Released");
        ProjectFileSnapshot start = editor.StartScenario();
        using VariableGridHarness grid = new VariableGridHarness(editor.Fixture);

        ScrubTo(editor, "0.5");
        ShownX(editor).ShouldBe(50f, tolerance: 0.5f);

        // Released moves to 300 in the Variables tab; the animation now passes through 150.
        StateSave released = button.Categories.Single().States.Single(state => state.Name == "Released");
        grid.Select(released);
        grid.TypeAndEnter("X", "300");
        VariableGridHarness.StoredValue(grid.ReadSaved(button), "X", grid.ReadSaved(button).Categories.Single().States.Single(state => state.Name == "Released")).ShouldBe(300f);
        grid.Select(button.GetDefaultStateOrThrow());
        ScrubTo(editor, "0.25");
        ScrubTo(editor, "0.5");
        ShownX(editor).ShouldBe(150f, tolerance: 0.5f);
        editor.Click(editor.PlayButton);
        editor.Wait(TimeSpan.FromMilliseconds(100));
        editor.Click(editor.PlayButton);
        editor.ViewModel.IsPlaying.ShouldBeFalse();

        // Undoing the edit brings the animation back to 50 at the same time.
        editor.Undo();
        button.Categories.Single().States.Single(state => state.Name == "Released").GetValue("X").ShouldBe(100f);
        ScrubTo(editor, "0.25");
        ScrubTo(editor, "0.5");
        ShownX(editor).ShouldBe(50f, tolerance: 0.5f);
        editor.SnapshotFiles().ShouldMatch(start, "undoing the state edit should restore the files");
        editor.Redo();
        ScrubTo(editor, "0.25");
        ScrubTo(editor, "0.5");
        ShownX(editor).ShouldBe(150f, tolerance: 0.5f);

        editor.AssertOracles();
    }

    private static void ScrubTo(AnimationEditorHarness editor, string seconds) =>
        editor.TypeAndEnter(editor.TimelineTimeBox, seconds);

    /// <summary>The X the tool shows for the animation at the scrubbed time.</summary>
    private static float ShownX(AnimationEditorHarness editor) =>
        (float)editor.SelectedState.CustomCurrentStateSave.ShouldNotBeNull().GetValue("X")!;

    #endregion

    #region Locked instances

    [AvaloniaFact]
    [Trait("Feature", "COMBO-036")]
    public void TheAlignmentTab_LeavesALockedInstanceWhereItIs()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Card");
        AddObject(tree, Component(tree, "Card"), "Container", "Panel");
        tree.Click(tree.NodeFor(Instance(Component(tree, "Card"), "Panel")));
        tree.Grid.TypeAndEnter("X", "25");
        tree.RightClick(tree.NodeFor(Instance(Component(tree, "Card"), "Panel")));
        tree.PickMenu("Lock Panel");
        ProjectFileSnapshot locked = tree.SnapshotFiles();
        global::Avalonia.Controls.Control alignmentView = (global::Avalonia.Controls.Control)
            ((AvaloniaTabManager)TestAppBuilder.Services.GetRequiredService<ITabManager>()).AllTabs.Single(tab => tab.Title == "Alignment").Content;
        using HeadlessWindowDriver alignment = new HeadlessWindowDriver(alignmentView, 500, 700, framesFolderName: "GumComboScenarios");

        foreach (string tip in new[] { "Anchor Bottom Right", "Fill", "Size to Children" })
        {
            alignment.Click(alignment.Window.GetVisualDescendants().OfType<global::Avalonia.Controls.Button>()
                .Single(button => global::Avalonia.Controls.ToolTip.GetTip(button) as string == tip));
            tree.ThrowIfCrashed();
        }

        tree.SnapshotFiles().ShouldMatch(locked, "a locked instance should keep its place and size");
        tree.AssertOracles();
    }

    #endregion

    #region Behaviors

    [AvaloniaFact]
    [Trait("Feature", "COMBO-037")]
    public void ABehaviorAddedToAComponent_KeepsItsRequiredStateUntilTheBehaviorIsRemoved()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Dialogs.AnswerNextUserString("Clickable");
        tree.RightClick(tree.RootNode("Behaviors"));
        tree.PickMenu("Add Behavior");
        StatesTabHarness states = tree.States;
        tree.Dialogs.AnswerNext<AddCategoryDialogViewModel>(dialog => { dialog.Value = "Looks"; return true; });
        states.ClickNewCategory();
        tree.Dialogs.AnswerNext<AddStateDialogViewModel>(dialog => { dialog.Value = "Pressed"; return true; });
        states.ClickAddState(states.ItemFor("Looks"));
        tree.Project.AddComponent("Button");
        tree.SaveAll();
        ProjectFileSnapshot start = tree.SnapshotFiles();
        AddObject(tree, Component(tree, "Button"), "Text", "Label");
        tree.Click(tree.NodeFor(Component(tree, "Button")));

        // Adding the behavior gives Button the category and state it requires.
        BehaviorsViewModel behaviors = BehaviorsTab();
        behaviors.EditCommand.Execute(null);
        behaviors.AllBehaviors.Single(item => item.Name == "Clickable").IsChecked = true;
        behaviors.ConfirmEditCommand.Execute(null);
        tree.ThrowIfCrashed();
        states.Shown().ShouldBe(new[] { "Looks: Pressed" });
        VariableGridHarness grid = tree.Grid;
        ComponentSave saved = grid.ReadSaved(Component(tree, "Button"));
        saved.Behaviors.Select(behavior => behavior.BehaviorName).ShouldBe(new[] { "Clickable" });
        saved.Categories.Single().States.Select(state => state.Name).ShouldBe(new[] { "Pressed" });

        // The required state gets a value, then the user tries to delete it: the behavior needs
        // it, so the tool says so and keeps it.
        tree.Click(tree.NodeFor(Instance(Component(tree, "Button"), "Label")));
        states.Click(states.ItemFor("Looks", "Pressed"));
        grid.TypeAndEnter("Width", "90");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        ProjectFileSnapshot withBehavior = tree.SnapshotFiles();
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        states.RightClick(states.ItemFor("Looks", "Pressed"));
        states.PickMenu("Delete [Pressed]");
        tree.Dialogs.Messages.Last().ShouldContain("Clickable");
        states.Shown().ShouldBe(new[] { "Looks: Pressed" });
        tree.SnapshotFiles().ShouldMatch(withBehavior, "a refused delete should change no file");

        // Without the behavior, the state is Button's own to delete.
        behaviors.EditCommand.Execute(null);
        behaviors.AllBehaviors.Single(item => item.Name == "Clickable").IsChecked = false;
        behaviors.ConfirmEditCommand.Execute(null);
        tree.ThrowIfCrashed();
        Component(tree, "Button").Behaviors.ShouldBeEmpty();
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        states.RightClick(states.ItemFor("Looks", "Pressed"));
        states.PickMenu("Delete [Pressed]");
        states.Shown().ShouldBe(new[] { "Looks: " });
        ProjectFileSnapshot finished = tree.SnapshotFiles();

        List<(string Element, int Steps)> undone = UndoEverything(tree, Component(tree, "Button"));
        tree.SnapshotFiles().ShouldMatch(start, "undoing every step in Button should restore the files");
        RedoEverything(tree, undone);
        tree.SnapshotFiles().ShouldMatch(finished, "redoing every step in Button should restore the finished files");

        tree.AssertOracles();
    }

    /// <summary>The view model behind the head's Behaviors tab.</summary>
    private static BehaviorsViewModel BehaviorsTab() =>
        ((AvaloniaTabManager)TestAppBuilder.Services.GetRequiredService<ITabManager>()).AllTabs
            .Select(tab => tab.Content is global::Avalonia.Controls.Control view ? view.DataContext : tab.Content)
            .OfType<BehaviorsViewModel>().Single();

    #endregion

    #region Code generation

    [AvaloniaFact]
    [Trait("Feature", "COMBO-027")]
    [Trait("Feature", "COMBO-028")]
    public void AComponentWithStatesAnimationsAndCodeSettings_IsDuplicatedMovedAndRenamed_AndItsCodeFollows()
    {
        using CodeTabHarness code = new CodeTabHarness();
        ProjectTreeHarness tree = code.Tree;
        tree.Dialogs.AnswerNext<AddFolderDialogViewModel>(dialog => { dialog.Value = "Controls"; return true; });
        tree.RightClick(tree.RootNode("Components"));
        tree.PickMenu("Add Folder");
        ComponentSave button = code.Project.AddComponent("Button");
        code.Project.AddInstance(button, "Label", "Text");
        StateSaveCategory looks = code.Project.AddCategory(button, "Looks");
        code.Project.AddState(button, looks, "Pressed");
        string components = Path.Combine(code.Project.ProjectFolder, "Components");
        File.WriteAllText(Path.Combine(components, "ButtonAnimations.ganx"), AnimationFileWithKeyframe("Press", "Looks/Pressed"));
        tree.SaveAll();
        // The Animations tab read Button's (then missing) file when Button was added; selecting
        // another element and back makes it read the file written above.
        tree.Click(tree.RootNode("Components"));
        code.Select(Component(tree, "Button"));

        code.SetUpManualGeneration();
        code.TypeAndEnter("Root Namespace", "MyGame");
        code.TypeAndLeave("Using Statements", "using System.Numerics;");
        code.ClickGenerate();
        File.ReadAllText(code.CodeFile("Components/Button.Generated.cs")).ShouldContain("partial class Button");
        File.ReadAllText(Path.Combine(components, "Button.codsj")).ShouldContain("System.Numerics");

        // Duplicate: the copy has Button's states, animations and element code settings.
        tree.Dialogs.AnswerNextUserString("ButtonCopy");
        tree.RightClick(tree.NodeFor(Component(tree, "Button")));
        tree.PickMenu("Duplicate Button");
        ComponentSave copy = Component(tree, "ButtonCopy");
        copy.Categories.Single().States.Select(state => state.Name).ShouldBe(new[] { "Pressed" });
        copy.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Label" });
        File.ReadAllText(Path.Combine(components, "ButtonCopyAnimations.ganx")).ShouldContain("<StateName>Looks/Pressed</StateName>");
        code.Select(copy);
        code.Member("Using Statements").Value.ShouldBe("using System.Numerics;");
        code.ClickGenerate();
        string copyCode = File.ReadAllText(code.CodeFile("Components/ButtonCopy.Generated.cs"));
        copyCode.ShouldContain("partial class ButtonCopy");
        copyCode.ShouldContain("using System.Numerics;");
        ProjectFileSnapshot duplicated = tree.SnapshotFiles();

        // Move the copy into the Controls folder with a drag, then generate: the code moves with it.
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Drag(tree.NodeFor(copy), tree.FolderNode("Components", "Controls"));
        tree.Dialogs.Messages.Last().ShouldContain("Are you sure you want to move ButtonCopy?");
        copy.Name.ShouldBe("Controls/ButtonCopy");
        File.ReadAllText(Path.Combine(components, "Controls", "ButtonCopyAnimations.ganx")).ShouldContain("<StateName>Looks/Pressed</StateName>");
        File.Exists(Path.Combine(components, "Controls", "ButtonCopy.codsj")).ShouldBeTrue();
        code.Select(copy);
        code.ClickGenerate();
        string movedCode = File.ReadAllText(code.CodeFile("Components/Controls/ButtonCopy.Generated.cs"));
        movedCode.ShouldContain("namespace MyGame.Components.Controls");
        movedCode.ShouldContain("partial class ButtonCopy");
        File.Exists(code.CodeFile("Components/ButtonCopy.Generated.cs")).ShouldBeFalse("the move takes the generated file along");

        // Rename it, then regenerate: one class under the new name, no file left under the old one.
        tree.Dialogs.AnswerNext<RenameElementDialogViewModel>(dialog => { dialog.Value = "IconButton"; return true; });
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Click(tree.NodeFor(copy));
        tree.Press(Key.F2, PhysicalKey.F2);
        copy.Name.ShouldBe("Controls/IconButton");
        code.ClickGenerate();
        File.ReadAllText(code.CodeFile("Components/Controls/IconButton.Generated.cs")).ShouldContain("partial class IconButton");
        File.ReadAllText(code.CodeFile("Components/Controls/IconButton.cs")).ShouldContain("partial class IconButton");
        Directory.GetFiles(code.CodeFolder, "ButtonCopy*", SearchOption.AllDirectories).ShouldBeEmpty();
        File.ReadAllText(Path.Combine(components, "Controls", "IconButtonAnimations.ganx")).ShouldContain("<StateName>Looks/Pressed</StateName>");
        ProjectFileSnapshot finished = tree.SnapshotFiles();

        // Undo the rename and the move in the copy; its files and code go back where they were.
        tree.Undo();
        copy.Name.ShouldBe("Controls/ButtonCopy");
        tree.Undo();
        copy.Name.ShouldBe("ButtonCopy");
        tree.SnapshotFiles().ShouldMatch(duplicated, "undoing the rename and the move should restore the files");
        tree.Redo();
        tree.Redo();
        copy.Name.ShouldBe("Controls/IconButton");
        tree.SnapshotFiles().ShouldMatch(finished, "redoing the move and the rename should restore the finished files");

        code.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "COMBO-025")]
    public void DuplicatingAComponentWithStatesAnimationsAndCodeSettings_CopiesAllThree()
    {
        using CodeTabHarness code = new CodeTabHarness();
        ProjectTreeHarness tree = code.Tree;
        ComponentSave button = code.Project.AddComponent("Button");
        StateSaveCategory looks = code.Project.AddCategory(button, "Looks");
        code.Project.AddState(button, looks, "Pressed");
        string components = Path.Combine(code.Project.ProjectFolder, "Components");
        File.WriteAllText(Path.Combine(components, "ButtonAnimations.ganx"), AnimationFileWithKeyframe("Press", "Looks/Pressed"));
        tree.SaveAll();
        tree.Click(tree.RootNode("Components"));
        code.Select(Component(tree, "Button"));
        code.SetUpManualGeneration();
        code.TypeAndLeave("Using Statements", "using System.Numerics;");

        tree.Dialogs.AnswerNextUserString("ButtonCopy");
        tree.RightClick(tree.NodeFor(Component(tree, "Button")));
        tree.PickMenu("Duplicate Button");

        ComponentSave copy = Component(tree, "ButtonCopy");
        copy.Categories.Single().States.Select(state => state.Name).ShouldBe(new[] { "Pressed" });
        File.ReadAllText(Path.Combine(components, "ButtonCopyAnimations.ganx")).ShouldContain("<StateName>Looks/Pressed</StateName>");
        File.Exists(Path.Combine(components, "ButtonCopy.codsj")).ShouldBeTrue("the copy keeps the element's code settings");
        code.Select(copy);
        code.Member("Using Statements").Value.ShouldBe("using System.Numerics;");
        code.Member("Generation Behavior").Value?.ToString().ShouldBe("GenerateManually");

        code.AssertOracles();
    }

    #endregion

    #region Helpers

    private static string AnimationFileWithKeyframe(string animationName, string stateName) =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
        "<ElementAnimationsSave xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\r\n" +
        "  <Animations>\r\n" +
        "    <AnimationSave>\r\n" +
        "      <Loops>false</Loops>\r\n" +
        $"      <Name>{animationName}</Name>\r\n" +
        "      <States>\r\n" +
        "        <AnimatedStateSave>\r\n" +
        $"          <StateName>{stateName}</StateName>\r\n" +
        "          <Time>0</Time>\r\n" +
        "          <InterpolationType>Linear</InterpolationType>\r\n" +
        "          <Easing>Out</Easing>\r\n" +
        "        </AnimatedStateSave>\r\n" +
        "      </States>\r\n" +
        "      <Animations />\r\n" +
        "      <Events />\r\n" +
        "    </AnimationSave>\r\n" +
        "  </Animations>\r\n" +
        "</ElementAnimationsSave>";

    /// <summary>
    /// Selects each element in turn and presses Ctrl+Z until it has nothing left to undo (undo
    /// history is per element). Returns how many steps each element undid, for <see cref="RedoEverything"/>.
    /// </summary>
    private static List<(string Element, int Steps)> UndoEverything(ProjectTreeHarness tree, params ElementSave[] elements)
    {
        List<(string Element, int Steps)> undone = new List<(string Element, int Steps)>();
        foreach (ElementSave element in elements)
        {
            tree.Click(tree.NodeFor(element));
            int steps = 0;
            while (tree.UndoManager.CanUndo())
            {
                steps.ShouldBeLessThan(100, $"{element.Name} kept offering undo");
                tree.Undo();
                steps++;
            }
            undone.Add((element.Name, steps));
        }
        return undone;
    }

    /// <summary>
    /// Redoes what <see cref="UndoEverything"/> undid, element by element in the reverse order; a
    /// step that was already undone before it (and so is still on the redo stack) stays undone.
    /// </summary>
    private static void RedoEverything(ProjectTreeHarness tree, List<(string Element, int Steps)> undone)
    {
        foreach ((string elementName, int steps) in Enumerable.Reverse(undone))
        {
            ElementSave element = tree.Project.Project.Screens.Cast<ElementSave>().Concat(tree.Project.Project.Components)
                .Single(candidate => candidate.Name == elementName);
            tree.Click(tree.NodeFor(element));
            for (int i = 0; i < steps; i++)
            {
                tree.UndoManager.CanRedo().ShouldBeTrue($"{elementName} has only {i} of its {steps} steps to redo");
                tree.Redo();
            }
        }
    }

    /// <summary>
    /// Adds a <paramref name="type"/> at <paramref name="element"/>'s root from its right-click
    /// menu and names it <paramref name="name"/> with F2, as a user building a layout does.
    /// </summary>
    private static InstanceSave AddObject(ProjectTreeHarness tree, ElementSave element, string type, string name)
    {
        tree.RightClick(tree.NodeFor(element));
        tree.PickMenu($"Add object to {element.Name}", type);
        tree.Dialogs.AnswerNextUserString(name);
        tree.Press(Key.F2, PhysicalKey.F2);
        return Instance(element, name);
    }

    private static InstanceSave Instance(ElementSave element, string name) =>
        element.Instances.SingleOrDefault(instance => instance.Name == name)
            ?? throw new InvalidOperationException($"{element.Name} has no instance {name}; it has [{string.Join(", ", element.Instances.Select(instance => instance.Name))}].");

    private static ScreenSave Screen(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Screens.Single(screen => screen.Name == name);

    private static ComponentSave Component(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Components.Single(component => component.Name == name);

    #endregion
}
