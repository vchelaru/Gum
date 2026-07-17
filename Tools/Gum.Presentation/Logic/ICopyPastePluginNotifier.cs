using Gum.DataTypes;

namespace Gum.Logic;

/// <summary>
/// Narrow notification port <see cref="CopyPasteLogic"/> uses to inform plugins that a copy/paste
/// operation just changed the model. It exposes only the headless (<c>GumDataTypes</c>) plugin
/// calls <c>CopyPasteLogic</c> makes, so the copy/paste logic no longer needs to depend on the
/// concrete, MEF/WinForms-entangled <c>PluginManager</c> for those notifications (ADR-0005 Phase 3).
/// The live implementation is <c>PluginManager</c>, bridged via DI.
/// </summary>
public interface ICopyPastePluginNotifier
{
    void InstanceAdd(ElementSave elementSave, InstanceSave instance);

    void ElementDuplicate(ElementSave oldElement, ElementSave newElement);
}
