using Gum.DataTypes;

namespace Gum.Logic;

/// <summary>
/// Narrow headless port <see cref="RenameLogic"/> uses to read the loaded project (for reacting to
/// element/instance renames in <c>RenameAllReferencesTo</c>), so the rename logic no longer needs to
/// depend on the concrete, WinForms-entangled <c>IProjectManager</c> — whose wider surface drags
/// <c>GeneralSettingsFile</c> and the whole load/save flow — for that one access (ADR-0005 Phase 3).
/// The live implementation is <c>ProjectManager</c>, bridged via DI to the same singleton.
/// </summary>
public interface IRenameProjectProvider
{
    GumProjectSave? GumProjectSave { get; }
}
