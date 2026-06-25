using Gum.DataTypes;

namespace Gum.Managers;

/// <summary>
/// Narrow headless port the delete subsystem uses to read the loaded project. It exposes only the
/// single <see cref="GumProjectSave"/> property that <c>DeleteLogic</c> reads, so the delete logic
/// no longer needs to depend on the concrete, WinForms-entangled <see cref="IProjectManager"/> —
/// whose wider surface drags <c>GeneralSettingsFile</c> and the whole load/save flow — for that one
/// access (ADR-0005 Phase 3). The return type is the headless <c>GumDataTypes</c>
/// <see cref="DataTypes.GumProjectSave"/>, so this port carries no WPF/WinForms coupling. The live
/// implementation is <c>ProjectManager</c>, bridged via DI to the same singleton.
/// </summary>
public interface IDeleteProjectProvider
{
    GumProjectSave? GumProjectSave { get; }
}
