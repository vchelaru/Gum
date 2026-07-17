using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services.Dialogs;
using System.IO;

namespace Gum.Dialogs;

public class RenameElementDialogViewModel : GetUserStringDialogBaseViewModel
{
    private readonly ISetVariableLogic _setVariableLogic;
    private readonly INameVerifier _nameVerifier;

    public override string? Title => "Rename Element";
    public override string Message => "Enter new name:";
    
    public ElementSave? ElementSave { get => Get<ElementSave?>(); set => Set(value); }

    public RenameElementDialogViewModel(ISetVariableLogic setVariableLogic,
        INameVerifier nameVerifier)
    {
        _setVariableLogic = setVariableLogic;
        _nameVerifier = nameVerifier;
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

    public override void OnAffirmative()
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

    protected override string? Validate(string? value)
    {
        var folderName = Prefix ?? string.Empty;

        _nameVerifier.IsElementNameValid(this.Value, folderName, ElementSave, out string whyNotValid);

        if(!string.IsNullOrEmpty(whyNotValid))
                    {
            return whyNotValid;
        }

        return base.Validate(value);
    }
}