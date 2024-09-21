﻿using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum2.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum2.Plugins.TreeViewPlugin
{
    [Export(typeof(PluginBase))]
    internal class MainTreeViewPlugin : InternalPlugin
    {
        public override void StartUp()
        {
            var view = new MainTreeView();

            GumCommands.Self.GuiCommands.AddControl(view, "TreeView", TabLocation.Left);
        }
    }
}
