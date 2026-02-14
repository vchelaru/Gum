using Gum.DataTypes;
using Gum.Managers;
using Gum.Services;
using Moq;
using Shouldly;
using System.Collections.Generic;

namespace GumToolUnitTests.Managers;

public class FavoriteComponentManagerTests : BaseTestClass
{
    private readonly FavoriteComponentManager _favoriteComponentManager;
    private readonly Mock<ICircularReferenceManager> _circularReferenceManager;
    private readonly Mock<IProjectManager> _mockProjectManager;

    public FavoriteComponentManagerTests()
    {
        _mockProjectManager = new Mock<IProjectManager>();
        _favoriteComponentManager = new FavoriteComponentManager(_mockProjectManager.Object);
        _circularReferenceManager = new Mock<ICircularReferenceManager>();
    }

    #region AddToFavorites

    [Fact]
    public void AddToFavorites_ShouldAddComponentToList()
    {
        // Arrange
        var project = new GumProjectSave();
        project.FavoriteComponents = new List<string>();
        ObjectFinder.Self.GumProjectSave = project;

        var component = new ComponentSave { Name = "TestComponent" };

        // Act
        _favoriteComponentManager.AddToFavorites(component);

        // Assert
        project.FavoriteComponents.ShouldContain("TestComponent");
        _mockProjectManager.Verify(pm => pm.SaveProject(), Times.Once);
    }

    [Fact]
    public void AddToFavorites_ShouldNotAddDuplicate_WhenAlreadyFavorited()
    {
        // Arrange
        var project = new GumProjectSave();
        project.FavoriteComponents = new List<string> { "TestComponent" };
        ObjectFinder.Self.GumProjectSave = project;

        var component = new ComponentSave { Name = "TestComponent" };

        // Act
        _favoriteComponentManager.AddToFavorites(component);

        // Assert
        project.FavoriteComponents.Count.ShouldBe(1);
        project.FavoriteComponents.ShouldContain("TestComponent");
        _mockProjectManager.Verify(pm => pm.SaveProject(), Times.Never);
    }


    [Fact]
    public void AddToFavorites_ShouldHandleNullProject()
    {
        // Arrange
        ObjectFinder.Self.GumProjectSave = null;
        var component = new ComponentSave { Name = "TestComponent" };

        // Act & Assert - should not throw
        _favoriteComponentManager.AddToFavorites(component);
        _mockProjectManager.Verify(pm => pm.SaveProject(), Times.Never);
    }

    #endregion

    #region GetFavoritedComponentsForCurrentProject

    [Fact]
    public void GetFavoritedComponentsForCurrentProject_ShouldReturnFavoritedComponents()
    {
        // Arrange
        var project = new GumProjectSave();
        var component1 = new ComponentSave { Name = "Component1" };
        var component2 = new ComponentSave { Name = "Component2" };

        project.Components.Add(component1);
        project.Components.Add(component2);
        project.FavoriteComponents = new List<string> { "Component1", "Component2" };

        ObjectFinder.Self.GumProjectSave = project;

        // Act
        var result = _favoriteComponentManager.GetFavoritedComponentsForCurrentProject();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(component1);
        result.ShouldContain(component2);
    }

    [Fact]
    public void GetFavoritedComponentsForCurrentProject_ShouldFilterOutDeletedComponents()
    {
        // Arrange
        var project = new GumProjectSave();
        var existingComponent = new ComponentSave { Name = "ExistingComponent" };

        project.Components.Add(existingComponent);
        project.FavoriteComponents = new List<string> { "ExistingComponent", "DeletedComponent" };

        ObjectFinder.Self.GumProjectSave = project;

        // Act
        var result = _favoriteComponentManager.GetFavoritedComponentsForCurrentProject();

        // Assert
        result.Count.ShouldBe(1);
        result.ShouldContain(existingComponent);
    }

    [Fact]
    public void GetFavoritedComponentsForCurrentProject_ShouldReturnEmptyList_WhenNoProjectLoaded()
    {
        // Arrange
        ObjectFinder.Self.GumProjectSave = null;

        // Act
        var result = _favoriteComponentManager.GetFavoritedComponentsForCurrentProject();

        // Assert
        result.ShouldBeEmpty();
    }


    #endregion

    #region GetFilteredFavoritedComponentsFor

    [Fact]
    public void GetFilteredFavoritedComponentsFor_ShouldReturnFavoritesThatCanBeAdded()
    {
        // Arrange
        var project = new GumProjectSave();
        var component1 = new ComponentSave { Name = "Component1" };
        var component2 = new ComponentSave { Name = "Component2" };
        var parent = new ComponentSave { Name = "Parent" };

        project.Components.Add(component1);
        project.Components.Add(component2);
        project.Components.Add(parent);
        project.FavoriteComponents = new List<string> { "Component1", "Component2" };

        ObjectFinder.Self.GumProjectSave = project;

        _circularReferenceManager
            .Setup(x => x.CanTypeBeAddedToElement(parent, "Component1"))
            .Returns(true);
        _circularReferenceManager
            .Setup(x => x.CanTypeBeAddedToElement(parent, "Component2"))
            .Returns(true);

        // Act
        var result = _favoriteComponentManager.GetFilteredFavoritedComponentsFor(parent, _circularReferenceManager.Object);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(component1);
        result.ShouldContain(component2);
    }

    [Fact]
    public void GetFilteredFavoritedComponentsFor_ShouldFilterOutCircularReferences()
    {
        // Arrange
        var project = new GumProjectSave();
        var component1 = new ComponentSave { Name = "Component1" };
        var component2 = new ComponentSave { Name = "Component2" };
        var parent = new ComponentSave { Name = "Parent" };

        project.Components.Add(component1);
        project.Components.Add(component2);
        project.Components.Add(parent);
        project.FavoriteComponents = new List<string> { "Component1", "Component2" };

        ObjectFinder.Self.GumProjectSave = project;

        _circularReferenceManager
            .Setup(x => x.CanTypeBeAddedToElement(parent, "Component1"))
            .Returns(true);
        _circularReferenceManager
            .Setup(x => x.CanTypeBeAddedToElement(parent, "Component2"))
            .Returns(false); // Would create circular reference

        // Act
        var result = _favoriteComponentManager.GetFilteredFavoritedComponentsFor(parent, _circularReferenceManager.Object);

        // Assert
        result.Count.ShouldBe(1);
        result.ShouldContain(component1);
        result.ShouldNotContain(component2);
    }

    #endregion

    #region HandleComponentDeleted

    [Fact]
    public void HandleComponentDeleted_ShouldRemoveComponentFromFavorites()
    {
        // Arrange
        var project = new GumProjectSave();
        project.FavoriteComponents = new List<string> { "Component1", "Component2" };
        ObjectFinder.Self.GumProjectSave = project;

        var component = new ComponentSave { Name = "Component1" };

        // Act
        _favoriteComponentManager.HandleComponentDeleted(component);

        // Assert
        project.FavoriteComponents.Count.ShouldBe(1);
        project.FavoriteComponents.ShouldNotContain("Component1");
        project.FavoriteComponents.ShouldContain("Component2");
        _mockProjectManager.Verify(pm => pm.SaveProject(), Times.Once);
    }

    [Fact]
    public void HandleComponentDeleted_ShouldDoNothing_WhenComponentNotInFavorites()
    {
        // Arrange
        var project = new GumProjectSave();
        project.FavoriteComponents = new List<string> { "Component1" };
        ObjectFinder.Self.GumProjectSave = project;

        var component = new ComponentSave { Name = "Component2" };

        // Act
        _favoriteComponentManager.HandleComponentDeleted(component);

        // Assert
        project.FavoriteComponents.Count.ShouldBe(1);
        project.FavoriteComponents.ShouldContain("Component1");
        _mockProjectManager.Verify(pm => pm.SaveProject(), Times.Never);
    }

    [Fact]
    public void HandleComponentDeleted_ShouldHandleNullProject()
    {
        // Arrange
        ObjectFinder.Self.GumProjectSave = null;
        var component = new ComponentSave { Name = "Component1" };

        // Act & Assert - should not throw
        _favoriteComponentManager.HandleComponentDeleted(component);
        _mockProjectManager.Verify(pm => pm.SaveProject(), Times.Never);
    }

    #endregion

    #region HandleComponentRenamed

    [Fact]
    public void HandleComponentRenamed_ShouldUpdateComponentNameInFavorites()
    {
        // Arrange
        var project = new GumProjectSave();
        project.FavoriteComponents = new List<string> { "OldName", "Component2" };
        ObjectFinder.Self.GumProjectSave = project;

        // Act
        _favoriteComponentManager.HandleComponentRenamed("OldName", "NewName");

        // Assert
        project.FavoriteComponents.Count.ShouldBe(2);
        project.FavoriteComponents.ShouldNotContain("OldName");
        project.FavoriteComponents.ShouldContain("NewName");
        project.FavoriteComponents.ShouldContain("Component2");
        _mockProjectManager.Verify(pm => pm.SaveProject(), Times.Once);
    }

    [Fact]
    public void HandleComponentRenamed_ShouldDoNothing_WhenOldNameNotInFavorites()
    {
        // Arrange
        var project = new GumProjectSave();
        project.FavoriteComponents = new List<string> { "Component1", "Component2" };
        ObjectFinder.Self.GumProjectSave = project;

        // Act
        _favoriteComponentManager.HandleComponentRenamed("OldName", "NewName");

        // Assert
        project.FavoriteComponents.Count.ShouldBe(2);
        project.FavoriteComponents.ShouldContain("Component1");
        project.FavoriteComponents.ShouldContain("Component2");
        project.FavoriteComponents.ShouldNotContain("NewName");
        _mockProjectManager.Verify(pm => pm.SaveProject(), Times.Never);
    }

    [Fact]
    public void HandleComponentRenamed_ShouldHandleEmptyOldName()
    {
        // Arrange
        var project = new GumProjectSave();
        project.FavoriteComponents = new List<string> { "Component1" };
        ObjectFinder.Self.GumProjectSave = project;

        // Act & Assert - should not throw
        _favoriteComponentManager.HandleComponentRenamed("", "NewName");
        project.FavoriteComponents.Count.ShouldBe(1);
        _mockProjectManager.Verify(pm => pm.SaveProject(), Times.Never);
    }

    [Fact]
    public void HandleComponentRenamed_ShouldHandleEmptyNewName()
    {
        // Arrange
        var project = new GumProjectSave();
        project.FavoriteComponents = new List<string> { "OldName" };
        ObjectFinder.Self.GumProjectSave = project;

        // Act
        _favoriteComponentManager.HandleComponentRenamed("OldName", "");

        // Assert
        project.FavoriteComponents.Count.ShouldBe(1);
        project.FavoriteComponents.ShouldContain(""); // Empty string replaces old name
        _mockProjectManager.Verify(pm => pm.SaveProject(), Times.Once);
    }

    [Fact]
    public void HandleComponentRenamed_ShouldHandleNullProject()
    {
        // Arrange
        ObjectFinder.Self.GumProjectSave = null;

        // Act & Assert - should not throw
        _favoriteComponentManager.HandleComponentRenamed("OldName", "NewName");
        _mockProjectManager.Verify(pm => pm.SaveProject(), Times.Never);
    }

    #endregion

    #region IsFavorite

    [Fact]
    public void IsFavorite_ShouldReturnFalse_ForNonFavoritedComponent()
    {
        // Arrange
        var project = new GumProjectSave();
        project.FavoriteComponents = new List<string> { "Component1" };
        ObjectFinder.Self.GumProjectSave = project;

        var component = new ComponentSave { Name = "Component2" };

        // Act
        var result = _favoriteComponentManager.IsFavorite(component);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsFavorite_ShouldReturnFalse_WhenNoProjectLoaded()
    {
        // Arrange
        ObjectFinder.Self.GumProjectSave = null;
        var component = new ComponentSave { Name = "Component1" };

        // Act
        var result = _favoriteComponentManager.IsFavorite(component);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsFavorite_ShouldReturnTrue_ForFavoritedComponent()
    {
        // Arrange
        var project = new GumProjectSave();
        project.FavoriteComponents = new List<string> { "Component1" };
        ObjectFinder.Self.GumProjectSave = project;

        var component = new ComponentSave { Name = "Component1" };

        // Act
        var result = _favoriteComponentManager.IsFavorite(component);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region RemoveFromFavorites

    [Fact]
    public void RemoveFromFavorites_ShouldDoNothing_WhenComponentNotInFavorites()
    {
        // Arrange
        var project = new GumProjectSave();
        project.FavoriteComponents = new List<string> { "Component1" };
        ObjectFinder.Self.GumProjectSave = project;

        var component = new ComponentSave { Name = "Component2" };

        // Act
        _favoriteComponentManager.RemoveFromFavorites(component);

        // Assert
        project.FavoriteComponents.Count.ShouldBe(1);
        project.FavoriteComponents.ShouldContain("Component1");
        _mockProjectManager.Verify(pm => pm.SaveProject(), Times.Never);
    }

    [Fact]
    public void RemoveFromFavorites_ShouldRemoveComponentFromFavorites()
    {
        // Arrange
        var project = new GumProjectSave();
        project.FavoriteComponents = new List<string> { "Component1", "Component2" };
        ObjectFinder.Self.GumProjectSave = project;

        var component = new ComponentSave { Name = "Component1" };

        // Act
        _favoriteComponentManager.RemoveFromFavorites(component);

        // Assert
        project.FavoriteComponents.Count.ShouldBe(1);
        project.FavoriteComponents.ShouldNotContain("Component1");
        project.FavoriteComponents.ShouldContain("Component2");
        _mockProjectManager.Verify(pm => pm.SaveProject(), Times.Once);
    }

    #endregion
}
