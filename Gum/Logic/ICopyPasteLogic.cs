using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using System.Collections.Generic;

namespace Gum.Logic;

public interface ICopyPasteLogic
{
    CopiedData CopiedData { get; }
    void ForceSelectionChanged();
    void OnCopy(CopyType copyType);
    void OnCut(CopyType copyType);
    void OnPaste(CopyType copyType, TopOrRecursive topOrRecursive = TopOrRecursive.Recursive);
    List<InstanceSave> PasteInstanceSaves(List<InstanceSave> instancesToCopy,
        List<StateSave> copiedStates,
        ElementSave targetElement,
        InstanceSave? selectedInstance,
        ISelectedState? forcedSelectedState = null);
}
