using Gum.DataTypes;
using Gum.DataTypes.Behaviors;

namespace Gum.Undo;

/// <summary>
/// Narrow notification port the undo subsystem uses to inform plugins that an
/// undo/redo just changed the model. It exposes only the three plugin calls
/// <see cref="UndoManager"/> actually makes, all of which take headless
/// (<c>GumDataTypes</c>) parameters, so the undo logic no longer needs to depend
/// on the concrete, MEF/WinForms-entangled <c>PluginManager</c> (ADR-0005 Phase 3).
/// The live implementation is <c>PluginManager</c>, bridged via DI.
/// </summary>
public interface IUndoPluginNotifier
{
    void InstanceAdd(ElementSave elementSave, InstanceSave instance);

    void InstancesDelete(ElementSave elementSave, InstanceSave[] instances);

    void BehaviorSelected(BehaviorSave? behaviorSave);
}
