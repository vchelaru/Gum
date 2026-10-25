// Compiled only into GumCommon (not shared to FRB through GumCoreShared.projitems), like
// ElementSaveFormsExtensions.cs.
#if !FRB
using System;
using Gum.Wireframe;

namespace Gum.Forms;

/// <summary>
/// A code-built default visual template that FormsUtilities.InitializeDefaults registered, as opposed to
/// one user code registered. Lets the framework tell the two apart, so a from-file project's component
/// can replace the built-in default but never a user's template.
/// </summary>
internal sealed class BuiltInVisualTemplate : VisualTemplate
{
    public BuiltInVisualTemplate(Func<object?, bool, GraphicalUiElement> creationFunc) : base(creationFunc) { }
}
#endif
