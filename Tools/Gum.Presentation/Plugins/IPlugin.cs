using System;

namespace Gum.Plugins
{
    public enum PluginShutDownReason
    {
        UserDisabled,
        PluginException,
        PluginInitiated,
        GumxUnload,
        GumShutDown
    }

    public interface IPlugin
    {
        string FriendlyName { get; }
        string UniqueId { get; set; }
        Version Version { get; }
        /// <summary>
        /// Runs once per plugin instance, when the plugin is first enabled. Turning the plugin off and
        /// on again in Manage Plugins does not run it again.
        /// </summary>
        void StartUp();

        /// <summary>
        /// Called when the plugin is turned off. Return false to stay enabled. For
        /// <see cref="PluginShutDownReason.UserDisabled"/> the user can turn the plugin back on without
        /// <see cref="StartUp"/> running again, so don't dispose or remove what StartUp created.
        /// </summary>
        bool ShutDown(PluginShutDownReason shutDownReason);
    }
}
