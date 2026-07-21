using Gum.ProjectServices.CodeGeneration;

namespace Gum.Plugins;

/// <summary>
/// Narrow, headless-safe seam over the WPF <c>Views.CodeWindow</c> control for the code-output tab's
/// element/project settings. <c>CodeOutputElementSettings</c> is read back mid-logic (a live fallback
/// default, not just a sink), so its accessor is two-way; <c>CodeOutputProjectSettings</c> is only ever
/// pushed onto the control and never read back, so its accessor is write-only. Both settings types
/// already live in the headless <c>Gum.ProjectServices</c> assembly (no WPF dependency), so they're
/// safe to expose directly - same shape as <see cref="ITabVisibility"/>/<see cref="Gum.Wireframe.IContextMenuState"/>.
/// </summary>
public interface ICodeOutputTabView
{
    CodeOutputElementSettings? CodeOutputElementSettings { get; set; }

    CodeOutputProjectSettings? CodeOutputProjectSettings { set; }
}
