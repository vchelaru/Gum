using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// The Editor tab remembers each element's camera once the user moved it there (#5854).
/// </summary>
public class ElementCameraMemoryTests
{
    private readonly ElementCameraMemory _memory;

    public ElementCameraMemoryTests()
    {
        _memory = new ElementCameraMemory();
    }

    [Fact]
    public void Show_RestoresEachMovedElementsCamera_AndLeavesUnmovedOnesAlone()
    {
        ScreenSave screen = new ScreenSave { Name = "DialogueScreen" };
        ComponentSave button = new ComponentSave { Name = "ButtonStandard" };
        ComponentSave icon = new ComponentSave { Name = "Icon" };
        CameraView start = new CameraView(-30, -30, 100);
        CameraView screenView = new CameraView(400, 250, 400);
        CameraView buttonView = new CameraView(-10, 5, 200);

        _memory.Show(screen, start).ShouldBeNull();
        _memory.Show(button, screenView).ShouldBeNull("the button was never moved");
        _memory.Show(icon, buttonView).ShouldBeNull();
        _memory.Show(screen, buttonView).ShouldBe(screenView);
        _memory.Show(icon, screenView).ShouldBeNull("the icon was shown but never moved");
        _memory.Show(button, screenView).ShouldBe(buttonView);
        _memory.Show(screen, buttonView).ShouldBe(screenView, "a restored element not moved again keeps its camera");
    }

    [Fact]
    public void Show_SameElementAgain_ReturnsNothing_AndNoElementBetweenStillRecords()
    {
        ScreenSave screen = new ScreenSave { Name = "DialogueScreen" };
        CameraView start = new CameraView(-30, -30, 100);
        CameraView moved = new CameraView(400, 250, 400);

        _memory.Show(screen, start);
        _memory.Show(screen, moved).ShouldBeNull("selecting an instance re-shows the same element");
        _memory.Show(null, moved).ShouldBeNull();

        _memory.Show(screen, start).ShouldBe(moved);
    }

    [Fact]
    public void ForgetAndClear_DropRememberedCameras_IncludingTheShownElements()
    {
        ScreenSave screen = new ScreenSave { Name = "DialogueScreen" };
        ComponentSave button = new ComponentSave { Name = "ButtonStandard" };
        CameraView start = new CameraView(-30, -30, 100);
        CameraView moved = new CameraView(400, 250, 400);

        _memory.Show(screen, start);
        _memory.Forget(screen);
        _memory.Show(button, moved);
        _memory.Show(screen, moved).ShouldBeNull("the screen was forgotten while shown, so leaving it records nothing");

        _memory.Show(button, start).ShouldBeNull("the button was never moved");
        _memory.Show(screen, moved).ShouldNotBeNull("the screen was moved before switching to the button");
        _memory.Clear();
        _memory.Show(screen, start).ShouldBeNull();
    }
}
