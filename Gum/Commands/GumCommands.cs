using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.Commands;
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

        public FileCommands FileCommands
        {
            get;
            private set;
        }


        public GumCommands()
        {
            GuiCommands = new GuiCommands();
            FileCommands = new FileCommands();
        }

        public void Initialize(MainWindow mainWindow)
        {
            GuiCommands.Initialize(mainWindow);

        }

    }
}
