using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

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
        object Tag { get; set; }
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
        /// Returns the world X coordinate of the argument RenderableIpso.
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
                return ipso.X + ipso.Parent.GetAbsoluteX();
            }
        }

        /// <summary>
        /// Returns the world Y coordinate of the argument RenderableIpso.
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
            return ipso.GetAbsoluteX() + ipso.Width / 2.0f;
        }

        public static float GetAbsoluteCenterY(this IRenderableIpso ipso)
        {
            return ipso.GetAbsoluteTop() + ipso.Height / 2.0f;
        }

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

        public static float GetAbsoluteRotation(this IRenderableIpso ipso)
        {

            if (ipso.Parent == null)
            {
                return ipso.Rotation;
            }
            else
            {
                return ipso.Rotation + ipso.Parent.GetAbsoluteRotation();
            }
        }

    }

}
