using Gum.DataTypes;

namespace Gum.SelectionHistory;

public interface ISelectionHistory
{
    bool CanNavigateBack { get; }
    bool CanNavigateForward { get; }

    void RecordSelection(ElementSave? element, InstanceSave? instance);
    void NavigateBack();
    void NavigateForward();
}
