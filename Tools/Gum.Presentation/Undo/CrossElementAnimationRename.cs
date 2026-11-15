namespace Gum.Undo;

/// <summary>
/// An animation rename attached to the undo action of the element that owns the animation. Other
/// elements play the animation through instances, so undo and redo re-run the rename over the
/// references that exist at that moment rather than replaying what the rename itself changed.
/// </summary>
public class CrossElementAnimationRename
{
    public string OldName { get; set; } = "";

    public string NewName { get; set; } = "";
}
