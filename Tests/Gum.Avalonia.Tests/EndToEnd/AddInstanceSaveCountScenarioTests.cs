using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.TreeView;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// Every add gesture writes the element it adds to once, and that one write holds the finished add:
/// the file matches what Save All writes afterwards (#8417). Each add goes inside a container, the case
/// where the add also sets the new instance's Parent variable.
/// </summary>
[Trait("Category", "EndToEnd")]
public class AddInstanceSaveCountScenarioTests
{
    [AvaloniaFact]
    public void ChipClick_SavesTheScreenOnce_AndUndoRemovesTheAdd()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        InstanceSave container = tree.Project.AddInstance(screen, "Container1", "Container");
        tree.Click(tree.NodeFor(container));

        List<string> writes = WritesDuring(tree, screen, () => ClickChip(tree, "Sprite"));

        ParentOf(screen, Single(screen, "Sprite")).ShouldBe("Container1");
        ShouldBeOneFinishedWrite(tree, screen, writes);
        tree.Undo();
        screen.Instances.ShouldNotContain(instance => instance.BaseType == "Sprite");
        tree.Redo();
        ParentOf(screen, Single(screen, "Sprite")).ShouldBe("Container1");
    }

    [AvaloniaFact]
    public void ChipDrop_SavesTheScreenOnce()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        InstanceSave container = tree.Project.AddInstance(screen, "Container1", "Container");
        tree.Click(tree.NodeFor(container));

        List<string> writes = WritesDuring(tree, screen,
            () => tree.DropChipOn(ProjectTreeHarness.StandardChipData("Sprite"), tree.NodeFor(container), 0.5));

        ParentOf(screen, Single(screen, "Sprite")).ShouldBe("Container1");
        ShouldBeOneFinishedWrite(tree, screen, writes);
    }

    [AvaloniaFact]
    public void ComponentDraggedOntoAnInstance_SavesTheScreenOnce()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        InstanceSave container = tree.Project.AddInstance(screen, "Container1", "Container");
        tree.Click(tree.NodeFor(container));

        List<string> writes = WritesDuring(tree, screen,
            () => tree.Drag(tree.NodeFor(button), tree.NodeFor(container)));

        ParentOf(screen, Single(screen, "Button")).ShouldBe("Container1");
        ShouldBeOneFinishedWrite(tree, screen, writes);
    }

    [AvaloniaFact]
    public void AddInstanceDialog_SavesTheScreenOnce()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        InstanceSave container = tree.Project.AddInstance(screen, "Container1", "Container");
        tree.Click(tree.NodeFor(container));
        tree.Dialogs.AnswerNext<AddInstanceDialogViewModel>(dialog => { dialog.TypeToCreate = "Sprite"; dialog.Value = "Icon"; return true; });

        List<string> writes = WritesDuring(tree, screen, () => tree.PickMainMenu("Edit", "Add", "Instance"));

        Single(screen, "Sprite").Name.ShouldBe("Icon");
        ShouldBeOneFinishedWrite(tree, screen, writes);
    }

    [AvaloniaFact]
    public void PasteAndDuplicate_InsideAContainer_EachSaveTheScreenOnce()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        InstanceSave container = tree.Project.AddInstance(screen, "Container1", "Container");
        tree.Click(tree.NodeFor(container));
        ClickChip(tree, "Sprite");
        InstanceSave sprite = Single(screen, "Sprite");
        tree.Click(tree.NodeFor(sprite));
        tree.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);

        List<string> pasteWrites = WritesDuring(tree, screen,
            () => tree.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control));

        screen.Instances.Count(instance => instance.BaseType == "Sprite").ShouldBe(2);
        ShouldBeOneFinishedWrite(tree, screen, pasteWrites);

        tree.Click(tree.NodeFor(sprite));
        List<string> duplicateWrites = WritesDuring(tree, screen,
            () => tree.Press(Key.D, PhysicalKey.D, RawInputModifiers.Control));

        screen.Instances.Count(instance => instance.BaseType == "Sprite").ShouldBe(3);
        ShouldBeOneFinishedWrite(tree, screen, duplicateWrites);
    }

    /// <summary>
    /// Runs <paramref name="action"/> with Auto Save on and returns the content of each write to
    /// <paramref name="element"/>'s file, in order. A save is announced before it writes, so the file
    /// read at each announcement holds the previous write and the file at the end holds the last.
    /// </summary>
    private static List<string> WritesDuring(ProjectTreeHarness tree, ElementSave element, Action action)
    {
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();
        PluginManager pluginManager = (PluginManager)TestAppBuilder.Services.GetRequiredService<IPluginManager>();
        PluginBase plugin = pluginManager.InitializedPlugins.First();
        string file = ElementFile(tree, element);
        int saves = 0;
        List<string> writes = new List<string>();
        Action<ElementSave> onSave = saved =>
        {
            if (saved != element)
            {
                return;
            }
            if (saves > 0)
            {
                writes.Add(File.ReadAllText(file));
            }
            saves++;
        };

        bool autoSave = projectManager.AutoSave;
        projectManager.AutoSave = true;
        plugin.BeforeElementSave += onSave;
        try
        {
            action();
            tree.ThrowIfCrashed();
        }
        finally
        {
            plugin.BeforeElementSave -= onSave;
            projectManager.AutoSave = autoSave;
        }
        if (saves > 0)
        {
            writes.Add(File.ReadAllText(file));
        }
        return writes;
    }

    private static void ShouldBeOneFinishedWrite(ProjectTreeHarness tree, ElementSave element, List<string> writes)
    {
        writes.Count.ShouldBe(1);
        tree.SaveAll();
        File.ReadAllText(ElementFile(tree, element)).ShouldBe(writes[0]);
    }

    private static string ElementFile(ProjectTreeHarness tree, ElementSave element) =>
        Directory.GetFiles(tree.Project.ProjectFolder, element.Name + ".gus*", SearchOption.AllDirectories).Single();

    private static void ClickChip(ProjectTreeHarness tree, string typeName)
    {
        // The chip is the innermost Border around the type name's TextBlock.
        Border chip = tree.Input.Window.GetVisualDescendants()
            .OfType<AvaloniaStandardsPalette>().Single()
            .GetVisualDescendants().OfType<Border>()
            .Where(border => border.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Text == typeName))
            .OrderBy(border => border.GetVisualDescendants().Count())
            .First();
        tree.Input.Click(chip);
        tree.ThrowIfCrashed();
    }

    private static InstanceSave Single(ElementSave element, string baseType) =>
        element.Instances.Single(instance => instance.BaseType == baseType);

    private static string? ParentOf(ElementSave element, InstanceSave instance) =>
        element.GetDefaultStateOrThrow().GetValue($"{instance.Name}.Parent") as string;
}
