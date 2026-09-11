using FlatRedBall.SpecializedXnaControls.RegionSelection;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Color = System.Drawing.Color;

namespace TextureCoordinateSelectionPlugin.Logic;

public class NineSliceGuideManager : IVisualOverlayManager
{
    // [0] - left vertical line
    // [1] - right vertical line
    // [2] - top horizontal line
    // [3] - bottom horizontal line
    private readonly Line[] _nineSliceGuideLines = new Line[4];

    public bool ShowGuides { get; set; }
    public Texture2D CurrentTexture { get; set; }
    public RectangleSelector Selector { get; set; }
    public float? CustomFrameWidth { get; set; }

    public void Initialize(SystemManagers systemManagers)
    {
        for (int i = 0; i < 4; i++)
        {
            _nineSliceGuideLines[i] = new Line(systemManagers);
            _nineSliceGuideLines[i].Visible = false;
            _nineSliceGuideLines[i].Z = 1;
            _nineSliceGuideLines[i].Color = Color.White;
            _nineSliceGuideLines[i].IsDotted = true;

            var alpha = (int)(0.6f * 0xFF);

            _nineSliceGuideLines[i].Color =
                Color.FromArgb(alpha, alpha, alpha, alpha);

            systemManagers.Renderer.MainLayer.Add(_nineSliceGuideLines[i]);
        }
    }

    public void Refresh()
    {
        for (int i = 0; i < 4; i++)
        {
            _nineSliceGuideLines[i].Visible = ShowGuides;
        }

        // todo - this hasn't been tested extensively to make sure it aligns
        // pixel-perfect with how NineSlices work, but it's a good initial guess
        if (ShowGuides && CurrentTexture != null)
        {
            var texture = CurrentTexture;

            var textureWidth = texture.Width;
            var textureHeight = texture.Height;

            float left = 0;
            float top = 0;
            float right = CurrentTexture.Width;
            float bottom = CurrentTexture.Height;

            float width = CurrentTexture.Width;
            float height = CurrentTexture.Height;

            if (Selector != null)
            {
                left = Selector.Left;
                right = Selector.Right;
                top = Selector.Top;
                bottom = Selector.Bottom;

                width = Selector.Width;
                height = Selector.Height;
            }

            var guideLeft = left + width / 3.0f;
            var guideRight = left + width * 2.0f / 3.0f;
            var guideTop = top + height / 3.0f;
            var guideBottom = top + height * 2.0f / 3.0f;

            if (CustomFrameWidth != null)
            {
                guideLeft = left + CustomFrameWidth.Value;
                guideRight = right - CustomFrameWidth.Value;
                guideTop = top + CustomFrameWidth.Value;
                guideBottom = bottom - CustomFrameWidth.Value;
            }

            var leftLine = _nineSliceGuideLines[0];
            leftLine.X = guideLeft;
            leftLine.Y = top;
            leftLine.RelativePoint.X = 0;
            leftLine.RelativePoint.Y = bottom - top;

            var rightLine = _nineSliceGuideLines[1];
            rightLine.X = guideRight;
            rightLine.Y = top;
            rightLine.RelativePoint.X = 0;
            rightLine.RelativePoint.Y = bottom - top;

            var topLine = _nineSliceGuideLines[2];
            topLine.X = left;
            topLine.Y = guideTop;
            topLine.RelativePoint.X = right - left;
            topLine.RelativePoint.Y = 0;

            var bottomLine = _nineSliceGuideLines[3];
            bottomLine.X = left;
            bottomLine.Y = guideBottom;
            bottomLine.RelativePoint.X = right - left;
            bottomLine.RelativePoint.Y = 0;
        }
    }
}
