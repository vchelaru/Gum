using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using System;
using System.ComponentModel.Composition;

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


            var viewFontCache = this.AddMenuItem(new[]
            {
                "Content", "View Font Cache"});
            viewFontCache.Click += HandleViewFontCache;


            refreshFontCacheMenuItem.Click += HandleRefreshFontCache;

            this.ProjectLoad += HandleProjectLoaded;

        }

        private void HandleProjectLoaded(GumProjectSave save)
        {
            FontManager.Self.CreateAllMissingFontFiles(
                ProjectState.Self.GumProjectSave);
        }

        private void HandleClearFontCache(object sender, EventArgs e)
        {
            FontManager.Self.DeleteFontCacheFolder();
        }

        private void HandleViewFontCache(object sender, EventArgs e)
        {
            if(!System.IO.Directory.Exists(FontManager.Self.AbsoluteFontCacheFolder))
            {
                System.IO.Directory.CreateDirectory(FontManager.Self.AbsoluteFontCacheFolder);
            }
            System.Diagnostics.Process.Start(FontManager.Self.AbsoluteFontCacheFolder);
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
