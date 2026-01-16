using Gum.Managers;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe
{
    public partial class GraphicalUiElement
    {
        #region AnimationChain 

#if !FRB
        /// <summary>
        /// Updates the current animation state based on the elapsed time.
        /// </summary>
        /// <param name="secondDifference">The time elapsed since the last update, in seconds.</param>
        private void RunAnimation(double secondDifference)
        {
            if (currentAnimation != null)
            {
                currentAnimationTime += secondDifference;
                currentAnimation.ApplyAtTimeTo(currentAnimationTime, this);
                if (!currentAnimation.Loops && currentAnimationTime >= currentAnimation.Length)
                {
                    currentAnimation = null;
                }
            }
        }
#endif

        /// <summary>
        /// Performs AnimationChain (.achx) animation on this and all children recurisvely.
        /// This is typically called on the top-level object (usually Screen) when Gum is running
        /// in a game.
        /// </summary>
        public void AnimateSelf(double secondDifference)
        {
            var asSprite = mContainedObjectAsIpso as ITextureCoordinate;
            var asAnimatable = mContainedObjectAsIpso as IAnimatable;
            //////////////////Early Out/////////////////////
            // Check mContainedObjectAsIVisible - if it's null, then this is a Screen and we should animate it

            // December 6, 2023 - Not sure why this was added here
            // but by checking if this is null, we skip animating screens
            // which breaks recursive animations. We need to early out only
            // if the contained object is not null.
            //if(asSprite== null || asAnimatable == null)
            //{
            //    return;
            //}

            if (mContainedObjectAsIVisible != null && Visible == false)
            {
                return;
            }
            ////////////////End Early Out///////////////////
#if !FRB
            RunAnimation(secondDifference);
#endif

            var didSpriteUpdate = asAnimatable?.AnimateSelf(secondDifference) ?? false;

            if (didSpriteUpdate)
            {
                // update this texture coordinates:

                UpdateTextureValuesFrom(asSprite!);
            }

            if (Children != null)
            {
                for (int i = 0; i < this.Children.Count; i++)
                {
                    var child = this.Children[i];
                    child.AnimateSelf(secondDifference);

                }
            }
            else
            {
                for (int i = 0; i < this.mWhatThisContains.Count; i++)
                {
                    var child = mWhatThisContains[i];
                    child.AnimateSelf(secondDifference);

                }
            }
        }

        public void UpdateTextureValuesFrom(ITextureCoordinate asSprite)
        {
            // suspend layouts while we do this so that previou values don't apply:
            var isSuspended = this.IsLayoutSuspended;
            this.SuspendLayout();

            // The AnimationChain (source file) could get set before the name desired name is set, so tolerate 
            // if there's a missing source rectangle:
            if (asSprite.SourceRectangle != null)
            {
                this.TextureLeft = asSprite.SourceRectangle.Value.Left;
                this.TextureWidth = asSprite.SourceRectangle.Value.Width;

                this.TextureTop = asSprite.SourceRectangle.Value.Top;
                this.TextureHeight = asSprite.SourceRectangle.Value.Height;
            }

            this.FlipHorizontal = asSprite.FlipHorizontal;

            if (this.TextureAddress == TextureAddress.EntireTexture)
            {
                this.TextureAddress = TextureAddress.Custom; // If it's not custom, then the animation chain won't apply. I think we should force this.
            }
            if (isSuspended == false)
            {
                this.ResumeLayout();
            }
        }

        #endregion
    }
}
