// Compiled ONLY into GumCommon (and therefore every runtime that references it) — intentionally
// NOT shared to FRB via GumCoreShared.projitems, mirroring GraphicalUiElement.Forms.cs. FRB has its
// own FrameworkElement, so it must not see this.
#if !FRB
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;

namespace Gum;

/// <summary>
/// Extensions for creating Forms controls directly from loaded project elements. Available on every
/// runtime (MonoGame, KNI, FNA, raylib, Skia, Sokol) under just <c>using Gum;</c>.
/// </summary>
public static class ElementSaveFormsExtensions
{
    /// <summary>
    /// Instantiates the element's visual and returns its Forms control, or null if the element has
    /// none (for example a plain container or screen). Like <c>ToGraphicalUiElement</c>, the visual
    /// is not added to the managers or the root.
    /// </summary>
    /// <param name="elementSave">The element to instantiate.</param>
    /// <param name="systemManagers">
    /// The managers to create the visual with, or <see cref="ISystemManagers.Default"/> when omitted.
    /// </param>
    public static FrameworkElement? ToForms(this ElementSave elementSave, ISystemManagers? systemManagers = null)
    {
        systemManagers ??= ISystemManagers.Default;
        GraphicalUiElement visual = elementSave.ToGraphicalUiElement(systemManagers, addToManagers: false);
        return (visual as InteractiveGue)?.FormsControlAsObject as FrameworkElement;
    }

    /// <summary>
    /// Instantiates the element's visual and returns its Forms control as <typeparamref name="T"/>,
    /// or null if the element has no Forms control or the control is not a <typeparamref name="T"/>.
    /// </summary>
    /// <param name="elementSave">The element to instantiate.</param>
    /// <param name="systemManagers">
    /// The managers to create the visual with, or <see cref="ISystemManagers.Default"/> when omitted.
    /// </param>
    public static T? ToForms<T>(this ElementSave elementSave, ISystemManagers? systemManagers = null)
        where T : FrameworkElement
    {
        return elementSave.ToForms(systemManagers) as T;
    }
}
#endif
