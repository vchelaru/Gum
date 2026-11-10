using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using GumRuntime;
using Gum.Commands;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

// FindAncestor<BehaviorName>() in a behavior tool-only reference line (#7827), as a TabControl tab uses it:
//   TabItemCategoryState = @FindAncestor<TabControlBehavior>().SelectedIndex == @Index ? "Selected" : "Unselected"
public class BehaviorFindAncestorReferenceTests : BaseTestClass
{
    private const string Line =
        "TabItemCategoryState = @FindAncestor<TabControlBehavior>().SelectedIndex == @Index ? \"Selected\" : \"Unselected\"";

    private readonly Func<string, ISystemManagers, IRenderable?>? _previousRenderableFunc;

    public BehaviorFindAncestorReferenceTests()
    {
        GumExpressionService.Initialize();
        _previousRenderableFunc = ElementSaveExtensions.CustomCreateGraphicalComponentFunc;
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = (name, _) =>
            ObjectFinder.Self.GumProjectSave?.StandardElements.Any(standard => standard.Name == name) == true
                ? new StateSelectionCanvasHarness.RecordingRenderable()
                : null;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = _previousRenderableFunc;
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.VariableChangedThroughReference = null;
        base.Dispose();
    }

    private GumProjectSave? _project;

    private ScreenSave Build(Action<ScreenSave, StateSave>? customize = null, int? selectedIndex = 1,
        string line = Line, Action<ComponentSave>? customizeTabControl = null, Action<ComponentSave>? customizeTabItem = null)
    {
        BehaviorSave tabControlBehavior = new BehaviorSave { Name = "TabControlBehavior" };
        tabControlBehavior.FormsProperties.Add(new VariableSave { Type = "int", Name = "SelectedIndex", Value = 0 });

        BehaviorSave tabItemBehavior = new BehaviorSave { Name = "TabItemBehavior" };
        tabItemBehavior.ToolOnlyVariableReferences.Add(line);

        ComponentSave tabControl = new ComponentSave { Name = "Controls/TabControl", BaseType = "Container" };
        tabControl.States.Add(new StateSave { Name = "Default", ParentContainer = tabControl });
        tabControl.Instances.Add(new InstanceSave { Name = "TabStripInstance", BaseType = "Container", ParentContainer = tabControl });
        tabControl.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "TabControlBehavior" });

        ComponentSave tabItem = new ComponentSave { Name = "Controls/TabItem", BaseType = "Container" };
        tabItem.States.Add(new StateSave { Name = "Default", ParentContainer = tabItem });
        tabItem.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "TabItemBehavior" });

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);

        screen.Instances.Add(new InstanceSave { Name = "TabControl1", BaseType = "Controls/TabControl", ParentContainer = screen });
        if (selectedIndex != null)
        {
            screenDefault.Variables.Add(new VariableSave
            {
                Type = "int", Name = "TabControl1.SelectedIndex", Value = selectedIndex, SetsValue = true
            });
        }

        for (int i = 0; i < 3; i++)
        {
            screen.Instances.Add(new InstanceSave { Name = "Tab" + i, BaseType = "Controls/TabItem", ParentContainer = screen });
            screenDefault.Variables.Add(new VariableSave
            {
                Type = "string", Name = $"Tab{i}.Parent", Value = "TabControl1.TabStripInstance", SetsValue = true
            });
        }

        customizeTabControl?.Invoke(tabControl);
        customizeTabItem?.Invoke(tabItem);
        customize?.Invoke(screen, screenDefault);

        StandardElementSave container = StateSelectionCanvasHarness.BuildStandard("Container");

        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(container);
        _project = project;
        project.Components.Add(tabControl);
        project.Components.Add(tabItem);
        project.Screens.Add(screen);
        project.Behaviors.Add(tabControlBehavior);
        project.Behaviors.Add(tabItemBehavior);
        ObjectFinder.Self.GumProjectSave = project;
        return screen;
    }

    private static string?[] States(StateSave state) =>
        Enumerable.Range(0, 3).Select(i => state.GetValue($"Tab{i}.TabItemCategoryState") as string).ToArray();

    [Fact]
    public void Apply_SelectedIndexAuthored_OnlyThatTabIsSelected()
    {
        ScreenSave screen = Build();
        BehaviorToolOnlyReferencesApplier.Apply(screen, screen.DefaultState);
        States(screen.DefaultState).ShouldBe(new string?[] { "Unselected", "Selected", "Unselected" });
    }

    [Fact]
    public void Apply_SelectedIndexNotAuthored_FirstTabIsSelected()
    {
        // The control's declared default is 0; nothing is authored on the screen.
        ScreenSave screen = Build(selectedIndex: null);
        BehaviorToolOnlyReferencesApplier.Apply(screen, screen.DefaultState);
        States(screen.DefaultState).ShouldBe(new string?[] { "Selected", "Unselected", "Unselected" });
    }

    [Fact]
    public void Apply_SelectedIndexOutOfRange_NoTabIsSelected()
    {
        ScreenSave screen = Build(selectedIndex: 7);
        BehaviorToolOnlyReferencesApplier.Apply(screen, screen.DefaultState);
        States(screen.DefaultState).ShouldBe(new string?[] { "Unselected", "Unselected", "Unselected" });
    }

    [Fact]
    public void Apply_TabsInsideIntermediateContainer_StillFindsTheControl()
    {
        ScreenSave screen = Build((s, state) =>
        {
            s.Instances.Add(new InstanceSave { Name = "Strip", BaseType = "Container", ParentContainer = s });
            state.Variables.Add(new VariableSave
            {
                Type = "string", Name = "Strip.Parent", Value = "TabControl1.TabStripInstance", SetsValue = true
            });
            for (int i = 0; i < 3; i++)
            {
                state.Variables.First(v => v.Name == $"Tab{i}.Parent").Value = "Strip";
            }
        });
        BehaviorToolOnlyReferencesApplier.Apply(screen, screen.DefaultState);
        States(screen.DefaultState).ShouldBe(new string?[] { "Unselected", "Selected", "Unselected" });
    }

    [Fact]
    public void Apply_TabWithNoTabControlAncestor_WritesNothing()
    {
        ScreenSave screen = Build((s, state) =>
        {
            state.Variables.First(v => v.Name == "Tab0.Parent").Value = "";
        });
        BehaviorToolOnlyReferencesApplier.Apply(screen, screen.DefaultState);
        States(screen.DefaultState)[0].ShouldBeNull();
    }

    [Fact]
    public void Apply_TwoTabControls_EachTabFollowsItsOwn()
    {
        ScreenSave screen = Build((s, state) =>
        {
            s.Instances.Add(new InstanceSave { Name = "TabControl2", BaseType = "Controls/TabControl", ParentContainer = s });
            state.Variables.Add(new VariableSave
            {
                Type = "int", Name = "TabControl2.SelectedIndex", Value = 0, SetsValue = true
            });
            s.Instances.Add(new InstanceSave { Name = "Other0", BaseType = "Controls/TabItem", ParentContainer = s });
            s.Instances.Add(new InstanceSave { Name = "Other1", BaseType = "Controls/TabItem", ParentContainer = s });
            state.Variables.Add(new VariableSave { Type = "string", Name = "Other0.Parent", Value = "TabControl2.TabStripInstance", SetsValue = true });
            state.Variables.Add(new VariableSave { Type = "string", Name = "Other1.Parent", Value = "TabControl2.TabStripInstance", SetsValue = true });
        });
        BehaviorToolOnlyReferencesApplier.Apply(screen, screen.DefaultState);
        screen.DefaultState.GetValue("Other0.TabItemCategoryState").ShouldBe("Selected");
        screen.DefaultState.GetValue("Other1.TabItemCategoryState").ShouldBe("Unselected");
        // Index counts within the instance's own Parent group, so TabControl1's tabs are unaffected.
        States(screen.DefaultState).ShouldBe(new string?[] { "Unselected", "Selected", "Unselected" });
    }

    [Theory]
    [InlineData("TabItemCategoryState = FindAncestor<TabControlBehavior>().SelectedIndex == @Index ? \"Selected\" : \"Unselected\"")]
    [InlineData("TabItemCategoryState = @FindAncestor < TabControlBehavior > ( ).SelectedIndex == @Index ? \"Selected\" : \"Unselected\"")]
    public void Apply_SpellingVariants_AllResolve(string line)
    {
        ScreenSave screen = Build(line: line);
        BehaviorToolOnlyReferencesApplier.Apply(screen, screen.DefaultState);
        States(screen.DefaultState).ShouldBe(new string?[] { "Unselected", "Selected", "Unselected" });
    }

    [Fact]
    public void Apply_CallTextInsideStringLiteral_IsNotRewritten()
    {
        // The selected tab (index 1) gets the literal's text back unchanged; the others get "Unselected".
        ScreenSave screen = Build(line:
            "TabItemCategoryState = @FindAncestor<TabControlBehavior>().SelectedIndex == @Index ? \"@FindAncestor<TabControlBehavior>()\" : \"Unselected\"");
        BehaviorToolOnlyReferencesApplier.Apply(screen, screen.DefaultState);
        States(screen.DefaultState).ShouldBe(new string?[] { "Unselected", "@FindAncestor<TabControlBehavior>()", "Unselected" });
    }

    [Fact]
    public void Apply_UnknownBehaviorName_WritesNothing()
    {
        ScreenSave screen = Build(line:
            "TabItemCategoryState = @FindAncestor<NoSuchBehavior>().SelectedIndex == @Index ? \"Selected\" : \"Unselected\"");
        BehaviorToolOnlyReferencesApplier.Apply(screen, screen.DefaultState);
        States(screen.DefaultState).ShouldBe(new string?[] { null, null, null });
    }

    [Fact]
    public void Apply_ControlInstanceNameIsNotAnIdentifier_StillResolves()
    {
        ScreenSave screen = Build((s, state) =>
        {
            s.Instances.First(i => i.Name == "TabControl1").Name = "tab-control";
            foreach (VariableSave variable in state.Variables)
            {
                variable.Name = variable.Name.Replace("TabControl1.", "tab-control.");
                if (variable.Value is string text)
                {
                    variable.Value = text.Replace("TabControl1.", "tab-control.");
                }
            }
        });
        BehaviorToolOnlyReferencesApplier.Apply(screen, screen.DefaultState);
        States(screen.DefaultState).ShouldBe(new string?[] { "Unselected", "Selected", "Unselected" });
    }

    [Fact]
    public void Apply_NestedTabControls_EachTabFollowsTheNearestControl()
    {
        ScreenSave screen = Build((s, state) =>
        {
            // Inner control sits in the outer control's strip; its tabs sit in the inner strip.
            s.Instances.Add(new InstanceSave { Name = "TabControl2", BaseType = "Controls/TabControl", ParentContainer = s });
            state.Variables.Add(new VariableSave { Type = "string", Name = "TabControl2.Parent", Value = "TabControl1.TabStripInstance", SetsValue = true });
            state.Variables.Add(new VariableSave { Type = "int", Name = "TabControl2.SelectedIndex", Value = 0, SetsValue = true });
            s.Instances.Add(new InstanceSave { Name = "InnerA", BaseType = "Controls/TabItem", ParentContainer = s });
            s.Instances.Add(new InstanceSave { Name = "InnerB", BaseType = "Controls/TabItem", ParentContainer = s });
            state.Variables.Add(new VariableSave { Type = "string", Name = "InnerA.Parent", Value = "TabControl2.TabStripInstance", SetsValue = true });
            state.Variables.Add(new VariableSave { Type = "string", Name = "InnerB.Parent", Value = "TabControl2.TabStripInstance", SetsValue = true });
        });
        BehaviorToolOnlyReferencesApplier.Apply(screen, screen.DefaultState);
        screen.DefaultState.GetValue("InnerA.TabItemCategoryState").ShouldBe("Selected");
        screen.DefaultState.GetValue("InnerB.TabItemCategoryState").ShouldBe("Unselected");
        States(screen.DefaultState).ShouldBe(new string?[] { "Unselected", "Selected", "Unselected" });
    }

    [Fact]
    public void Apply_ControlIsAComponentDerivedFromOneWithTheBehavior_StillResolves()
    {
        ScreenSave screen = Build();
        ComponentSave styled = new ComponentSave { Name = "Controls/StyledTabControl", BaseType = "Controls/TabControl" };
        styled.States.Add(new StateSave { Name = "Default", ParentContainer = styled });
        _project!.Components.Add(styled);
        screen.Instances.First(i => i.Name == "TabControl1").BaseType = "Controls/StyledTabControl";
        BehaviorToolOnlyReferencesApplier.Apply(screen, screen.DefaultState);
        States(screen.DefaultState).ShouldBe(new string?[] { "Unselected", "Selected", "Unselected" });
    }

    [Fact]
    public void Apply_ComponentDefaultSelectedIndexAndNothingAuthored_UsesTheComponentDefault()
    {
        ScreenSave screen = Build(selectedIndex: null, customizeTabControl: tabControl =>
            tabControl.DefaultState.Variables.Add(new VariableSave { Type = "int", Name = "SelectedIndex", Value = 2, SetsValue = true }));
        BehaviorToolOnlyReferencesApplier.Apply(screen, screen.DefaultState);
        States(screen.DefaultState).ShouldBe(new string?[] { "Unselected", "Unselected", "Selected" });
    }

    [Fact]
    public void Apply_AuthoredSelectedIndexDiffersFromComponentDefault_IsWrittenAsADeviation()
    {
        // Resting comes from the component default (2), not the behavior default (0), so authored 0 deviates
        // from resting and the line's result is written.
        ScreenSave screen = Build(selectedIndex: 0,
            line: "TabItemCategoryState = FindAncestor<TabControlBehavior>().SelectedIndex == 2 ? \"Selected\" : \"Unselected\"",
            customizeTabControl: tabControl =>
                tabControl.DefaultState.Variables.Add(new VariableSave { Type = "int", Name = "SelectedIndex", Value = 2, SetsValue = true }));
        BehaviorToolOnlyReferencesApplier.Apply(screen, screen.DefaultState);
        States(screen.DefaultState).ShouldBe(new string?[] { "Unselected", "Unselected", "Unselected" });
    }

    [Fact]
    public void Apply_ParentIsBareControlName_Resolves()
    {
        ScreenSave screen = Build((s, state) =>
        {
            for (int i = 0; i < 3; i++)
            {
                state.Variables.First(v => v.Name == $"Tab{i}.Parent").Value = "TabControl1";
            }
        }, selectedIndex: 0, customizeTabControl: tabControl =>
            tabControl.DefaultState.Variables.Add(new VariableSave { Type = "int", Name = "SelectedIndex", Value = 2, SetsValue = true }));
        BehaviorToolOnlyReferencesApplier.Apply(screen, screen.DefaultState);
        States(screen.DefaultState).ShouldBe(new string?[] { "Selected", "Unselected", "Unselected" });
    }

    [Fact]
    public void Apply_ControlDeclaredInBaseScreen_DerivedScreenTabFindsIt()
    {
        ScreenSave baseScreen = Build();
        ScreenSave derived = new ScreenSave { Name = "DerivedScreen", BaseType = "TestScreen" };
        StateSave derivedDefault = new StateSave { Name = "Default", ParentContainer = derived };
        derived.States.Add(derivedDefault);
        derived.Instances.Add(new InstanceSave { Name = "Tab3", BaseType = "Controls/TabItem", ParentContainer = derived });
        derivedDefault.Variables.Add(new VariableSave
        {
            Type = "string", Name = "Tab3.Parent", Value = "TabControl1.TabStripInstance", SetsValue = true
        });
        baseScreen.DefaultState.Variables.First(v => v.Name == "TabControl1.SelectedIndex").Value = 3;
        _project!.Screens.Add(derived);

        BehaviorToolOnlyReferencesApplier.Apply(derived, derivedDefault);

        // Tab0..Tab2 are the base screen's; Tab3 is the fourth tab under the base screen's control.
        derivedDefault.GetValue("Tab3.TabItemCategoryState").ShouldBe("Selected");
    }

    [Fact]
    public void Wireframe_SelectedTabShowsItsContentSlot_OthersHideTheirs()
    {
        // The tab component's own states set its own slot's Visible; nothing sets a child from outside.
        ScreenSave screen = Build(customizeTabItem: tabItem =>
        {
            tabItem.Instances.Add(new InstanceSave { Name = "ContentSlot", BaseType = "Container", ParentContainer = tabItem });
            StateSaveCategory category = new StateSaveCategory { Name = "TabItemCategory" };
            foreach ((string name, bool visible) in new[] { ("Selected", true), ("Unselected", false) })
            {
                StateSave state = new StateSave { Name = name, ParentContainer = tabItem };
                state.Variables.Add(new VariableSave { Type = "bool", Name = "ContentSlot.Visible", Value = visible, SetsValue = true });
                category.States.Add(state);
            }
            tabItem.Categories.Add(category);
        });
        BehaviorToolOnlyReferencesApplier.Apply(screen, screen.DefaultState);

        GraphicalUiElement root = StateSelectionCanvasHarness.Show(screen, screen.DefaultState);

        bool[] contentVisible = Enumerable.Range(0, 3)
            .Select(i => root.GetGraphicalUiElementByName($"Tab{i}")!.GetGraphicalUiElementByName("ContentSlot")!.Visible)
            .ToArray();
        contentVisible.ShouldBe(new[] { false, true, false });
    }

    private static VariableReferenceLogic CreateLogic()
    {
        Mock<ICompositeMemberRegistry> registry = new Mock<ICompositeMemberRegistry>();
        registry.Setup(x => x.Descriptors).Returns(new List<CompositeMemberDescriptor>());
        return new VariableReferenceLogic(
            new Mock<IGuiCommands>().Object, new Mock<IWireframeCommands>().Object, new Mock<IDialogService>().Object,
            new Mock<IFileCommands>().Object, registry.Object, new Mock<IDispatcher>().Object,
            new Mock<IWireframeObjectManager>().Object);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DoVariableReferenceReaction_SelectedIndexEdited_TabsFollowOnlyOnCommit(bool isFullCommit)
    {
        ScreenSave screen = Build();
        BehaviorToolOnlyReferencesApplier.Apply(screen, screen.DefaultState);
        screen.DefaultState.SetValue("TabControl1.SelectedIndex", 2, screen.Instances.First(i => i.Name == "TabControl1"));

        CreateLogic().DoVariableReferenceReaction(
            screen, screen.Instances.First(i => i.Name == "TabControl1"), "SelectedIndex",
            screen.DefaultState, "TabControl1.SelectedIndex", trySave: false, isFullCommit: isFullCommit);

        string?[] expected = isFullCommit
            ? new string?[] { "Unselected", "Unselected", "Selected" }
            : new string?[] { "Unselected", "Selected", "Unselected" }; // stale during a drag tick
        States(screen.DefaultState).ShouldBe(expected);
    }

    [Fact]
    public void ReapplySiblingDependentReferences_AfterReorderingTabs_UpdatesTabStates()
    {
        // SelectedIndex = 1. Moving Tab2 to the front makes it index 0, so Tab0 becomes index 1 (selected).
        ScreenSave screen = Build();
        BehaviorToolOnlyReferencesApplier.Apply(screen, screen.DefaultState);
        States(screen.DefaultState).ShouldBe(new string?[] { "Unselected", "Selected", "Unselected" });

        InstanceSave tab2 = screen.Instances.First(i => i.Name == "Tab2");
        screen.Instances.Remove(tab2);
        screen.Instances.Insert(1, tab2); // TabControl1, Tab2, Tab0, Tab1

        CreateLogic().ReapplySiblingDependentReferences(screen);

        // Tab0 is now at index 1 and must be the selected one.
        States(screen.DefaultState).ShouldBe(new string?[] { "Selected", "Unselected", "Unselected" });
    }
}
