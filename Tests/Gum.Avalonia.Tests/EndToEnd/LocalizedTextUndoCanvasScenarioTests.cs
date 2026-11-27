using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Plugins.PropertiesWindowPlugin;
using Gum.Wireframe;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// Undo and redo of a Text's text in a localized project: the canvas shows the translation of the
/// restored value, as it does after the edit itself (#8493). Runs where <see cref="CanvasHarness"/> can.
/// </summary>
[Trait("Category", "EndToEnd")]
public class LocalizedTextUndoCanvasScenarioTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [Fact]
    public void UndoAndRedo_OfAPickedStringId_OnAnInstance_ShowTheRestoredTranslation()
    {
        OnLocalizedCanvas((canvas, button) =>
        {
            ProjectTreeHarness tree = canvas.Tree;
            tree.Click(tree.NodeFor(Instance(button, "Label")));
            string? before = ShownText(canvas, "Label");

            tree.Grid.PickComboItem("Text", "T_Play");
            canvas.Frame();
            ShownText(canvas, "Label").ShouldBe("Jouer");

            tree.Undo();
            canvas.Frame();
            canvas.SavedValue(button, "Label.Text").ShouldBeNull();
            ShownText(canvas, "Label").ShouldBe(before, canvas.Describe());

            tree.Redo();
            canvas.Frame();
            canvas.SavedValue(button, "Label.Text").ShouldBe("T_Play");
            ShownText(canvas, "Label").ShouldBe("Jouer");
        });
    }

    [Fact]
    public void Undo_OfAPickedStringId_AfterATypedStringId_ShowsTheTypedKeysTranslation()
    {
        OnLocalizedCanvas((canvas, button) =>
        {
            ProjectTreeHarness tree = canvas.Tree;
            tree.Click(tree.NodeFor(Instance(button, "Label")));
            tree.Grid.Combo("Text").Text = "T_Quit";
            tree.Grid.PickComboItem("Text", "T_Quit");
            canvas.Frame();
            ShownText(canvas, "Label").ShouldBe("Quitter");
            tree.Grid.PickComboItem("Text", "T_Play");
            canvas.Frame();

            tree.Undo();
            canvas.Frame();

            canvas.SavedValue(button, "Label.Text").ShouldBe("T_Quit");
            ShownText(canvas, "Label").ShouldBe("Quitter");
            tree.Grid.ComboText("Text").ShouldBe("T_Quit");
        });
    }

    [Fact]
    public void UndoAndRedo_OfAPickedStringId_OnTheElementItself_ShowTheRestoredTranslation()
    {
        OnLocalizedCanvas((canvas, _) =>
        {
            ComponentSave title = canvas.Project.AddComponent("Title", baseType: "Text");
            ProjectTreeHarness tree = canvas.Tree;
            tree.Click(tree.NodeFor(title));
            tree.Grid.PickComboItem("Text", "T_Quit");
            canvas.Frame();
            tree.Grid.PickComboItem("Text", "T_Play");
            canvas.Frame();
            RootText(canvas).ShouldBe("Jouer");

            tree.Undo();
            canvas.Frame();
            canvas.SavedValue(title, "Text").ShouldBe("T_Quit");
            RootText(canvas).ShouldBe("Quitter", canvas.Describe());

            tree.Redo();
            canvas.Frame();
            RootText(canvas).ShouldBe("Jouer");
        });
    }

    [Fact]
    public void Undo_OfAPickedStringId_InAState_ShowsTheRestoredTranslation()
    {
        OnLocalizedCanvas((canvas, button) =>
        {
            StateSaveCategory category = canvas.Project.AddCategory(button, "Look");
            StateSave state = canvas.Project.AddState(button, category, "Pressed");
            ProjectTreeHarness tree = canvas.Tree;
            tree.Click(tree.NodeFor(Instance(button, "Label")));
            tree.Grid.Select(state);
            tree.Grid.PickComboItem("Text", "T_Quit");
            canvas.Frame();
            tree.Grid.PickComboItem("Text", "T_Play");
            canvas.Frame();
            ShownText(canvas, "Label").ShouldBe("Jouer");

            tree.Undo();
            canvas.Frame();
            ShownText(canvas, "Label").ShouldBe("Quitter", canvas.Describe());
        });
    }

    [Fact]
    public void UndoAndRedo_OfAPickedStringId_OnAnExposedTextOfAComponentInstance_ShowTheRestoredTranslation()
    {
        OnLocalizedCanvas((canvas, button) =>
        {
            using (canvas.Project.UndoManager.RequestLock(button))
            {
                button.DefaultState!.SetValue("Label.Text", "Hello", "string");
                button.DefaultState.Variables.Single(variable => variable.Name == "Label.Text").ExposedAsName = "ButtonText";
            }
            ScreenSave menu = canvas.Project.AddScreen("Menu");
            canvas.AddInstance(menu, "Play", "Button", x: 10, y: 10);
            ProjectTreeHarness tree = canvas.Tree;
            tree.Click(tree.NodeFor(Instance(menu, "Play")));
            tree.Grid.PickComboItem("ButtonText", "T_Quit");
            canvas.Frame();
            tree.Grid.PickComboItem("ButtonText", "T_Play");
            canvas.Frame();
            NestedText(canvas, "Play", "Label").ShouldBe("Jouer");

            tree.Undo();
            canvas.Frame();
            canvas.SavedValue(menu, "Play.ButtonText").ShouldBe("T_Quit");
            NestedText(canvas, "Play", "Label").ShouldBe("Quitter", canvas.Describe());

            tree.Redo();
            canvas.Frame();
            NestedText(canvas, "Play", "Label").ShouldBe("Jouer");
        });
    }

    [Fact]
    public void Undo_OfATypedStringId_ShowsTheRestoredTranslation_AndLeavingTheFieldKeepsIt()
    {
        OnLocalizedCanvas((canvas, button) =>
        {
            ProjectTreeHarness tree = canvas.Tree;
            tree.Click(tree.NodeFor(Instance(button, "Label")));
            TypeIntoCombo(tree.Grid, "Text", "T_Quit");
            canvas.Frame();
            canvas.SavedValue(button, "Label.Text").ShouldBe("T_Quit");
            ShownText(canvas, "Label").ShouldBe("Quitter");
            TypeIntoCombo(tree.Grid, "Text", "T_Play");
            canvas.Frame();
            ShownText(canvas, "Label").ShouldBe("Jouer");

            tree.Undo();
            canvas.Frame();
            canvas.SavedValue(button, "Label.Text").ShouldBe("T_Quit");
            ShownText(canvas, "Label").ShouldBe("Quitter", canvas.Describe());
            tree.Grid.ComboText("Text").ShouldBe("T_Quit");
        });
    }

    [Fact]
    public void Undo_WhileThePickedComboKeepsFocus_ThenLeavingIt_KeepsTheRestoredValue()
    {
        OnLocalizedCanvas((canvas, button) =>
        {
            ProjectTreeHarness tree = canvas.Tree;
            tree.Click(tree.NodeFor(Instance(button, "Label")));
            tree.Grid.PickComboItem("Text", "T_Quit");
            TextBox box = tree.Grid.Combo("Text").GetVisualDescendants().OfType<TextBox>().Single();
            box.Focus();
            tree.Grid.PickComboItem("Text", "T_Play");
            canvas.Frame();

            box.IsFocused.ShouldBeTrue("the drop-down keeps focus after the pick, as in the tool");
            // Ctrl+Z reaches the tool's undo while the combo still has focus.
            tree.Grid.Undo();
            tree.Grid.Settle();
            tree.Grid.Input.Press(Key.Tab, PhysicalKey.Tab);
            tree.Grid.Settle();
            canvas.Frame();

            canvas.SavedValue(button, "Label.Text").ShouldBe("T_Quit");
            ShownText(canvas, "Label").ShouldBe("Quitter", canvas.Describe());
            tree.Grid.ComboText("Text").ShouldBe("T_Quit");
        });
    }

    [Fact]
    public void UndoAndRedo_OfAPickedStringId_OnAnInstanceWhoseTextIsExposed_RestoreTheValueAndTheCanvas()
    {
        OnLocalizedCanvas((canvas, button) =>
        {
            ProjectTreeHarness tree = canvas.Tree;
            tree.Click(tree.NodeFor(Instance(button, "Label")));
            tree.Grid.PickComboItem("Text", "T_Quit");
            using (canvas.Project.UndoManager.RequestLock(button))
            {
                button.DefaultState!.Variables.Single(variable => variable.Name == "Label.Text").ExposedAsName = "LabelText";
            }
            tree.Click(tree.NodeFor(button));
            tree.Click(tree.NodeFor(Instance(button, "Label")));
            tree.Grid.PickComboItem("Text", "T_Play");
            canvas.Frame();
            ShownText(canvas, "Label").ShouldBe("Jouer");

            tree.Undo();
            canvas.Frame();
            ElementSave restored = canvas.Project.SelectedState.SelectedElement!;
            restored.DefaultState!.GetValue("Label.Text").ShouldBe("T_Quit", "the undo restores the stored key and nothing writes the undone one back");
            canvas.SavedValue(button, "Label.Text").ShouldBe("T_Quit");
            ShownText(canvas, "Label").ShouldBe("Quitter", canvas.Describe());
            tree.Grid.ComboText("Text").ShouldBe("T_Quit");

            tree.Redo();
            canvas.Frame();
            canvas.Project.SelectedState.SelectedElement!.DefaultState!.GetValue("Label.Text").ShouldBe("T_Play");
            ShownText(canvas, "Label").ShouldBe("Jouer");
        });
    }

    private static void TypeIntoCombo(VariableGridHarness grid, string memberName, string text)
    {
        TextBox box = grid.Combo(memberName).GetVisualDescendants().OfType<TextBox>().Single();
        grid.Input.TypeInto(box, text);
        grid.Input.Press(Key.Tab, PhysicalKey.Tab);
        grid.Settle();
    }

    private static string? NestedText(CanvasHarness canvas, string instanceName, string textName)
    {
        GraphicalUiElement root = canvas.Wireframe.RootGue ?? throw new InvalidOperationException("Nothing is shown.");
        GraphicalUiElement text = root.GetGraphicalUiElementByName(instanceName)?.GetGraphicalUiElementByName(textName) as GraphicalUiElement
            ?? throw new InvalidOperationException($"The canvas shows no {instanceName}.{textName}. {canvas.Describe()}");
        return (text.RenderableComponent as global::RenderingLibrary.Graphics.Text)?.RawText;
    }

    private static string? ShownText(CanvasHarness canvas, string name)
    {
        GraphicalUiElement shown = canvas.Wireframe.AllIpsos.OfType<GraphicalUiElement>()
            .SingleOrDefault(ipso => ipso.Tag is InstanceSave instance && instance.Name == name)
            ?? throw new InvalidOperationException($"The canvas shows no {name}. {canvas.Describe()}");
        return (shown.RenderableComponent as global::RenderingLibrary.Graphics.Text)?.RawText;
    }

    private static string? RootText(CanvasHarness canvas) =>
        (canvas.Wireframe.RootGue?.RenderableComponent as global::RenderingLibrary.Graphics.Text)?.RawText;

    private static InstanceSave Instance(ElementSave element, string name) =>
        element.Instances.Single(instance => instance.Name == name);

    /// <summary>A Button with a Text instance named Label, in a project with a French/English CSV, French picked.</summary>
    private static void OnLocalizedCanvas(Action<CanvasHarness, ComponentSave> scenario)
    {
        Assert.SkipUnless(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        CanvasHarness.OnUiThread(() =>
        {
            using CanvasHarness canvas = new CanvasHarness();
            LocalizationService localization = Services.GetRequiredService<LocalizationService>();
            ProjectPropertiesViewModel properties = SettingsPagesForTests.ProjectProperties();
            try
            {
                ComponentSave button = canvas.Project.AddComponent("Button");
                canvas.AddInstance(button, "Label", "Text", x: 20, y: 20, width: 200, height: 40);
                File.WriteAllText(Path.Combine(canvas.Project.ProjectFolder, "Strings.csv"),
                    "String ID,English,French\nT_Play,Play,Jouer\nT_Quit,Quit,Quitter\n");
                properties.LocalizationFiles = new List<string> { "Strings.csv" };
                properties.LanguageName = "French";
                scenario(canvas, button);
                canvas.AssertOracles();
            }
            finally
            {
                // The service is the tool's for the session; the next test's project has no strings.
                localization.Clear();
                localization.CurrentLanguage = 0;
            }
        });
    }
}
