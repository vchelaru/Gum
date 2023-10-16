using System;
using Gum.Commands;
using Gum.Controls;
using Gum.Managers;

namespace Gum
{
    public class GumCommands : Singleton<GumCommands>
    {
        public GuiCommands GuiCommands
        {
            get;
            private set;
        }

        public WireframeCommands WireframeCommands
        {
            get;
            private set;
        }

        public FileCommands FileCommands
        {
            get;
            private set;
        }

        public EditCommands Edit
        {
            get;
            private set;
        }

        public ToolCommands.ProjectCommands ProjectCommands { get; private set; }

        public GumCommands()
        {
            GuiCommands = new GuiCommands();
            FileCommands = new FileCommands();
            Edit = new EditCommands();
            WireframeCommands = new WireframeCommands();
            ProjectCommands = Gum.ToolCommands.ProjectCommands.Self;
        }

        /// <summary>
        /// Performs the argument action multiple times, swalling exceptions every time except the last. 
        /// This can be used to perform operations which may fail from time to time, like writing to disk.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="numberOfTimesToTry">The number of times to try.</param>
        public void TryMultipleTimes(Action action, int numberOfTimesToTry = 5)
        {
            const int msSleep = 200;
            int failureCount = 0;

            while (failureCount < numberOfTimesToTry)
            {
                try
                {
                    action();
                    break;
                }


                catch (Exception e)
                {
                    failureCount++;
                    System.Threading.Thread.Sleep(msSleep);
                    if (failureCount >= numberOfTimesToTry)
                    {
                        throw e;
                    }
                }
            }
        }

        public void Initialize(MainWindow mainWindow, MainPanelControl mainPanelControl)
        {
            GuiCommands.Initialize(mainWindow, mainPanelControl);
            FileCommands.Initialize(mainWindow);
        }

    }
}
