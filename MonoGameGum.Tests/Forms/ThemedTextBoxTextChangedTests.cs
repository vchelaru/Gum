using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using GumKeys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A TextBox loaded from each packaged theme's component files by the real loader raises TextChanged,
/// pushes TwoWay bindings, hides the placeholder and clamps the caret when its text changes, like a
/// code-built TextBox does.
/// </summary>
public class ThemedTextBoxTextChangedTests : BeginnerDriverTestBase
{
    public ThemedTextBoxTextChangedTests()
    {
        // BaseTestClass.Dispose resets the registry, so a later test would find the shape defaults gone.
        Gum.Renderables.DefaultStrokedCircleRenderable.RegisterRuntimeTypes();
        Gum.Renderables.DefaultFilledRectangleRenderable.RegisterRuntimeTypes();
        Gum.Renderables.DefaultStrokedRectangleRenderable.RegisterRuntimeTypes();
    }

    private static readonly string[] ThemeNames =
        { "", "Bubblegum", "DarkPro", "Editor", "ForestGlade", "Hazard", "Meadow", "Neon", "Retro95" };

    public static IEnumerable<object[]> Themes() => ThemeNames.Select(theme => new object[] { theme });

    #region Helpers

    private GumProjectSave LoadTheme(string theme)
    {
        string themeFolder = theme.Length == 0 ? "FormsTemplate" : Path.Combine("FormsThemes", theme);
        string gumx = Path.Combine(LocateRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", themeFolder, "GumProject.gumx");
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;
        ElementSaveExtensions.RegisterGueInstantiationType("Container", typeof(Gum.GueDeriving.ContainerRuntime));
        ElementSaveExtensions.RegisterGueInstantiation("Text", () => new Gum.GueDeriving.TextRuntime());
        FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        return project;
    }

    private (InteractiveGue Visual, TextBox Box) Place(string theme, Action<TextBox>? configure = null)
    {
        GumProjectSave project = LoadTheme(theme);
        string component = project.Components.Single(item => item.Name.EndsWith("Controls/TextBox")).Name;
        ScreenSave screen = new ScreenSave { Name = "Screen" + project.Screens.Count };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Box", BaseType = component, ParentContainer = screen });
        project.Screens.Add(screen);
        project.Initialize();
        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.AddToRoot();
        Driver.Step(2);
        InteractiveGue visual = (InteractiveGue)screenVisual.GetGraphicalUiElementByName("Box")!;
        TextBox box = (TextBox)visual.FormsControlAsObject!;
        box.Width = 200;
        configure?.Invoke(box);
        Driver.Step(2);
        return (visual, box);
    }

    private void FocusByClick(InteractiveGue visual)
    {
        int x = (int)(visual.AbsoluteLeft + visual.AbsoluteWidth / 2);
        int y = (int)(visual.AbsoluteTop + visual.AbsoluteHeight / 2);
        Driver.Click(x, y);
    }

    private static string LocateRepoRoot()
    {
        string? directory = AppContext.BaseDirectory;
        while (directory != null && !File.Exists(Path.Combine(directory, "GumFull.sln")))
        {
            directory = Path.GetDirectoryName(directory);
        }
        return directory ?? throw new InvalidOperationException("Repo root not found.");
    }

    private static string Describe(string theme) => (theme.Length == 0 ? "FormsTemplate" : theme) + " TextBox";

    private sealed class TextViewModel : Gum.Mvvm.ViewModel
    {
        public string? Text
        {
            get => Get<string>();
            set => Set(value);
        }
    }

    #endregion

    [Theory]
    [MemberData(nameof(Themes))]
    public void TextChanged_ThemedTextBox_FiresOnceWhenTextIsSetFromCode(string theme)
    {
        (InteractiveGue _, TextBox box) = Place(theme);
        int fired = 0;
        box.TextChanged += (_, _) => fired++;

        box.Text = "abc";
        Driver.Step(1);

        fired.ShouldBe(1, Describe(theme));
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TextChanged_ThemedTextBox_FiresOncePerTypedCharacter(string theme)
    {
        (InteractiveGue visual, TextBox box) = Place(theme);
        int fired = 0;
        box.TextChanged += (_, _) => fired++;
        FocusByClick(visual);

        Driver.Type("abc");

        fired.ShouldBe(3, Describe(theme));
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void Placeholder_ThemedTextBox_HiddenOnceTextIsTypedAndShownAgainWhenCleared(string theme)
    {
        (InteractiveGue visual, TextBox box) = Place(theme, b => b.Placeholder = "Type here");
        GraphicalUiElement? placeholder = visual.GetGraphicalUiElementByName("PlaceholderTextInstance");
        if (placeholder == null)
        {
            return;
        }

        placeholder.AbsoluteVisible.ShouldBeTrue($"{Describe(theme)}: empty");
        FocusByClick(visual);
        Driver.Type("a");
        placeholder.AbsoluteVisible.ShouldBeFalse($"{Describe(theme)}: typed");
        Driver.PressKey(GumKeys.Back);
        placeholder.AbsoluteVisible.ShouldBeTrue($"{Describe(theme)}: cleared");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void Placeholder_ThemedTextBox_HiddenWhenTextIsSetFromCode(string theme)
    {
        (InteractiveGue visual, TextBox box) = Place(theme, b => b.Placeholder = "Type here");
        GraphicalUiElement? placeholder = visual.GetGraphicalUiElementByName("PlaceholderTextInstance");

        box.Text = "abc";
        Driver.Step(2);

        placeholder!.AbsoluteVisible.ShouldBeFalse(Describe(theme));
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TwoWayBinding_ThemedTextBox_TypedTextReachesTheViewModel(string theme)
    {
        (InteractiveGue visual, TextBox box) = Place(theme);
        TextViewModel viewModel = new TextViewModel();
        box.BindingContext = viewModel;
        box.SetBinding(nameof(TextBox.Text), new Gum.Forms.Data.Binding(nameof(TextViewModel.Text)) { Mode = Gum.Forms.Data.BindingMode.TwoWay });
        FocusByClick(visual);

        Driver.Type("abc");

        viewModel.Text.ShouldBe("abc", Describe(theme));
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TextSetFromCodeWhileFocused_ShorterText_CaretIsClampedToTheText(string theme)
    {
        (InteractiveGue visual, TextBox box) = Place(theme);
        FocusByClick(visual);
        Driver.Type("hello world");

        box.Text = "hi";
        Driver.Step(2);

        box.CaretIndex.ShouldBeInRange(0, 2, Describe(theme));
    }
}
