using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.Settings;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.TreeView;

public class TreeViewStateServiceTests : BaseTestClass
{
    private readonly Mock<IUserProjectSettingsManager> _mockSettingsManager;
    private readonly Mock<IOutputManager> _mockOutputManager;
    private readonly TreeViewStateService _service;
    private readonly List<ITreeNode> _roots;

    public TreeViewStateServiceTests()
    {
        _mockSettingsManager = new Mock<IUserProjectSettingsManager>();
        _mockOutputManager = new Mock<IOutputManager>();
        _service = new TreeViewStateService(_mockSettingsManager.Object, _mockOutputManager.Object);
        _roots = new List<ITreeNode>();
    }

    private GumTreeNode AddRoot(string text)
    {
        GumTreeNode root = new GumTreeNode(text);
        _roots.Add(root);
        return root;
    }

    [Fact]
    public void CaptureAndSaveState_ShouldCaptureExpandedNodes()
    {
        // Arrange
        UserProjectSettings settings = new UserProjectSettings { TreeViewState = new TreeViewState() };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        string rootNodeName = "Components";
        string childNodeName = "Button";

        GumTreeNode rootNode = AddRoot(rootNodeName);
        rootNode.AddChild(childNodeName);
        rootNode.Expand();

        // Act
        _service.CaptureAndSaveState(_roots);

        // Assert
        settings.TreeViewState.ExpandedNodes.ShouldNotBeNull();
        settings.TreeViewState.ExpandedNodes.Count.ShouldBe(1);
        settings.TreeViewState.ExpandedNodes.ShouldContain(rootNodeName);
    }

    [Fact]
    public void CaptureAndSaveState_ShouldCaptureNestedExpandedNodes()
    {
        // Arrange
        UserProjectSettings settings = new UserProjectSettings { TreeViewState = new TreeViewState() };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        string componentsNodeName = "Components";
        string buttonsFolderName = "Buttons";
        string primaryButtonName = "PrimaryButton";
        string expectedComponentsPath = componentsNodeName;
        string expectedButtonsFolderPath = $"{componentsNodeName}/{buttonsFolderName}";

        GumTreeNode componentsNode = AddRoot(componentsNodeName);
        GumTreeNode buttonsFolder = (GumTreeNode)componentsNode.AddChild(buttonsFolderName);
        buttonsFolder.AddChild(primaryButtonName);

        componentsNode.Expand();
        buttonsFolder.Expand();

        // Act
        _service.CaptureAndSaveState(_roots);

        // Assert
        settings.TreeViewState.ExpandedNodes.Count.ShouldBe(2);
        settings.TreeViewState.ExpandedNodes.ShouldContain(expectedComponentsPath);
        settings.TreeViewState.ExpandedNodes.ShouldContain(expectedButtonsFolderPath);
    }

    [Fact]
    public void CaptureAndSaveState_ShouldCreateTreeViewState_WhenNull()
    {
        // Arrange
        UserProjectSettings settings = new UserProjectSettings { TreeViewState = null };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        string rootNodeName = "Components";
        GumTreeNode rootNode = AddRoot(rootNodeName);
        rootNode.Expand();

        // Act
        _service.CaptureAndSaveState(_roots);

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
        _service.CaptureAndSaveState(_roots);

        // Assert - should not throw
        _mockOutputManager.Verify(x => x.AddError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void CaptureAndSaveState_ShouldDoNothing_WhenRootsIsNull()
    {
        // Arrange
        UserProjectSettings settings = new UserProjectSettings();
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
        _mockSettingsManager.Setup(x => x.CurrentSettings).Throws<Exception>();

        GumTreeNode rootNode = AddRoot("Components");
        rootNode.Expand();

        // Act
        _service.CaptureAndSaveState(_roots);

        // Assert
        _mockOutputManager.Verify(x => x.AddError(It.Is<string>(msg => msg.Contains("Error capturing tree view state"))), Times.Once);
    }

    [Fact]
    public void CaptureAndSaveState_WithCollapsedTree_ShouldReturnEmptyList()
    {
        // Arrange
        UserProjectSettings settings = new UserProjectSettings { TreeViewState = new TreeViewState() };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        AddRoot("Components");
        AddRoot("Screens");
        // Don't expand any nodes

        // Act
        _service.CaptureAndSaveState(_roots);

        // Assert
        settings.TreeViewState.ExpandedNodes.ShouldBeEmpty();
    }

    [Fact]
    public void CaptureAndSaveState_WithMultipleRootNodes_ShouldCaptureAll()
    {
        // Arrange
        UserProjectSettings settings = new UserProjectSettings { TreeViewState = new TreeViewState() };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        string componentsNodeName = "Components";
        string screensNodeName = "Screens";
        string standardsNodeName = "Standards";

        GumTreeNode componentsNode = AddRoot(componentsNodeName);
        GumTreeNode screensNode = AddRoot(screensNodeName);
        AddRoot(standardsNodeName);

        componentsNode.Expand();
        screensNode.Expand();
        // Leave standardsNode collapsed

        // Act
        _service.CaptureAndSaveState(_roots);

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
        GumTreeNode componentsNode = AddRoot("Components");

        // Act
        _service.LoadAndApplyState(_roots);

        // Assert - should not throw or expand nodes
        componentsNode.IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void LoadAndApplyState_ShouldDoNothing_WhenRootsIsNull()
    {
        // Arrange
        UserProjectSettings settings = new UserProjectSettings();
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
        UserProjectSettings settings = new UserProjectSettings { TreeViewState = null };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);
        GumTreeNode componentsNode = AddRoot("Components");

        // Act
        _service.LoadAndApplyState(_roots);

        // Assert
        componentsNode.IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void LoadAndApplyState_ShouldExpandCorrectNodes()
    {
        // Arrange
        string componentsNodeName = "Components";
        string screensNodeName = "Screens";
        string standardsNodeName = "Standards";
        List<string> expandedPaths = new List<string> { componentsNodeName, screensNodeName };

        UserProjectSettings settings = new UserProjectSettings
        {
            TreeViewState = new TreeViewState
            {
                ExpandedNodes = expandedPaths
            }
        };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        GumTreeNode componentsNode = AddRoot(componentsNodeName);
        GumTreeNode screensNode = AddRoot(screensNodeName);
        GumTreeNode standardsNode = AddRoot(standardsNodeName);

        // Act
        _service.LoadAndApplyState(_roots);

        // Assert
        componentsNode.IsExpanded.ShouldBeTrue();
        screensNode.IsExpanded.ShouldBeTrue();
        standardsNode.IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void LoadAndApplyState_ShouldExpandNestedNodes()
    {
        // Arrange
        string componentsNodeName = "Components";
        string buttonsFolderName = "Buttons";
        string primaryButtonName = "PrimaryButton";
        List<string> expandedPaths = new List<string>
        {
            componentsNodeName,
            $"{componentsNodeName}/{buttonsFolderName}"
        };

        UserProjectSettings settings = new UserProjectSettings
        {
            TreeViewState = new TreeViewState
            {
                ExpandedNodes = expandedPaths
            }
        };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        GumTreeNode componentsNode = AddRoot(componentsNodeName);
        GumTreeNode buttonsFolder = (GumTreeNode)componentsNode.AddChild(buttonsFolderName);
        GumTreeNode primaryButton = (GumTreeNode)buttonsFolder.AddChild(primaryButtonName);

        // Act
        _service.LoadAndApplyState(_roots);

        // Assert
        componentsNode.IsExpanded.ShouldBeTrue();
        buttonsFolder.IsExpanded.ShouldBeTrue();
        primaryButton.IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void LoadAndApplyState_ShouldHandleError_Gracefully()
    {
        // Arrange
        _mockSettingsManager.Setup(x => x.CurrentSettings).Throws<Exception>();

        // Act
        _service.LoadAndApplyState(_roots);

        // Assert
        _mockOutputManager.Verify(x => x.AddError(It.Is<string>(msg => msg.Contains("Error applying tree view state"))), Times.Once);
    }

    [Fact]
    public void LoadAndApplyState_ShouldIgnoreNonExistentPaths()
    {
        // Arrange
        string componentsNodeName = "Components";
        List<string> expandedPaths = new List<string>
        {
            componentsNodeName,
            "NonExistent/Path",
            "Components/DoesNotExist"
        };

        UserProjectSettings settings = new UserProjectSettings
        {
            TreeViewState = new TreeViewState
            {
                ExpandedNodes = expandedPaths
            }
        };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        GumTreeNode componentsNode = AddRoot(componentsNodeName);

        // Act
        _service.LoadAndApplyState(_roots);

        // Assert - should not throw
        componentsNode.IsExpanded.ShouldBeTrue();
        _mockOutputManager.Verify(x => x.AddError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void LoadAndApplyState_WithEmptyPathsList_ShouldDoNothing()
    {
        // Arrange
        UserProjectSettings settings = new UserProjectSettings
        {
            TreeViewState = new TreeViewState
            {
                ExpandedNodes = new List<string>()
            }
        };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        GumTreeNode componentsNode = AddRoot("Components");

        // Act
        _service.LoadAndApplyState(_roots);

        // Assert
        componentsNode.IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void RoundTrip_ShouldPreserveExpandedState()
    {
        // Arrange - Create first tree with some nodes expanded
        UserProjectSettings settings = new UserProjectSettings { TreeViewState = new TreeViewState() };
        _mockSettingsManager.Setup(x => x.CurrentSettings).Returns(settings);

        string componentsNodeName = "Components";
        string buttonsFolderName = "Buttons";
        string screensNodeName = "Screens";
        List<string> expectedExpandedPaths = new List<string>
        {
            componentsNodeName,
            $"{componentsNodeName}/{buttonsFolderName}"
        };

        GumTreeNode componentsNode = AddRoot(componentsNodeName);
        GumTreeNode buttonsFolder = (GumTreeNode)componentsNode.AddChild(buttonsFolderName);
        AddRoot(screensNodeName);

        componentsNode.Expand();
        buttonsFolder.Expand();
        // The Screens root is intentionally left collapsed

        // Act - Capture state from first tree
        _service.CaptureAndSaveState(_roots);

        // Assert - Verify captured state matches expectations
        settings.TreeViewState.ExpandedNodes.ShouldBe(expectedExpandedPaths);

        // Create a fresh tree with the same structure (all nodes collapsed by default)
        GumTreeNode freshComponentsNode = new GumTreeNode(componentsNodeName);
        GumTreeNode freshButtonsFolder = (GumTreeNode)freshComponentsNode.AddChild(buttonsFolderName);
        GumTreeNode freshScreensNode = new GumTreeNode(screensNodeName);
        List<ITreeNode> freshRoots = new List<ITreeNode> { freshComponentsNode, freshScreensNode };

        // Verify fresh tree starts collapsed
        freshComponentsNode.IsExpanded.ShouldBeFalse();
        freshButtonsFolder.IsExpanded.ShouldBeFalse();
        freshScreensNode.IsExpanded.ShouldBeFalse();

        // Act - Restore state to fresh tree
        _service.LoadAndApplyState(freshRoots);

        // Assert - Previously expanded nodes are now expanded in fresh tree
        freshComponentsNode.IsExpanded.ShouldBeTrue();
        freshButtonsFolder.IsExpanded.ShouldBeTrue();
        freshScreensNode.IsExpanded.ShouldBeFalse();
    }
}
