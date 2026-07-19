using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.PropertyGridHelpers;
using Gum.Services.Dialogs;
using Gum.Undo;
using Moq;
using Shouldly;
using System.Collections.Generic;

namespace Gum.Presentation.Tests;
public class VariableInCategoryPropagationLogicTests : BaseTestClass
{
    private readonly Mock<IGuiCommands> _guiCommandsMock;
    private readonly VariableInCategoryPropagationLogic _variableInCategoryPropagationLogic;


    public VariableInCategoryPropagationLogicTests()
    {
        _guiCommandsMock = new Mock<IGuiCommands>();
        _variableInCategoryPropagationLogic = new VariableInCategoryPropagationLogic(
            new Mock<IUndoManager>().Object,
            _guiCommandsMock.Object,
            new Mock<IFileCommands>().Object,
            new Mock<IDialogService>().Object,
            new Mock<IPluginManager>().Object);

        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave.StandardElements.Add(new StandardElementSave()
        {
            Name = "Sprite",
            States = new List<StateSave>()
            {
                new StateSave()
                {
                    Name = "Default",
                    Variables = new List<VariableSave>()
                    {
                        new VariableSave()
                        {
                            Name = "X",
                            Type = "float",
                            Value = 0.0f
                        }
                    }
                }
            }
        });
    }

    [Fact]
    public void PropagateVariablesInCategory_ShouldAssignValue_IfDefaultStateHasNoVariable()
    {
        var element = new ComponentSave()
        {

        };

        var defaultState = new StateSave
        {
            Name = "Default",
            ParentContainer = element
        };

        element.States.Add(defaultState);

        element.Instances.Add(new InstanceSave()
        {
            Name = "Instance1",
            BaseType = "Sprite"
        });

        element.Categories.Add(new StateSaveCategory()
        {
            Name = "MyCategory"
        });

        element.Categories[0].States.Add(new StateSave()
        {
            Name = "First"
        });
        
        element.Categories[0].States.Add(new StateSave()
        {
            Name = "Second"
        });

        _variableInCategoryPropagationLogic.PropagateVariablesInCategory(
            "Instance1.X",
            element,
            element.Categories[0]);

        element.Categories[0].States[0].GetVariableSave("Instance1.X")!.Value.ShouldBe(0);
        element.Categories[0].States[1].GetVariableSave("Instance1.X")!.Value.ShouldBe(0);

    }

    [Fact]
    public void PropagateVariablesInCategory_ShouldAssignValue_IfDefaultStateHasVariableWithNull()
    {
        var element = new ComponentSave()
        {

        };

        var defaultState = new StateSave
        {
            Name = "Default",
            ParentContainer = element
        };

        defaultState.Variables.Add(new VariableSave()
        {
            Name = "Instance1.X",
            Type = "float",
            Value = null
        });

        element.States.Add(defaultState);

        element.Instances.Add(new InstanceSave()
        {
            Name = "Instance1",
            BaseType = "Sprite"
        });

        element.Categories.Add(new StateSaveCategory()
        {
            Name = "MyCategory"
        });

        element.Categories[0].States.Add(new StateSave()
        {
            Name = "First"
        });

        element.Categories[0].States.Add(new StateSave()
        {
            Name = "Second"
        });

        _variableInCategoryPropagationLogic.PropagateVariablesInCategory(
            "Instance1.X",
            element,
            element.Categories[0]);

        element.Categories[0].States[0].GetVariableSave("Instance1.X")!.Value.ShouldBe(0);
        element.Categories[0].States[1].GetVariableSave("Instance1.X")!.Value.ShouldBe(0);

    }
}
