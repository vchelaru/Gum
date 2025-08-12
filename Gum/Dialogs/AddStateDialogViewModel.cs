using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;

namespace Gum.Dialogs;

public class AddStateDialogViewModel : GetUserStringDialogBaseViewModel
{
    public override string Title => "Add State";
    public override string Message => "Enter new state name";
    
    private readonly ISelectedState _selectedState;
    private readonly INameVerifier _nameVerifier;
    private readonly IUndoManager _undoManager;
    private readonly IElementCommands _elementCommands;
    
    public AddStateDialogViewModel(
        ISelectedState selectedState,
        INameVerifier nameVerifier, 
        IUndoManager undoManager, 
        IElementCommands elementCommands)
    {
        _selectedState = selectedState;
        _nameVerifier = nameVerifier;
        _undoManager = undoManager;
        _elementCommands = elementCommands;
    }

    protected override void OnAffirmative()
    {
        if (Error is not null) return;
        
        using (_undoManager.RequestLock())
        {
            StateSave stateSave = _elementCommands.AddState(
                _selectedState.SelectedStateContainer,
                _selectedState.SelectedStateCategorySave, 
                Value);
            
            _selectedState.SelectedStateSave = stateSave;
        }
        
        base.OnAffirmative();
    }

    protected override string? Validate(string? value)
    {
        if (_selectedState is not { SelectedStateCategorySave: { } category })
        {
            return "You must first select an element or a behavior category to add a state";
        }
        
        return _nameVerifier.IsStateNameValid(value, category, null,
            out string whyNotValid)
            ? base.Validate(value)
            : whyNotValid;
    }
}