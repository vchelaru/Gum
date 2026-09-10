using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using Gum.Input;

namespace Gum.Plugins;

/// <summary>
/// What a UI head contributes to the plugin host: the assemblies that hold its internal plugins,
/// the head-specific services it exports into the plugin container, whether an external plugin
/// assembly can run on it, and the cursor the editor canvas exposes to plugins.
/// </summary>
public interface IPluginHostConfiguration
{
    /// <summary>Assemblies whose <c>[Export(typeof(PluginBase))]</c> parts are this head's built-in plugins.</summary>
    IEnumerable<Assembly> InternalPluginAssemblies { get; }

    /// <summary>
    /// Adds this head's own exports (its shell view models, menu model, and the like) to the
    /// composition batch, after the host has added every headless service.
    /// </summary>
    void AddHeadExports(CompositionBatch batch);

    /// <summary>
    /// Whether an external plugin assembly can be hosted here. A WPF head accepts anything; a
    /// cross-platform head rejects assemblies that reference WPF or WinForms, giving the reason.
    /// </summary>
    bool CanHostExternalAssembly(Assembly assembly, out string? reason);

    /// <summary>The cursor state plugins query for the world position under the pointer, or null if no canvas exists.</summary>
    IGumCursorState? CursorState { get; }
}

/// <summary>
/// Marks a plugin whose event handlers run before those of other plugins, so the tool's own
/// plugins react to a change before third-party ones see it.
/// </summary>
public interface IPriorityPlugin
{
}

/// <summary>
/// A plugin that participates in the delete-confirmation dialog. The dialog object is opaque here
/// because its type belongs to the head; the WPF base class casts it to its window.
/// </summary>
public interface IDeleteOptionsDialogPlugin
{
    /// <summary>Lets the plugin add options to the dialog before it is shown.</summary>
    void CallDeleteOptionsWindowShow(object optionsWindow, System.Array objectsToDelete);

    /// <summary>Tells the plugin the user confirmed the delete, with the dialog it decorated.</summary>
    void CallDeleteConfirmed(object optionsWindow, System.Array deletedObjects);
}
