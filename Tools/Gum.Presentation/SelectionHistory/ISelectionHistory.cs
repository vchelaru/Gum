using Gum.DataTypes;

namespace Gum.SelectionHistory;

public interface ISelectionHistory
{
    bool CanNavigateBack { get; }
    bool CanNavigateForward { get; }

    void RecordSelection(ElementSave? element, InstanceSave? instance);
    void NavigateBack();
    void NavigateForward();

    /// <summary>Drops every step that selects <paramref name="element"/> or one of its instances, once it is deleted.</summary>
    void ForgetElement(ElementSave element);

    /// <summary>Drops every step that selects <paramref name="instance"/>, once it is deleted.</summary>
    void ForgetInstance(InstanceSave instance);
}
