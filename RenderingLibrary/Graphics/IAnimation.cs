using Microsoft.Xna.Framework.Graphics;
using Rectangle = System.Drawing.Rectangle;

namespace RenderingLibrary.Graphics
{
    public interface IAnimation
    {
        bool FlipHorizontal
        {
            get;
        }

        bool FlipVertical
        {
            get;
        }

        Texture2D CurrentTexture
        {
            get;
        }

        Rectangle? SourceRectangle
        {
            get;
        }

        void AnimationActivity(double currentTime);

        int CurrentFrameIndex
        {
            get;
        }
    }
}
