using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;

namespace Gum.Dialogs;

public class AddCategoryDialogViewModel : GetUserStringDialogBaseViewModel
{
    public override string Title => "New Category";
    public override string Message => "Enter new Category name";
    
    private readonly IElementCommands _elementCommands;
    private readonly IUndoManager _undoManager;
    private readonly INameVerifier _nameVerifier;
    private readonly ISelectedState _selectedState;
    private IStateContainer? StateContainer => _selectedState.SelectedStateContainer;

    public AddCategoryDialogViewModel(
        ISelectedState selectedState,
        IElementCommands elementCommands,
        IUndoManager undoManager,
        INameVerifier nameVerifier)
    {
        _selectedState = selectedState;
        _elementCommands = elementCommands;
        _undoManager = undoManager;
        _nameVerifier = nameVerifier;
    }

    public override void OnAffirmative()
    {
        if (StateContainer is null || string.IsNullOrWhiteSpace(Value) || Error is not null) return;
        
        using var undoLock = _undoManager.RequestLock();
        StateSaveCategory category = _elementCommands.AddCategory(StateContainer, Value);
        _selectedState.SelectedStateCategorySave = category;
        
        base.OnAffirmative();
    }

    protected override string? Validate(string? value)
    {
        if (StateContainer != null && !_nameVerifier.IsCategoryNameValid(value, StateContainer, out var whyNotValid))
        {
            return whyNotValid;
        }

        return base.Validate(value);
    }
}