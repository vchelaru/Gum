using System;
using Gum.Plugins.BaseClasses;

namespace Gum.Plugins
{
    public class PluginContainer
    {
        #region Properties

        public IPlugin Plugin
        {
            get;
            private set;
        }


        /// <summary>
        /// Whether the plugin receives events. Turning it off also disables the menu entries it
        /// added through <see cref="PluginBase.AddMenuEntry(System.Action?, string[])"/> and hides
        /// the tabs it added, so nothing calls into or shows a plugin that is off.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                (Plugin as PluginBase)?.SetAddedUiSuspended(isSuspended: !value);
            }
        }
        private bool _isEnabled;

        /// <summary>
        /// Whether <see cref="IPlugin.StartUp"/> has completed on this instance. StartUp runs once
        /// per instance: turning a plugin off and on again only toggles <see cref="IsEnabled"/>,
        /// since running it again would repeat everything it adds (menu entries, event handlers).
        /// </summary>
        public bool HasStartedUp { get; private set; }

        /// <summary>Runs the plugin's StartUp unless it has already completed on this instance.</summary>
        public void StartUpIfNeeded()
        {
            if (!HasStartedUp)
            {
                Plugin.StartUp();
                HasStartedUp = true;
            }
        }

        public string Name
        {
            get
            {
                string toReturn = "Unnamed Plugin";
                try
                {
                    toReturn = Plugin.FriendlyName;
                }
                catch (Exception e)
                {
                    Fail(e, "Failed getting FriendlyName");

                    toReturn = Plugin.GetType() + " (FriendlyName threw an exception)";
                }

                return toReturn;
            }
        }

        public Exception? FailureException
        {
            get;
            private set;
        }

        public string? FailureDetails
        {
            get;
            private set;
        }

        #endregion

        public PluginContainer(IPlugin plugin)
        {
            Plugin = plugin;
            IsEnabled = true;
        }

        public void Fail(Exception exception, string details)
        {
            IsEnabled = false;
            FailureException = exception;
            FailureDetails = details;

            try
            {
                this.Plugin.ShutDown(PluginShutDownReason.PluginException);
            }
            catch (Exception)
            {
                this.FailureDetails += "\nPlugin also failed during shutdown";
            }
        }

        /// <summary>
        /// Turns the plugin back on and forgets its failure without running StartUp again. For test
        /// hosts that put a plugin an earlier test turned off back as it was.
        /// </summary>
        internal void RestoreEnabled()
        {
            FailureException = null;
            FailureDetails = null;
            IsEnabled = true;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(FailureDetails))
            {
                return Name;
            }
            else
            {
                return Name + "(" + FailureDetails + ")";
            }
        }
    }
}