using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
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

        public GumCommands()
        {
            GuiCommands = new GuiCommands();
            FileCommands = new FileCommands();
            Edit = new EditCommands();
            WireframeCommands = new WireframeCommands();
        }

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

        public void Initialize(MainWindow mainWindow)
        {
            GuiCommands.Initialize(mainWindow);
            FileCommands.Initialize(mainWindow);
        }
        
    }
}
