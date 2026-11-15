using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms.Themed;

/// <summary>
/// A reload of a project nobody edited changes nothing on screen: every tool theme, a control placed alone on a
/// screen, the position and size of it and of everything inside it match before and after. A state the control
/// applied while it was built must not be replaced by a value from a component default (a standalone vertical
/// ScrollBar in DarkPro and Neon grew from 128 to the screen height). #8108
/// </summary>
public class ThemedNoOpHotReloadTests : FileThemeInputTestBase
{
    private ScreenSave? addedScreen;
    private GumProjectSave? addedTo;

    public override void Dispose()
    {
        addedTo?.Screens.Remove(addedScreen!);
        base.Dispose();
    }

    public static IEnumerable<object[]> ThemeAndControl() =>
        AllThemes().Select(row => (string)row[0]).SelectMany(theme =>
            new[] { "ScrollBar", "Slider", "ComboBox", "TextBoxMultiLine", "ListBox", "ScrollViewer", "ItemsControl" }
                .Select(control => new object[] { theme, control }));

    private static void Snapshot(GraphicalUiElement element, string path, Dictionary<string, string> into)
    {
        if (!element.AbsoluteVisible) { return; } // a hidden element's layout is deferred until it shows
        into[path] = $"{element.AbsoluteLeft:0.##},{element.AbsoluteTop:0.##},{element.AbsoluteWidth:0.##},{element.AbsoluteHeight:0.##},{element.AbsoluteVisible}";
        foreach (GraphicalUiElement child in element.Children.OfType<GraphicalUiElement>())
        {
            Snapshot(child, path + "/" + (child.Name ?? "?"), into);
        }
    }

    [Theory]
    [MemberData(nameof(ThemeAndControl))]
    public void ReloadWithNoEdit_ShouldNotMoveOrResizeAnything(string theme, string control)
    {
        GumProjectSave project = LoadTheme(theme);
        string component = control.StartsWith("ScrollBar") ? "ScrollBar" : control.StartsWith("TextBox") ? "TextBox" : control;
        ScreenSave screen = new ScreenSave { Name = "AloneScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Box", BaseType = ComponentName(project, component), ParentContainer = screen });
        project.Screens.Add(screen);
        addedScreen = screen;
        addedTo = project;
        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.AddToRoot();
        Driver.Step(2);
        FrameworkElement box = (FrameworkElement)((InteractiveGue)screenVisual.GetGraphicalUiElementByName("Box")!).FormsControlAsObject!;
        if (control == "TextBoxMultiLine") { ((TextBox)box).TextWrapping = Gum.Forms.TextWrapping.Wrap; ((TextBox)box).AcceptsReturn = true; ((TextBox)box).Text = "a\nb\nc"; }
        Driver.Step(2);
        Dictionary<string, string> before = new Dictionary<string, string>();
        Snapshot(screenVisual, "", before);

        Gum.GumHotReloadManager.ApplyDiff(new[] { screenVisual }, project, RenderingLibrary.SystemManagers.Default);
        Driver.Step(2);

        Dictionary<string, string> after = new Dictionary<string, string>();
        Snapshot(screenVisual, "", after);
        string[] changed = before.Where(pair => !after.TryGetValue(pair.Key, out string? value) || value != pair.Value)
            .Select(pair => $"{pair.Key}: {pair.Value} -> {(after.TryGetValue(pair.Key, out string? now) ? now : "gone")}").ToArray();
        changed.ShouldBeEmpty($"{theme} {control}");
    }
}
