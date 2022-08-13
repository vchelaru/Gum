using Gum.DataTypes;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.ScreenshotPlugin
{
    [Export(typeof(PluginBase))]
    internal class MainScreenshotPlugin : InternalPlugin
    {
        string nextScreenshotFileLocation = null;
        Microsoft.Xna.Framework.Graphics.RenderTarget2D renderTarget;

        public override void StartUp()
        {
            var item = this.AddMenuItem("File", "Export as Image");
            item.Click += HandleExportAsImageClicked;

            BeforeRender += HandleBeforeRender;
            AfterRender += HandleAfterRender;
        }

        private void HandleExportAsImageClicked(object sender, EventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();



            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG Files (*.png)|*.png";


            var result = dlg.ShowDialog();


            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                nextScreenshotFileLocation = dlg.FileName;
                
            }
        }

        bool wereCanvasBoundsVisible;
        bool wereRulersVisible;
        bool wasBackgroundVisible;
        bool wereHighlightsVisible;
        private void HandleBeforeRender()
        {
            if(nextScreenshotFileLocation != null)
            {
                wereRulersVisible =
                    GumCommands.Self.WireframeCommands.AreRulersVisible;
                wereCanvasBoundsVisible =
                    GumCommands.Self.WireframeCommands.AreCanvasBoundsVisible;
                wasBackgroundVisible =
                    GumCommands.Self.WireframeCommands.IsBackgroundGridVisible;
                wereHighlightsVisible =
                    GumCommands.Self.WireframeCommands.AreHighlightsVisible;


                GumCommands.Self.WireframeCommands.AreRulersVisible = false;
                GumCommands.Self.WireframeCommands.AreCanvasBoundsVisible = false;
                GumCommands.Self.WireframeCommands.IsBackgroundGridVisible = false;

                SelectedState.Self.SelectedIpso = null;

                var graphicsDevice = Renderer.Self.GraphicsDevice;

                var width = graphicsDevice.Viewport.Width;
                var height= graphicsDevice.Viewport.Height;

                renderTarget = new Microsoft.Xna.Framework.Graphics.RenderTarget2D(
                    graphicsDevice, width, height);


                graphicsDevice.SetRenderTarget(renderTarget);

                graphicsDevice.Clear(
                    Microsoft.Xna.Framework.Color.Transparent);
            }
        }

        private void HandleAfterRender()
        {
            if(nextScreenshotFileLocation != null && renderTarget != null)
            {
                var graphicsDevice = Renderer.Self.GraphicsDevice;
                graphicsDevice.SetRenderTarget(null);

                try
                {
                    using(var stream = System.IO.File.OpenWrite(nextScreenshotFileLocation))
                    {
                        renderTarget.SaveAsPng(stream, renderTarget.Width, renderTarget.Height);
                    }
                }
                catch(Exception e)
                {
                    GumCommands.Self.GuiCommands.PrintOutput(e.ToString());
                }
                renderTarget.Dispose();
                renderTarget = null;
                nextScreenshotFileLocation = null;


                GumCommands.Self.WireframeCommands.AreRulersVisible = wereRulersVisible;
                GumCommands.Self.WireframeCommands.AreCanvasBoundsVisible = wereCanvasBoundsVisible;
                GumCommands.Self.WireframeCommands.IsBackgroundGridVisible = wasBackgroundVisible;
                GumCommands.Self.WireframeCommands.AreHighlightsVisible = wereHighlightsVisible;

            }
        }
        
    }
}
