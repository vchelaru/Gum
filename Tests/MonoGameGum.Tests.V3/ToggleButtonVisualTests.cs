using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V3;

public class ToggleButtonVisualTests
{
    [Fact]
    public void FormsControl_ShouldBeToggleButton()
    {
        // Arrange & Act
        var visual = new ToggleButtonVisual();

        // Assert
        visual.FormsControl.ShouldBeOfType<ToggleButton>();
    }

    [Fact]
    public void ToggleButton_Visual_HasEvents_IsTrue()
    {
        // Arrange & Act
        ToggleButton sut = new();

        // Assert
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void ToggleButton_Visual_IsToggleButtonVisual()
    {
        // Arrange & Act
        ToggleButton sut = new();

        // Assert
        sut.Visual.ShouldBeOfType<ToggleButtonVisual>();
    }

    [Fact]
    public void ToggleCategory_ShouldContainAllOnOffStates()
    {
        // Arrange
        var visual = new ToggleButtonVisual();
        var expectedStateNames = new[]
        {
            "EnabledOn", "EnabledOff",
            "HighlightedOn", "HighlightedOff",
            "PushedOn", "PushedOff",
            "DisabledOn", "DisabledOff",
            "FocusedOn", "FocusedOff",
            "HighlightedFocusedOn", "HighlightedFocusedOff",
            "DisabledFocusedOn", "DisabledFocusedOff",
        };

        // Act
        var stateNames = visual.ToggleCategory.States.Select(s => s.Name).ToList();

        // Assert
        stateNames.Count.ShouldBe(expectedStateNames.Length);
        foreach (var expected in expectedStateNames)
        {
            stateNames.ShouldContain(expected);
        }
    }

    [Fact]
    public void ToggleCategory_Name_ShouldBeToggleCategory()
    {
        // Arrange & Act
        var visual = new ToggleButtonVisual();

        // Assert
        visual.ToggleCategory.Name.ShouldBe("ToggleCategory");
    }

    [Fact]
    public void ToggleCategory_States_ShouldAllHaveApplyDelegates()
    {
        // Arrange
        var visual = new ToggleButtonVisual();

        // Act & Assert
        foreach (var state in visual.ToggleCategory.States)
        {
            state.Apply.ShouldNotBeNull($"State '{state.Name}' should have an Apply delegate");
        }
    }

    [Fact]
    public void ToggleCategory_States_ShouldBeInvokableWithoutError()
    {
        // Arrange
        var visual = new ToggleButtonVisual();

        // Act & Assert — applying every state should not throw
        foreach (var state in visual.ToggleCategory.States)
        {
            Should.NotThrow(() => state.Apply(), $"State '{state.Name}' Apply threw an exception");
        }
    }
}
