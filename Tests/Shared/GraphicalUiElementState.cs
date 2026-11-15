#nullable enable
using Gum.Wireframe;

namespace Gum.Tests.Shared;

/// <summary>
/// The process-wide <see cref="GraphicalUiElement"/> settings that SystemManagers.Initialize sets (and
/// GumService.Uninitialize clears). A test base class captures them when a test starts and restores
/// them when it ends, so a test that initializes a runtime does not leave a later test throwing on a
/// missing file or laying out against another canvas (#6556).
/// </summary>
public sealed class GraphicalUiElementState
{
    private readonly MissingFileBehavior? _missingFileBehavior;
    private readonly float _canvasWidth;
    private readonly float _canvasHeight;

    private GraphicalUiElementState()
    {
        // The raw value, so a test that started with nothing assigned leaves nothing assigned and the next
        // runtime Initialize still applies its ThrowException default (#7963).
        _missingFileBehavior = GraphicalUiElement.AssignedMissingFileBehavior;
        _canvasWidth = GraphicalUiElement.CanvasWidth;
        _canvasHeight = GraphicalUiElement.CanvasHeight;
    }

    public static GraphicalUiElementState Capture() => new GraphicalUiElementState();

    public void Restore()
    {
        GraphicalUiElement.AssignedMissingFileBehavior = _missingFileBehavior;
        GraphicalUiElement.CanvasWidth = _canvasWidth;
        GraphicalUiElement.CanvasHeight = _canvasHeight;
    }
}
