using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;

namespace Gum.Logic;

/// <summary>
/// Narrow notification port <see cref="RenameLogic"/> uses to inform plugins that a rename just
/// changed the model. It exposes only the headless (<c>GumDataTypes</c>) plugin calls
/// <c>RenameLogic</c> makes, so the rename logic no longer needs to depend on the concrete,
/// MEF/WinForms-entangled <c>PluginManager</c> for those notifications (ADR-0005 Phase 3).
/// The live implementation is <c>PluginManager</c>, bridged via DI.
/// </summary>
public interface IRenamePluginNotifier
{
    void StateRename(StateSave stateSave, string oldName);

    void CategoryRename(StateSaveCategory category, string oldName);

    void ElementRename(ElementSave elementSave, string oldName);

    void InstanceRename(ElementSave element, InstanceSave instanceSave, string oldName);
}
