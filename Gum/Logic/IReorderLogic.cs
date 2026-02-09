using Gum.DataTypes;

namespace Gum.Logic;

public interface IReorderLogic
{
    void MoveSelectedInstanceForward();
    void MoveSelectedInstanceBackward();
    void MoveSelectedInstanceToFront();
    void MoveSelectedInstanceToBack();
    void MoveSelectedInstanceInFrontOf(InstanceSave whatToMoveInFrontOf);
    void RefreshInResponseToReorder(InstanceSave instance);
}
