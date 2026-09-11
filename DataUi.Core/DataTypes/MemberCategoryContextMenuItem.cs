using System;
using System.Windows.Input;

namespace WpfDataUi.DataTypes;

/// <summary>
/// One entry in a <see cref="MemberCategory"/>'s right-click menu. The item is its own
/// <see cref="ICommand"/> so the menu can be built purely by binding (unlike
/// <see cref="InstanceMember.ContextMenuEvents"/>, whose menu each displayer assembles in code).
/// </summary>
/// <remarks>
/// <see cref="ICommand"/> is the framework-neutral BCL interface. A head that re-evaluates commands
/// on its own schedule (WPF's <c>CommandManager.RequerySuggested</c>) wraps the item at binding
/// time; one that builds the menu when it opens needs nothing extra.
/// </remarks>
public class MemberCategoryContextMenuItem : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>The text shown in the menu.</summary>
    public string Header { get; }

    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged;

    public MemberCategoryContextMenuItem(string header, Action execute, Func<bool>? canExecute = null)
    {
        Header = header;
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <inheritdoc/>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <inheritdoc/>
    public void Execute(object? parameter) => _execute();

    /// <summary>Tells bound menus to re-read <see cref="CanExecute"/>.</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
