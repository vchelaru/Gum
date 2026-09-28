using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.VisualTree;
using AvaloniaDataUi;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Panels;
using Gum.Avalonia.Plugins.TreeView;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.Avalonia.Services;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.PropertiesWindowPlugin;
using Gum.Services.Dialogs;
using Gum.Wireframe;
using Microsoft.Extensions.DependencyInjection;
using RenderingLibrary.Graphics;
using Shouldly;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on the grid editors (inventory area DISP), the Project Properties tab
/// (PROP) and the Standards palette (PAL): each drives the real editor, tab or chip with pointer
/// and key input, checks what reached the project and its files, undoes back to identical files
/// where the gesture edits the project, and ends with the shared oracles.
/// </summary>
[Trait("Category", "EndToEnd")]
public class DisplayPropertiesScenarioTests
{
    private static readonly TimeSpan AsyncWork = TimeSpan.FromSeconds(60);

    private static IServiceProvider Services => TestAppBuilder.Services;

    #region Variables tab editors

    [AvaloniaFact]
    [Trait("Feature", "DISP-008")]
    [Trait("Feature", "DISP-010")]
    [Trait("Feature", "DISP-011")]
    public void SliderAngleAndFileEditors_OnASprite_SaveWhatTheyShow_AndUndoRestoresTheFiles()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave icon = tree.Project.AddInstance(button, "Icon", "Sprite");
        string texture = Path.Combine(tree.Project.ProjectFolder, "Hero.png");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Content", "ExampleSpriteFrame.png"), texture);
        tree.Click(tree.NodeFor(icon));
        VariableGridHarness grid = tree.Grid;
        ProjectFileSnapshot start = tree.SnapshotFiles();

        // The Alpha slider's thumb, dragged past the left end of its track.
        Slider alpha = grid.Row("Alpha").GetVisualDescendants().OfType<Slider>().Single();
        Control thumb = alpha.GetVisualDescendants().OfType<global::Avalonia.Controls.Primitives.Thumb>().Single();
        Point thumbCenter = grid.Input.CenterOf(thumb);
        Point trackLeft = grid.Input.CenterOf(alpha).WithX(grid.Input.CenterOf(alpha).X - alpha.Bounds.Width);
        grid.Input.Drag(thumbCenter, trackLeft, steps: 8);
        grid.Settle();

        // The angle's field takes degrees.
        grid.Input.TypeAndEnter(grid.Editor<AngleSelectorDisplay>("Rotation").TextBox, "45");
        grid.Settle();

        // The file editor's "..." opens the tool's file picker.
        tree.Dialogs.AnswerNextOpenFile(texture);
        grid.Input.Click(FileButton(grid.Row("SourceFile")));
        grid.Settle();

        ComponentSave saved = grid.ReadSaved(Component(tree, "Button"));
        VariableGridHarness.StoredValue(saved, "Icon.Alpha").ShouldBe(0);
        VariableGridHarness.StoredValue(saved, "Icon.Rotation").ShouldBe(45f);
        VariableGridHarness.StoredValue(saved, "Icon.SourceFile").ShouldBe("Hero.png");
        grid.Editor<FileSelectionDisplay>("SourceFile").TextBox.Text.ShouldBe("Hero.png");

        tree.Undo();
        tree.Undo();
        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the three edits should restore the files");
        grid.Editor<AngleSelectorDisplay>("Rotation").TextBox.Text.ShouldBe("0");

        tree.Redo();
        tree.Redo();
        tree.Redo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Icon.SourceFile").ShouldBe("Hero.png");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DISP-015")]
    [Trait("Feature", "DISP-020")]
    [Trait("Feature", "DISP-021")]
    [Trait("Feature", "DISP-022")]
    [Trait("Feature", "DISP-023")]
    [Trait("Feature", "DISP-024")]
    public void ToggleEditors_OnAText_SaveTheOptionPressed_AndUndoRestoresTheFiles()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(label));
        VariableGridHarness grid = tree.Grid;
        ProjectFileSnapshot start = tree.SnapshotFiles();

        grid.PressToggle("WidthUnits", DimensionUnitType.PercentageOfParent);
        grid.PressToggle("XUnits", PositionUnitType.PixelsFromRight);
        grid.PressToggle("XOrigin", HorizontalAlignment.Right);
        grid.PressToggle("HorizontalAlignment", HorizontalAlignment.Center);
        grid.PressToggle("TextOverflowVerticalMode", TextOverflowVerticalMode.TruncateLine);

        ComponentSave edited = Component(tree, "Button");
        VariableGridHarness.StoredValue(edited, "Label.WidthUnits").ShouldBe(DimensionUnitType.PercentageOfParent);
        VariableGridHarness.StoredValue(edited, "Label.XUnits").ShouldBe(PositionUnitType.PixelsFromRight);
        VariableGridHarness.StoredValue(edited, "Label.XOrigin").ShouldBe(HorizontalAlignment.Right);
        VariableGridHarness.StoredValue(edited, "Label.HorizontalAlignment").ShouldBe(HorizontalAlignment.Center);
        VariableGridHarness.StoredValue(edited, "Label.TextOverflowVerticalMode").ShouldBe(TextOverflowVerticalMode.TruncateLine);
        ComponentSave saved = grid.ReadSaved(edited);
        Convert.ToInt32(VariableGridHarness.StoredValue(saved, "Label.XUnits")).ShouldBe((int)PositionUnitType.PixelsFromRight);
        Convert.ToInt32(VariableGridHarness.StoredValue(saved, "Label.TextOverflowVerticalMode")).ShouldBe((int)TextOverflowVerticalMode.TruncateLine);
        // The pressed toggle is the one shown checked.
        grid.Toggles("XOrigin").Single(toggle => toggle.IsChecked == true).Tag.ShouldBeOfType<ToggleButtonOption>().Value.ShouldBe(HorizontalAlignment.Right);

        for (int i = 0; i < 5; i++)
        {
            tree.Undo();
        }
        tree.SnapshotFiles().ShouldMatch(start, "undoing the five toggles should restore the files");
        grid.Toggles("XOrigin").Single(toggle => toggle.IsChecked == true).Tag.ShouldBeOfType<ToggleButtonOption>().Value.ShouldBe(HorizontalAlignment.Left);

        tree.Redo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Label.WidthUnits").ShouldBe(DimensionUnitType.PercentageOfParent);

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DISP-017")]
    public void CornerRadius_TypedLinked_ThenUnlinkedAndOneCornerTyped_SavesEachChannel_AndUndoRestoresTheFiles()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave background = tree.Project.AddInstance(button, "Background", "Rectangle");
        tree.Click(tree.NodeFor(background));
        VariableGridHarness grid = tree.Grid;
        ProjectFileSnapshot start = tree.SnapshotFiles();

        CornerRadiusDisplay corners = grid.Editor<CornerRadiusDisplay>("CornerRadius");
        grid.Input.TypeAndEnter(corners.UniformTextBox, "6");
        grid.Settle();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Background.CornerRadius").ShouldBe(6f);

        // Unlinking shows the four corners, each starting at the uniform radius.
        corners = grid.Editor<CornerRadiusDisplay>("CornerRadius");
        grid.Input.Click(corners.GetVisualDescendants().OfType<Button>().Single());
        grid.Settle();
        corners = grid.Editor<CornerRadiusDisplay>("CornerRadius");
        corners.IsLinked.ShouldBeFalse();
        grid.Input.TypeAndEnter(corners.CornerTextBoxes[3], "12");
        grid.Settle();

        ComponentSave saved = grid.ReadSaved(Component(tree, "Button"));
        VariableGridHarness.StoredValue(saved, "Background.CornerRadius").ShouldBe(6f);
        VariableGridHarness.StoredValue(saved, "Background.CustomRadiusBottomRight").ShouldBe(12f);

        // The typed radius, the unlink (which seeds the corners) and the typed corner.
        for (int undo = 0; undo < 5 && Component(tree, "Button").DefaultState!.Variables.Any(variable => variable.Name.StartsWith("Background.C", StringComparison.Ordinal) && variable.SetsValue); undo++)
        {
            tree.Undo();
        }
        tree.SnapshotFiles().ShouldMatch(start, "undoing the radius edits should restore the files");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DISP-018")]
    public void RemoveButton_OnACategorysVariable_RemovesItFromEveryState_AndUndoRestoresTheFiles()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        StateSaveCategory size = tree.Project.AddCategory(button, "Size");
        StateSave big = tree.Project.AddState(button, size, "Big");
        StateSave small = tree.Project.AddState(button, size, "Small");
        using (tree.UndoManager.RequestLock(button))
        {
            big.SetValue("Width", 300f, "float");
            small.SetValue("Width", 50f, "float");
        }
        tree.SaveAll();
        tree.Click(tree.NodeFor(button));
        StatesTabHarness states = tree.States;
        states.Click(states.ItemFor("Size"));
        VariableGridHarness grid = tree.Grid;
        ProjectFileSnapshot start = tree.SnapshotFiles();

        VariableRemoveButton remove = grid.Editor<VariableRemoveButton>("Width");
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        grid.Input.Click(remove.GetVisualDescendants().OfType<Button>().Single());
        grid.Settle();

        StateSaveCategory edited = Component(tree, "Button").Categories.Single();
        edited.States.ShouldAllBe(state => state.GetVariableSave("Width") == null);
        grid.ReadSaved(Component(tree, "Button")).Categories.Single().States.ShouldAllBe(state => state.GetVariableSave("Width") == null);

        tree.Undo();
        Component(tree, "Button").Categories.Single().States.Single(state => state.Name == "Big").GetValue("Width").ShouldBe(300f);
        tree.SnapshotFiles().ShouldMatch(start, "undoing the removal should restore the files");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DISP-007")]
    public void ListEditor_OnAPolygon_AddsAPoint_AndUndoRestoresTheFiles()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave outline = tree.Project.AddInstance(button, "Outline", "Polygon");
        tree.Click(tree.NodeFor(outline));
        VariableGridHarness grid = tree.Grid;
        int shownPoints = grid.Editor<ListBoxDisplay>("Points").ListBox.ItemCount;
        ProjectFileSnapshot start = tree.SnapshotFiles();

        ListBoxDisplay points = grid.Editor<ListBoxDisplay>("Points");
        grid.Input.Click(points.GetVisualDescendants().OfType<Button>().Single(candidate => candidate.Content as string == "+"));
        TextBox entry = points.GetVisualDescendants().OfType<TextBox>().Single(box => box.IsEffectivelyVisible);
        grid.Input.TypeAndEnter(entry, "7,9");
        grid.Settle();

        grid.Editor<ListBoxDisplay>("Points").ListBox.ItemCount.ShouldBe(shownPoints + 1);
        VariableListSave saved = grid.ReadSaved(Component(tree, "Button")).DefaultState!.GetVariableListSave("Outline.Points").ShouldNotBeNull();
        saved.ValueAsIList.Cast<System.Numerics.Vector2>().Last().ShouldBe(new System.Numerics.Vector2(7, 9));

        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the added point should restore the files");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DISP-004")]
    public void NullableBoolEditor_OnABehaviorsBoolQuestionFormsProperty_SavesTrueFalseAndNone_AndUndoRestoresTheFiles()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Dialogs.AnswerNextUserString("Toggleable");
        tree.RightClick(tree.RootNode("Behaviors"));
        tree.PickMenu("Add Behavior");
        // The tool has no gesture that declares a Forms property, so this one stands for a .behx
        // written by hand. It is the one bool? row the grid shows with NullableBool: a bool? element
        // variable gets a type converter whose options turn its row into a combo box.
        BehaviorSave toggleable = tree.Project.Project.Behaviors.Single();
        toggleable.FormsProperties.Add(new VariableSave { Name = "IsToggled", Type = "bool?" });
        Services.GetRequiredService<Gum.Commands.IFileCommands>().TryAutoSaveBehavior(toggleable);
        button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "Toggleable" });
        tree.Project.SaveAndReload();
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        VariableGridHarness grid = tree.Grid;
        ProjectFileSnapshot start = tree.SnapshotFiles();

        ClickOption(grid, "True");
        VariableGridHarness.StoredValue(grid.ReadSaved(Component(tree, "Button")), "IsToggled").ShouldBe(true);

        ClickOption(grid, "False");
        VariableGridHarness.StoredValue(grid.ReadSaved(Component(tree, "Button")), "IsToggled").ShouldBe(false);

        ClickOption(grid, "None");
        VariableGridHarness.StoredValue(grid.ReadSaved(Component(tree, "Button")), "IsToggled").ShouldBeNull();
        OptionButton(grid, "None").IsChecked.ShouldBe(true);

        tree.Undo();
        OptionButton(grid, "False").IsChecked.ShouldBe(true, "undo shows the restored value");
        tree.Undo();
        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the three picks should restore the files");

        tree.AssertOracles();

        static RadioButton OptionButton(VariableGridHarness grid, string option) =>
            grid.Editor<NullableBoolDisplay>("IsToggled").GetVisualDescendants().OfType<RadioButton>().Single(button => button.Content as string == option);

        static void ClickOption(VariableGridHarness grid, string option)
        {
            grid.Input.Click(OptionButton(grid, option));
            grid.Settle();
        }
    }

    #endregion

    #region Project Properties

    [AvaloniaFact]
    [Trait("Feature", "PROP-002")]
    [Trait("Feature", "PROP-006")]
    [Trait("Feature", "PROP-008")]
    [Trait("Feature", "PROP-016")]
    [Trait("Feature", "PROP-017")]
    public void ProjectProperties_EditedInTheTabsGrid_SaveIntoTheProjectFile()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        using ProjectPropertiesTab properties = new ProjectPropertiesTab(tree);
        // The canvas size is process-wide, and only a canvas's project load sets it again.
        (float Width, float Height) originalCanvas = (GraphicalUiElement.CanvasWidth, GraphicalUiElement.CanvasHeight);
        try
        {
            EditEachProperty(tree, properties);
        }
        finally
        {
            (GraphicalUiElement.CanvasWidth, GraphicalUiElement.CanvasHeight) = originalCanvas;
        }

        tree.AssertOracles();
    }

    private static void EditEachProperty(ProjectTreeHarness tree, ProjectPropertiesTab properties)
    {
        properties.TypeAndEnter("CanvasWidth", "1024");
        properties.TypeAndEnter("CanvasHeight", "600");
        properties.PickComboItem("TextureFilter", nameof(TextureFilter.Linear));
        properties.ClickCheckBox("RestrictFileNamesForAndroid");
        properties.ClickCheckBox("AutoSizeFontOutputs");
        if (OperatingSystem.IsWindows())
        {
            FontGeneratorType generator = properties.ViewModel.FontGenerator == FontGeneratorType.KernSmith ? FontGeneratorType.BmFont : FontGeneratorType.KernSmith;
            properties.PickComboItem("FontGenerator", generator.ToString());
            tree.WaitUntil(() => SavedProject(tree).FontGenerator == generator, AsyncWork, "the font generator to save");
        }
        else
        {
            // bmfont.exe runs only on Windows, so elsewhere the row is read-only and KernSmith is used.
            properties.Row("FontGenerator").GetVisualDescendants().OfType<ComboBox>().Single().IsEffectivelyEnabled.ShouldBeFalse();
            tree.WaitUntil(() => SavedProject(tree).AutoSizeFontOutputs != new GumProjectSave().AutoSizeFontOutputs, AsyncWork, "the font outputs setting to save");
        }

        GumProjectSave saved = SavedProject(tree);
        (saved.DefaultCanvasWidth, saved.DefaultCanvasHeight).ShouldBe((1024, 600));
        saved.TextureFilter.ShouldBe(nameof(TextureFilter.Linear));
        saved.RestrictFileNamesForAndroid.ShouldBeTrue();
        saved.AutoSizeFontOutputs.ShouldNotBe(new GumProjectSave().AutoSizeFontOutputs);
        (GraphicalUiElement.CanvasWidth, GraphicalUiElement.CanvasHeight).ShouldBe((1024f, 600f), "the canvas size applies at once");
    }

    [SkippableFact]
    [Trait("Feature", "PROP-003")]
    [Trait("Feature", "PROP-004")]
    [Trait("Feature", "PROP-005")]
    public void Guides_TurnedOffAndOnInTheTab_DisappearFromAndReturnToTheCanvas()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            canvas.AddInstance(button, "Panel", "Container", x: 300, y: 200, width: 100, height: 80);
            canvas.Tree.Click(canvas.Tree.NodeFor(button));
            using ProjectPropertiesTab properties = new ProjectPropertiesTab(canvas.Tree);
            ProjectPropertiesViewModel viewModel = properties.ViewModel;
            (bool Outlines, bool CanvasOutline, bool Checker) original = (viewModel.ShowOutlines, viewModel.ShowCanvasOutline, viewModel.ShowCheckerBackground);
            bool originalLineRectangles = GraphicalUiElement.ShowLineRectangles;
            try
            {
                // Each guide starts on, so the canvas shows it.
                foreach (string guide in new[] { "ShowOutlines", "ShowCanvasOutline", "ShowCheckerBackground" })
                {
                    if (properties.Editor<CheckBoxDisplay>(guide).CheckBox.IsChecked != true)
                    {
                        properties.ClickCheckBox(guide);
                    }
                }
                canvas.Frame();
                // The panel's outline runs down its left edge; the canvas outline along the canvas's bottom
                // edge; the checkerboard fills the empty canvas.
                Point panelEdge = canvas.WindowPointOf(300, 240);
                Point canvasEdge = canvas.WindowPointOf(GraphicalUiElement.CanvasWidth / 2, GraphicalUiElement.CanvasHeight);
                Point empty = canvas.WindowPointOf(600, 450);
                HashSet<global::Avalonia.Media.Color> panelEdgeOn = Pixels(canvas, panelEdge, radius: 2);
                HashSet<global::Avalonia.Media.Color> canvasEdgeOn = Pixels(canvas, canvasEdge, radius: 2);
                Pixels(canvas, empty, radius: 12).Count.ShouldBeGreaterThan(1, "the checkerboard shows two colors");

                properties.ClickCheckBox("ShowOutlines");
                canvas.Frame();
                Pixels(canvas, panelEdge, radius: 2).ShouldNotBe(panelEdgeOn, "the panel's outline is gone");
                Pixels(canvas, canvasEdge, radius: 2).ShouldBe(canvasEdgeOn, "the canvas outline is its own setting");

                properties.ClickCheckBox("ShowCanvasOutline");
                canvas.Frame();
                Pixels(canvas, canvasEdge, radius: 2).ShouldNotBe(canvasEdgeOn, "the canvas outline is gone");

                properties.ClickCheckBox("ShowCheckerBackground");
                canvas.Frame();
                Pixels(canvas, empty, radius: 12).Count.ShouldBe(1, "without the checkerboard the empty canvas is one color");

                GumProjectSave saved = SavedProject(canvas.Tree);
                (saved.ShowOutlines, saved.ShowCanvasOutline, saved.ShowCheckerBackground).ShouldBe((false, false, false));

                properties.ClickCheckBox("ShowOutlines");
                properties.ClickCheckBox("ShowCanvasOutline");
                properties.ClickCheckBox("ShowCheckerBackground");
                canvas.Frame();
                Pixels(canvas, panelEdge, radius: 2).ShouldBe(panelEdgeOn, "the panel's outline is back");
                Pixels(canvas, canvasEdge, radius: 2).ShouldBe(canvasEdgeOn, "the canvas outline is back");
                Pixels(canvas, empty, radius: 12).Count.ShouldBeGreaterThan(1, "the checkerboard is back");
            }
            finally
            {
                viewModel.ShowOutlines = original.Outlines;
                viewModel.ShowCanvasOutline = original.CanvasOutline;
                viewModel.ShowCheckerBackground = original.Checker;
                GraphicalUiElement.ShowLineRectangles = originalLineRectangles;
            }

            canvas.AssertOracles();
        });
    }

    [AvaloniaFact]
    [Trait("Feature", "PROP-001")]
    public void AutoSave_TurnedOff_LeavesEditsUnsaved_UntilSaveAll_AndOnSavesAgain()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Click(tree.NodeFor(button));
        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        bool originalAutoSave = projectManager.AutoSave;
        try
        {
            using (ProjectPropertiesTab properties = new ProjectPropertiesTab(tree))
            {
                properties.ClickCheckBox("AutoSave");
                properties.ViewModel.AutoSave.ShouldBeFalse();
            }
            projectManager.AutoSave.ShouldBeFalse();
            ProjectFileSnapshot beforeEdit = tree.SnapshotFiles();

            tree.Grid.TypeAndEnter("Width", "210");
            VariableGridHarness.StoredValue(Component(tree, "Button"), "Width").ShouldBe(210f);
            tree.SnapshotFiles().ShouldMatch(beforeEdit, "with Auto Save off an edit stays in memory");

            using (ProjectPropertiesTab properties = new ProjectPropertiesTab(tree))
            {
                properties.ClickCheckBox("AutoSave");
            }
            projectManager.AutoSave.ShouldBeTrue();
            tree.Grid.TypeAndEnter("Width", "220");
            VariableGridHarness.StoredValue(tree.Grid.ReadSaved(Component(tree, "Button")), "Width").ShouldBe(220f);
        }
        finally
        {
            projectManager.AutoSave = originalAutoSave;
        }

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "PROP-010")]
    [Trait("Feature", "PROP-011")]
    [Trait("Feature", "DISP-012")]
    public void LocalizationFile_AddedInTheTab_OffersItsLanguages_AndTheLanguagePickedTranslatesText()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        using (tree.UndoManager.RequestLock(button))
        {
            button.DefaultState!.SetValue("Label.Text", "T_Greeting", "string");
        }
        tree.SaveAll();
        string csv = Path.Combine(tree.Project.ProjectFolder, "Strings.csv");
        File.WriteAllText(csv, "String ID,English,French\nT_Greeting,Hello,Bonjour\n");
        Gum.Localization.ILocalizationService localization = Services.GetRequiredService<Gum.Localization.ILocalizationService>();
        int originalLanguage = localization.CurrentLanguage;
        try
        {
            using ProjectPropertiesTab properties = new ProjectPropertiesTab(tree);
            MultiFileDisplay files = properties.Editor<MultiFileDisplay>("LocalizationFiles");
            tree.Dialogs.AnswerNextOpenFile(csv);
            properties.Input.Click(files.GetVisualDescendants().OfType<Button>().Single(candidate => candidate.Content as string == "Add..."));
            properties.Settle();

            properties.ViewModel.LocalizationFiles.ShouldHaveSingleItem().ShouldBe("Strings.csv", "the picked file is stored relative to the project");
            SavedProject(tree).LocalizationFiles.ShouldBe(new[] { "Strings.csv" });
            properties.Editor<MultiFileDisplay>("LocalizationFiles").ListBox.ItemCount.ShouldBe(1);

            properties.PickComboItem("LanguageName", "French");
            tree.WaitUntil(() => SavedProject(tree).CurrentLanguageIndex == 2, AsyncWork, "the language to save");
            localization.CurrentLanguage.ShouldBe(2);
            localization.Translate("T_Greeting").ShouldBe("Bonjour");
        }
        finally
        {
            localization.CurrentLanguage = originalLanguage;
        }

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "PROP-013")]
    [Trait("Feature", "PROP-014")]
    public void FontRanges_TypedAreSaved_AndAFontCharacterFileReplacesThemAndLocksTheRow()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        using ProjectPropertiesTab properties = new ProjectPropertiesTab(tree);

        properties.TypeAndEnter("FontRanges", "32-126,160-255");
        tree.WaitUntil(() => SavedProject(tree).FontRanges == "32-126,160-255", AsyncWork, "the font ranges to save");

        // The project's character file, which saving the project writes with the default characters.
        string characterFile = Path.Combine(tree.Project.ProjectFolder, ".gumfcs");
        string defaultCharacters = File.ReadAllText(characterFile);
        File.WriteAllText(characterFile, "AB");
        try
        {
            properties.ClickCheckBox("UseFontCharacterFile");
            tree.WaitUntil(() => SavedProject(tree).UseFontCharacterFile, AsyncWork, "the character file setting to save");

            properties.ViewModel.FontRanges.ShouldBe(global::RenderingLibrary.Graphics.Fonts.BmfcSave.GenerateRangesFromFile(characterFile));
            properties.Member("FontRanges").IsReadOnly.ShouldBeTrue("the ranges come from the file while it is used");

            properties.ClickCheckBox("UseFontCharacterFile");
            tree.WaitUntil(() => !SavedProject(tree).UseFontCharacterFile, AsyncWork, "the character file setting to clear");
            properties.ViewModel.FontRanges.ShouldBe(global::RenderingLibrary.Graphics.Fonts.BmfcSave.DefaultRanges);
            properties.Member("FontRanges").IsReadOnly.ShouldBeFalse();
        }
        finally
        {
            File.WriteAllText(characterFile, defaultCharacters);
        }

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "PROP-018")]
    [Trait("Feature", "DISP-011")]
    public void SinglePixelTexture_PickedWithTheFileEditor_IsSavedRelative_WithItsBounds()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        string texture = Path.Combine(tree.Project.ProjectFolder, "Atlas.png");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Content", "ExampleSpriteFrame.png"), texture);
        using ProjectPropertiesTab properties = new ProjectPropertiesTab(tree);

        tree.Dialogs.AnswerNextOpenFile(texture);
        properties.Input.Click(FileButton(properties.Row("SinglePixelTextureFile")));
        properties.Settle();
        // Each bound starts null: its field is disabled until "Is Null" is cleared.
        foreach ((string bound, string value) in new[] { ("Left", "1"), ("Top", "2"), ("Right", "3"), ("Bottom", "4") })
        {
            string name = "SinglePixelTexture" + bound;
            properties.Editor<TextBoxDisplay>(name).TextBox.IsEffectivelyEnabled.ShouldBeFalse();
            properties.Input.Click(properties.Editor<TextBoxDisplay>(name).GetVisualDescendants().OfType<CheckBox>().Single(box => box.Content as string == "Is Null"));
            properties.Settle();
            properties.TypeAndEnter(name, value);
        }
        tree.WaitUntil(() => SavedProject(tree).SinglePixelTextureBottom == 4, AsyncWork, "the texture bounds to save");

        GumProjectSave saved = SavedProject(tree);
        saved.SinglePixelTextureFile.ShouldBe("Atlas.png");
        (saved.SinglePixelTextureLeft, saved.SinglePixelTextureTop, saved.SinglePixelTextureRight, saved.SinglePixelTextureBottom).ShouldBe((1, 2, 3, 4));
        properties.Editor<FileSelectionDisplay>("SinglePixelTextureFile").TextBox.Text.ShouldBe("Atlas.png");

        tree.AssertOracles();
    }

    #endregion

    #region Standards palette

    [AvaloniaFact]
    [Trait("Feature", "PAL-003")]
    public void EditDefaults_OnAChip_SelectsTheStandard_HighlightsTheChip_AndItsGridEditsTheDefault()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Click(tree.NodeFor(button));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Input.RightClick(Chip(tree, "Text"));
        tree.Input.ContextMenuHeaders().ShouldBe(new[] { "Add to Button", "Edit defaults..." });
        tree.Input.PickContextMenuItem("Edit defaults...");
        tree.ThrowIfCrashed();

        StandardElementSave text = tree.Project.Standard("Text");
        tree.SelectedState.SelectedElement.ShouldBeSameAs(text);
        Chip(tree, "Text").BorderBrush.ShouldNotBe(Chip(tree, "Sprite").BorderBrush);

        VariableGridHarness grid = tree.Grid;
        grid.TypeAndEnter("FontSize", "30");
        VariableGridHarness.StoredValue(tree.Project.Standard("Text"), "FontSize").ShouldBe(30);
        VariableGridHarness.StoredValue(grid.ReadSaved(tree.Project.Standard("Text")), "FontSize").ShouldBe(30);

        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the default's edit should restore the files");

        tree.Click(tree.NodeFor(button));
        Chip(tree, "Text").BorderBrush.ShouldBe(Chip(tree, "Sprite").BorderBrush, "selecting a component clears the highlight");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "PAL-001")]
    public void ClickOnAChip_AddsItToTheSelectedElement_AndUndoRestoresTheFiles()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Click(tree.NodeFor(button));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Input.Click(Chip(tree, "Sprite"));
        tree.ThrowIfCrashed();

        InstanceSave added = Component(tree, "Button").Instances.ShouldHaveSingleItem();
        added.BaseType.ShouldBe("Sprite");
        ParentOf(Component(tree, "Button"), added).ShouldBeNull();
        tree.SelectedState.SelectedInstance.ShouldBeSameAs(added);
        SavedComponent(tree, "Button").Instances.ShouldHaveSingleItem().Name.ShouldBe(added.Name);

        tree.Undo();
        Component(tree, "Button").Instances.ShouldBeEmpty();
        tree.SnapshotFiles().ShouldMatch(start, "one undo should take back the click's add");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "PAL-001")]
    public void ClickOnAChip_WithAContainerInstanceSelected_AddsItInsideTheContainer_AndUndoRestoresTheFiles()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave holder = tree.Project.AddInstance(button, "Holder", "Container");
        tree.Click(tree.NodeFor(holder));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Input.Click(Chip(tree, "Text"));
        tree.ThrowIfCrashed();

        InstanceSave added = Component(tree, "Button").Instances.Single(instance => instance.BaseType == "Text");
        ParentOf(Component(tree, "Button"), added).ShouldBe("Holder");
        ParentOf(SavedComponent(tree, "Button"), SavedComponent(tree, "Button").Instances.Single(instance => instance.Name == added.Name)).ShouldBe("Holder");

        tree.Undo();
        Component(tree, "Button").Instances.ShouldHaveSingleItem().Name.ShouldBe("Holder");
        tree.SnapshotFiles().ShouldMatch(start, "one undo should take back the click's add");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "PAL-001")]
    public void ClickOnAChip_WithNoScreenOrComponentSelected_DoesNothing_LikeTheDisabledMenuItem()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Input.RightClick(Chip(tree, "Text"));
        tree.Input.PickContextMenuItem("Edit defaults...");
        tree.SelectedState.SelectedElement.ShouldBeSameAs(tree.Project.Standard("Text"));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Input.Click(Chip(tree, "Sprite"));
        tree.ThrowIfCrashed();

        tree.Dialogs.Messages.ShouldBeEmpty("a click with nowhere to add is ignored, not an error");
        Component(tree, "Button").Instances.ShouldBeEmpty();
        tree.SnapshotFiles().ShouldMatch(start, "the click should change nothing");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "PAL-001")]
    [Trait("Feature", "PAL-004")]
    public void DraggingAChip_OnlyDrops_AndDoesNotAlsoAddToTheSelectedElement()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        ComponentSave card = tree.Project.AddComponent("Card");
        tree.Click(tree.NodeFor(button));
        Point chipCenter = tree.Input.CenterOf(Chip(tree, "Text"));

        tree.BeginDrag(Chip(tree, "Text"));
        tree.DropOn(tree.NodeFor(card));
        // Whether or not the platform's drag loop swallows it, a release back over the chip after
        // a drag is not a click.
        tree.Input.Window.MouseUp(chipCenter, MouseButton.Left, RawInputModifiers.None);
        tree.Input.Layout();
        tree.ThrowIfCrashed();

        // The drop selects what it added, so a click's add would land in Card as a second instance.
        Component(tree, "Button").Instances.ShouldBeEmpty();
        Component(tree, "Card").Instances.ShouldHaveSingleItem("the release that ends a drag must not also add").BaseType.ShouldBe("Text");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "PAL-002")]
    public void AddToCurrent_OnAChip_AddsAnInstanceToTheOpenElement_AndCtrlZLeavesTheFilesAsTheyWere()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Click(tree.NodeFor(button));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Input.RightClick(Chip(tree, "Sprite"));
        tree.Input.PickContextMenuItem("Add to Button");
        tree.ThrowIfCrashed();

        InstanceSave added = Component(tree, "Button").Instances.ShouldHaveSingleItem();
        added.BaseType.ShouldBe("Sprite");
        tree.ChildTexts(tree.NodeFor(Component(tree, "Button"))).ShouldBe(new[] { added.Name });
        tree.SelectedState.SelectedInstance.ShouldBeSameAs(added);
        SavedComponent(tree, "Button").Instances.ShouldHaveSingleItem().Name.ShouldBe(added.Name);

        tree.Undo();
        Component(tree, "Button").Instances.ShouldBeEmpty();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the add should restore the files");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "PAL-004")]
    public void ChipDroppedOnAComponentRow_AddsThatStandardToIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        ComponentSave card = tree.Project.AddComponent("Card");
        tree.Click(tree.NodeFor(button));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        DropOnRow(tree, tree.NodeFor(card), StandardChip("Text"));

        Component(tree, "Button").Instances.ShouldBeEmpty("the drop target, not the selection, gets the instance");
        InstanceSave added = Component(tree, "Card").Instances.ShouldHaveSingleItem();
        added.BaseType.ShouldBe("Text");
        SavedComponent(tree, "Card").Instances.ShouldHaveSingleItem().Name.ShouldBe(added.Name);

        tree.Undo();
        Component(tree, "Card").Instances.ShouldBeEmpty();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the drop should restore the files");

        tree.AssertOracles();
    }

    [SkippableFact]
    [Trait("Feature", "PAL-005")]
    public void ChipDroppedOnTheCanvas_AddsThatStandardWhereItFell()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            canvas.Tree.Click(canvas.Tree.NodeFor(button));
            ProjectFileSnapshot start = canvas.Tree.SnapshotFiles();

            canvas.DropOnCanvas(canvas.WindowPointOf(40, 50), StandardChip("Rectangle")).ShouldBe(DragDropEffects.Copy);

            InstanceSave added = button.Instances.ShouldHaveSingleItem();
            added.BaseType.ShouldBe("Rectangle");
            canvas.SavedValue(button, $"{added.Name}.X").ShouldBe(40f);
            canvas.SavedValue(button, $"{added.Name}.Y").ShouldBe(50f);

            canvas.Undo();
            canvas.Tree.SnapshotFiles().ShouldMatch(start, "undoing the drop should restore the files");

            canvas.AssertOracles();
        });
    }

    #endregion

    #region Helpers

    private static ComponentSave Component(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Components.Single(component => component.Name == name);

    private static ComponentSave SavedComponent(ProjectTreeHarness tree, string name) =>
        SavedProject(tree).Components.Single(component => component.Name == name);

    private static string? ParentOf(ElementSave element, InstanceSave instance) =>
        element.GetDefaultStateOrThrow().GetValue($"{instance.Name}.Parent") as string;

    private static GumProjectSave SavedProject(ProjectTreeHarness tree) =>
        GumProjectSave.Load(tree.Project.ProjectFilePath, out _) ?? throw new InvalidOperationException("The project file did not load.");

    /// <summary>The distinct colors the canvas window shows within <paramref name="radius"/> of <paramref name="center"/>.</summary>
    private static HashSet<global::Avalonia.Media.Color> Pixels(CanvasHarness canvas, Point center, int radius)
    {
        HashSet<global::Avalonia.Media.Color> colors = new HashSet<global::Avalonia.Media.Color>();
        canvas.Input.AnyPixelNear(center, radius, color =>
        {
            colors.Add(color);
            return false;
        });
        return colors;
    }

    /// <summary>The "..." button of a file editor row.</summary>
    private static Button FileButton(Control row) =>
        row.GetVisualDescendants().OfType<Button>().Single(button => button.Content as string == "...");

    /// <summary>The Standards palette chip showing <paramref name="typeName"/>, in the Project tab.</summary>
    private static Border Chip(ProjectTreeHarness tree, string typeName)
    {
        tree.Input.Layout();
        return ((Control)tree.View.Content).GetVisualDescendants().OfType<AvaloniaStandardsPalette>().Single()
            .GetVisualDescendants().OfType<Border>()
            .Where(border => border.ClipToBounds && border.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Text == typeName))
            .Single();
    }

    /// <summary>What the Standards palette puts in its drag (AvaloniaStandardsPalette).</summary>
    private static DataTransfer StandardChip(string typeName)
    {
        DataTransfer data = new DataTransfer();
        data.Add(DataTransferItem.Create(AvaloniaDragFormats.StandardElementName, typeName));
        return data;
    }

    /// <summary>
    /// Drags <paramref name="data"/> in from outside and drops it on <paramref name="node"/>'s row,
    /// as the platform delivers a drag from the palette; headless Avalonia has no drag source.
    /// </summary>
    private static void DropOnRow(ProjectTreeHarness tree, GumTreeNode node, DataTransfer data)
    {
        Control row = tree.View.Tree.GetVisualDescendants().OfType<TreeRowView>().Single(candidate => candidate.Row?.Node == node);
        Point point = tree.Input.CenterOf(row);
        Window window = tree.Input.Window;
        window.DragDrop(point, RawDragEventType.DragEnter, data, DragDropEffects.Copy);
        window.DragDrop(point, RawDragEventType.DragOver, data, DragDropEffects.Copy);
        window.DragDrop(point, RawDragEventType.Drop, data, DragDropEffects.Copy);
        tree.Input.Layout();
        tree.ThrowIfCrashed();
    }

    private static void OnCanvas(Action<CanvasHarness> scenario)
    {
        Skip.IfNot(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        CanvasHarness.OnUiThread(() =>
        {
            using CanvasHarness canvas = new CanvasHarness();
            scenario(canvas);
        });
    }

    /// <summary>
    /// The head's Project Properties tab, opened with Edit > Properties and hosted in a window of its
    /// own, driven through its property grid's editors.
    /// </summary>
    private sealed class ProjectPropertiesTab : IDisposable
    {
        private readonly ProjectTreeHarness _tree;

        public ProjectPropertiesTab(ProjectTreeHarness tree)
        {
            _tree = tree;
            tree.PickMainMenu("Edit", "Properties");
            AvaloniaPluginTab tab = ((AvaloniaTabManager)Services.GetRequiredService<ITabManager>()).AllTabs.Single(candidate => candidate.Title == "Project Properties");
            tab.IsVisible.ShouldBeTrue();
            View = (ProjectPropertiesView)tab.Content;
            ViewModel = (ProjectPropertiesViewModel)View.DataContext!;
            Input = new HeadlessWindowDriver(View, width: 520, height: 1600, framesFolderName: "GumProjectProperties");
        }

        public ProjectPropertiesView View { get; }

        public ProjectPropertiesViewModel ViewModel { get; }

        public HeadlessWindowDriver Input { get; }

        public InstanceMember Member(string name) =>
            View.Grid.Categories.SelectMany(category => category.Members).FirstOrDefault(member => member.Name == name)
            ?? throw new InvalidOperationException($"The Project Properties grid has no {name} row.");

        public SingleDataUiContainer Row(string name)
        {
            Input.Layout();
            InstanceMember member = Member(name);
            SingleDataUiContainer row = View.Grid.LiveContainers.FirstOrDefault(candidate => candidate.Member == member)
                ?? throw new InvalidOperationException($"The {name} row has no live editor.");
            row.BringIntoView();
            Input.Layout();
            return row;
        }

        public T Editor<T>(string name) where T : Control =>
            Row(name).Displayer as T
            ?? throw new InvalidOperationException($"The {name} row shows a {Row(name).Displayer?.GetType().Name ?? "nothing"}, not a {typeof(T).Name}.");

        public void TypeAndEnter(string name, string text)
        {
            Input.TypeAndEnter(Editor<TextBoxDisplay>(name).TextBox, text);
            Settle();
        }

        public void ClickCheckBox(string name)
        {
            Input.Click(Editor<CheckBoxDisplay>(name).CheckBox);
            Settle();
        }

        /// <summary>Picks <paramref name="item"/> in a combo row, as a click in its open drop-down does.</summary>
        public void PickComboItem(string name, string item)
        {
            ComboBox combo = Row(name).GetVisualDescendants().OfType<ComboBox>().Single();
            if (!combo.IsEffectivelyEnabled)
            {
                throw new InvalidOperationException($"The {name} combo is disabled; a user cannot pick from it.");
            }
            combo.IsDropDownOpen = true;
            Input.Layout();
            combo.SelectedItem = combo.Items.Cast<object?>().FirstOrDefault(candidate => candidate?.ToString() == item)
                ?? throw new InvalidOperationException($"The {name} combo has no \"{item}\"; it has [{string.Join(", ", combo.Items.Cast<object?>())}].");
            combo.IsDropDownOpen = false;
            Settle();
        }

        public void Settle()
        {
            Input.Layout();
            _tree.ThrowIfCrashed();
        }

        public void Dispose() => Input.Dispose();
    }

    #endregion
}
