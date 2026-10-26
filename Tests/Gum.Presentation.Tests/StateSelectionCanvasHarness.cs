using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolStates;
using Gum.Wireframe;
using GumRuntime;
using Moq;
using Shouldly;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace Gum.Presentation.Tests;

/// <summary>
/// Shared builders and the real-RefreshAll driver for the #6851 state-selection canvas tests.
/// </summary>
internal static class StateSelectionCanvasHarness
{
    internal sealed class RecordingRenderable : InvisibleRenderable
    {
        public int Red { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }
    }

    internal static readonly (int Red, int Green, int Blue) Teal = (6, 159, 177);
    internal static readonly (int Red, int Green, int Blue) DarkGray = (70, 70, 80);

    internal static (int, int, int) ShowSelectedAndReadBackground(ComponentSave component, StateSave selected)
    {
        GraphicalUiElement root = Show(component, selected);
        RecordingRenderable background = BackgroundOf(root);
        return (background.Red, background.Green, background.Blue);
    }

    internal static float ShowSelectedAndReadBackgroundWidth(ComponentSave component, StateSave selected)
    {
        GraphicalUiElement root = Show(component, selected);
        return root.GetGraphicalUiElementByName("Background")!.Width;
    }

    internal static RecordingRenderable BackgroundOf(GraphicalUiElement root) =>
        (RecordingRenderable)root.GetGraphicalUiElementByName("Background")!.RenderableComponent!;

    internal static GraphicalUiElement Show(ElementSave element, StateSave selected) =>
        new CanvasSession(element, selected).Root!;

    /// <summary>
    /// A real <see cref="WireframeObjectManager"/> kept across selections, so a test can click a state,
    /// click another, and edit the data between refreshes the way the tool does.
    /// </summary>
    internal sealed class CanvasSession
    {
        private readonly WireframeObjectManager _manager;
        private StateSave _selected;

        public GraphicalUiElement? Root { get; private set; }

        public CanvasSession(ElementSave element, StateSave selected)
        {
            _selected = selected;

            Mock<IPluginManager> pluginManager = new Mock<IPluginManager>();
            pluginManager.Setup(x => x.CreateGraphicalUiElement(element)).Returns(() =>
            {
                Root = element.ToGraphicalUiElement(systemManagers: null!, addToManagers: false);
                return Root;
            });

            Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
            selectedState.SetupGet(x => x.SelectedElements).Returns(new[] { element });
            selectedState.SetupGet(x => x.SelectedElement).Returns(element);
            selectedState.SetupGet(x => x.SelectedStateSave).Returns(() => _selected);

            _manager = new WireframeObjectManager(
                Mock.Of<IFontManager>(),
                selectedState.Object,
                Mock.Of<IDialogService>(),
                Mock.Of<IGuiCommands>(),
                new LocalizationService(),
                pluginManager.Object,
                Mock.Of<IProjectState>());

            Refresh();
        }

        public void Select(StateSave state)
        {
            _selected = state;
            Refresh();
        }

        public void Refresh()
        {
            _manager.RefreshAll(forceLayout: true);
            Root.ShouldNotBeNull();
        }
    }

    internal static StandardElementSave BuildStandard(string name)
    {
        StandardElementSave standard = new StandardElementSave { Name = name };
        StateSave defaults = StandardElementsManager.Self.GetDefaultStateFor(name).Clone();
        defaults.ParentContainer = standard;
        standard.States.Add(defaults);
        return standard;
    }

    internal static void AddReference(StateSave state, string owner, string line)
    {
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = owner + ".VariableReferences" };
        list.Value.Add(line);
        state.VariableLists.Add(list);
    }

    // A Rectangle's color channels are FillRed/Green/Blue; a NineSlice's (the button Background) are Red/Green/Blue.
    internal static void SetFill(StateSave state, string owner, (int Red, int Green, int Blue) color, string prefix = "Fill")
    {
        state.Variables.Add(new VariableSave { Name = owner + "." + prefix + "Red", Value = color.Red, Type = "int", SetsValue = true });
        state.Variables.Add(new VariableSave { Name = owner + "." + prefix + "Green", Value = color.Green, Type = "int", SetsValue = true });
        state.Variables.Add(new VariableSave { Name = owner + "." + prefix + "Blue", Value = color.Blue, Type = "int", SetsValue = true });
    }

    /// <summary>The project's Styles component: Primary is teal, DarkGray is dark gray.</summary>
    internal static ComponentSave BuildStyles()
    {
        ComponentSave styles = new ComponentSave { Name = "Styles", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = styles };
        styles.States.Add(defaultState);
        styles.Instances.Add(new InstanceSave { Name = "Primary", BaseType = "Rectangle", ParentContainer = styles });
        styles.Instances.Add(new InstanceSave { Name = "DarkGray", BaseType = "Rectangle", ParentContainer = styles });
        SetFill(defaultState, "Primary", Teal);
        SetFill(defaultState, "DarkGray", DarkGray);
        return styles;
    }

    internal static GumProjectSave BuildProject(params ComponentSave[] components)
    {
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(BuildStandard("Container"));
        project.StandardElements.Add(BuildStandard("Rectangle"));
        project.StandardElements.Add(BuildStandard("NineSlice"));
        foreach (ComponentSave component in components)
        {
            project.Components.Add(component);
        }
        ObjectFinder.Self.GumProjectSave = project;
        return project;
    }

    /// <summary>
    /// ButtonStandard as the Forms template ships it: Background (NineSlice), the Button behavior,
    /// a default state with a hard teal color and, when asked, the reference row
    /// <c>Color = Components/Styles.Primary.FillColor</c>, and ButtonCategory with Enabled (teal,
    /// Primary) and Disabled (dark gray stored, <c>Color = Components/Styles.DarkGray.FillColor</c>).
    /// </summary>
    internal static ComponentSave BuildButton(bool defaultHasColorReference)
    {
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        behavior.ToolOnlyVariableReferences.Add("ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"");

        ComponentSave button = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });
        button.Instances.Add(new InstanceSave { Name = "Background", BaseType = "NineSlice", ParentContainer = button });

        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = button };
        button.States.Add(defaultState);
        SetFill(defaultState, "Background", Teal, prefix: "");
        defaultState.Variables.Add(new VariableSave { Name = "ButtonCategoryState", Type = "ButtonCategory", SetsValue = false });
        if (defaultHasColorReference)
        {
            AddReference(defaultState, "Background", "Color = Components/Styles.Primary.FillColor");
        }

        StateSaveCategory category = new StateSaveCategory { Name = "ButtonCategory" };
        StateSave enabled = new StateSave { Name = "Enabled", ParentContainer = button };
        SetFill(enabled, "Background", Teal, prefix: "");
        AddReference(enabled, "Background", "Color = Components/Styles.Primary.FillColor");
        StateSave disabled = new StateSave { Name = "Disabled", ParentContainer = button };
        SetFill(disabled, "Background", DarkGray, prefix: "");
        AddReference(disabled, "Background", "Color = Components/Styles.DarkGray.FillColor");
        category.States.Add(enabled);
        category.States.Add(disabled);
        button.Categories.Add(category);

        GumProjectSave project = BuildProject(BuildStyles(), button);
        project.Behaviors.Add(behavior);
        return button;
    }

    /// <summary>A Panel whose default Background width follows a reference and whose Large state sets 200.</summary>
    internal static ComponentSave BuildPanelWithSizeCategory()
    {
        ComponentSave panel = new ComponentSave { Name = "Panel", BaseType = "Container" };
        panel.Instances.Add(new InstanceSave { Name = "Background", BaseType = "Rectangle", ParentContainer = panel });
        panel.Instances.Add(new InstanceSave { Name = "Reference", BaseType = "Rectangle", ParentContainer = panel });

        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = panel };
        panel.States.Add(defaultState);
        defaultState.Variables.Add(new VariableSave { Name = "Reference.Width", Value = 50f, Type = "float", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "Background.Width", Value = 50f, Type = "float", SetsValue = true });
        AddReference(defaultState, "Background", "Width = Reference.Width");

        StateSaveCategory category = new StateSaveCategory { Name = "SizeCategory" };
        StateSave large = new StateSave { Name = "Large", ParentContainer = panel };
        large.Variables.Add(new VariableSave { Name = "Background.Width", Value = 200f, Type = "float", SetsValue = true });
        category.States.Add(large);
        panel.Categories.Add(category);

        BuildProject(panel);
        return panel;
    }


}
