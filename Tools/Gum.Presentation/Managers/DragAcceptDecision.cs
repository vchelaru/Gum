namespace Gum.Managers;

/// <summary>
/// The framework-neutral result of deciding whether a wireframe drag should be
/// accepted. Produced by <see cref="IDragDropManager.DecideWireframeDragEffect"/>
/// so the WinForms drag-enter glue in the view can set the drag effect and
/// surface a blocked reason without the manager naming any WinForms type.
/// </summary>
/// <param name="Accept">
/// True when the drag should be accepted (the view sets the Copy effect).
/// </param>
/// <param name="BlockedReason">
/// A human-readable reason the drop was rejected, for the view to surface in the
/// output window (#3128); null when the drop is accepted or there is nothing to
/// report.
/// </param>
public sealed record DragAcceptDecision(bool Accept, string? BlockedReason);
