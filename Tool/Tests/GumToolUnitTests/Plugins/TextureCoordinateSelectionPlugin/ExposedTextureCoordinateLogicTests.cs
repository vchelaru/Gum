using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Moq;
using Shouldly;
using TextureCoordinateSelectionPlugin.Logic;

namespace GumToolUnitTests.Plugins.TextureCoordinateSelectionPlugin;

public class ExposedTextureCoordinateLogicTests : BaseTestClass
{
    private readonly Mock<IObjectFinder> _objectFinderMock;
    private readonly ExposedTextureCoordinateLogic _sut;

    public ExposedTextureCoordinateLogicTests()
    {
        _objectFinderMock = new Mock<IObjectFinder>();
        _sut = new ExposedTextureCoordinateLogic(_objectFinderMock.Object);
    }

    private static ComponentSave CreateComponent(string name)
    {
        var component = new ComponentSave { Name = name };
        component.States.Add(new StateSave());
        return component;
    }

    #region IsDirectSpriteOrNineSlice

    [Fact]
    public void IsDirectSpriteOrNineSlice_ReturnsFalse_ForComponentWithNoRelevantBaseElements()
    {
        var component = CreateComponent("MyComponent");
        _objectFinderMock
            .Setup(f => f.GetBaseElements(component))
            .Returns([new StandardElementSave { Name = "Text" }]);

        _sut.IsDirectSpriteOrNineSlice(component).ShouldBeFalse();
    }

    [Fact]
    public void IsDirectSpriteOrNineSlice_ReturnsFalse_ForStandardElementWithNonSpriteName()
    {
        var element = new StandardElementSave { Name = "Text" };

        _sut.IsDirectSpriteOrNineSlice(element).ShouldBeFalse();
    }

    [Fact]
    public void IsDirectSpriteOrNineSlice_ReturnsTrue_ForComponentWhoseBaseElementsIncludeSprite()
    {
        var component = CreateComponent("MyComponent");
        _objectFinderMock
            .Setup(f => f.GetBaseElements(component))
            .Returns([new StandardElementSave { Name = "Sprite" }]);

        _sut.IsDirectSpriteOrNineSlice(component).ShouldBeTrue();
    }

    [Fact]
    public void IsDirectSpriteOrNineSlice_ReturnsTrue_ForStandardElementNamedNineSlice()
    {
        var element = new StandardElementSave { Name = "NineSlice" };

        _sut.IsDirectSpriteOrNineSlice(element).ShouldBeTrue();
    }

    [Fact]
    public void IsDirectSpriteOrNineSlice_ReturnsTrue_ForStandardElementNamedSprite()
    {
        var element = new StandardElementSave { Name = "Sprite" };

        _sut.IsDirectSpriteOrNineSlice(element).ShouldBeTrue();
    }

    #endregion

    #region GetExposedSets

    [Fact]
    public void GetExposedSets_ReturnsEmptyList_WhenInstanceElementIsNotSpriteOrNineSlice()
    {
        var instance = new InstanceSave { Name = "mySprite", BaseType = "Text" };
        var textElement = new StandardElementSave { Name = "Text" };
        var variable = new VariableSave { Name = "mySprite.TextureLeft", ExposedAsName = "LeftEdge" };

        var element = CreateComponent("MyComponent");
        element.Instances.Add(instance);
        element.DefaultState.Variables.Add(variable);

        _objectFinderMock
            .Setup(f => f.GetElementSave(instance))
            .Returns(textElement);

        _sut.GetExposedSets(element).ShouldBeEmpty();
    }

    [Fact]
    public void GetExposedSets_ReturnsEmptyList_WhenInstanceIsNotFoundInElement()
    {
        var variable = new VariableSave { Name = "missingSprite.TextureLeft", ExposedAsName = "LeftEdge" };

        var element = CreateComponent("MyComponent");
        // No instances added — the instance referenced by the variable does not exist.
        element.DefaultState.Variables.Add(variable);

        _sut.GetExposedSets(element).ShouldBeEmpty();
    }

    [Fact]
    public void GetExposedSets_ReturnsEmptyList_WhenNoVariablesExist()
    {
        var element = CreateComponent("MyComponent");

        _sut.GetExposedSets(element).ShouldBeEmpty();
    }

    [Fact]
    public void GetExposedSets_ReturnsEmptyList_WhenVariablesHaveNoExposedAsName()
    {
        var instance = new InstanceSave { Name = "mySprite", BaseType = "Sprite" };
        var variable = new VariableSave { Name = "mySprite.TextureLeft", ExposedAsName = "" };

        var element = CreateComponent("MyComponent");
        element.Instances.Add(instance);
        element.DefaultState.Variables.Add(variable);

        _sut.GetExposedSets(element).ShouldBeEmpty();
    }

    [Fact]
    public void GetExposedSets_ReturnsSetWithAllFourNames_WhenAllTextureCoordinatesAreExposed()
    {
        var instance = new InstanceSave { Name = "mySprite", BaseType = "Sprite" };
        var spriteElement = new StandardElementSave { Name = "Sprite" };

        var leftVar = new VariableSave { Name = "mySprite.TextureLeft", ExposedAsName = "LeftEdge" };
        var topVar = new VariableSave { Name = "mySprite.TextureTop", ExposedAsName = "TopEdge" };
        var widthVar = new VariableSave { Name = "mySprite.TextureWidth", ExposedAsName = "WidthEdge" };
        var heightVar = new VariableSave { Name = "mySprite.TextureHeight", ExposedAsName = "HeightEdge" };

        var element = CreateComponent("MyComponent");
        element.Instances.Add(instance);
        element.DefaultState.Variables.Add(leftVar);
        element.DefaultState.Variables.Add(topVar);
        element.DefaultState.Variables.Add(widthVar);
        element.DefaultState.Variables.Add(heightVar);

        _objectFinderMock
            .Setup(f => f.GetElementSave(instance))
            .Returns(spriteElement);

        var result = _sut.GetExposedSets(element);

        result.Count.ShouldBe(1);
        result[0].SourceObjectName.ShouldBe("mySprite");
        result[0].ExposedLeftName.ShouldBe("LeftEdge");
        result[0].ExposedTopName.ShouldBe("TopEdge");
        result[0].ExposedWidthName.ShouldBe("WidthEdge");
        result[0].ExposedHeightName.ShouldBe("HeightEdge");
    }

    [Fact]
    public void GetExposedSets_ReturnsSetWithExposedLeftName_ForValidTextureLeftVariableOnSpriteInstance()
    {
        var instance = new InstanceSave { Name = "mySprite", BaseType = "Sprite" };
        var spriteElement = new StandardElementSave { Name = "Sprite" };
        var variable = new VariableSave { Name = "mySprite.TextureLeft", ExposedAsName = "LeftEdge" };

        var element = CreateComponent("MyComponent");
        element.Instances.Add(instance);
        element.DefaultState.Variables.Add(variable);

        _objectFinderMock
            .Setup(f => f.GetElementSave(instance))
            .Returns(spriteElement);

        var result = _sut.GetExposedSets(element);

        result.Count.ShouldBe(1);
        result[0].SourceObjectName.ShouldBe("mySprite");
        result[0].ExposedLeftName.ShouldBe("LeftEdge");
        result[0].ExposedTopName.ShouldBeNull();
        result[0].ExposedWidthName.ShouldBeNull();
        result[0].ExposedHeightName.ShouldBeNull();
    }

    #endregion
}
