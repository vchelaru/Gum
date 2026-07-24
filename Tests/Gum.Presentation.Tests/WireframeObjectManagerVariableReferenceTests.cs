using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolStates;
using Gum.Wireframe;
using GumRuntime;
using Moq;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

public class WireframeObjectManagerVariableReferenceTests : BaseTestClass
{
    /// <summary>
    /// Root-cause pin for #4015: a variable reference that reads an Absolute* property
    /// ("RectangleInstance1.X = RectangleInstance.AbsoluteRight") is resolved during wireframe
    /// construction, while WireframeObjectManager.RefreshAll has IsAllLayoutSuspended = true. Under
    /// suspension a Width/X setter updates the GUE's cached field but its UpdateLayout no-ops, so the
    /// underlying renderable geometry that AbsoluteRight reads stays stale (0). RefreshAll resumes
    /// layout and calls UpdateLayout afterward, but nothing re-resolved the reference against the
    /// now-correct geometry - so the sibling was left at the pre-layout value. This drives the real
    /// RefreshAll end-to-end and asserts the live position is correct once layout has caught up.
    /// </summary>
    [Fact]
    public void RefreshAll_AbsoluteReferenceResolvedDuringSuspendedConstruction_IsCorrectAfterLayoutResumes()
    {
        GumExpressionService.Initialize();

        ScreenSave screen = new ScreenSave { Name = "RectScreen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);

        InstanceSave rectInstance = new InstanceSave { Name = "RectangleInstance", BaseType = "Container", ParentContainer = screen };
        InstanceSave rectInstance1 = new InstanceSave { Name = "RectangleInstance1", BaseType = "Container", ParentContainer = screen };
        screen.Instances.Add(rectInstance);
        screen.Instances.Add(rectInstance1);

        StandardElementSave containerStandard = new StandardElementSave { Name = "Container" };
        containerStandard.States.Add(new StateSave { Name = "Default", ParentContainer = containerStandard });

        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(containerStandard);
        project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = project;

        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.Width", Value = 225f, Type = "float", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.X", Value = 394f, Type = "float", SetsValue = true });
        // Existing scalar the reference overwrites; also supplies the left-side type ("float") the cast needs.
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance1.X", Value = 0f, Type = "float", SetsValue = true });

        VariableListSave<string> refs = new VariableListSave<string> { Type = "string", Name = "RectangleInstance1.VariableReferences" };
        refs.Value.Add("X=RectangleInstance.AbsoluteRight");
        defaultState.VariableLists.Add(refs);

        GraphicalUiElement? rectGue1Live = null;

        Mock<IPluginManager> pluginManager = new Mock<IPluginManager>();
        // Runs inside RefreshAll, after it has set IsAllLayoutSuspended = true. Mirrors what real
        // construction (SetVariablesRecursively) does under that suspension: set instance geometry
        // (Width/X setters' UpdateLayout no-op while suspended, so the renderable AbsoluteRight reads
        // stays 0) and resolve the variable references against that stale geometry.
        pluginManager.Setup(x => x.CreateGraphicalUiElement(screen)).Returns(() =>
        {
            GraphicalUiElement root = new GraphicalUiElement(new InvisibleRenderable()) { Name = "RectScreen" };
            GraphicalUiElement rectGue = new GraphicalUiElement(new InvisibleRenderable()) { Name = "RectangleInstance", Tag = rectInstance, Parent = root };
            GraphicalUiElement rectGue1 = new GraphicalUiElement(new InvisibleRenderable()) { Name = "RectangleInstance1", Tag = rectInstance1, Parent = root };
            rectGue.X = 394f;
            rectGue.Width = 225f;
            root.ApplyVariableReferences(defaultState);
            rectGue1Live = rectGue1;
            return root;
        });

        Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
        selectedState.SetupGet(x => x.SelectedElements).Returns(new[] { screen });
        selectedState.SetupGet(x => x.SelectedElement).Returns(screen);
        selectedState.SetupGet(x => x.SelectedStateSave).Returns(defaultState);

        WireframeObjectManager wireframeObjectManager = new WireframeObjectManager(
            Mock.Of<IFontManager>(),
            selectedState.Object,
            Mock.Of<IDialogService>(),
            Mock.Of<IGuiCommands>(),
            new LocalizationService(),
            pluginManager.Object,
            Mock.Of<IProjectState>());

        wireframeObjectManager.RefreshAll(forceLayout: true);

        rectGue1Live.ShouldNotBeNull();
        // RectangleInstance.AbsoluteRight = X(394) + Width(225) = 619. Before the fix the reference
        // resolved against the pre-layout stale width (0), leaving RectangleInstance1 at 0.
        rectGue1Live!.X.ShouldBe(619f);
    }
}
