using Gum.DataTypes;

namespace Gum.Logic;

/// <summary>
/// Narrow headless port <see cref="CopyPasteLogic"/> uses to read the loaded project (for the
/// element-name-uniqueness check in <c>PasteCopiedElement</c>), so the copy/paste logic no
/// longer needs to depend on the concrete, WinForms-entangled <c>IProjectManager</c> — whose wider
/// surface drags <c>GeneralSettingsFile</c> and the whole load/save flow — for that one access
/// (ADR-0005 Phase 3). The live implementation is <c>ProjectManager</c>, bridged via DI to the same
/// singleton.
/// </summary>
public interface ICopyPasteProjectProvider
{
    GumProjectSave? GumProjectSave { get; }
}
