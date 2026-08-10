using Gum.Commands;
using Gum.DataTypes;
using Gum.Gui.Plugins;
using Gum.Managers;
using Moq;
using Shouldly;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Tests for the file-reconciliation half of InstanceDeletionHelper. An element's XML file is user
/// data that Gum's undo cannot restore, so deleting an element must not destroy it outright.
/// </summary>
public class InstanceDeletionHelperTests : BaseTestClass
{
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly InstanceDeletionHelper _instanceDeletionHelper;
    private readonly string _projectDirectory;

    public InstanceDeletionHelperTests()
    {
        _projectDirectory = Path.Combine(Path.GetTempPath(), "GumInstanceDeletionHelperTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_projectDirectory);

        _instanceDeletionHelper = new InstanceDeletionHelper(
            Mock.Of<IDeleteLogic>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<IWireframeCommands>(),
            _fileCommands.Object);
    }

    public override void Dispose()
    {
        base.Dispose();
        if (Directory.Exists(_projectDirectory))
        {
            Directory.Delete(_projectDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Creates the element's XML file on disk and points IFileCommands at it, the way the tool does
    /// for a saved element.
    /// </summary>
    private ComponentSave GivenComponentWithXmlFileOnDisk(string name)
    {
        ComponentSave component = new ComponentSave { Name = name };

        string xmlPath = Path.Combine(_projectDirectory, name + ".gucx");
        File.WriteAllText(xmlPath, "<ComponentSave />");

        _fileCommands
            .Setup(x => x.GetFullPathXmlFile(component, name))
            .Returns(new FilePath(xmlPath));

        return component;
    }

    [Fact]
    public void TryRecycleXmlFileForObject_WhenTheFileCannotBeRemoved_ReturnsAnExplanatoryMessage()
    {
        ComponentSave component = GivenComponentWithXmlFileOnDisk("LockedComponent");
        _fileCommands
            .Setup(x => x.MoveToRecycleBin(It.IsAny<FilePath>()))
            .Throws(new IOException("The process cannot access the file because it is being used by another process."));

        GeneralResponse response = _instanceDeletionHelper.TryRecycleXmlFileForObject(component);

        response.Succeeded.ShouldBeFalse();
        response.Message.ShouldContain("LockedComponent.gucx");
        response.Message.ShouldContain("read-only");
    }

    [Fact]
    public void TryRecycleXmlFileForObject_WhenTheFileExists_MovesItToTheRecycleBin()
    {
        ComponentSave component = GivenComponentWithXmlFileOnDisk("MyComponent");

        GeneralResponse response = _instanceDeletionHelper.TryRecycleXmlFileForObject(component);

        response.Succeeded.ShouldBeTrue();
        _fileCommands.Verify(
            x => x.MoveToRecycleBin(new FilePath(Path.Combine(_projectDirectory, "MyComponent.gucx"))),
            Times.Once);
    }
}
