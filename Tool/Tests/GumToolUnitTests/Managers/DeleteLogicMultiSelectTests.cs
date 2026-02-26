using System;
using System.IO;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace GumToolUnitTests.Managers;

public class DeleteLogicMultiSelectTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly IDeleteLogic _deleteLogic;
    private readonly Mock<ProjectCommands> _projectCommands;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<PluginManager> _pluginManager;
    private readonly Mock<IWireframeObjectManager> _wireframeObjectManager;
    private readonly Mock<IProjectManager> _projectManager;

    public DeleteLogicMultiSelectTests()
    {
        _mocker = new AutoMocker();
        _projectCommands = _mocker.GetMock<ProjectCommands>();
        _selectedState = _mocker.GetMock<ISelectedState>();
        _dialogService = _mocker.GetMock<IDialogService>();
        _guiCommands = _mocker.GetMock<IGuiCommands>();
        _fileCommands = _mocker.GetMock<IFileCommands>();
        _pluginManager = _mocker.GetMock<PluginManager>();
        _wireframeObjectManager = _mocker.GetMock<IWireframeObjectManager>();
        _projectManager = _mocker.GetMock<IProjectManager>();

        _deleteLogic = new DeleteLogic(
            _projectCommands.Object,
            _selectedState.Object,
            _dialogService.Object,
            _guiCommands.Object,
            _fileCommands.Object,
            _pluginManager.Object,
            _wireframeObjectManager.Object,
            _projectManager.Object);

        var gumProject = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = gumProject;
    }

    [Fact]
    public void RemoveInstance_WithChildren_RemovesOnlyInstance()
    {
        var screen = CreateScreenWithInstances("Parent");
        var parent = screen.Instances[0];
        var child = AddChild(screen, "Child", "Parent");

        _deleteLogic.RemoveInstance(parent, screen);

        screen.Instances.ShouldNotContain(parent);
        screen.Instances.ShouldContain(child, "Child should still exist after parent removal");
    }

    [Fact]
    public void RemoveParentReferencesToInstance_RemovesParentVariables()
    {
        var screen = CreateScreenWithInstances("Parent");
        var parent = screen.Instances[0];
        AddChild(screen, "Child1", "Parent");
        AddChild(screen, "Child2", "Parent");

        _deleteLogic.RemoveParentReferencesToInstance(parent, screen);

        screen.DefaultState.Variables
            .Where(v => v.GetRootName() == "Parent" && v.Value as string == "Parent")
            .ShouldBeEmpty("All parent references to Parent should be removed");
    }

    [Fact]
    public void RemoveParentReferencesToInstance_WithDottedReference_RemovesIt()
    {
        var screen = CreateScreenWithInstances("Parent");
        var parent = screen.Instances[0];
        AddChild(screen, "Child", "Parent.Container");

        _deleteLogic.RemoveParentReferencesToInstance(parent, screen);

        screen.DefaultState.Variables
            .Where(v => v.Value is string s && s.StartsWith("Parent."))
            .ShouldBeEmpty("Dotted parent references should be removed");
    }

    [Fact]
    public void GetFolderDeletionBlocker_EmptyFolder_ReturnsNull()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GumTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        try
        {
            DeleteLogic.GetFolderDeletionBlocker(tempDir).ShouldBeNull();
        }
        finally
        {
            Directory.Delete(tempDir);
        }
    }

    [Fact]
    public void GetFolderDeletionBlocker_FolderWithFiles_ReturnsBlocker()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GumTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "test.txt"), "");
        try
        {
            var result = DeleteLogic.GetFolderDeletionBlocker(tempDir);
            result.ShouldNotBeNull();
            result.ShouldBe("contains a file");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GetFolderDeletionBlocker_FolderWithSubdirectories_ReturnsBlocker()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GumTest_" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(tempDir, "subfolder"));
        try
        {
            var result = DeleteLogic.GetFolderDeletionBlocker(tempDir);
            result.ShouldNotBeNull();
            result.ShouldBe("contains a folder");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GetFolderDeletionBlocker_NonExistentFolder_ReturnsNull()
    {
        var fakePath = Path.Combine(Path.GetTempPath(), "GumTest_DoesNotExist_" + Guid.NewGuid());
        DeleteLogic.GetFolderDeletionBlocker(fakePath).ShouldBeNull();
    }

    [Fact]
    public void GetFolderDeletionBlocker_MultipleFiles_ReturnsPluralBlocker()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GumTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "a.txt"), "");
        File.WriteAllText(Path.Combine(tempDir, "b.txt"), "");
        try
        {
            var result = DeleteLogic.GetFolderDeletionBlocker(tempDir);
            result.ShouldBe("contains 2 files");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GetFolderDeletionBlocker_FilesAndSubdirectories_ReturnsCombinedBlocker()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GumTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "a.txt"), "");
        Directory.CreateDirectory(Path.Combine(tempDir, "subfolder"));
        try
        {
            var result = DeleteLogic.GetFolderDeletionBlocker(tempDir);
            result.ShouldBe("contains a file and a folder");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private ScreenSave CreateScreenWithInstances(params string[] instanceNames)
    {
        var screen = new ScreenSave();
        screen.Name = "TestScreen";
        
        var defaultState = new StateSave();
        defaultState.ParentContainer = screen;
        defaultState.Name = "Default";
        screen.States.Add(defaultState);

        foreach (var name in instanceNames)
        {
            var instance = new InstanceSave();
            instance.Name = name;
            instance.ParentContainer = screen;
            screen.Instances.Add(instance);
        }

        return screen;
    }

    private InstanceSave AddChild(ScreenSave screen, string childName, string parentName)
    {
        var child = new InstanceSave();
        child.Name = childName;
        child.ParentContainer = screen;
        screen.Instances.Add(child);

        screen.DefaultState.SetValue($"{childName}.Parent", parentName, "string");

        return child;
    }
}
