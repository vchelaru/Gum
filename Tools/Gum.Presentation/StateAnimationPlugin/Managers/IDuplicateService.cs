using Gum.DataTypes;

namespace StateAnimationPlugin.Managers;

/// <summary>
/// Copies an element's animation sidecar (.ganx) file when the element is duplicated. Implemented by
/// the tool-side <c>DuplicateService</c> (no WPF dependency of its own - only headless deps - but the
/// concrete type is physically homed in the WPF-referencing plugin project; this interface is the seam
/// that lets headless callers like <c>AnimationTabController</c> depend on it).
/// </summary>
public interface IDuplicateService
{
    void HandleDuplicate(ElementSave oldElement, ElementSave newElement);
}
