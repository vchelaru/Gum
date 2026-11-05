using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using GumRuntime;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// A component that derives from a Forms control follows the control's behaviors (the runtime applies the
/// FormsProperty declarations of the whole base chain), so the tool-only references of an inherited
/// behavior drive its design-time preview too.
/// </summary>
public class BehaviorToolOnlyReferencesApplierInheritanceTests : BaseTestClass
{
    private readonly ComponentSave _control;
    private readonly ComponentSave _derived;
    private readonly ScreenSave _screen;
    private readonly StateSave _screenDefault;

    public BehaviorToolOnlyReferencesApplierInheritanceTests()
    {
        GumExpressionService.Initialize();

        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        behavior.ToolOnlyVariableReferences.Add("ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"");

        _control = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        _control.States.Add(new StateSave { Name = "Default", ParentContainer = _control });
        _control.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });

        _derived = new ComponentSave { Name = "Controls/FancyButton", BaseType = _control.Name };
        _derived.States.Add(new StateSave { Name = "Default", ParentContainer = _derived });

        _screen = new ScreenSave { Name = "TestScreen" };
        _screenDefault = new StateSave { Name = "Default", ParentContainer = _screen };
        _screen.States.Add(_screenDefault);

        StandardElementSave containerStandard = new StandardElementSave { Name = "Container" };
        containerStandard.States.Add(new StateSave { Name = "Default", ParentContainer = containerStandard });

        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(containerStandard);
        project.Components.Add(_control);
        project.Components.Add(_derived);
        project.Screens.Add(_screen);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.VariableChangedThroughReference = null;
        base.Dispose();
    }

    [Fact]
    public void Apply_InstanceOfTheControlItself_StillWritesTheCategoryState()
    {
        InstanceSave direct = new InstanceSave { Name = "DirectInstance", BaseType = _control.Name, ParentContainer = _screen };
        _screen.Instances.Add(direct);
        _screenDefault.Variables.Add(new VariableSave { Type = "bool", Name = "DirectInstance.IsEnabled", Value = false, SetsValue = true });

        BehaviorToolOnlyReferencesApplier.Apply(_screen, _screenDefault);

        _screenDefault.GetValue("DirectInstance.ButtonCategoryState").ShouldBe("Disabled");
    }
}
