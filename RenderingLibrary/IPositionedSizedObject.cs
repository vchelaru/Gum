using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RenderingLibrary
{
    public interface IPositionedSizedObject
    {
        float X { get; set; }
        float Y { get; set; }
        float Z { get; set; }
        float Rotation { get; set; }
        float Width { get; set; }
        float Height { get; set; }
        string Name { get; set; }
        IPositionedSizedObject Parent { get; set; }
        List<IPositionedSizedObject> Children{ get; }
        void SetParentDirect(IPositionedSizedObject newParent);
        object Tag { get; set; }
    }

    public static class IPositionedSizedObjectExtensionMethods
    {
        public static Matrix GetRotationMatrix(this IPositionedSizedObject ipso)
        {
            return Matrix.CreateRotationZ(-MathHelper.ToRadians(ipso.Rotation));
        }
        
        public static float GetAbsoluteX(this IPositionedSizedObject ipso)
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

        public static float GetAbsoluteY(this IPositionedSizedObject ipso)
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

        public static float GetAbsoluteLeft(this IPositionedSizedObject ipso)
        {
            return ipso.GetAbsoluteX();
        }

        public static float GetAbsoluteTop(this IPositionedSizedObject ipso)
        {
            return ipso.GetAbsoluteY();
        }

        public static float GetAbsoluteRight(this IPositionedSizedObject ipso)
        {
            return ipso.GetAbsoluteX() + ipso.Width;
        }

        public static float GetAbsoluteBottom(this IPositionedSizedObject ipso)
        {
            return ipso.GetAbsoluteY() + ipso.Height;
        }

        public static bool HasCursorOver(this IPositionedSizedObject ipso, float x, float y)
        {
            float absoluteX = ipso.GetAbsoluteX();
            float absoluteY = ipso.GetAbsoluteY();

            // put the cursor in object space:
            x -= absoluteX;
            y -= absoluteY;

            // normally it's negative, but we are going to * -1 to rotate the other way
            var matrix = Matrix.CreateRotationZ(-MathHelper.ToRadians(ipso.Rotation) * -1);

            var relativePosition = new Vector2(x, y);
            relativePosition = Vector2.Transform(relativePosition, matrix);
            return
                relativePosition.X > 0 && relativePosition.Y > 0 && relativePosition.X < ipso.Width && relativePosition.Y < ipso.Height;
        }


        public static IPositionedSizedObject GetTopParent(this IPositionedSizedObject ipso)
        {
            if (ipso.Parent == null)
            {
                return ipso;
            }
            else
            {
                return ipso.Parent;
            }

        }



        //public static void (this IPositionedSizedObject instance, IPositionedSizedObject newParent)
        //{
        //    instance.Parent = newParent;

        //}
    }
}
