using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;

namespace Gum.Dialogs;

public class AddCategoryDialogViewModel : GetUserStringDialogBaseViewModel
{
    public override string Title => "New Category";
    public override string Message => "Enter new Category name";
    
    private readonly ElementCommands _elementCommands;
    private readonly UndoManager _undoManager;
    private readonly ISelectedState _selectedState;
    private IStateContainer StateContainer => _selectedState.SelectedStateContainer;

    public AddCategoryDialogViewModel(
        ISelectedState selectedState,
        ElementCommands elementCommands,
        UndoManager undoManager)
    {
        _selectedState = selectedState;
        _elementCommands = elementCommands;
        _undoManager = undoManager;
    }

    protected override void OnAffirmative()
    {
        if (StateContainer is null || string.IsNullOrWhiteSpace(Value) || Error is not null) return;
        
        using var undoLock = _undoManager.RequestLock();
        StateSaveCategory category = _elementCommands.AddCategory(StateContainer, Value);
        _selectedState.SelectedStateCategorySave = category;
        
        base.OnAffirmative();
    }

    protected override string? Validate(string? value)
    {
        
        if (StateContainer is ElementSave element &&
            element.GetStateSaveCategoryRecursively(value, out ElementSave categoryContainer) is not null)
        {
            return
                $"Cannot add category - a category with the name {value} is already defined in {categoryContainer}";
        }

        return base.Validate(value);
    }
}