using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public void IsChecked_ShouldUncheckSiblingsWithSameGroupName()
    {
        ContainerRuntime parent = new();
        RadioButton first = new("Group");
        RadioButton second = new("Group");
        parent.AddChild(first.Visual);
        parent.AddChild(second.Visual);

        first.IsChecked = true;
        second.IsChecked = true;

        first.IsChecked.ShouldBe(false);
        second.IsChecked.ShouldBe(true);
    }

    [Fact]
    public void IsChecked_ShouldNotUncheckSiblingsWithDifferentGroupName()
    {
        ContainerRuntime parent = new();
        RadioButton groupA = new("A");
        RadioButton groupB = new("B");
        parent.AddChild(groupA.Visual);
        parent.AddChild(groupB.Visual);

        groupA.IsChecked = true;
        groupB.IsChecked = true;

        groupA.IsChecked.ShouldBe(true);
        groupB.IsChecked.ShouldBe(true);
    }

    [Fact]
    public void IsChecked_ShouldGroupSiblings_WhenNoGroupNameIsSet()
    {
        ContainerRuntime parent = new();
        RadioButton first = new();
        RadioButton second = new();
        parent.AddChild(first.Visual);
        parent.AddChild(second.Visual);

        first.IsChecked = true;
        second.IsChecked = true;

        first.IsChecked.ShouldBe(false);
    }

    [Fact]
    public void IsChecked_ShouldNotUncheckRadioButtonsUnderOtherParents()
    {
        ContainerRuntime firstParent = new();
        ContainerRuntime secondParent = new();
        RadioButton inFirst = new("Group");
        RadioButton inSecond = new("Group");
        firstParent.AddChild(inFirst.Visual);
        secondParent.AddChild(inSecond.Visual);

        inFirst.IsChecked = true;
        inSecond.IsChecked = true;

        inFirst.IsChecked.ShouldBe(true, "because the same group name under a different parent is a separate group");
    }

    [Fact]
    public void IsChecked_ShouldNotUncheckRadioButtonsNestedInAChildContainer()
    {
        ContainerRuntime parent = new();
        ContainerRuntime nestedContainer = new();
        RadioButton direct = new("Group");
        RadioButton nested = new("Group");
        parent.AddChild(direct.Visual);
        parent.AddChild(nestedContainer);
        nestedContainer.AddChild(nested.Visual);

        direct.IsChecked = true;
        nested.IsChecked = true;

        direct.IsChecked.ShouldBe(true, "because groups are scoped to the direct parent");
    }

    [Fact]
    public void IsChecked_ShouldLeaveNonRadioSiblingsAlone()
    {
        ContainerRuntime parent = new();
        CheckBox checkBox = new();
        RadioButton radioButton = new("Group");
        parent.AddChild(checkBox.Visual);
        parent.AddChild(radioButton.Visual);
        checkBox.IsChecked = true;

        radioButton.IsChecked = true;

        checkBox.IsChecked.ShouldBe(true);
    }

    [Fact]
    public void IsChecked_ShouldNotUncheckOthers_WhenRadioButtonsHaveNoParent()
    {
        RadioButton first = new("Group");
        RadioButton second = new("Group");

        first.IsChecked = true;
        second.IsChecked = true;

        first.IsChecked.ShouldBe(true, "because groups need a parent visual to scope them");
        second.IsChecked.ShouldBe(true);
    }

    [Fact]
    public void GroupName_ShouldMoveRadioButtonToNewGroup()
    {
        ContainerRuntime parent = new();
        RadioButton moving = new("A");
        RadioButton staying = new("A");
        parent.AddChild(moving.Visual);
        parent.AddChild(staying.Visual);

        moving.GroupName = "B";
        moving.IsChecked = true;
        staying.IsChecked = true;

        moving.IsChecked.ShouldBe(true, "because it no longer shares a group with the button left in A");
    }

    [Fact]
    public void GroupName_ShouldJoinExistingGroup_WhenChangedToItsName()
    {
        ContainerRuntime parent = new();
        RadioButton existing = new("B");
        RadioButton joining = new("A");
        parent.AddChild(existing.Visual);
        parent.AddChild(joining.Visual);
        existing.IsChecked = true;

        joining.GroupName = "B";
        joining.IsChecked = true;

        existing.IsChecked.ShouldBe(false);
    }

    [Fact]
    public void ParentChanged_ShouldMoveRadioButtonToGroupOfNewParent()
    {
        ContainerRuntime oldParent = new();
        ContainerRuntime newParent = new();
        RadioButton moving = new("Group");
        RadioButton oldSibling = new("Group");
        RadioButton newSibling = new("Group");
        oldParent.AddChild(moving.Visual);
        oldParent.AddChild(oldSibling.Visual);
        newParent.AddChild(newSibling.Visual);

        newParent.AddChild(moving.Visual);
        oldSibling.IsChecked = true;
        newSibling.IsChecked = true;
        moving.IsChecked = true;

        oldSibling.IsChecked.ShouldBe(true, "because the moved button left the old group");
        newSibling.IsChecked.ShouldBe(false, "because the moved button joined the new group");
    }

    [Fact]
    public void ParentChanged_ShouldLeaveGroup_WhenRadioButtonIsDetached()
    {
        ContainerRuntime parent = new();
        RadioButton detached = new("Group");
        RadioButton staying = new("Group");
        parent.AddChild(detached.Visual);
        parent.AddChild(staying.Visual);

        detached.Visual.Parent = null;
        staying.IsChecked = true;
        detached.IsChecked = true;

        staying.IsChecked.ShouldBe(true);
    }

    [Fact]
    public void Visual_ShouldLeaveGroup_WhenSetToNull()
    {
        ContainerRuntime parent = new();
        RadioButton removed = new("Group");
        RadioButton staying = new("Group");
        parent.AddChild(removed.Visual);
        parent.AddChild(staying.Visual);

        removed.Visual = null;
        staying.IsChecked = true;
        removed.IsChecked = true;

        staying.IsChecked.ShouldBe(true);
    }

    [Fact]
    public void Click_ShouldUncheckSiblingsWithSameGroupName()
    {
        ContainerRuntime parent = new();
        RadioButton first = new("Group");
        RadioButton second = new("Group");
        parent.AddChild(first.Visual);
        parent.AddChild(second.Visual);
        first.IsChecked = true;

        second.Visual.CallClick();

        first.IsChecked.ShouldBe(false);
        second.IsChecked.ShouldBe(true);
    }

    [Fact]
    public void RemovedScreen_ShouldNotBeKeptAlive_ByRadioButtonGroups()
    {
        WeakReference screenReference = BuildAndDiscardScreenWithRadioButtons();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        screenReference.IsAlive.ShouldBeFalse(
            "because radio button groups must not keep a removed screen's visual tree reachable");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference BuildAndDiscardScreenWithRadioButtons()
    {
        ContainerRuntime screen = new();
        ContainerRuntime panel = new();
        screen.AddChild(panel);
        RadioButton first = new("Group");
        RadioButton second = new("Group");
        panel.AddChild(first.Visual);
        panel.AddChild(second.Visual);
        first.IsChecked = true;
        second.IsChecked = true;

        return new WeakReference(panel);
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
