using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Wireframe;
using GumRuntime;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>Editing a behavior reapplies its tool-only references to components derived from the control that has it (#7651).</summary>
public class VariableReferenceLogicDerivedBehaviorTests : BaseTestClass
{
    public VariableReferenceLogicDerivedBehaviorTests()
    {
        GumExpressionService.Initialize();
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.VariableChangedThroughReference = null;
        base.Dispose();
    }

    [Fact]
    public void ReapplyToolOnlyReferences_ABehaviorEdit_ReachesAnInstanceOfAComponentDerivedFromTheControl()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        behavior.ToolOnlyVariableReferences.Add("ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"");

        ComponentSave control = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        control.States.Add(new StateSave { Name = "Default", ParentContainer = control });
        control.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });
        ComponentSave derived = new ComponentSave { Name = "Controls/FancyButton", BaseType = control.Name };
        derived.States.Add(new StateSave { Name = "Default", ParentContainer = derived });

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        screen.Instances.Add(new InstanceSave { Name = "FancyInstance", BaseType = derived.Name, ParentContainer = screen });
        screenDefault.Variables.Add(new VariableSave { Type = "bool", Name = "FancyInstance.IsEnabled", Value = false, SetsValue = true });

        StandardElementSave containerStandard = new StandardElementSave { Name = "Container" };
        containerStandard.States.Add(new StateSave { Name = "Default", ParentContainer = containerStandard });
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(containerStandard);
        project.Components.Add(control);
        project.Components.Add(derived);
        project.Screens.Add(screen);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        VariableReferenceLogic sut = new VariableReferenceLogic(
            new Mock<IGuiCommands>().Object,
            new Mock<IWireframeCommands>().Object,
            new Mock<IDialogService>().Object,
            new Mock<IFileCommands>().Object,
            new Mock<ICompositeMemberRegistry>().Object,
            new Mock<IDispatcher>().Object,
            new Mock<IWireframeObjectManager>().Object);

        sut.ReapplyToolOnlyReferences(behavior, previous: null, trySave: false);

        screenDefault.GetValue("FancyInstance.ButtonCategoryState").ShouldBe("Disabled");
    }
}
