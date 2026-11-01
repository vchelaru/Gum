using Gum.GueDeriving;
using Gum;
using RenderingLibrary;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// Projects with a tool-authored TextBox, for edits of variables that map to Forms control
/// properties (Placeholder, AcceptsReturn, MaxLength, ...). The first project is what the running
/// app loaded, the second is what the reload reads.
/// </summary>
internal static class HotReloadTextBoxProjects
{
    internal const string ScreenName = "TestScreen";

    internal static void Set(StateSave state, string name, string type, object? value)
    {
        state.Variables.RemoveAll(v => v.Name == name);
        if (value != null)
        {
            state.Variables.Add(new VariableSave { Name = name, Type = type, Value = value, SetsValue = true });
        }
    }

    /// <summary>
    /// <paramref name="edits"/> runs against the screen's default state (instance Text1) and the
    /// component's default state, so one builder serves instance and component edits.
    /// </summary>
    internal static GumProjectSave Build(
        Action<StateSave>? screenEdits = null, Action<StateSave>? componentEdits = null,
        Action<ComponentSave, ScreenSave>? structure = null)
    {
        StandardElementsManager.Self.Initialize();
        GumProjectSave project = new GumProjectSave();
        foreach (string standard in new[] { "Container", "ColoredRectangle", "Text" })
        {
            StandardElementSave standardElement = new StandardElementSave { Name = standard };
            standardElement.States.Add(StandardElementsManager.Self.GetDefaultStateFor(standard)!);
            standardElement.DefaultState!.ParentContainer = standardElement;
            project.StandardElements.Add(standardElement);
        }

        BehaviorSave behavior = new BehaviorSave { Name = StandardFormsBehaviorNames.TextBoxBehaviorName };
        foreach ((string name, string type) in new[]
        {
            ("Placeholder", "string"), ("AcceptsReturn", "bool"), ("IsReadOnly", "bool"), ("IsEnabled", "bool"),
            ("MaxLength", "int?"), ("TextWrapping", "TextWrapping")
        })
        {
            behavior.FormsProperties.Add(new VariableSave { Name = name, Type = type });
        }
        project.Behaviors.Add(behavior);

        ComponentSave textBox = new ComponentSave { Name = "Controls/TextBoxStandard", BaseType = "Container" };
        StateSave componentDefault = new StateSave { Name = "Default", ParentContainer = textBox };
        textBox.States.Add(componentDefault);
        textBox.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behavior.Name });
        foreach (string instance in new[] { "Background", "TextInstance", "PlaceholderTextInstance", "CaretInstance" })
        {
            textBox.Instances.Add(new InstanceSave
            {
                Name = instance, BaseType = instance == "Background" || instance == "CaretInstance" ? "ColoredRectangle" : "Text",
                ParentContainer = textBox
            });
        }
        componentDefault.Variables.Add(new VariableSave
        {
            Name = "PlaceholderTextInstance.Text", Type = "string", Value = "Component placeholder",
            SetsValue = true, ExposedAsName = "Placeholder"
        });
        Set(componentDefault, "TextInstance.Text", "string", "");
        componentEdits?.Invoke(componentDefault);
        project.Components.Add(textBox);

        ScreenSave screen = new ScreenSave { Name = ScreenName };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        screen.Instances.Add(new InstanceSave { Name = "Text1", BaseType = textBox.Name, ParentContainer = screen });
        screenEdits?.Invoke(screenDefault);
        structure?.Invoke(textBox, screen);
        project.Screens.Add(screen);

        project.Initialize();
        return project;
    }

    internal static GraphicalUiElement Load(GumProjectSave project)
    {
        ObjectFinder.Self.GumProjectSave = project;
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        StandardRuntimeRegistrations.Register();
        return project.Screens.Single(s => s.Name == ScreenName)
            .ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
    }

    internal static void Reload(GraphicalUiElement root, GumProjectSave newProject)
    {
        ObjectFinder.Self.GumProjectSave = newProject;
        GumHotReloadManager.ApplyDiff(new[] { root }, newProject, SystemManagers.Default);
    }

    internal static TextBox Box(GraphicalUiElement screen, string name = "Text1") =>
        (TextBox)((InteractiveGue)screen.GetGraphicalUiElementByName(name)!).FormsControlAsObject!;
}

/// <summary>
/// A variable the tool saves for a TextBox instance that maps to a Forms property reaches the running
/// control on a reload with the value a restart shows.
/// </summary>
public class HotReloadFormsPropertyEditTests : BaseTestClass
{
    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    [Fact]
    public void ApplyDiff_InstancePlaceholderEdited_ControlShowsNewPlaceholder()
    {
        GraphicalUiElement screen = HotReloadTextBoxProjects.Load(HotReloadTextBoxProjects.Build(
            s => HotReloadTextBoxProjects.Set(s, "Text1.Placeholder", "string", "First hint")));
        TextBox box = HotReloadTextBoxProjects.Box(screen);
        box.Placeholder.ShouldBe("First hint");

        HotReloadTextBoxProjects.Reload(screen, HotReloadTextBoxProjects.Build(
            s => HotReloadTextBoxProjects.Set(s, "Text1.Placeholder", "string", "Second hint")));

        box.Placeholder.ShouldBe("Second hint");
        ((TextRuntime)box.Visual.GetGraphicalUiElementByName("PlaceholderTextInstance")!).Text.ShouldBe("Second hint");
    }

    private static GumProjectSave WithBool(string variable, bool? value) =>
        HotReloadTextBoxProjects.Build(s => HotReloadTextBoxProjects.Set(s, "Text1." + variable, "bool", value));

    [Fact]
    public void ApplyDiff_AuthoredBoolPropertyRemoved_RevertsLikeARestart()
    {
        GraphicalUiElement screen = HotReloadTextBoxProjects.Load(WithBool("IsReadOnly", true));
        TextBox box = HotReloadTextBoxProjects.Box(screen);
        box.IsReadOnly.ShouldBeTrue();

        HotReloadTextBoxProjects.Reload(screen, WithBool("IsReadOnly", null));

        box.IsReadOnly.ShouldBeFalse();
    }

    [Fact]
    public void ApplyDiff_AuthoredPlaceholderRemoved_RevertsToTheComponentPlaceholder()
    {
        GraphicalUiElement screen = HotReloadTextBoxProjects.Load(HotReloadTextBoxProjects.Build(
            s => HotReloadTextBoxProjects.Set(s, "Text1.Placeholder", "string", "Hint")));
        TextBox box = HotReloadTextBoxProjects.Box(screen);

        HotReloadTextBoxProjects.Reload(screen, HotReloadTextBoxProjects.Build());

        box.Placeholder.ShouldBe("Component placeholder");
    }

    [Fact]
    public void ApplyDiff_PropertySetInCodeAndFileUnchanged_KeepsTheCodeValue()
    {
        GraphicalUiElement screen = HotReloadTextBoxProjects.Load(WithBool("IsReadOnly", true));
        TextBox box = HotReloadTextBoxProjects.Box(screen);
        box.IsReadOnly = false;

        HotReloadTextBoxProjects.Reload(screen, WithBool("IsReadOnly", true));

        box.IsReadOnly.ShouldBeFalse();
    }

    [Fact]
    public void ApplyDiff_PropertySetInCodeAndFileChangesIt_TheFileWins()
    {
        GraphicalUiElement screen = HotReloadTextBoxProjects.Load(WithBool("IsReadOnly", false));
        TextBox box = HotReloadTextBoxProjects.Box(screen);
        box.IsReadOnly = true;

        HotReloadTextBoxProjects.Reload(screen, WithBool("IsReadOnly", true));
        box.IsReadOnly = false;
        HotReloadTextBoxProjects.Reload(screen, WithBool("IsReadOnly", false));

        box.IsReadOnly.ShouldBeFalse();
        box.IsReadOnly = true;
        HotReloadTextBoxProjects.Reload(screen, WithBool("IsReadOnly", null));
        box.IsReadOnly.ShouldBeFalse();
    }

    [Fact]
    public void ApplyDiff_PlaceholderEditedWhileFocused_TextCaretAndSelectionUnchanged()
    {
        GraphicalUiElement screen = HotReloadTextBoxProjects.Load(HotReloadTextBoxProjects.Build(
            s => HotReloadTextBoxProjects.Set(s, "Text1.Placeholder", "string", "First")));
        TextBox box = HotReloadTextBoxProjects.Box(screen);
        box.Text = "hello world";
        box.IsFocused = true;
        box.CaretIndex = 6;
        box.SelectionLength = 3;

        HotReloadTextBoxProjects.Reload(screen, HotReloadTextBoxProjects.Build(
            s => HotReloadTextBoxProjects.Set(s, "Text1.Placeholder", "string", "Second")));

        box.Placeholder.ShouldBe("Second");
        box.Text.ShouldBe("hello world");
        box.CaretIndex.ShouldBe(6);
        box.SelectionLength.ShouldBe(3);
    }

    [Fact]
    public void Load_ComponentDefaultSetsMaxLengthLettersAndLines_DoesNotThrowAndAppliesThem()
    {
        GumProjectSave project = HotReloadTextBoxProjects.Build(
            componentEdits: s =>
            {
                HotReloadTextBoxProjects.Set(s, "MaxLength", "int?", 7);
                HotReloadTextBoxProjects.Set(s, "MaxLettersToShow", "int?", 5);
                HotReloadTextBoxProjects.Set(s, "MaxNumberOfLines", "int?", 3);
            });

        GraphicalUiElement screen = HotReloadTextBoxProjects.Load(project);

        TextBox box = HotReloadTextBoxProjects.Box(screen);
        box.MaxLength.ShouldBe(7);
        box.MaxLettersToShow.ShouldBe(5);
        box.MaxNumberOfLines.ShouldBe(3);
    }
}
