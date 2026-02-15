using CommonFormsAndControls;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.Settings;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Windows.Forms;

namespace GumToolUnitTests.Plugins.InternalPlugins.TreeView;

public class TreeViewStateServiceTests : BaseTestClass
{
    private readonly Mock<IUserProjectSettingsManager> _mockSettingsManager;
    private readonly Mock<IOutputManager> _mockOutputManager;
    private readonly TreeViewStateService _service;
    private readonly MultiSelectTreeView _treeView;

    public TreeViewStateServiceTests()
    {
        _mockSettingsManager = new Mock<IUserProjectSettingsManager>();
        _mockOutputManager = new Mock<IOutputManager>();
        _service = new TreeViewStateService(_mockSettingsManager.Object, _mockOutputManager.Object);
        _treeView = new MultiSelectTreeView();
    }

    public override void Dispose()
    {
        base.Dispose();
        _treeView?.Dispose();
    }

    [Fact]
    public void CaptureAndSaveState_ShouldCaptureExpandedNodes()
    {
        // Arrange
        var settings = new UserProjectSettings { TreeViewState = new TreeViewState() };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        var rootNodeName = "Components";
        var childNodeName = "Button";

        var rootNode = _treeView.Nodes.Add(rootNodeName);
        var childNode = rootNode.Nodes.Add(childNodeName);
        rootNode.Expand();

        // Act
        _service.CaptureAndSaveState(_treeView);

        // Assert
        settings.TreeViewState.ExpandedNodes.ShouldNotBeNull();
        settings.TreeViewState.ExpandedNodes.Count.ShouldBe(1);
        settings.TreeViewState.ExpandedNodes.ShouldContain(rootNodeName);
    }

    [Fact]
    public void CaptureAndSaveState_ShouldCaptureNestedExpandedNodes()
    {
        // Arrange
        var settings = new UserProjectSettings { TreeViewState = new TreeViewState() };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        var componentsNodeName = "Components";
        var buttonsFolderName = "Buttons";
        var primaryButtonName = "PrimaryButton";
        var expectedComponentsPath = componentsNodeName;
        var expectedButtonsFolderPath = $"{componentsNodeName}/{buttonsFolderName}";

        var componentsNode = _treeView.Nodes.Add(componentsNodeName);
        var buttonsFolder = componentsNode.Nodes.Add(buttonsFolderName);
        var primaryButton = buttonsFolder.Nodes.Add(primaryButtonName);

        componentsNode.Expand();
        buttonsFolder.Expand();

        // Act
        _service.CaptureAndSaveState(_treeView);

        // Assert
        settings.TreeViewState.ExpandedNodes.Count.ShouldBe(2);
        settings.TreeViewState.ExpandedNodes.ShouldContain(expectedComponentsPath);
        settings.TreeViewState.ExpandedNodes.ShouldContain(expectedButtonsFolderPath);
    }

    [Fact]
    public void CaptureAndSaveState_ShouldCreateTreeViewState_WhenNull()
    {
        // Arrange
        var settings = new UserProjectSettings { TreeViewState = null };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        var rootNodeName = "Components";
        var rootNode = _treeView.Nodes.Add(rootNodeName);
        rootNode.Expand();

        // Act
        _service.CaptureAndSaveState(_treeView);

        // Assert
        settings.TreeViewState.ShouldNotBeNull();
        settings.TreeViewState.ExpandedNodes.ShouldContain(rootNodeName);
    }

    [Fact]
    public void CaptureAndSaveState_ShouldDoNothing_WhenCurrentSettingsIsNull()
    {
        // Arrange
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns((UserProjectSettings?)null);

        // Act
        _service.CaptureAndSaveState(_treeView);

        // Assert - should not throw
        _mockOutputManager.Verify(x => x.AddError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void CaptureAndSaveState_ShouldDoNothing_WhenTreeViewIsNull()
    {
        // Arrange
        var settings = new UserProjectSettings();
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        // Act
        _service.CaptureAndSaveState(null!);

        // Assert - should not throw
        _mockOutputManager.Verify(x => x.AddError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void CaptureAndSaveState_ShouldHandleError_Gracefully()
    {
        // Arrange
        _mockSettingsManager.Setup(x => x.CurrentSettings).Throws<System.Exception>();

        var rootNode = _treeView.Nodes.Add("Components");
        rootNode.Expand();

        // Act
        _service.CaptureAndSaveState(_treeView);

        // Assert
        _mockOutputManager.Verify(x => x.AddError(It.Is<string>(msg => msg.Contains("Error capturing tree view state"))), Times.Once);
    }

    [Fact]
    public void CaptureAndSaveState_WithCollapsedTree_ShouldReturnEmptyList()
    {
        // Arrange
        var settings = new UserProjectSettings { TreeViewState = new TreeViewState() };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        _treeView.Nodes.Add("Components");
        _treeView.Nodes.Add("Screens");
        // Don't expand any nodes

        // Act
        _service.CaptureAndSaveState(_treeView);

        // Assert
        settings.TreeViewState.ExpandedNodes.ShouldBeEmpty();
    }

    [Fact]
    public void CaptureAndSaveState_WithMultipleRootNodes_ShouldCaptureAll()
    {
        // Arrange
        var settings = new UserProjectSettings { TreeViewState = new TreeViewState() };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        var componentsNodeName = "Components";
        var screensNodeName = "Screens";
        var standardsNodeName = "Standards";

        var componentsNode = _treeView.Nodes.Add(componentsNodeName);
        var screensNode = _treeView.Nodes.Add(screensNodeName);
        var standardsNode = _treeView.Nodes.Add(standardsNodeName);

        componentsNode.Expand();
        screensNode.Expand();
        // Leave standardsNode collapsed

        // Act
        _service.CaptureAndSaveState(_treeView);

        // Assert
        settings.TreeViewState.ExpandedNodes.Count.ShouldBe(2);
        settings.TreeViewState.ExpandedNodes.ShouldContain(componentsNodeName);
        settings.TreeViewState.ExpandedNodes.ShouldContain(screensNodeName);
        settings.TreeViewState.ExpandedNodes.ShouldNotContain(standardsNodeName);
    }

    [Fact]
    public void LoadAndApplyState_ShouldDoNothing_WhenCurrentSettingsIsNull()
    {
        // Arrange
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns((UserProjectSettings?)null);
        _treeView.Nodes.Add("Components");

        // Act
        _service.LoadAndApplyState(_treeView);

        // Assert - should not throw or expand nodes
        _treeView.Nodes[0].IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void LoadAndApplyState_ShouldDoNothing_WhenTreeViewIsNull()
    {
        // Arrange
        var settings = new UserProjectSettings();
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        // Act
        _service.LoadAndApplyState(null!);

        // Assert - should not throw
        _mockOutputManager.Verify(x => x.AddError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void LoadAndApplyState_ShouldDoNothing_WhenTreeViewStateIsNull()
    {
        // Arrange
        var settings = new UserProjectSettings { TreeViewState = null };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);
        _treeView.Nodes.Add("Components");

        // Act
        _service.LoadAndApplyState(_treeView);

        // Assert
        _treeView.Nodes[0].IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void LoadAndApplyState_ShouldExpandCorrectNodes()
    {
        // Arrange
        var componentsNodeName = "Components";
        var screensNodeName = "Screens";
        var standardsNodeName = "Standards";
        var expandedPaths = new List<string> { componentsNodeName, screensNodeName };

        var settings = new UserProjectSettings
        {
            TreeViewState = new TreeViewState
            {
                ExpandedNodes = expandedPaths
            }
        };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        _treeView.Nodes.Add(componentsNodeName);
        _treeView.Nodes.Add(screensNodeName);
        _treeView.Nodes.Add(standardsNodeName);

        // Act
        _service.LoadAndApplyState(_treeView);

        // Assert
        _treeView.Nodes[0].IsExpanded.ShouldBeTrue();  // Components
        _treeView.Nodes[1].IsExpanded.ShouldBeTrue();  // Screens
        _treeView.Nodes[2].IsExpanded.ShouldBeFalse(); // Standards
    }

    [Fact]
    public void LoadAndApplyState_ShouldExpandNestedNodes()
    {
        // Arrange
        var componentsNodeName = "Components";
        var buttonsFolderName = "Buttons";
        var primaryButtonName = "PrimaryButton";
        var expandedPaths = new List<string>
        {
            componentsNodeName,
            $"{componentsNodeName}/{buttonsFolderName}"
        };

        var settings = new UserProjectSettings
        {
            TreeViewState = new TreeViewState
            {
                ExpandedNodes = expandedPaths
            }
        };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        var componentsNode = _treeView.Nodes.Add(componentsNodeName);
        var buttonsFolder = componentsNode.Nodes.Add(buttonsFolderName);
        var primaryButton = buttonsFolder.Nodes.Add(primaryButtonName);

        // Act
        _service.LoadAndApplyState(_treeView);

        // Assert
        componentsNode.IsExpanded.ShouldBeTrue();
        buttonsFolder.IsExpanded.ShouldBeTrue();
        primaryButton.IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void LoadAndApplyState_ShouldHandleError_Gracefully()
    {
        // Arrange
        _mockSettingsManager.Setup(x => x.CurrentSettings).Throws<System.Exception>();

        // Act
        _service.LoadAndApplyState(_treeView);

        // Assert
        _mockOutputManager.Verify(x => x.AddError(It.Is<string>(msg => msg.Contains("Error applying tree view state"))), Times.Once);
    }

    [Fact]
    public void LoadAndApplyState_ShouldIgnoreNonExistentPaths()
    {
        // Arrange
        var componentsNodeName = "Components";
        var expandedPaths = new List<string>
        {
            componentsNodeName,
            "NonExistent/Path",
            "Components/DoesNotExist"
        };

        var settings = new UserProjectSettings
        {
            TreeViewState = new TreeViewState
            {
                ExpandedNodes = expandedPaths
            }
        };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        _treeView.Nodes.Add(componentsNodeName);

        // Act
        _service.LoadAndApplyState(_treeView);

        // Assert - should not throw
        _treeView.Nodes[0].IsExpanded.ShouldBeTrue();
        _mockOutputManager.Verify(x => x.AddError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void LoadAndApplyState_WithEmptyPathsList_ShouldDoNothing()
    {
        // Arrange
        var settings = new UserProjectSettings
        {
            TreeViewState = new TreeViewState
            {
                ExpandedNodes = new List<string>()
            }
        };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        _treeView.Nodes.Add("Components");

        // Act
        _service.LoadAndApplyState(_treeView);

        // Assert
        _treeView.Nodes[0].IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void RoundTrip_ShouldPreserveExpandedState()
    {
        // Arrange - Create first tree with some nodes expanded
        var settings = new UserProjectSettings { TreeViewState = new TreeViewState() };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        var componentsNodeName = "Components";
        var buttonsFolderName = "Buttons";
        var screensNodeName = "Screens";
        var expectedExpandedPaths = new List<string>
        {
            componentsNodeName,
            $"{componentsNodeName}/{buttonsFolderName}"
        };

        var componentsNode = _treeView.Nodes.Add(componentsNodeName);
        var buttonsFolder = componentsNode.Nodes.Add(buttonsFolderName);
        var screensNode = _treeView.Nodes.Add(screensNodeName);

        componentsNode.Expand();
        buttonsFolder.Expand();
        // screensNode is intentionally left collapsed

        // Act - Capture state from first tree
        _service.CaptureAndSaveState(_treeView);

        // Assert - Verify captured state matches expectations
        settings.TreeViewState.ExpandedNodes.ShouldBe(expectedExpandedPaths);

        // Create a fresh tree with the same structure (all nodes collapsed by default)
        using var freshTreeView = new MultiSelectTreeView();
        var freshComponentsNode = freshTreeView.Nodes.Add(componentsNodeName);
        var freshButtonsFolder = freshComponentsNode.Nodes.Add(buttonsFolderName);
        var freshScreensNode = freshTreeView.Nodes.Add(screensNodeName);

        // Verify fresh tree starts collapsed
        freshComponentsNode.IsExpanded.ShouldBeFalse();
        freshButtonsFolder.IsExpanded.ShouldBeFalse();
        freshScreensNode.IsExpanded.ShouldBeFalse();

        // Act - Restore state to fresh tree
        _service.LoadAndApplyState(freshTreeView);

        // Assert - Previously expanded nodes are now expanded in fresh tree
        freshComponentsNode.IsExpanded.ShouldBeTrue();
        freshButtonsFolder.IsExpanded.ShouldBeTrue();
        freshScreensNode.IsExpanded.ShouldBeFalse();
    }
}
