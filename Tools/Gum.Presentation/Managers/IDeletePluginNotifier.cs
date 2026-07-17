using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;

namespace Gum.Managers;

/// <summary>
/// Narrow notification port the delete subsystem uses to inform plugins that a delete just
/// changed the model. It exposes only the headless (<c>GumDataTypes</c>) plugin calls
/// <c>DeleteLogic</c> makes, so the delete logic no longer needs to depend on the concrete,
/// MEF/WinForms-entangled <c>PluginManager</c> for those notifications (ADR-0005 Phase 3).
/// The two WPF-coupled delete plugin calls — <c>ShowDeleteDialog</c> and <c>DeleteConfirmed</c>,
/// which both take the WPF <c>DeleteOptionsWindow</c> — never belonged on a headless interface;
/// they now live only on the concrete tool-side <c>PluginManager</c> (dropped from
/// <see cref="Gum.Plugins.IPluginManager"/> entirely, #3754) and are called by
/// <c>DeleteDialogService</c>, which depends on the concrete type for exactly this reason.
/// </summary>
public interface IDeletePluginNotifier
{
    bool TryHandleDelete();

    void ElementDelete(ElementSave element);

    void StateDelete(StateSave stateSave);

    void CategoryDelete(StateSaveCategory category);

    void BehaviorDeleted(BehaviorSave behavior);

    void InstanceDelete(ElementSave elementSave, InstanceSave instance);

    void InstancesDelete(ElementSave elementSave, InstanceSave[] instances);

    void BehaviorInstanceDelete(BehaviorSave behavior, BehaviorInstanceSave instance);
}
