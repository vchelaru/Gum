using System.IO;
using Gum.DataTypes;
using Gum.PropertyGridHelpers;
using Gum.Services.Dialogs;

namespace Gum.Dialogs;

public class RenameElementDialogViewModel : GetUserStringDialogBaseViewModel
{
    private readonly SetVariableLogic _setVariableLogic;
    
    public override string? Title => "Rename Element";
    public override string Message => "Enter new name:";
    
    public ElementSave? ElementSave { get => Get<ElementSave?>(); set => Set(value); }

    public RenameElementDialogViewModel(SetVariableLogic setVariableLogic)
    {
        _setVariableLogic = setVariableLogic;
        PreSelect = true;
        PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ElementSave))
            {
                if (ElementSave is null)
                {
                    Value = null;
                }
                else
                {
                    string elementName = ElementSave.Name;
                    if (elementName.Contains("/"))
                    {
                        Value = Path.GetFileName(elementName);
                        Prefix = Path.GetDirectoryName(elementName).Replace("\\", "/") + "/";
                    }
                    else
                    {
                        Value = elementName;
                    }
                }
            }
        };
    }
    
    protected override void OnAffirmative()
    {
        if (Value is null || ElementSave is null || Error is not null) return;
        
        string? oldName = ElementSave?.Name;
        string newName = Prefix + Value;
        
        ElementSave.Name = newName;
        
        _setVariableLogic.PropertyValueChanged("Name",
            oldName,
            null,
            ElementSave.DefaultState,
            refresh: true,
            recordUndo: true,
            trySave: true);
        
        base.OnAffirmative();
    }
}