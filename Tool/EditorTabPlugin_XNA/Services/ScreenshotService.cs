using Gum.Commands;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.ComponentModel.Composition;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

internal class ScreenshotService
{
    string? nextScreenshotFileLocation = null;
    Microsoft.Xna.Framework.Graphics.RenderTarget2D renderTarget;
    private readonly SelectionManager _selectionManager;
    private readonly WireframeCommands _wireframeCommands;
    private readonly IGuiCommands _guiCommands;

    public ScreenshotService(SelectionManager selectionManager)
    {
        _selectionManager = selectionManager;
        _wireframeCommands = Locator.GetRequiredService<WireframeCommands>();
        _guiCommands = Locator.GetRequiredService<IGuiCommands>();
    }

    public void InitializeMenuItem(ToolStripMenuItem item)
    {
        item.Click += HandleExportAsImageClicked;
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

    public void HandleBeforeRender()
    {
        if (nextScreenshotFileLocation != null)
        {
            wereRulersVisible =
                _wireframeCommands.AreRulersVisible;
            wereCanvasBoundsVisible =
                _wireframeCommands.AreCanvasBoundsVisible;
            wasBackgroundVisible =
                _wireframeCommands.IsBackgroundGridVisible;
            wereHighlightsVisible =
                _wireframeCommands.AreHighlightsVisible;


            _wireframeCommands.AreRulersVisible = false;
            _wireframeCommands.AreCanvasBoundsVisible = false;
            _wireframeCommands.IsBackgroundGridVisible = false;
            _wireframeCommands.AreHighlightsVisible = false;

            _selectionManager.SelectedGue = null;

            var graphicsDevice = Renderer.Self.GraphicsDevice;

            var width = graphicsDevice.Viewport.Width;
            var height = graphicsDevice.Viewport.Height;

            renderTarget = new Microsoft.Xna.Framework.Graphics.RenderTarget2D(
                graphicsDevice, width, height);

            graphicsDevice.SetRenderTarget(renderTarget);

            graphicsDevice.Clear(
                Microsoft.Xna.Framework.Color.Transparent);
        }
    }

    public void HandleAfterRender()
    {
        if (nextScreenshotFileLocation != null && renderTarget != null)
        {
            var graphicsDevice = Renderer.Self.GraphicsDevice;
            graphicsDevice.SetRenderTarget(null);

            try
            {
                using (var stream = System.IO.File.OpenWrite(nextScreenshotFileLocation))
                {
                    renderTarget.SaveAsPng(stream, renderTarget.Width, renderTarget.Height);
                }
            }
            catch (Exception e)
            {
                _guiCommands.PrintOutput(e.ToString());
            }
            renderTarget.Dispose();
            renderTarget = null;
            nextScreenshotFileLocation = null;


            _wireframeCommands.AreRulersVisible = wereRulersVisible;
            _wireframeCommands.AreCanvasBoundsVisible = wereCanvasBoundsVisible;
            _wireframeCommands.IsBackgroundGridVisible = wasBackgroundVisible;
            _wireframeCommands.AreHighlightsVisible = wereHighlightsVisible;

        }
    }

}
