using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.Fonts
{
    [Export(typeof(PluginBase))]
    public class MainFontPlugin : InternalPlugin
    {
        public override void StartUp()
        {
            var clearFontCacheMenuItem = this.AddMenuItem(new[] {
                "Content", "Clear Font Cache" });
            clearFontCacheMenuItem.Click += HandleClearFontCache;

            var refreshFontCacheMenuItem = this.AddMenuItem(new[]
            {
                "Content", "Refresh Font Cache"
            });
            refreshFontCacheMenuItem.Click += HandleRefreshFontCache;

        }

        private void HandleClearFontCache(object sender, EventArgs e)
        {
            FontManager.Self.DeleteFontCacheFolder();
        }

        private void HandleRefreshFontCache(object sender, EventArgs e)
        {
            var gumProjectSave = ProjectState.Self.GumProjectSave;
            if(gumProjectSave == null)
            {
                GumCommands.Self.GuiCommands.ShowMessage(
                    "A Gum project must first be loaded before recreating font files");
            }
            else
            {
                FontManager.Self.CreateAllMissingFontFiles(
                    ProjectState.Self.GumProjectSave);
            }
        }
    }
}
