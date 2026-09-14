using System;

namespace Gum.Plugins.BaseClasses;

/// <summary>
/// Base for the tool's built-in plugins that run under both heads. They live in this assembly,
/// which each head lists in <see cref="IPluginHostConfiguration.InternalPluginAssemblies"/>, receive
/// events before other plugins (<see cref="IPriorityPlugin"/>), and hand their tabs a ViewModel that
/// the head resolves to its own view. The defaults match the WPF head's <c>PriorityPlugin</c>, minus
/// its WPF base class.
/// </summary>
public abstract class CorePriorityPlugin : PluginBase, IPriorityPlugin
{
    /// <inheritdoc/>
    public override string FriendlyName => GetType().Name;

    /// <inheritdoc/>
    public override Version Version => new Version();

    /// <inheritdoc/>
    public override bool ShutDown(PluginShutDownReason shutDownReason) => false;
}
