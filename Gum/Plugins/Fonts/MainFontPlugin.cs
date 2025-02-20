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
                "Content", "Re-create missing font files"
            });
            refreshFontCacheMenuItem.Click += (_,_) => HandleRefreshFontCache(forceRecreate:false);


            var forceFontRecreationMenuItem = this.AddMenuItem(new[]
            {
                "Content", "Force re-create all font files"
            });
            refreshFontCacheMenuItem.Click += (_, _) => HandleRefreshFontCache(forceRecreate: true);

            var viewFontCache = this.AddMenuItem(new[]
            {
                "Content", "View Font Cache"});
            viewFontCache.Click += HandleViewFontCache;



            this.ProjectLoad += HandleProjectLoaded;

        }

        private void HandleProjectLoaded(GumProjectSave save)
        {
            FontManager.Self.CreateAllMissingFontFiles(
                ProjectState.Self.GumProjectSave);
        }

        private void HandleClearFontCache(object sender, EventArgs e)
        {
            try
            {
                FontManager.Self.DeleteFontCacheFolder();
            }
            catch
            {
                System.Windows.Forms.MessageBox.Show("Error deleting font cache:\n" + e.ToString());
            }
        }

        private void HandleViewFontCache(object sender, EventArgs e)
        {
            if(!System.IO.Directory.Exists(FontManager.Self.AbsoluteFontCacheFolder))
            {
                System.IO.Directory.CreateDirectory(FontManager.Self.AbsoluteFontCacheFolder);
            }
            System.Diagnostics.Process.Start(FontManager.Self.AbsoluteFontCacheFolder);
        }

        private void HandleRefreshFontCache(bool forceRecreate)
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
                    ProjectState.Self.GumProjectSave, forceRecreate:forceRecreate);
            }
        }
    }
}
