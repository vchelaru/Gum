using System;
using System.Windows.Input;

namespace WpfDataUi.DataTypes;

/// <summary>
/// One entry in a <see cref="MemberCategory"/>'s right-click menu. The item is its own
/// <see cref="ICommand"/> so the menu can be built purely by binding (unlike
/// <see cref="InstanceMember.ContextMenuEvents"/>, whose menu each displayer assembles in code).
/// </summary>
public class MemberCategoryContextMenuItem : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>The text shown in the menu.</summary>
    public string Header { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// Forwarded to <see cref="CommandManager.RequerySuggested"/> so WPF re-evaluates
    /// <see cref="CanExecute"/> when the menu opens, rather than only when the category is rebuilt.
    /// </remarks>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

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
}
