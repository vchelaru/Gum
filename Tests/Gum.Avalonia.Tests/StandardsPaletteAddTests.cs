using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.TreeView;
using Gum.Avalonia.Shell;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolStates;
using Gum.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Every add gesture, driven with real pointer input through the head's real service graph on a
/// tree manager of its own (the head's singleton manager keeps its tab for the main-window tests):
/// each add lands at the add destination, the container the user last clicked, and paste follows
/// the same destination.
/// </summary>
public class StandardsPaletteAddTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    public void EveryAddGesture_AddsAtTheAddDestination_AndPasteFollowsIt()
    {
        AvaloniaTabManager tabManager = (AvaloniaTabManager)Services.GetRequiredService<ITabManager>();
        ISelectedState selectedState = Services.GetRequiredService<ISelectedState>();
        ICopyPasteLogic copyPasteLogic = Services.GetRequiredService<ICopyPasteLogic>();
        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        projectManager.CreateNewProject();
        GumProjectSave project = projectManager.GumProjectSave!;
        // The tree refresh resolves folders off the project file; nothing is written there.
        project.FullFileName = Path.Combine(Path.GetTempPath(), "GumAvaloniaTests", Guid.NewGuid().ToString("N"), "Project.gumx");

        ComponentSave component = NewComponent("MyComponent");
        InstanceSave container = new InstanceSave { Name = "Container", BaseType = "Container", ParentContainer = component };
        component.Instances.Add(container);
        project.Components.Add(component);
        ComponentSave button = NewComponent("Button");
        project.Components.Add(button);

        // A manager of this test's own, so its panel can live in this test's window.
        ElementTreeViewManager treeViewManager = ActivatorUtilities.CreateInstance<ElementTreeViewManager>(Services);
        treeViewManager.Initialize();
        AvaloniaPluginTab projectTab = tabManager.Left.Last(tab => tab.Title == "Project");
        Window window = new Window { Content = projectTab.Content, Width = 400, Height = 900 };
        window.Show();
        AvaloniaGumTreeView tree = window.GetVisualDescendants().OfType<AvaloniaGumTreeView>().Single();
        try
        {
            treeViewManager.RefreshUi();
            GumTreeNode componentNode = treeViewManager.GetTreeNodeFor(component)!;
            GumTreeNode containerNode = treeViewManager.GetTreeNodeFor(container, componentNode)!;
            GumTreeNode buttonNode = treeViewManager.GetTreeNodeFor(button)!;
            componentNode.Parent!.Expand();
            componentNode.Expand();
            Layout(window);

            // A click on the Container row picks it as the add destination.
            ClickRow(window, tree, containerNode, RawInputModifiers.None);
            selectedState.SelectedInstance.ShouldBeSameAs(container);

            // Chip Ctrl+Shift+click, chip Ctrl+click and Ctrl+Shift+click on a Component node all
            // add under Container, even though each add selects what it created.
            ClickChip(window, "Text", RawInputModifiers.Control | RawInputModifiers.Shift);
            InstanceSave text = component.Instances.Single(instance => instance.BaseType == "Text");
            ParentOf(component, text).ShouldBe("Container");
            selectedState.SelectedInstance.ShouldBeSameAs(text);

            ClickChip(window, "Sprite", RawInputModifiers.Control);
            InstanceSave sprite = component.Instances.Single(instance => instance.BaseType == "Sprite");
            ParentOf(component, sprite).ShouldBe("Container");
            selectedState.SelectedInstance.ShouldBeSameAs(sprite);

            Layout(window);
            ClickRow(window, tree, buttonNode, RawInputModifiers.Control | RawInputModifiers.Shift);
            InstanceSave buttonInstance = component.Instances.Single(instance => instance.BaseType == "Button");
            ParentOf(component, buttonInstance).ShouldBe("Container");
            selectedState.SelectedInstance.ShouldBeSameAs(buttonInstance);

            // The tree's right-click Add menu adds there too.
            InvokeAddMenuItem(treeViewManager, "Circle");
            InstanceSave circle = component.Instances.Single(instance => instance.BaseType == "Circle");
            ParentOf(component, circle).ShouldBe("Container");

            // Copy the last add and paste: the paste follows the same destination.
            copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
            copyPasteLogic.OnPaste(CopyType.InstanceOrElement);
            InstanceSave pastedCircle = component.Instances.Where(instance => instance.BaseType == "Circle").Skip(1).Single();
            ParentOf(component, pastedCircle).ShouldBe("Container");

            // Clicking a different node moves the destination: the Component's root.
            Layout(window);
            ClickRow(window, tree, componentNode, RawInputModifiers.None);
            selectedState.SelectedInstance.ShouldBeNull();
            ClickChip(window, "Text", RawInputModifiers.Control);
            InstanceSave rootText = component.Instances.Where(instance => instance.BaseType == "Text").Skip(1).Single();
            ParentOf(component, rootText).ShouldBeNull();
        }
        finally
        {
            selectedState.SelectedInstance = null;
            selectedState.SelectedElement = null;
            ObjectFinder.Self.GumProjectSave = null;
            window.Content = null;
            window.Close();
            tabManager.RemoveTab(projectTab);
        }
    }

    private static ComponentSave NewComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        return component;
    }

    private static string? ParentOf(ElementSave element, InstanceSave instance) =>
        element.DefaultState.GetValue($"{instance.Name}.Parent") as string;

    private static void Layout(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    private static void ClickRow(Window window, AvaloniaGumTreeView tree, GumTreeNode node, RawInputModifiers modifiers)
    {
        TreeRowView row = tree.GetVisualDescendants().OfType<TreeRowView>().Single(view => view.Row?.Node == node);
        Click(window, row, modifiers);
    }

    private static void ClickChip(Window window, string typeName, RawInputModifiers modifiers)
    {
        // The chip is the innermost Border around the type name's TextBlock.
        Border chip = window.GetVisualDescendants()
            .OfType<AvaloniaStandardsPalette>().Single()
            .GetVisualDescendants().OfType<Border>()
            .Where(border => border.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Text == typeName))
            .OrderBy(border => border.GetVisualDescendants().Count())
            .First();
        Click(window, chip, modifiers);
    }

    private static void Click(Window window, Control control, RawInputModifiers modifiers)
    {
        Point point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left, modifiers);
        window.MouseUp(point, MouseButton.Left, modifiers);
        Dispatcher.UIThread.RunJobs();
    }

    private static void InvokeAddMenuItem(ElementTreeViewManager treeViewManager, string typeName)
    {
        ContextMenuItemViewModel addMenu = treeViewManager.BuildContextMenuItems()
            .Single(item => item.Text.StartsWith("Add child object to"));
        addMenu.Children.Single(item => item.Text == typeName).Action!();
    }
}
