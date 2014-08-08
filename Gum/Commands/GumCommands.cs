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

        public void Initialize(MainWindow mainWindow)
        {
            GuiCommands.Initialize(mainWindow);

        }

    }
}
