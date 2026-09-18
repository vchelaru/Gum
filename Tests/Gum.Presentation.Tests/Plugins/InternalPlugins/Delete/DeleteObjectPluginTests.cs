using System.Linq;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Gui.Plugins;
using Gum.Managers;
using Gum.Services.Dialogs;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// The delete confirmation's "Delete children?" choice should remember what the user picked last
/// time, so repeated deletes of the same kind (e.g. always "delete parent and children") can be
/// confirmed with Enter instead of re-selecting the option every time.
/// </summary>
public class DeleteObjectPluginTests : BaseTestClass
{
    private readonly DeleteObjectPlugin _plugin;

    public DeleteObjectPluginTests()
    {
        _plugin = new DeleteObjectPlugin(
            Mock.Of<IGuiCommands>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<IDeleteLogic>(),
            Mock.Of<IWireframeCommands>());
        _plugin.StartUp();
    }

    [Fact]
    public void DeleteOptionsShow_DefaultsToDeleteOnlyParents_WhenNothingWasChosenBefore()
    {
        object[] instances = GivenInstanceWithChild();

        DeleteOptionsDialogViewModel dialog = new DeleteOptionsDialogViewModel();
        _plugin.CallDeleteOptionsShow(dialog, instances);

        DeleteOptionCheckboxViewModel[] options = dialog.Choices.Single().Options.ToArray();
        options[0].IsChecked.ShouldBeTrue();
        options[1].IsChecked.ShouldBeFalse();
    }

    [Fact]
    public void DeleteOptionsShow_PreselectsDeleteChildren_AfterTheUserPreviouslyChoseIt()
    {
        object[] instances = GivenInstanceWithChild();

        DeleteOptionsDialogViewModel first = new DeleteOptionsDialogViewModel();
        _plugin.CallDeleteOptionsShow(first, instances);
        DeleteOptionCheckboxViewModel[] firstOptions = first.Choices.Single().Options.ToArray();
        firstOptions[0].IsChecked = false;
        firstOptions[1].IsChecked = true;
        _plugin.CallDeleteOptionsConfirmed(first, instances);

        DeleteOptionsDialogViewModel second = new DeleteOptionsDialogViewModel();
        _plugin.CallDeleteOptionsShow(second, instances);

        DeleteOptionCheckboxViewModel[] secondOptions = second.Choices.Single().Options.ToArray();
        secondOptions[0].IsChecked.ShouldBeFalse();
        secondOptions[1].IsChecked.ShouldBeTrue();
    }

    private static object[] GivenInstanceWithChild()
    {
        ComponentSave element = new ComponentSave();
        element.States.Add(new StateSave());

        InstanceSave parent = new InstanceSave { Name = "Parent1", ParentContainer = element };
        InstanceSave child = new InstanceSave { Name = "Child1", ParentContainer = element };
        element.Instances.Add(parent);
        element.Instances.Add(child);

        element.States[0].Variables.Add(new VariableSave { Name = "Child1.Parent", Value = "Parent1" });

        return new object[] { parent };
    }
}
