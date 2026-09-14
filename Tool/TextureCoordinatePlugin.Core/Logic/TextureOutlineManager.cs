using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Math.Geometry;
using Color = System.Drawing.Color;

namespace TextureCoordinateSelectionPlugin.Logic;

public class TextureOutlineManager : IVisualOverlayManager
{
    private LineRectangle _textureOutlineRectangle;
    private SystemManagers _systemManagers;

    public Texture2D CurrentTexture { get; set; }

    public void Initialize(SystemManagers systemManagers)
    {
        _systemManagers = systemManagers;
    }

    public void Refresh()
    {
        var shouldShowOutline = CurrentTexture != null;
        if (shouldShowOutline)
        {
            if (_textureOutlineRectangle == null)
            {
                _textureOutlineRectangle = new LineRectangle(_systemManagers);
                _textureOutlineRectangle.IsDotted = false;
                _textureOutlineRectangle.Color = Color.FromArgb(128, 255, 255, 255);
                _systemManagers.ShapeManager.Add(_textureOutlineRectangle);
            }
            _textureOutlineRectangle.Width = CurrentTexture.Width;
            _textureOutlineRectangle.Height = CurrentTexture.Height;
            _textureOutlineRectangle.Visible = true;
        }
        else
        {
            if (_textureOutlineRectangle != null)
            {
                _textureOutlineRectangle.Visible = false;
            }
        }
    }
}
