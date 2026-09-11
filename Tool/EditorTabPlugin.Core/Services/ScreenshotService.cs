using Gum.Commands;
using Gum.Services.Dialogs;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.ComponentModel.Composition;
using System.Security.Cryptography;
namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

#pragma warning disable CA1001 // Types that own disposable fields should be disposable - this never gets disposed
internal class ScreenshotService
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
{
    string? nextScreenshotFileLocation = null;
    Microsoft.Xna.Framework.Graphics.RenderTarget2D renderTarget;
    private readonly SelectionManager _selectionManager;
    private readonly IWireframeCommands _wireframeCommands;
    private readonly IGuiCommands _guiCommands;
    private readonly IDialogService _dialogService;

    public ScreenshotService(
        SelectionManager selectionManager,
        IWireframeCommands wireframeCommands,
        IGuiCommands guiCommands,
        IDialogService dialogService)
    {
        _selectionManager = selectionManager;
        _wireframeCommands = wireframeCommands;
        _guiCommands = guiCommands;
        _dialogService = dialogService;
    }

    /// <summary>
    /// Asks for a PNG path and captures the canvas on the next render. Bound to
    /// File > Export > Export as Image by the plugin.
    /// </summary>
    public void ExportAsImage()
    {
        string? fileName = _dialogService.SaveFile(new SaveFileDialogOptions
        {
            Title = "Export as Image",
            Filter = "PNG Files (*.png)|*.png",
        });

        if (!string.IsNullOrEmpty(fileName))
        {
            nextScreenshotFileLocation = fileName;
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
