using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using MonoGameGum.Input;
using Moq;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Input;

public class CursorExtensionsTests : BaseTestClass
{
    private static InteractiveGue CreateVisibleElement(string name)
    {
        InteractiveGue element = new InteractiveGue(new InvisibleRenderable());
        element.Name = name;
        element.Width = 200;
        element.Height = 100;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.HeightUnits = DimensionUnitType.Absolute;
        element.HasEvents = true;
        return element;
    }

    [Fact]
    public void GetEventFailureReason_ShouldContinuePastManagers_WhenInLastEventRoots()
    {
        // Simulate a GumBatch scenario: element has no managers but was
        // passed as a root to Update.
        InteractiveGue element = CreateVisibleElement("BatchElement");

        Mock<ICursor> cursor = new();
        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns(50);
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(50);
        cursor.Setup(x => x.X).Returns(50);
        cursor.Setup(x => x.Y).Returns(50);

        // Call Update with element as a root to populate LastEventRoots
        GumService.Default.Update(
            new Microsoft.Xna.Framework.GameTime(),
            new GraphicalUiElement[] { element });

        string? reason = cursor.Object.GetEventFailureReason(element);

        // Should not contain "EffectiveManagers" — that check should be skipped
        if (reason != null)
        {
            reason.ShouldNotContain("EffectiveManagers");
        }
    }

    [Fact]
    public void GetEventFailureReason_ShouldMentionUpdate_WhenNotInLastEventRootsAndNoManagers()
    {
        InteractiveGue element = CreateVisibleElement("OrphanElement");

        Mock<ICursor> cursor = new();
        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns(50);
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(50);
        cursor.Setup(x => x.X).Returns(50);
        cursor.Setup(x => x.Y).Returns(50);

        // Call Update with a different element so this one is NOT in LastEventRoots
        InteractiveGue otherElement = new InteractiveGue(new InvisibleRenderable());
        GumService.Default.Update(
            new Microsoft.Xna.Framework.GameTime(),
            new GraphicalUiElement[] { otherElement });

        string? reason = cursor.Object.GetEventFailureReason(element);

        reason.ShouldNotBeNull();
        reason.ShouldContain("GumService.Update()");
    }

    [Fact]
    public void GetEventFailureReason_ShouldReportCursorPosition_WhenInLastEventRootsButCursorOutside()
    {
        InteractiveGue element = CreateVisibleElement("BatchElement");

        Mock<ICursor> cursor = new();
        // Position cursor outside the element
        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns(500);
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(500);
        cursor.Setup(x => x.X).Returns(500);
        cursor.Setup(x => x.Y).Returns(500);

        GumService.Default.Update(
            new Microsoft.Xna.Framework.GameTime(),
            new GraphicalUiElement[] { element });

        string? reason = cursor.Object.GetEventFailureReason(element);

        reason.ShouldNotBeNull();
        // Should give cursor-position info, not "EffectiveManagers"
        reason.ShouldNotContain("EffectiveManagers");
        reason.ShouldContain("cursor");
    }

    [Fact]
    public void GetEventFailureReason_ShouldSkipRootParentCheck_WhenInLastEventRoots()
    {
        // In GumBatch mode, the element won't be under Root/PopupRoot/ModalRoot.
        // The diagnostic should not report "orphan object" if it was in the event roots.
        InteractiveGue element = CreateVisibleElement("BatchElement");

        Mock<ICursor> cursor = new();
        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns(50);
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(50);
        cursor.Setup(x => x.X).Returns(50);
        cursor.Setup(x => x.Y).Returns(50);

        GumService.Default.Update(
            new Microsoft.Xna.Framework.GameTime(),
            new GraphicalUiElement[] { element });

        string? reason = cursor.Object.GetEventFailureReason(element);

        if (reason != null)
        {
            reason.ShouldNotContain("orphan");
        }
    }

    [Fact]
    public void GetEventFailureReason_ShouldIdentifyChildOverlap_WhenCursorOverChildOfTarget()
    {
        InteractiveGue parent = CreateVisibleElement("WindowVisual");

        InteractiveGue child = CreateVisibleElement("TitleBarInstance");
        parent.AddChild(child);

        Mock<ICursor> cursor = new();
        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns(50);
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(50);
        cursor.Setup(x => x.X).Returns(50);
        cursor.Setup(x => x.Y).Returns(50);
        // Simulate the cursor being over the child, not the parent
        cursor.SetupGet(x => x.VisualOver).Returns(child);

        parent.AddToRoot();

        string? reason = cursor.Object.GetEventFailureReason(parent);

        reason.ShouldNotBeNull();
        reason.ShouldContain("child");
        reason.ShouldContain("TitleBarInstance");
        reason.ShouldContain("not directly over");
    }

    // -------------------------------------------------------------------------
    // Overloads that look up the target by type and/or name
    // -------------------------------------------------------------------------

    [Fact]
    public void GetEventFailureReason_ByType_ShouldReportNoMatch_WhenNoElementFound()
    {
        Mock<ICursor> cursor = new();

        string? reason = cursor.Object.GetEventFailureReason<Button>();

        reason.ShouldNotBeNull();
        reason.ShouldContain("No FrameworkElement");
        reason.ShouldContain("Button");
    }

    [Fact]
    public void GetEventFailureReason_ByType_ShouldReturnSingleDiagnostic_WhenExactlyOneMatch()
    {
        Button button = new Button();
        GumService.Default.Root.AddChild(button.Visual);

        Mock<ICursor> cursor = new();
        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns(-500);
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(-500);
        cursor.Setup(x => x.X).Returns(-500);
        cursor.Setup(x => x.Y).Returns(-500);

        string? lookupReason = cursor.Object.GetEventFailureReason<Button>();
        string? directReason = cursor.Object.GetEventFailureReason(button);

        lookupReason.ShouldBe(directReason);
    }

    [Fact]
    public void GetEventFailureReason_ByType_ShouldListAllMatches_WhenMultipleMatch()
    {
        Button first = new Button { Name = "First" };
        Button second = new Button { Name = "Second" };
        GumService.Default.Root.AddChild(first.Visual);
        GumService.Default.Root.AddChild(second.Visual);

        Mock<ICursor> cursor = new();

        string? reason = cursor.Object.GetEventFailureReason<Button>();

        reason.ShouldNotBeNull();
        reason.ShouldContain("Found 2");
        reason.ShouldContain("First");
        reason.ShouldContain("Second");
    }

    [Fact]
    public void GetEventFailureReason_ByName_ShouldFindElement()
    {
        Button button = new Button { Name = "ConfirmButton" };
        GumService.Default.Root.AddChild(button.Visual);

        Mock<ICursor> cursor = new();
        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns(-500);
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(-500);
        cursor.Setup(x => x.X).Returns(-500);
        cursor.Setup(x => x.Y).Returns(-500);

        string? lookupReason = cursor.Object.GetEventFailureReason("ConfirmButton");
        string? directReason = cursor.Object.GetEventFailureReason(button);

        lookupReason.ShouldBe(directReason);
    }

    [Fact]
    public void GetEventFailureReason_ByTypeAndName_ShouldDisambiguate()
    {
        Button matchingButton = new Button { Name = "Save" };
        Button otherButton = new Button { Name = "Cancel" };
        CheckBox sameNameDifferentType = new CheckBox { Name = "Save" };
        GumService.Default.Root.AddChild(matchingButton.Visual);
        GumService.Default.Root.AddChild(otherButton.Visual);
        GumService.Default.Root.AddChild(sameNameDifferentType.Visual);

        Mock<ICursor> cursor = new();
        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns(-500);
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(-500);
        cursor.Setup(x => x.X).Returns(-500);
        cursor.Setup(x => x.Y).Returns(-500);

        string? lookupReason = cursor.Object.GetEventFailureReason<Button>("Save");
        string? directReason = cursor.Object.GetEventFailureReason(matchingButton);

        lookupReason.ShouldBe(directReason);
    }

    [Fact]
    public void GetEventFailureReason_ShouldSearchNested_WhenElementIsGrandchildOfRoot()
    {
        Panel panel = new Panel();
        Button nested = new Button { Name = "Nested" };
        panel.AddChild(nested);
        GumService.Default.Root.AddChild(panel.Visual);

        Mock<ICursor> cursor = new();
        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns(-500);
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(-500);
        cursor.Setup(x => x.X).Returns(-500);
        cursor.Setup(x => x.Y).Returns(-500);

        string? reason = cursor.Object.GetEventFailureReason("Nested");

        reason.ShouldNotBeNull();
        reason.ShouldNotStartWith("No FrameworkElement");
    }
}
