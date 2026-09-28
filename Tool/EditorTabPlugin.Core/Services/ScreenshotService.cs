using EditorTabPlugin_XNA.Services;
using Gum.Commands;
using Gum.Services.Dialogs;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.ComponentModel.Composition;
namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

#pragma warning disable CA1001 // Types that own disposable fields should be disposable - this never gets disposed
internal class ScreenshotService
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
{
    string? nextScreenshotFileLocation = null;
    Microsoft.Xna.Framework.Graphics.RenderTarget2D? renderTarget;
    private readonly SelectionManager _selectionManager;
    private readonly IWireframeCommands _wireframeCommands;
    private readonly IGuiCommands _guiCommands;
    private readonly IDialogService _dialogService;
    private readonly BackgroundManager _backgroundManager;

    public ScreenshotService(
        SelectionManager selectionManager,
        IWireframeCommands wireframeCommands,
        IGuiCommands guiCommands,
        IDialogService dialogService,
        BackgroundManager backgroundManager)
    {
        _selectionManager = selectionManager;
        _wireframeCommands = wireframeCommands;
        _guiCommands = guiCommands;
        _dialogService = dialogService;
        _backgroundManager = backgroundManager;
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
    bool wereHighlightsVisible;
    bool wasGridOverlayVisible;

    public void HandleBeforeRender()
    {
        if (nextScreenshotFileLocation != null &&
            // The render loop only runs once the graphics device exists.
            Renderer.Self.GraphicsDevice is { } graphicsDevice)
        {
            wereRulersVisible =
                _wireframeCommands.AreRulersVisible;
            wereCanvasBoundsVisible =
                _wireframeCommands.AreCanvasBoundsVisible;
            wereHighlightsVisible =
                _wireframeCommands.AreHighlightsVisible;
            wasGridOverlayVisible =
                _wireframeCommands.IsGridOverlayVisible;


            _wireframeCommands.AreRulersVisible = false;
            _wireframeCommands.AreCanvasBoundsVisible = false;
            // The export is the element alone on a transparent background.
            _backgroundManager.IsHiddenForExport = true;
            _wireframeCommands.AreHighlightsVisible = false;
            _wireframeCommands.IsGridOverlayVisible = false;

            _selectionManager.SelectedGue = null;

            var width = graphicsDevice.Viewport.Width;
            var height = graphicsDevice.Viewport.Height;

            // PreserveContents: an element drawn through its own render target switches targets
            // mid-frame, and a DiscardContents target comes back filled with the driver's discard
            // color instead of the transparent clear below.
            renderTarget = new Microsoft.Xna.Framework.Graphics.RenderTarget2D(
                graphicsDevice, width, height, mipMap: false,
                Microsoft.Xna.Framework.Graphics.SurfaceFormat.Color,
                Microsoft.Xna.Framework.Graphics.DepthFormat.None,
                preferredMultiSampleCount: 0,
                Microsoft.Xna.Framework.Graphics.RenderTargetUsage.PreserveContents);

            graphicsDevice.SetRenderTarget(renderTarget);

            graphicsDevice.Clear(
                Microsoft.Xna.Framework.Color.Transparent);
        }
    }

    public void HandleAfterRender()
    {
        if (nextScreenshotFileLocation != null && renderTarget != null)
        {
            Renderer.Self.GraphicsDevice?.SetRenderTarget(null);

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
            _backgroundManager.IsHiddenForExport = false;
            _wireframeCommands.AreHighlightsVisible = wereHighlightsVisible;
            _wireframeCommands.IsGridOverlayVisible = wasGridOverlayVisible;

        }
    }

}
