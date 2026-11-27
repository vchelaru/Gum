using System.Globalization;
using System.Text;
using Avalonia.VisualTree;
using Gum.Avalonia.Shell;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins.Errors;
using Gum.Plugins.PropertiesWindowPlugin;
using Gum.ToolStates;
using Gum.Wireframe;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// Undo shows what was shown before a Variables-tab edit, and redo what the edit itself showed, for each
/// kind of variable and each reaction an edit triggers: the canvas renderables (position, size, rotation,
/// color, text, font, texture), the layout of dependents, variable references, exposed variables, states,
/// localization and the Errors tab. The oracle is what the normal edit showed. Runs where
/// <see cref="CanvasHarness"/> can.
/// </summary>
[Trait("Category", "EndToEnd")]
public class UndoRedoCanvasScenarioTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [Theory]
    [InlineData("Label", "X", "33")]
    [InlineData("Label", "Width", "90")]
    [InlineData("Label", "Rotation", "15")]
    [InlineData("Label", "FontSize", "30")]
    [InlineData("Label", "FontScale", "2")]
    [InlineData("Label", "OutlineThickness", "2")]
    [InlineData("Label", "Color", "0A141E")]
    [InlineData("Label", "Alpha", "100")]
    [InlineData("Label", "LineHeightMultiplier", "2")]
    [InlineData("Icon", "Width", "90")]
    [InlineData("Icon", "Alpha", "100")]
    [InlineData("Icon", "Rotation", "15")]
    [InlineData("Icon", "Color", "0A141E")]
    [InlineData("", "Width", "300")]
    public void UndoAndRedo_OfATypedValue_ShowWhatTheEditShowedBeforeAndAfter(string instanceName, string member, string value)
    {
        OnCanvas((canvas, button) =>
        {
            Select(canvas, button, instanceName);
            EditUndoRedo(canvas, () => canvas.Tree.Grid.TypeAndEnter(member, value));
        });
    }

    [Fact]
    public void UndoAndRedo_OfTypedText_ShowWhatTheEditShowedBeforeAndAfter()
    {
        OnCanvas((canvas, button) =>
        {
            Select(canvas, button, "Label");
            EditUndoRedo(canvas, () => canvas.Tree.Grid.TypeAndLeave("Text", "Typed words"));
        });
    }

    [Fact]
    public void UndoAndRedo_OfTheElementsWidth_ReLayOutAChildSizedToIt()
    {
        OnCanvas((canvas, button) =>
        {
            StateSave defaults = button.GetDefaultStateOrThrow();
            defaults.SetValue("Label.WidthUnits", DimensionUnitType.RelativeToParent, "DimensionUnitType");
            defaults.SetValue("Label.Width", -10f, "float");
            ResetBaseline(canvas);

            Select(canvas, button, "");
            EditUndoRedo(canvas, () => canvas.Tree.Grid.TypeAndEnter("Width", "300"));
        });
    }

    [Fact]
    public void UndoAndRedo_OfAReferencedValue_ReApplyTheReferenceToItsFollower()
    {
        OnCanvas((canvas, button) =>
        {
            Select(canvas, button, "Icon");
            canvas.Tree.Grid.TypeLinesAndApply("VariableReferences", "X = Label.X");
            canvas.Frame();
            ResetBaseline(canvas);

            Select(canvas, button, "Label");
            EditUndoRedo(canvas, () => canvas.Tree.Grid.TypeAndEnter("X", "60"));
        });
    }

    [Fact]
    public void UndoAndRedo_OfAValueAnotherElementReferences_ReApplyTheReferenceInThatElement()
    {
        OnCanvas((canvas, button) =>
        {
            // Scratch follows the Button's Width, so the Button's undo snapshot does not hold Scratch's value.
            ProjectTreeHarness tree = canvas.Tree;
            ElementSave scratch = ObjectFinder.Self.GetElementSave("Scratch")!;
            tree.Click(tree.NodeFor(scratch));
            tree.Grid.TypeLinesAndApply("VariableReferences", "Width = Components/Button.Width");
            canvas.Frame();

            Select(canvas, button, "");
            EditUndoRedo(canvas, () => tree.Grid.TypeAndEnter("Width", "300"), observeOn: scratch);
        });
    }

    [Fact]
    public void UndoAndRedo_OfAVariableReferenceLine_ShowWhatTheEditShowedBeforeAndAfter()
    {
        OnCanvas((canvas, button) =>
        {
            Select(canvas, button, "Icon");
            EditUndoRedo(canvas, () => canvas.Tree.Grid.TypeLinesAndApply("VariableReferences", "Y = Label.Y"));
        });
    }

    [Fact]
    public void UndoAndRedo_OfAPickedSourceFile_ShowTheTextureBeforeAndAfter()
    {
        OnCanvas((canvas, button) =>
        {
            CopyTestPng(canvas, "frame.png");
            // The drop-down lists the images the project references, so another sprite references the file.
            canvas.AddInstance(button, "Hero", "Sprite", x: 100, y: 80, width: 40, height: 40);
            button.GetDefaultStateOrThrow().SetValue("Hero.SourceFile", "frame.png", "string");
            ResetBaseline(canvas);

            Select(canvas, button, "Icon");
            EditUndoRedo(canvas, () => canvas.Tree.Grid.PickComboItem("SourceFile", "frame.png"));
        });
    }

    [Fact]
    public void UndoAndRedo_OfATypedMissingSourceFile_ShowItsErrorOnlyAfterTheEdit()
    {
        OnCanvas((canvas, button) =>
        {
            Select(canvas, button, "Icon");
            VariableGrid.VariableGridHarness grid = canvas.Tree.Grid;
            EditUndoRedo(canvas, () => grid.Input.TypeAndEnter(
                grid.Row("SourceFile").GetVisualDescendants().OfType<global::Avalonia.Controls.TextBox>().First(), "Missing.png"));
        });
    }

    [Fact]
    public void UndoAndRedo_OfAValueInACategoryState_ShowWhatTheEditShowedBeforeAndAfter()
    {
        OnCanvas((canvas, button) =>
        {
            StateSaveCategory size = canvas.Project.AddCategory(button, "Size");
            StateSave big = canvas.Project.AddState(button, size, "Big");
            Select(canvas, button, "Label");
            canvas.Tree.Grid.Select(big);
            canvas.Frame();

            EditUndoRedo(canvas, () => canvas.Tree.Grid.TypeAndEnter("X", "70"));
        });
    }

    [Fact]
    public void UndoAndRedo_OfAnInstancesCategoryState_ShowWhatTheEditShowedBeforeAndAfter()
    {
        OnCanvas((canvas, button) =>
        {
            StateSaveCategory size = canvas.Project.AddCategory(button, "Size");
            StateSave big = canvas.Project.AddState(button, size, "Big");
            big.SetValue("Label.Width", 300f, "float");
            big.SetValue("Label.X", 5f, "float");
            ScreenSave title = canvas.Project.AddScreen("Title");
            canvas.AddInstance(title, "ButtonInstance", "Button", x: 10, y: 10);
            ResetBaseline(canvas);

            ProjectTreeHarness tree = canvas.Tree;
            tree.Click(tree.NodeFor(title.Instances.Single()));
            canvas.Frame();
            EditUndoRedo(canvas, () => tree.Grid.PickComboItem("SizeState", "Big"));
        });
    }

    [Fact]
    public void UndoAndRedo_OfAnExposedVariableOnAnInstance_ShowWhatTheEditShowedBeforeAndAfter()
    {
        OnCanvas((canvas, button) =>
        {
            Expose(button, "Label.Text", "LabelText", "Hello");
            ScreenSave title = canvas.Project.AddScreen("Title");
            canvas.AddInstance(title, "ButtonInstance", "Button", x: 10, y: 10);
            ResetBaseline(canvas);

            ProjectTreeHarness tree = canvas.Tree;
            tree.Click(tree.NodeFor(title.Instances.Single()));
            canvas.Frame();
            EditUndoRedo(canvas, () => tree.Grid.TypeAndLeave("LabelText", "Exposed words"));
        });
    }

    [Fact]
    public void UndoAndRedo_OfAComponentsExposedDefault_ShowOnTheInstancesThatUseIt()
    {
        OnCanvas((canvas, button) =>
        {
            Expose(button, "Label.Text", "LabelText", "Hello");
            ScreenSave title = canvas.Project.AddScreen("Title");
            canvas.AddInstance(title, "ButtonInstance", "Button", x: 10, y: 10);
            ResetBaseline(canvas);

            Select(canvas, button, "Label");
            EditUndoRedo(canvas, () => canvas.Tree.Grid.TypeAndLeave("Text", "Default words"), observeOn: title);
        });
    }

    [Fact]
    public void UndoAndRedo_OfAPickedLocalizedKey_ShowTheTranslationBeforeAndAfter()
    {
        OnCanvas((canvas, button) =>
        {
            LocalizationService localization = Services.GetRequiredService<LocalizationService>();
            try
            {
                File.WriteAllText(Path.Combine(canvas.Project.ProjectFolder, "Strings.csv"),
                    "String ID,English,French\nT_Play,Play,Jouer\nT_Quit,Quit,Quitter\n");
                ProjectPropertiesViewModel properties = SettingsPagesForTests.ProjectProperties();
                properties.LocalizationFiles = new List<string> { "Strings.csv" };
                properties.LanguageName = "French";
                ResetBaseline(canvas);

                Select(canvas, button, "Label");
                EditUndoRedo(canvas, () => canvas.Tree.Grid.PickComboItem("Text", "T_Quit"));
            }
            finally
            {
                // The service is the tool's for the session; the next test's project has no strings.
                localization.Clear();
                localization.CurrentLanguage = 0;
            }
        });
    }

    /// <summary>
    /// Edits, undoes and redoes, comparing what the tool shows after each step: undo against before the
    /// edit, redo against the edit itself. <paramref name="observeOn"/> reads another element's canvas
    /// (an instance's view of the edited component), then selects the edited object again.
    /// </summary>
    private static void EditUndoRedo(CanvasHarness canvas, Action edit, ElementSave? observeOn = null)
    {
        ProjectTreeHarness tree = canvas.Tree;
        SelectionToReturnTo back = SelectionToReturnTo.Take();
        string before = Observe(canvas, observeOn, back);

        edit();
        canvas.Frame();
        string edited = Observe(canvas, observeOn, back);
        edited.ShouldNotBe(before, "the edit should change what the tool shows");

        tree.Undo();
        canvas.Frame();
        Observe(canvas, observeOn, back).ShouldBe(before, "undo should show what was shown before the edit");

        tree.Redo();
        canvas.Frame();
        Observe(canvas, observeOn, back).ShouldBe(edited, "redo should show what the edit showed");
    }

    private static string Observe(CanvasHarness canvas, ElementSave? observeOn, SelectionToReturnTo back)
    {
        if (observeOn == null)
        {
            return Fingerprint(canvas);
        }
        ProjectTreeHarness tree = canvas.Tree;
        tree.Click(tree.NodeFor(ObjectFinder.Self.GetElementSave(observeOn.Name)!));
        canvas.Frame();
        string shown = Fingerprint(canvas);
        back.Restore(tree);
        canvas.Frame();
        return shown;
    }

    /// <summary>What the tree had selected, found again by name since undo replaces the element with a copy.</summary>
    private sealed class SelectionToReturnTo
    {
        private readonly string _elementName;
        private readonly string? _instanceName;

        private SelectionToReturnTo(string elementName, string? instanceName)
        {
            _elementName = elementName;
            _instanceName = instanceName;
        }

        public static SelectionToReturnTo Take()
        {
            ISelectedState selected = Services.GetRequiredService<ISelectedState>();
            return new SelectionToReturnTo(selected.SelectedElement!.Name, selected.SelectedInstance?.Name);
        }

        public void Restore(ProjectTreeHarness tree)
        {
            ElementSave element = ObjectFinder.Self.GetElementSave(_elementName)!;
            tree.Click(_instanceName == null
                ? tree.NodeFor(element)
                : tree.NodeFor(element.Instances.Single(instance => instance.Name == _instanceName)));
        }
    }

    private static void Select(CanvasHarness canvas, ComponentSave button, string instanceName)
    {
        ProjectTreeHarness tree = canvas.Tree;
        ElementSave live = ObjectFinder.Self.GetElementSave(button.Name)!;
        tree.Click(instanceName.Length == 0
            ? tree.NodeFor(live)
            : tree.NodeFor(live.Instances.Single(instance => instance.Name == instanceName)));
        canvas.Frame();
    }

    /// <summary>
    /// Clicks the Scratch component, so the next click on the scenario's element records a fresh undo
    /// baseline and rebuilds the canvas after setup that edited the data directly.
    /// </summary>
    private static void ResetBaseline(CanvasHarness canvas)
    {
        ProjectTreeHarness tree = canvas.Tree;
        tree.Click(tree.NodeFor(ObjectFinder.Self.GetElementSave("Scratch")!));
        canvas.Frame();
    }

    private static void Expose(ComponentSave component, string variableName, string exposedAs, string value)
    {
        component.GetDefaultStateOrThrow().SetValue(variableName, value, "string");
        component.GetDefaultStateOrThrow().GetVariableSave(variableName)!.ExposedAsName = exposedAs;
    }

    private static void CopyTestPng(CanvasHarness canvas, string fileName)
    {
        string source = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CodeGen_Maui_FullCodegen", "Content", "GumProject", "ExampleSpriteFrame.png");
        File.Copy(Path.GetFullPath(source), Path.Combine(canvas.Project.ProjectFolder, fileName), overwrite: true);
    }

    /// <summary>
    /// What the tool shows for the element: each canvas object's bounds, rotation, visibility, color,
    /// text, font values and texture, read from the visual and its renderable, plus the Errors tab.
    /// </summary>
    private static string Fingerprint(CanvasHarness canvas)
    {
        StringBuilder builder = new StringBuilder();
        foreach (GraphicalUiElement shown in ShownWithDescendants(canvas))
        {
            builder.Append(CultureInfo.InvariantCulture,
                $"{shown.Name}: {shown.AbsoluteLeft:0.##},{shown.AbsoluteTop:0.##} {shown.AbsoluteWidth:0.##}x{shown.AbsoluteHeight:0.##} r{shown.Rotation:0.##} v{shown.Visible}");
            switch (shown.RenderableComponent)
            {
                case global::RenderingLibrary.Graphics.Text text:
                    builder.Append(CultureInfo.InvariantCulture,
                        $" text[{text.RawText}] color{text.Color} rr{text.Rotation:0.##} font[{FontValues(shown)} {Path.GetFileName(text.BitmapFont?.FontFile)}] scale{text.FontScale:0.##} lines[{string.Join("|", text.WrappedText)}] lh{text.LineHeightMultiplier:0.##} max{text.MaxLettersToShow}");
                    break;
                case global::RenderingLibrary.Graphics.Sprite sprite:
                    builder.Append(CultureInfo.InvariantCulture,
                        $" texture[{sprite.Texture?.Name}] color{sprite.Color} rr{sprite.Rotation:0.##}");
                    break;
            }
            builder.AppendLine();
        }
        builder.Append("errors: ").AppendJoin(" | ", ErrorsTab().Errors.Select(error => error.Message));
        return builder.ToString();
    }

    /// <summary>The canvas objects and, for a component instance on a screen, the objects inside it.</summary>
    private static List<GraphicalUiElement> ShownWithDescendants(CanvasHarness canvas)
    {
        List<GraphicalUiElement> shown = new List<GraphicalUiElement>();
        HashSet<GraphicalUiElement> seen = new HashSet<GraphicalUiElement>();
        void Add(GraphicalUiElement visual)
        {
            if (!seen.Add(visual))
            {
                return;
            }
            shown.Add(visual);
            foreach (GraphicalUiElement child in visual.Children)
            {
                Add(child);
            }
        }
        foreach (GraphicalUiElement visual in canvas.Wireframe.AllIpsos.OfType<GraphicalUiElement>())
        {
            Add(visual);
        }
        return shown;
    }

    /// <summary>The font values the visual holds; the headless run has no font cache, so every size loads the same fallback file.</summary>
    private static string FontValues(GraphicalUiElement shown) => shown is global::Gum.GueDeriving.TextRuntime text
        ? string.Create(CultureInfo.InvariantCulture, $"{text.Font} {text.FontSize} o{text.OutlineThickness} b{text.IsBold} i{text.IsItalic}")
        : shown.GetType().Name;

    private static AllErrorsViewModel ErrorsTab()
    {
        AvaloniaTabManager tabs = (AvaloniaTabManager)Services.GetRequiredService<ITabManager>();
        return (AllErrorsViewModel)((global::Avalonia.Controls.Control)tabs.AllTabs.Single(tab => tab.Title == "Errors").Content).DataContext!;
    }

    /// <summary>A Button holding a Text named Label and a Sprite named Icon, and an empty Scratch component.</summary>
    private static void OnCanvas(Action<CanvasHarness, ComponentSave> scenario)
    {
        Assert.SkipUnless(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        CanvasHarness.OnUiThread(() =>
        {
            using CanvasHarness canvas = new CanvasHarness();
            canvas.Project.AddComponent("Scratch");
            ComponentSave button = canvas.Project.AddComponent("Button");
            canvas.AddInstance(button, "Label", "Text", x: 20, y: 20, width: 200, height: 40);
            canvas.AddInstance(button, "Icon", "Sprite", x: 20, y: 80, width: 40, height: 40);
            scenario(canvas, button);
            canvas.AssertOracles();
        });
    }
}
