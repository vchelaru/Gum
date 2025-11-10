using System.Numerics;
using RenderingLibrary.Graphics;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Matrix = System.Numerics.Matrix4x4;
using System;

namespace RenderingLibrary
{
    public interface IPositionedSizedObject
    {
        float X { get; set; }
        float Y { get; set; }
        float Z { get; set; }
        float Rotation { get; set; }
        bool FlipHorizontal { get; set; }
        float Width { get; set; }
        float Height { get; set; }
        string Name { get; set; }
        object? Tag { get; set; }
    }

    public static class IPositionedSizedObjectExtensionMethods
    {
        public static Matrix GetRotationMatrix(this IRenderableIpso ipso)
        {
            return Matrix.CreateRotationZ(-MathHelper.ToRadians(ipso.Rotation));
        }

        public static Matrix GetAbsoluteRotationMatrix(this IRenderableIpso ipso)
        {
            var flipHorizontal = ipso.GetAbsoluteFlipHorizontal();

            float rotationDegrees;
            if (flipHorizontal)
            {
                rotationDegrees = -ipso.GetAbsoluteRotation();
            }
            else
            {
                rotationDegrees = ipso.GetAbsoluteRotation();
            }

            return Matrix.CreateRotationZ(-MathHelper.ToRadians(rotationDegrees));
        }

        public static bool GetAbsoluteFlipHorizontal(this IRenderableIpso ipso)
        {
            var effectiveParentFlipHorizontal = ipso.Parent?.GetAbsoluteFlipHorizontal() ?? false;

            return ipso.FlipHorizontal != effectiveParentFlipHorizontal;
        }

        /// <summary>
        /// Returns the top-left world X coordinate of the argument RenderableIpso in screen space.
        /// </summary>
        /// <param name="ipso">The RenderableIpso to return the value for.</param>
        /// <returns>The world X coordinate.</returns>
        public static float GetAbsoluteX(this IRenderableIpso ipso)
        {
            if (ipso.Parent == null)
            {

                return ipso.X;
            }
            else
            {
                //var parentFlip = ipso.Parent.GetAbsoluteFlipHorizontal();

                //if (parentFlip)
                //{
                //    return
                //        -ipso.X - ipso.Width + 
                //        ipso.Parent.GetAbsoluteX() + ipso.Parent.Width;
                //}
                //else
                {
                    return ipso.X + ipso.Parent.GetAbsoluteX();
                }
            }
        }

        /// <summary>
        /// Returns the world Y coordinate of the argument RenderableIpso in screen space.
        /// </summary>
        /// <param name="ipso">The RenderableIpso to return the value for.</param>
        /// <returns>The world Y coordinate.</returns>
        public static float GetAbsoluteY(this IRenderableIpso ipso)
        {
            if (ipso.Parent == null)
            {
                return ipso.Y;
            }
            else
            {
                return ipso.Y + ipso.Parent.GetAbsoluteY();
            }
        }

        public static float GetAbsoluteLeft(this IRenderableIpso ipso)
        {
            return ipso.GetAbsoluteX();
        }

        public static float GetAbsoluteTop(this IRenderableIpso ipso)
        {
            return ipso.GetAbsoluteY();
        }

        public static float GetAbsoluteRight(this IRenderableIpso ipso)
        {
            return ipso.GetAbsoluteX() + ipso.Width;
        }

        public static float GetAbsoluteBottom(this IRenderableIpso ipso)
        {
            return ipso.GetAbsoluteY() + ipso.Height;
        }

        public static float GetAbsoluteCenterX(this IRenderableIpso ipso)
        {
            return ipso.GetAbsoluteX() + ipso.Width/2.0f;
        }

        public static float GetAbsoluteCenterY(this IRenderableIpso ipso)
        {
            return ipso.GetAbsoluteTop() + ipso.Height / 2.0f;
        }

        public static bool HasCursorOver(this IRenderableIpso ipso, float x, float y)
        {
            float absoluteX = ipso.GetAbsoluteX();
            float absoluteY = ipso.GetAbsoluteY();

            // put the cursor in object space:
            x -= absoluteX;
            y -= absoluteY;

            // normally it's negative, but we are going to * -1 to rotate the other way
            var matrix = Matrix.CreateRotationZ(-MathHelper.ToRadians(ipso.GetAbsoluteRotation()) * -1);

            var relativePosition = new Vector2(x, y);
            relativePosition = Vector2.Transform(relativePosition, matrix);

            bool isXInRange = false;
            if(ipso.Width < 0)
            {
                isXInRange = relativePosition.X <= 0 && relativePosition.X > ipso.Width;
            }
            else
            {
                isXInRange = relativePosition.X >= 0 && relativePosition.X < ipso.Width;
            }

            bool isYInRange = false;
            if(ipso.Height < 0)
            {
                isYInRange = relativePosition.Y <= 0 && relativePosition.Y > ipso.Height;
            }
            else
            {
                isYInRange = relativePosition.Y >= 0 && relativePosition.Y < ipso.Height;
            }

            return isXInRange && isYInRange;
        }


        /// <summary>
        /// Returns the topmost parent in the hierarchy, returning this if there is no parent.
        /// </summary>
        /// <param name="ipso">The argument ipso</param>
        /// <returns>The top parent</returns>
        public static IRenderableIpso GetTopParent(this IRenderableIpso ipso)
        {
            if (ipso.Parent == null)
            {
                return ipso;
            }
            else
            {
                return ipso.Parent.GetTopParent();
            }

        }

        /// <summary>
        /// Returns the absolute rotation in degrees.
        /// </summary>
        /// <param name="ipso">The object for which to return rotation.</param>
        /// <param name="ignoreParentRotationIfRenderTarget">If true, then the parent's rotation is ignored if the parent
        /// is a render target. This is needed because a child's rotation should not be modified by its parent if the parent
        /// is a render target since the rotation is automatically cascaded.</param>
        /// <returns>The rotation in degrees.</returns>
        public static float GetAbsoluteRotation(this IRenderableIpso ipso, bool ignoreParentRotationIfRenderTarget = false)
        {
            var thisRotation = ipso.Rotation;

            if(ignoreParentRotationIfRenderTarget && ipso.IsRenderTarget)
            {
                thisRotation = 0;
            }

            if (ipso.Parent == null)
            {

                return thisRotation;
            }
            else
            {
                return thisRotation + ipso.Parent.GetAbsoluteRotation(ignoreParentRotationIfRenderTarget);
            }
        }

        //public static void (this IPositionedSizedObject instance, IPositionedSizedObject newParent)
        //{
        //    instance.Parent = newParent;

        //}

        [Obsolete("Use X or Y since this method is a little ambiguous when working with GraphicalUiElements")]
        public static Vector2 GetPosition(this IPositionedSizedObject ipso) => new Vector2(ipso.X, ipso.Y);

        [Obsolete("Use X or Y since this method is a little ambiguous when working with GraphicalUiElements")]
        public static void SetPosition(this IPositionedSizedObject ipso, Vector2 newPosition)
        {
            ipso.X = newPosition.X;
            ipso.Y = newPosition.Y;
        }
    }
}
