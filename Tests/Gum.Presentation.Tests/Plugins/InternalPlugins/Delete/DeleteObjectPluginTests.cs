using System.Linq;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Gui.Plugins;
using Gum.Managers;
using Gum.Services.Dialogs;
using Moq;
using Shouldly;
using ToolsUtilities;

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

    [Fact]
    public void DeleteOptionsShow_NamesTheElementFileByItsExtension()
    {
        ComponentSave component = new ComponentSave { Name = "Button" };
        DeleteObjectPlugin plugin = GivenPluginWithFiles(
            (component, "C:/Project/Components/Button.gucj"));

        DeleteOptionsDialogViewModel dialog = new DeleteOptionsDialogViewModel();
        plugin.CallDeleteOptionsShow(dialog, new object[] { component });

        dialog.CheckBoxes.Single().Label.ShouldBe("Delete file (.gucj)");
    }

    [Fact]
    public void DeleteOptionsShow_OmitsTheExtension_WhenTheSelectionMixesFileTypes()
    {
        ComponentSave component = new ComponentSave { Name = "Button" };
        ScreenSave screen = new ScreenSave { Name = "MainMenu" };
        DeleteObjectPlugin plugin = GivenPluginWithFiles(
            (component, "C:/Project/Components/Button.gucj"),
            (screen, "C:/Project/Screens/MainMenu.gusj"));

        DeleteOptionsDialogViewModel dialog = new DeleteOptionsDialogViewModel();
        plugin.CallDeleteOptionsShow(dialog, new object[] { component, screen });

        dialog.CheckBoxes.Single().Label.ShouldBe("Delete file");
    }

    private static DeleteObjectPlugin GivenPluginWithFiles(params (ElementSave element, string file)[] files)
    {
        Mock<IFileCommands> fileCommands = new Mock<IFileCommands>();
        foreach ((ElementSave element, string file) in files)
        {
            fileCommands.Setup(item => item.GetFullPathXmlFile(element, element.Name))
                .Returns(new FilePath(file));
        }
        DeleteObjectPlugin plugin = new DeleteObjectPlugin(
            Mock.Of<IGuiCommands>(),
            fileCommands.Object,
            Mock.Of<IDeleteLogic>(),
            Mock.Of<IWireframeCommands>());
        plugin.StartUp();
        return plugin;
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
