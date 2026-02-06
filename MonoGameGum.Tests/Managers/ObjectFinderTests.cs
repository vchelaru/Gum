using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Managers;
public class ObjectFinderTests : BaseTestClass
{
    // ObjectFinder is used as a singleton in a variety of places in both FRB
    // and Gum. This isn't great, but this is a delicate refactor requiring testing all Gum 
    // runtimes including the one-off FlatRedBall implementation. Until that can be carefully
    // refactored, this must stay as a proper singleton. This test enforces that. Do not change
    // this from self access, and do not explicitly instantiate a ObjectFinder! Doing
    // so will no longer reflect how runtimes interact with ObjectFinder.
    ObjectFinder _objectFinder => ObjectFinder.Self;

    [Fact]
    public void GetScreen_ShouldReturnScreen()
    {
        GumProjectSave project = new ();
        project.Screens.Add(new ScreenSave
        {
            Name = "Screen1"
        });
        project.Screens.Add(new ScreenSave
        {
            Name = "Screen2"
        });


        _objectFinder.GumProjectSave = project;

        _objectFinder.GetScreen("Screen1").ShouldNotBeNull();
        _objectFinder.GetScreen("Screen2").ShouldNotBeNull();
        _objectFinder.GetScreen("Screen3").ShouldBeNull();
    }

    [Fact]
    public void IsVariableOrphaned_ShouldReturnFalse_ForVariablesInBaseTypes()
    {
        GumProjectSave project = new();
        _objectFinder.GumProjectSave = project;

        ComponentSave labelComponent  = new ComponentSave
        {
            Name = "LabelComponent",
            BaseType = "Text"
        };
        project.Components.Add(labelComponent);

        labelComponent.States.Add(new Gum.DataTypes.Variables.StateSave
        {
            ParentContainer = labelComponent
        });

        StandardElementSave textElement = new()
        {
            Name = "Text"
        };
        project.StandardElements.Add(textElement);
        textElement.States.Add(new StateSave
        {
            ParentContainer = textElement
        });
        textElement.DefaultState.Variables.Add(new VariableSave
        {
            Name = "Text",
            Type = "string",
            Value = "Hello"
        });


        VariableSave variableSave = new VariableSave
        {
            Name = "Text",
            Type = "string",
            Value = "Hello"
        };

        _objectFinder.IsVariableOrphaned(variableSave, labelComponent.DefaultState).ShouldBeFalse();
    }

    [Fact]
    public void IsVariableOrphaned_ForInstance_ShouldReturnFalse_ForVariablesInBaseTypes()
    {
        GumProjectSave project = new();
        _objectFinder.GumProjectSave = project;
        ComponentSave labelComponent = new ComponentSave
        {
            Name = "Label",
            BaseType = "Text"
        };
        project.Components.Add(labelComponent);
        labelComponent.States.Add(new Gum.DataTypes.Variables.StateSave
        {
            ParentContainer = labelComponent
        });
        StandardElementSave textElement = new()
        {
            Name = "Text"
        };
        project.StandardElements.Add(textElement);
        textElement.States.Add(new StateSave
        {
            ParentContainer = textElement
        });
        textElement.DefaultState.Variables.Add(new VariableSave
        {
            Name = "Text",
            Type = "string",
            Value = "Hello"
        });

        ScreenSave screen = new ScreenSave
        {
            Name = "Screen1"
        };
        screen.States.Add(new StateSave
        {
            ParentContainer = screen
        });
        project.Screens.Add(screen);

        InstanceSave instanceSave = new InstanceSave
        {
            Name = "Instance1",
            BaseType = "Label"
        };
        screen.Instances.Add(instanceSave);

        VariableSave variableSave = new VariableSave
        {
            Name = "Instance1.Text",
            Type = "string",
            Value = "Hello"
        };
        _objectFinder.IsVariableOrphaned(variableSave, screen.DefaultState).ShouldBeFalse();
    }

}
