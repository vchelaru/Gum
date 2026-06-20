// Forms-aware members of GraphicalUiElement. Kept in a partial compiled ONLY into GumCommon
// (and therefore every runtime that references it) — intentionally NOT shared to FRB via
// GumCoreShared.projitems, mirroring GumServiceCompat.cs. FRB has its own FrameworkElement and
// reaches the same behavior through FlatRedBall.Forms' extension methods, so it must not see
// these. The #if !FRB (covering the using as well as the type) is defensive in case this file is
// ever linked into an FRB-shared project — there, Gum.Forms.Controls does not exist.
#if !FRB
using Gum.Forms.Controls;

namespace Gum.Wireframe;

public partial class GraphicalUiElement
{
    /// <summary>
    /// Parents the supplied Forms control under this visual by adding the control's
    /// <see cref="FrameworkElement.Visual"/> to this element's <see cref="Children"/>.
    /// </summary>
    /// <remarks>
    /// This is an <b>instance</b> method rather than an extension (the matching extension lives in
    /// <c>Gum.Forms.Controls.FrameworkElementExt</c>) so that <c>gue.AddChild(formsControl)</c>
    /// compiles under just <c>using Gum;</c> — with no <c>using Gum.Forms.Controls;</c>, which would
    /// drag in the control type names (<c>Label</c>, <c>StackPanel</c>, …) and collide with
    /// user components of the same name. Instance methods also win overload resolution over the
    /// extension, so importing both namespaces is never ambiguous (CS0121). This supersedes the
    /// legacy <c>using MonoGameGum;</c> / <c>using RaylibGum;</c> shim for new code.
    /// </remarks>
    public void AddChild(FrameworkElement child) => Children.Add(child.Visual);

    /// <summary>
    /// Removes a Forms control previously parented via <see cref="AddChild(FrameworkElement)"/>.
    /// </summary>
    public void RemoveChild(FrameworkElement child) => Children.Remove(child.Visual);
}
#endif
