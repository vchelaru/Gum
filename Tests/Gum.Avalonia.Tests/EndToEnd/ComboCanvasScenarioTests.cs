using Avalonia.Input;
using Gum.Avalonia.Shell;
using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins.PropertiesWindowPlugin;
using Gum.Wireframe;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// The COMBO scenarios that need the Editor canvas: what the canvas does with a locked instance
/// and what a Text shows in the chosen language. They run where <see cref="CanvasHarness"/> can
/// (a display and a GL driver; on CI the Linux Xvfb step, whose filter matches this class name).
/// </summary>
[Trait("Category", "EndToEnd")]
public class ComboCanvasScenarioTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [SkippableFact]
    [Trait("Feature", "COMBO-036")]
    public void ALockedInstance_StaysPutForCanvasDragsAndNudges_AndATreeDragStillReordersIt()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            canvas.AddInstance(button, "Box", "Rectangle", x: 40, y: 40, width: 60, height: 40);
            canvas.AddInstance(button, "Other", "Rectangle", x: 200, y: 40, width: 60, height: 40);
            ProjectTreeHarness tree = canvas.Tree;
            tree.Click(tree.NodeFor(button));
            ProjectFileSnapshot start = tree.SnapshotFiles();

            canvas.Click(canvas.WindowPointOf(70, 60));
            canvas.Project.SelectedState.SelectedInstance.ShouldNotBeNull().Name.ShouldBe("Box", canvas.Describe());
            tree.RightClick(tree.NodeFor(Instance(button, "Box")));
            tree.PickMenu("Lock Box");
            Instance(button, "Box").Locked.ShouldBeTrue();
            ProjectFileSnapshot locked = tree.SnapshotFiles();

            // The canvas moves it neither by a drag nor by the arrow keys.
            canvas.Drag(canvas.WindowPointOf(70, 60), canvas.WindowPointOf(110, 90));
            canvas.Press(Key.Right, PhysicalKey.ArrowRight);
            canvas.Press(Key.Down, PhysicalKey.ArrowDown, RawInputModifiers.Shift);
            canvas.SavedValue(button, "Box.X").ShouldBe(40f);
            canvas.SavedValue(button, "Box.Y").ShouldBe(40f);
            tree.SnapshotFiles().ShouldMatch(locked, "a locked instance should not move on the canvas");

            // The lock is about where it sits on the canvas: dragging its row below Other in the
            // tree still reorders it.
            tree.Drag(tree.NodeFor(Instance(button, "Box")), tree.NodeFor(Instance(button, "Other")), rowFraction: 0.9);
            button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Other", "Box" });
            Instance(button, "Box").Locked.ShouldBeTrue();
            canvas.SavedValue(button, "Box.X").ShouldBe(40f);

            // Unlocked, the same nudge moves it.
            tree.RightClick(tree.NodeFor(Instance(button, "Box")));
            tree.PickMenu("Unlock Box");
            canvas.Click(canvas.WindowPointOf(70, 60));
            canvas.Press(Key.Right, PhysicalKey.ArrowRight);
            canvas.SavedValue(button, "Box.X").ShouldBe(41f);
            ProjectFileSnapshot finished = tree.SnapshotFiles();

            // Nudge, unlock, reorder and lock undo back to the start, and redo to the end.
            tree.Click(tree.NodeFor(button));
            for (int i = 0; i < 4; i++)
            {
                tree.Undo();
            }
            tree.SnapshotFiles().ShouldMatch(start, "undoing the nudge, unlock, reorder and lock should restore the files");
            for (int i = 0; i < 4; i++)
            {
                tree.Redo();
            }
            tree.SnapshotFiles().ShouldMatch(finished, "redoing them should restore the finished files");

            canvas.AssertOracles();
        });
    }

    [SkippableFact]
    [Trait("Feature", "COMBO-033")]
    public void ChangingTheLanguage_ThenPickingATextsStringId_ShowsItsTranslationOnTheCanvas()
    {
        OnCanvas(canvas =>
        {
            LocalizationService localization = Services.GetRequiredService<LocalizationService>();
            ProjectPropertiesViewModel properties = (ProjectPropertiesViewModel)((global::Avalonia.Controls.Control)
                ((AvaloniaTabManager)Services.GetRequiredService<ITabManager>()).AllTabs.Single(tab => tab.Title == "Project Properties").Content).DataContext!;
            try
            {
                ComponentSave button = canvas.Project.AddComponent("Button");
                canvas.AddInstance(button, "Label", "Text", x: 20, y: 20, width: 200, height: 40);
                File.WriteAllText(Path.Combine(canvas.Project.ProjectFolder, "Strings.csv"),
                    "String ID,English,French\nT_Play,Play,Jouer\nT_Quit,Quit,Quitter\n");
                properties.LocalizationFiles = new List<string> { "Strings.csv" };
                properties.LanguageName = "French";
                canvas.Project.Project.CurrentLanguageIndex.ShouldBe(2);

                ProjectTreeHarness tree = canvas.Tree;
                tree.Click(tree.NodeFor(Instance(button, "Label")));
                tree.Grid.Combo("Text").Items.Cast<object?>().Select(item => item?.ToString()).ShouldBe(new[] { "T_Play", "T_Quit" });
                tree.Grid.PickComboItem("Text", "T_Play");
                canvas.Frame();

                canvas.SavedValue(button, "Label.Text").ShouldBe("T_Play", "the Text stores the string ID, not the translation");
                ShownText(canvas, "Label").ShouldBe("Jouer");

                properties.LanguageName = "English";
                canvas.Frame();
                ShownText(canvas, "Label").ShouldBe("Play");
                tree.Grid.PickComboItem("Text", "T_Quit");
                canvas.Frame();
                ShownText(canvas, "Label").ShouldBe("Quit");

                tree.Undo();
                canvas.Frame();
                canvas.SavedValue(button, "Label.Text").ShouldBe("T_Play");
                ShownText(canvas, "Label").ShouldBe("Play");

                GumProjectSave.Load(canvas.Project.ProjectFilePath, out _).ShouldNotBeNull().CurrentLanguageIndex.ShouldBe(1);
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

    /// <summary>The text the canvas draws for the Text instance named <paramref name="name"/>.</summary>
    private static string? ShownText(CanvasHarness canvas, string name)
    {
        GraphicalUiElement shown = canvas.Wireframe.AllIpsos.OfType<GraphicalUiElement>()
            .SingleOrDefault(ipso => ipso.Tag is InstanceSave instance && instance.Name == name)
            ?? throw new InvalidOperationException($"The canvas shows no {name}. {canvas.Describe()}");
        return (shown.RenderableComponent as global::RenderingLibrary.Graphics.Text)?.RawText;
    }

    private static InstanceSave Instance(ElementSave element, string name) =>
        element.Instances.Single(instance => instance.Name == name);

    private static void OnCanvas(Action<CanvasHarness> scenario)
    {
        Skip.IfNot(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        CanvasHarness.OnUiThread(() =>
        {
            using CanvasHarness canvas = new CanvasHarness();
            scenario(canvas);
        });
    }
}
