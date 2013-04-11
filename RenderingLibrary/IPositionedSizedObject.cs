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
        float Width { get; set; }
        float Height { get; set; }
        string Name { get; set; }
        IPositionedSizedObject Parent { get; set; }
        ICollection<IPositionedSizedObject> Children{ get; }
        object Tag { get; set; }
    }

    public static class IPositionedSizedObjectExtensionMethods
    {
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


        public static bool HasCursorOver(this IPositionedSizedObject ipso, float x, float y)
        {
            float absoluteX = ipso.GetAbsoluteX();
            float absoluteY = ipso.GetAbsoluteY();

            return
                x > absoluteX && y > absoluteY && x < absoluteX + ipso.Width && y < absoluteY + ipso.Height;
        }

        public static string GetAttachmentQualifiedName(this IPositionedSizedObject ipso)
        {
            if (ipso.Parent == null)
            {
                return ipso.Name;
            }
            else
            {
                return ipso.Parent.GetAttachmentQualifiedName() + "." + ipso.Name;
            }

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
