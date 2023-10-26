using System;

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


        public bool IsEnabled
        {
            get;
            set;
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

        public Exception FailureException
        {
            get;
            private set;
        }

        public string FailureDetails
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