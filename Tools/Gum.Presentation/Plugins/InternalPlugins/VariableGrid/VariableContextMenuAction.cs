using System;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// A single right-click context menu entry for a variable row, headless replacement for
/// <c>WpfDataUi.DataTypes.InstanceMember.ContextMenuEvents.Add(string, RoutedEventHandler)</c>.
/// A view-side adapter wires <see cref="Execute"/> up to whatever menu-item click event its
/// framework uses.
/// </summary>
/// <param name="Label">The text shown in the context menu.</param>
/// <param name="Execute">Invoked when the user clicks the menu item.</param>
public record VariableContextMenuAction(string Label, Action Execute);
