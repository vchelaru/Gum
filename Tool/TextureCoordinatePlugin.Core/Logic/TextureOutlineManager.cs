using Gum.Services;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Math.Geometry;
using Color = System.Drawing.Color;

namespace TextureCoordinateSelectionPlugin.Logic;

public class TextureOutlineManager : IVisualOverlayManager
{
    private LineRectangle _textureOutlineRectangle;
    private readonly ICanvasDisplayScale _displayScale;

    public Texture2D CurrentTexture { get; set; }

    public TextureOutlineManager(ICanvasDisplayScale displayScale)
    {
        _displayScale = displayScale;
    }

    public void Initialize(SystemManagers systemManagers)
    {
        _textureOutlineRectangle = new LineRectangle(systemManagers);
        _textureOutlineRectangle.IsDotted = false;
        _textureOutlineRectangle.Color = Color.FromArgb(128, 255, 255, 255);
        _textureOutlineRectangle.Visible = false;
        systemManagers.ShapeManager.Add(_textureOutlineRectangle);
    }

    public void Refresh()
    {
        _textureOutlineRectangle.LinePixelWidth = _displayScale.DisplayScale;
        _textureOutlineRectangle.Visible = CurrentTexture != null;
        if (CurrentTexture != null)
        {
            _textureOutlineRectangle.Width = CurrentTexture.Width;
            _textureOutlineRectangle.Height = CurrentTexture.Height;
        }
    }
}
