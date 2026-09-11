using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AvaloniaDataUi;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Themes;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Plugins.VariableGrid;
using Gum.ToolStates;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;
using DrawingColor = System.Drawing.Color;
using HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment;

namespace Gum.Avalonia.Tests;

/// <summary>A member of each type the Gum-specific Variables tab editors handle.</summary>
public class GumEditorFixture
{
    public DrawingColor Color { get; set; } = DrawingColor.FromArgb(128, 10, 20, 30);
    public CornerRadiusComposite CornerRadius { get; set; } = new CornerRadiusComposite(4, null, null, null, null);
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

    public InstanceMember Member(string propertyName) => new InstanceMember(propertyName, this);
}

/// <summary>
/// The Avalonia Variables tab: the head composes its view and editors, the shared plugins load, the
/// tab binds to the shared view model, the Gum editors write through, and the shared
/// <see cref="PropertyGridManager"/> fills the head's grid for a real selection.
/// </summary>
public class VariablesTabTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    public void Head_RegistersAnEditorForEveryGumDisplayerKey()
    {
        AvaloniaVariableGridHead head = (AvaloniaVariableGridHead)Services.GetRequiredService<IVariableGridHead>();

        foreach (Type key in typeof(GumDisplayers).GetNestedTypes())
        {
            head.Displayers.IsRegistered(key).ShouldBeTrue(key.Name);
            typeof(Control).IsAssignableFrom(head.Displayers.ResolveControlType(key)).ShouldBeTrue(key.Name);
        }
    }

    [AvaloniaFact]
    public void PluginManager_LoadsTheSharedVariableGridPlugins()
    {
        PluginManager pluginManager = Services.GetRequiredService<PluginManager>();
        if (!pluginManager.IsInitialized)
        {
            pluginManager.Initialize();
        }

        Type[] loaded = pluginManager.Plugins.Select(plugin => plugin.GetType()).ToArray();

        loaded.ShouldContain(typeof(MainVariableGridPlugin));
        loaded.ShouldContain(typeof(ExclusionsPlugin));
    }

    [AvaloniaFact]
    public void ChromeIcons_ResolveEveryToggleOptionsIcon()
    {
        VariableGridToggleOptions options = Services.GetRequiredService<VariableGridToggleOptions>();
        ToggleButtonOption[][] sets =
        {
            options.XUnits, options.YUnits, options.XOrigin, options.AllYOrigins, options.AllWidthUnits,
            options.AllHeightUnits, options.ChildrenLayout, options.TextOverflowHorizontalMode, options.TextOverflowVerticalMode,
        };

        foreach (ToggleButtonOption option in sets.SelectMany(set => set))
        {
            GumIcon.Create(option.GumIconName!).ShouldNotBeNull(option.GumIconName);
        }
        GumIcon.Create("NoSuchIcon").ShouldBeNull();
    }

    [AvaloniaFact]
    public void TabView_BindsFilterAndRaisesItsEvents()
    {
        MainControlViewModel viewModel = new MainControlViewModel(
            Services.GetRequiredService<IDeleteVariableService>(),
            Services.GetRequiredService<Gum.Services.IEditVariableService>());
        viewModel.ShowVariableGrid = true;
        VariablesTabView view = new VariablesTabView(DataUiGrid.CreateStandardRegistry()) { DataContext = viewModel };
        Window window = new Window { Content = view, Width = 400, Height = 600 };
        window.Show();
        int addClicks = 0;
        view.AddVariableClicked += (_, _) => addClicks++;

        view.FilterTextBox.Text = "width";
        viewModel.VariableFilterText.ShouldBe("width");

        view.FilterTextBox.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
        viewModel.VariableFilterText.ShouldBe(string.Empty);

        Button addVariable = view.AddVariableButton;
        addVariable.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        addClicks.ShouldBe(1);
        window.Close();
    }

    [AvaloniaFact]
    public void ToggleDisplay_PressingAnOptionWritesItsValue()
    {
        GumEditorFixture fixture = new GumEditorFixture();
        TextHorizontalAlignmentDisplay display = new TextHorizontalAlignmentDisplay { InstanceMember = fixture.Member(nameof(GumEditorFixture.Alignment)) };
        VariableGridToggleOptions options = Services.GetRequiredService<VariableGridToggleOptions>();

        display.Buttons.Count.ShouldBe(options.TextHorizontalAlignment.Length);
        display.Press(options.TextHorizontalAlignment.First(option => option.Value.Equals(HorizontalAlignment.Right)));

        fixture.Alignment.ShouldBe(HorizontalAlignment.Right);
    }

    [AvaloniaFact]
    public void ColorDisplay_HexAndSlidersWriteTheColor_KeepingAlpha()
    {
        GumEditorFixture fixture = new GumEditorFixture();
        ColorDisplay display = new ColorDisplay { InstanceMember = fixture.Member(nameof(GumEditorFixture.Color)) };
        display.HexTextBox.Text.ShouldBe("0A141E");

        display.HexTextBox.Text = "FF8000";
        display.CommitHexText();
        fixture.Color.ShouldBe(DrawingColor.FromArgb(128, 255, 128, 0));

        display.HandleSliderMoved(2, 64);
        display.CommitPendingFull();
        fixture.Color.ShouldBe(DrawingColor.FromArgb(128, 255, 128, 64));
        display.HexTextBox.Text.ShouldBe("FF8040");

        display.HexTextBox.Text = "nonsense";
        display.CommitHexText();
        fixture.Color.ShouldBe(DrawingColor.FromArgb(128, 255, 128, 64));
        display.HexTextBox.Text.ShouldBe("FF8040");
    }

    [AvaloniaFact]
    public void CornerRadiusDisplay_UnlinkingSeedsTheCorners_AndCornerEditsWriteThrough()
    {
        GumEditorFixture fixture = new GumEditorFixture();
        CornerRadiusDisplay display = new CornerRadiusDisplay { InstanceMember = fixture.Member(nameof(GumEditorFixture.CornerRadius)) };
        display.IsLinked.ShouldBeTrue();
        display.UniformTextBox.Text.ShouldBe("4");

        display.ToggleLinked();
        fixture.CornerRadius.ShouldBe(new CornerRadiusComposite(4, 4, 4, 4, 4));

        display.CornerTextBoxes[3].Text = "9";
        display.Commit();
        fixture.CornerRadius.ShouldBe(new CornerRadiusComposite(4, 4, 4, 4, 9));
    }

    [AvaloniaFact]
    public void RemoveButton_WritesThroughTheMembersSetter()
    {
        bool removed = false;
        InstanceMember member = new InstanceMember("Removable", null!);
        member.CustomGetTypeEvent += _ => typeof(string);
        // As in the tool, the member reads back a value, so the button's null write is a change.
        member.CustomGetEvent += _ => "Removable";
        member.CustomSetPropertyEvent += (_, _) => removed = true;
        VariableRemoveButton display = new VariableRemoveButton { InstanceMember = member };

        display.Remove();

        removed.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void PropertyGridManager_FillsTheHeadsGridWithGumEditors_AndFiltersIt()
    {
        // Startup order: plugins first, since the tool's standard-state refresh goes through them.
        PluginManager pluginManager = Services.GetRequiredService<PluginManager>();
        if (!pluginManager.IsInitialized)
        {
            pluginManager.Initialize();
        }
        Services.GetRequiredService<Gum.Reflection.ITypeManager>().Initialize();
        StandardElementsManager.Self.Initialize();
        Services.GetRequiredService<IStandardElementsManagerGumTool>().Initialize();
        AvaloniaTabManager tabManager = (AvaloniaTabManager)Services.GetRequiredService<ITabManager>();
        ISelectedState selectedState = Services.GetRequiredService<ISelectedState>();
        // A fresh manager so this test owns its tab; the head's singleton is left alone.
        PropertyGridManager sut = ActivatorUtilities.CreateInstance<PropertyGridManager>(Services);
        sut.InitializeEarly();
        AvaloniaPluginTab tab = tabManager.CenterBottom.Last(candidate => candidate.Title == "Variables");
        VariablesTabView view = (VariablesTabView)tab.Content;
        Window window = new Window { Content = view, Width = 500, Height = 1400 };
        window.Show();

        // The in-memory new project the tool starts from, with its default standard elements.
        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        projectManager.CreateNewProject();
        StandardElementSave container = projectManager.GumProjectSave!.StandardElements.First(element => element.Name == "Container");
        try
        {
            selectedState.SelectedElement = container;
            sut.RefreshEntireGrid(force: true);
            window.UpdateLayout();

            DataUiGrid grid = (DataUiGrid)view.VariablesGrid;
            Dictionary<string, Type> editorByMember = grid.LiveContainers
                .Where(row => row.Member != null && row.Displayer != null)
                .GroupBy(row => row.Member!.Name)
                .ToDictionary(group => group.Key, group => group.First().Displayer!.GetType());
            editorByMember["XUnits"].ShouldBe(typeof(XUnitsDisplay));
            editorByMember["WidthUnits"].ShouldBe(typeof(WidthUnitsDisplay));
            editorByMember["ChildrenLayout"].ShouldBe(typeof(ChildrenLayoutDisplay));

            sut.VariableViewModel.VariableFilterText = "Units";
            // The filter narrows each category's members to the matches.
            List<string> shown = grid.Categories.SelectMany(category => category.Members).Select(member => member.Name).ToList();
            shown.ShouldContain("XUnits");
            shown.ShouldAllBe(name => name.Contains("Units"));
        }
        finally
        {
            selectedState.SelectedElement = null;
            ObjectFinder.Self.GumProjectSave = null;
            window.Close();
            tabManager.RemoveTab(tab);
        }
    }
}
