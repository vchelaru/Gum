using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolStates;
using Moq;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

public class ExclusionsLogicTests
{
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IGuiCommands> _guiCommands = new();
    private readonly ExclusionsLogic _logic;

    public ExclusionsLogicTests()
    {
        _logic = new ExclusionsLogic(_selectedState.Object, _guiCommands.Object);
    }

    [Fact]
    public void AutoGridHorizontalCells_HiddenWhenChildrenLayoutIsNotAutoGrid()
    {
        RecursiveVariableFinder finder = MakeFinder(("ChildrenLayout", ChildrenLayout.Regular));
        VariableSave variable = new() { Name = "AutoGridHorizontalCells" };

        _logic.GetIfVariableIsExcluded(variable, finder).ShouldBeTrue();
    }

    [Fact]
    public void AutoGridHorizontalCells_ShownWhenChildrenLayoutIsAutoGridHorizontal()
    {
        RecursiveVariableFinder finder = MakeFinder(("ChildrenLayout", ChildrenLayout.AutoGridHorizontal));
        VariableSave variable = new() { Name = "AutoGridHorizontalCells" };

        _logic.GetIfVariableIsExcluded(variable, finder).ShouldBeFalse();
    }

    [Fact]
    public void BaseType_HiddenOnlyWhenSingleEmptyScreenAndNoInstanceSelected()
    {
        GumProjectSave project = new();
        ScreenSave screen = new() { Name = "ScreenOnly", BaseType = "" };
        project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            _selectedState.Setup(s => s.SelectedScreen).Returns(screen);
            _selectedState.Setup(s => s.SelectedInstance).Returns((InstanceSave?)null);
            RecursiveVariableFinder finder = MakeFinder();
            VariableSave variable = new() { Name = "BaseType" };

            _logic.GetIfVariableIsExcluded(variable, finder).ShouldBeTrue();
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void BaseType_ShownWhenMultipleScreensExist()
    {
        GumProjectSave project = new();
        ScreenSave screen = new() { Name = "ScreenOne", BaseType = "" };
        project.Screens.Add(screen);
        project.Screens.Add(new ScreenSave { Name = "ScreenTwo" });
        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            _selectedState.Setup(s => s.SelectedScreen).Returns(screen);
            _selectedState.Setup(s => s.SelectedInstance).Returns((InstanceSave?)null);
            RecursiveVariableFinder finder = MakeFinder();
            VariableSave variable = new() { Name = "BaseType" };

            _logic.GetIfVariableIsExcluded(variable, finder).ShouldBeFalse();
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void GetIfVariableIsExcluded_ReturnsFalse_ForUnrecognizedVariable()
    {
        RecursiveVariableFinder finder = MakeFinder();
        VariableSave variable = new() { Name = "SomeUnrecognizedVariable" };

        _logic.GetIfVariableIsExcluded(variable, finder).ShouldBeFalse();
    }

    [Fact]
    public void HandleVariableSet_ChildrenLayoutChanged_ForcesVariableRefresh()
    {
        ElementSave element = new ScreenSave { Name = "Screen1" };

        _logic.HandleVariableSet(element, null, "ChildrenLayout", oldValue: null);

        _guiCommands.Verify(g => g.RefreshVariables(true), Times.Once);
    }

    [Fact]
    public void HandleVariableSet_OtherVariableChanged_DoesNotRefresh()
    {
        ElementSave element = new ScreenSave { Name = "Screen1" };

        _logic.HandleVariableSet(element, null, "X", oldValue: null);

        _guiCommands.Verify(g => g.RefreshVariables(It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void RenderTargetOnlyVariable_HiddenOnNonRenderTargetContainer()
    {
        _selectedState.Setup(s => s.SelectedElement).Returns(new StandardElementSave { Name = "Container" });
        _selectedState.Setup(s => s.SelectedInstance).Returns((InstanceSave?)null);
        RecursiveVariableFinder finder = MakeFinder(("IsRenderTarget", false));
        VariableSave variable = new() { Name = "Alpha" };

        _logic.GetIfVariableIsExcluded(variable, finder).ShouldBeTrue();
    }

    [Fact]
    public void RenderTargetOnlyVariable_ShownOnRenderTargetContainer()
    {
        _selectedState.Setup(s => s.SelectedElement).Returns(new StandardElementSave { Name = "Container" });
        _selectedState.Setup(s => s.SelectedInstance).Returns((InstanceSave?)null);
        RecursiveVariableFinder finder = MakeFinder(("IsRenderTarget", true));
        VariableSave variable = new() { Name = "Alpha" };

        _logic.GetIfVariableIsExcluded(variable, finder).ShouldBeFalse();
    }

    [Fact]
    public void RenderTargetOnlyVariable_ShownWhenSelectionIsNotAContainer()
    {
        _selectedState.Setup(s => s.SelectedElement).Returns(new StandardElementSave { Name = "Sprite" });
        _selectedState.Setup(s => s.SelectedInstance).Returns((InstanceSave?)null);
        RecursiveVariableFinder finder = MakeFinder(("IsRenderTarget", false));
        VariableSave variable = new() { Name = "Blend" };

        _logic.GetIfVariableIsExcluded(variable, finder).ShouldBeFalse();
    }

    [Fact]
    public void ShapeExclusion_DelegatesToShapeVariableExclusionLogic()
    {
        _selectedState.Setup(s => s.SelectedElement).Returns(new StandardElementSave { Name = "Circle" });
        _selectedState.Setup(s => s.SelectedInstance).Returns((InstanceSave?)null);
        RecursiveVariableFinder finder = MakeFinder(("StrokeWidth", 0f));
        VariableSave variable = new() { Name = "StrokeDashLength" };

        _logic.GetIfVariableIsExcluded(variable, finder).ShouldBeTrue();
    }

    [Fact]
    public void SpriteTextureTop_HiddenWhenTextureAddressIsEntireTexture()
    {
        _selectedState.Setup(s => s.SelectedElement).Returns((ElementSave?)null);
        RecursiveVariableFinder finder = MakeFinder(("TextureAddress", TextureAddress.EntireTexture));
        VariableSave variable = new() { Name = "TextureTop" };

        _logic.GetIfVariableIsExcluded(variable, finder).ShouldBeTrue();
    }

    [Fact]
    public void StackSpacing_HiddenWhenChildrenLayoutIsRegular()
    {
        RecursiveVariableFinder finder = MakeFinder(("ChildrenLayout", ChildrenLayout.Regular));
        VariableSave variable = new() { Name = "StackSpacing" };

        _logic.GetIfVariableIsExcluded(variable, finder).ShouldBeTrue();
    }

    [Fact]
    public void StackSpacing_ShownWhenChildrenLayoutIsLeftToRightStack()
    {
        RecursiveVariableFinder finder = MakeFinder(("ChildrenLayout", ChildrenLayout.LeftToRightStack));
        VariableSave variable = new() { Name = "StackSpacing" };

        _logic.GetIfVariableIsExcluded(variable, finder).ShouldBeFalse();
    }

    [Fact]
    public void TextFont_HiddenWhenUseCustomFontIsTrue()
    {
        _selectedState.Setup(s => s.SelectedElement).Returns((ElementSave?)null);
        RecursiveVariableFinder finder = MakeFinder(("UseCustomFont", true));
        VariableSave variable = new() { Name = "Font" };

        _logic.GetIfVariableIsExcluded(variable, finder).ShouldBeTrue();
    }

    [Fact]
    public void TextOverflowHorizontalMode_HiddenWhenVerticalModeIsSpillOver()
    {
        RecursiveVariableFinder finder = MakeFinder(("TextOverflowVerticalMode", TextOverflowVerticalMode.SpillOver));
        VariableSave variable = new() { Name = "TextOverflowHorizontalMode" };

        _logic.GetIfVariableIsExcluded(variable, finder).ShouldBeTrue();
    }

    [Fact]
    public void Wrap_HiddenWhenTextureAddressIsEntireTexture()
    {
        RecursiveVariableFinder finder = MakeFinder(("TextureAddress", TextureAddress.EntireTexture));
        VariableSave variable = new() { Name = "Wrap" };

        _logic.GetIfVariableIsExcluded(variable, finder).ShouldBeTrue();
    }

    [Fact]
    public void WrapsChildren_HiddenWhenChildrenLayoutIsRegular()
    {
        RecursiveVariableFinder finder = MakeFinder(("ChildrenLayout", ChildrenLayout.Regular));
        VariableSave variable = new() { Name = "WrapsChildren" };

        _logic.GetIfVariableIsExcluded(variable, finder).ShouldBeTrue();
    }

    [Fact]
    public void WrapsChildren_ShownWhenChildrenLayoutIsTopToBottomStack()
    {
        RecursiveVariableFinder finder = MakeFinder(("ChildrenLayout", ChildrenLayout.TopToBottomStack));
        VariableSave variable = new() { Name = "WrapsChildren" };

        _logic.GetIfVariableIsExcluded(variable, finder).ShouldBeFalse();
    }

    private static RecursiveVariableFinder MakeFinder(params (string name, object value)[] variables)
    {
        ComponentSave element = new() { Name = "Test" };
        StateSave state = new() { Name = "Default", ParentContainer = element };
        foreach ((string name, object value) in variables)
        {
            state.Variables.Add(new VariableSave { Name = name, Value = value, SetsValue = true });
        }
        return new RecursiveVariableFinder(state);
    }
}
