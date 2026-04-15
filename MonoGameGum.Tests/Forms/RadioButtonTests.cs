using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using Gum.Forms;
using Gum.Forms.Controls;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class RadioButtonTests : BaseTestClass
{
    public RadioButtonTests()
    {
    }

    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    [Fact]
    public void ScreenLoad_ShouldForceEnabledOff_WhenInstanceStateSetToEnabledOn()
    {
        // Reproduces: a Gum project authored with a RadioButton instance whose
        // state is EnabledOn at the screen level. IsChecked defaults to false
        // at runtime, so the visual should be forced to EnabledOff.
        GumProjectSave gumProject = new GumProjectSave();

        ComponentSave radioComponent = new ComponentSave
        {
            Name = "RadioButton",
        };
        radioComponent.Behaviors.Add(new ElementBehaviorReference
        {
            BehaviorName = StandardFormsBehaviorNames.RadioButtonBehaviorName
        });

        StateSave radioDefault = new StateSave { Name = "Default" };
        radioDefault.Variables.Add(new VariableSave { Name = "Width", Value = 10f, SetsValue = true, Type = "float" });
        radioComponent.States.Add(radioDefault);

        StateSaveCategory radioCategory = new StateSaveCategory { Name = "RadioButtonCategory" };
        StateSave enabledOn = new StateSave { Name = "EnabledOn" };
        enabledOn.Variables.Add(new VariableSave { Name = "Width", Value = 100f, SetsValue = true, Type = "float" });
        radioCategory.States.Add(enabledOn);
        StateSave enabledOff = new StateSave { Name = "EnabledOff" };
        enabledOff.Variables.Add(new VariableSave { Name = "Width", Value = 50f, SetsValue = true, Type = "float" });
        radioCategory.States.Add(enabledOff);
        radioComponent.Categories.Add(radioCategory);

        gumProject.Components.Add(radioComponent);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        InstanceSave radioInstance = new InstanceSave
        {
            Name = "RadioInstance",
            BaseType = "RadioButton",
            ParentContainer = screen
        };
        screen.Instances.Add(radioInstance);

        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screenDefault.Variables.Add(new VariableSave
        {
            Name = "RadioInstance.RadioButtonCategoryState",
            Value = "EnabledOn",
            SetsValue = true,
            Type = "RadioButtonCategory"
        });
        screen.States.Add(screenDefault);

        gumProject.Screens.Add(screen);

        ObjectFinder.Self.GumProjectSave = gumProject;
        FormsUtilities.RegisterFromFileFormRuntimeDefaults();

        GraphicalUiElement screenGue = screen.ToGraphicalUiElement();

        InteractiveGue radioGue = (InteractiveGue)screenGue.Children.OfType<object>().First();
        RadioButton radioButton = radioGue.FormsControlAsObject as RadioButton;

        radioButton.ShouldNotBeNull();
        radioButton.IsChecked.ShouldBe(false);
        radioGue.Width.ShouldBe(50f, "because IsChecked is false, the visual should have EnabledOff applied");
    }

    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        RadioButton sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public async Task IsChecked_ShouldUncheckOtherRadioButtons()
    {
        var radioButton1 = new RadioButton();
        radioButton1.AddToRoot();

        var radioButton2 = new RadioButton();
        radioButton2.AddToRoot();

        radioButton1.IsChecked.ShouldBe(false);
        radioButton2.IsChecked.ShouldBe(false);


        radioButton1.IsChecked = true;
        radioButton1.IsChecked.ShouldBe(true);
        radioButton2.IsChecked.ShouldBe(false);

        radioButton2.IsChecked = true;
        radioButton1.IsChecked.ShouldBe(false, "because checking the 2nd should uncheck the first");
        radioButton2.IsChecked.ShouldBe(true);
    }
}
