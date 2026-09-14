using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Gum.Gui.Windows;
using Gum.Managers;
using MenuItem = System.Windows.Controls.MenuItem;

namespace Gum.Plugins.BaseClasses;

/// <summary>
/// <see cref="PluginBase"/> plus the one member set that is still WPF-coupled: the
/// delete-confirmation dialog events that hand plugins the WPF
/// <c>Gum.Gui.Windows.DeleteOptionsWindow</c> to add controls to, plus an obsolete
/// <see cref="AddMenuItem(string[])"/> shim for external plugins. New code uses the neutral
/// <see cref="PluginBase.DeleteOptionsShow"/> / <see cref="PluginBase.DeleteOptionsConfirmed"/> pair,
/// which both heads render; CodeOutputPlugin is the last in-repo user of the WPF pair. Menus go
/// through <see cref="PluginBase.AddMenuEntry(Action?, string[])"/> on the shared menu model.
/// </summary>
public abstract class WpfPluginBase : PluginBase, IDeleteOptionsDialogPlugin
{
    // Satisfied by MEF property injection before StartUp runs.
    private MenuStripManager _menuStripManager = null!;

    [Import] public MenuStripManager MenuStripManager { get => _menuStripManager; set => _menuStripManager = value; }

    /// <summary>
    /// Kept for external WPF plugins built against the pre-Avalonia contract: adds the entry to
    /// the shared menu model and returns the WPF item rendered for it. New code uses
    /// <see cref="PluginBase.AddMenuEntry(Action?, string[])"/>, which also works under the
    /// Avalonia head; this overload goes away at cutover.
    /// </summary>
    [Obsolete("Use AddMenuEntry; the WPF MenuItem this returns does not exist under the Avalonia head.")]
    public MenuItem AddMenuItem(IEnumerable<string> menuAndSubmenus) =>
        _menuStripManager.GetMenuItem(AddMenuEntry(menuAndSubmenus));

    /// <inheritdoc cref="AddMenuItem(IEnumerable{string})"/>
    [Obsolete("Use AddMenuEntry; the WPF MenuItem this returns does not exist under the Avalonia head.")]
    public MenuItem AddMenuItem(params string[] menuAndSubmenus) => AddMenuItem((IEnumerable<string>)menuAndSubmenus);

    public event Action<DeleteOptionsWindow, Array>? DeleteOptionsWindowShow;
    public event Action<DeleteOptionsWindow, Array>? DeleteConfirmed;

    #region Event calling

    public void CallDeleteOptionsWindowShow(DeleteOptionsWindow optionsWindow, Array objectsToDelete) =>
        DeleteOptionsWindowShow?.Invoke(optionsWindow, objectsToDelete);

    void IDeleteOptionsDialogPlugin.CallDeleteOptionsWindowShow(object optionsWindow, Array objectsToDelete) =>
        CallDeleteOptionsWindowShow((DeleteOptionsWindow)optionsWindow, objectsToDelete);

    void IDeleteOptionsDialogPlugin.CallDeleteConfirmed(object optionsWindow, Array deletedObjects) =>
        CallDeleteConfirmed((DeleteOptionsWindow)optionsWindow, deletedObjects);

    public void CallDeleteConfirmed(DeleteOptionsWindow optionsWindow, Array deletedObjects) =>
        DeleteConfirmed?.Invoke(optionsWindow, deletedObjects);

    #endregion
}
