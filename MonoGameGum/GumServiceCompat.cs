#if MONOGAME || KNI || FNA
#define XNALIKE
#endif
using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary;
using System;

// Back-compat shims for the GumService namespace migration (issue #3119).
// The real types live in the Gum namespace (GumService.cs); these subclasses and
// forwarding extensions keep legacy `using MonoGameGum;` / `using RaylibGum;` code
// compiling. The GumService shim is intentionally permanent: Gum.GumService.Default
// is declared as (and constructs) this derived type so code typed against the legacy
// name keeps working — see the Default property in GumService.cs. The extension
// forwarders below are not permanent and carry dated retention comments.
//
// NOTE for FRB1 maintainers: this file is intentionally NOT in GumCoreShared.projitems.
// It shims Gum.GumService, which is also not FRB-shared.

// This entire shim only makes sense for the two legacy namespaces below. A new backend
// (e.g. a future Silk.NET backend) has no legacy MonoGameGum/RaylibGum namespace to shim,
// so none of this file should compile for it — hence the wrapper rather than an #else
// that would just relocate the "pick a default namespace" landmine here instead.
#if XNALIKE || RAYLIB

#if XNALIKE
namespace MonoGameGum;
#elif RAYLIB
namespace RaylibGum;
#endif

/// <summary>
/// Back-compat alias for <see cref="Gum.GumService"/>. The live singleton returned by
/// <c>Default</c> is an instance of this class so legacy declarations keep compiling.
/// New code should use <see cref="Gum.GumService"/>.
/// </summary>
// Permanent: this shim is never removed or turned into an error. Gum.GumService.Default returns this
// type, so removing it would break every `MonoGameGum.GumService x = GumService.Default;` declaration
// until Default is retyped (done together with deleting the other shims, not before).
[Obsolete("Use Gum.GumService instead. This legacy namespace alias is permanent: Gum.GumService.Default returns it, so it is never removed.")]
public class GumService : global::Gum.GumService
{
}

/// <summary>
/// Legacy home of the AddChild extension used by generated code (syntax versions 0-2),
/// which imports this namespace instead of <c>Gum.Forms.Controls</c> (to avoid name
/// collisions with user-authored components named like built-in Forms types:
/// <c>Label</c>, <c>ListBox</c>, etc.). Syntax version 3+ codegen no longer needs it.
/// AddToRoot/RemoveFromRoot are gone from here — they are instance methods on
/// <see cref="GraphicalUiElement"/> and need no using directive at all.
/// </summary>
public static class GraphicalUiElementExtensionMethods
{
    /// <summary>
    /// Parents the supplied Forms control under this <see cref="GraphicalUiElement"/>. Used by
    /// MonoGameForms-output codegen at syntax versions 0-2 — the generated <c>AssignParents()</c>
    /// body routinely calls <c>someRuntime.AddChild(someFormsControl)</c> to attach a Forms child
    /// to a runtime visual, and resolves it through this extension. Call the instance method
    /// <see cref="GraphicalUiElement.AddChild(Gum.Forms.Controls.FrameworkElement)"/> instead.
    /// </summary>
    /// <remarks>
    /// The instance method <c>GraphicalUiElement.AddChild(FrameworkElement)</c> wins overload
    /// resolution over this extension, so ordinary <c>gue.AddChild(formsControl)</c> calls never
    /// reach it. Only an explicit static call or a runtime built before the instance method existed does.
    /// </remarks>
    // Obsoleted 2026-10-20. Warning now; error:true no earlier than May 2027; remove no earlier than November 2027.
    [Obsolete("Call the GraphicalUiElement.AddChild(FrameworkElement) instance method instead (it needs no using directive). This legacy namespace forwarder becomes an error no earlier than May 2027 and is removed no earlier than November 2027.")]
    public static void AddChild(this GraphicalUiElement element, Gum.Forms.Controls.FrameworkElement child) =>
        Gum.Forms.Controls.FrameworkElementExt.AddChild(element, child);
}

/// <summary>
/// Back-compat forwarder for <see cref="global::Gum.ElementSaveExtensionMethods"/>.
/// </summary>
public static class ElementSaveExtensionMethods
{
    /// <inheritdoc cref="global::Gum.ElementSaveExtensionMethods.ToGraphicalUiElement(ElementSave, SystemManagers?)"/>
    // Obsoleted 2026-06-12. Warning now; error:true no earlier than May 2027; remove no earlier than November 2027.
    [Obsolete("Use Gum.ElementSaveExtensionMethods.ToGraphicalUiElement instead (via `using Gum;`). This legacy namespace forwarder becomes an error no earlier than May 2027 and is removed no earlier than November 2027.")]
    public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave, SystemManagers? systemManagers = null) =>
        global::Gum.ElementSaveExtensionMethods.ToGraphicalUiElement(elementSave, systemManagers);
}
#endif
