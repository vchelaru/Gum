using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InputLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary;
using Microsoft.Xna.Framework;

namespace Gum.Input
{
    public static class CursorExtensionMethods
    {
        public static void TransformVector(ref Vector3 vectorToTransform, ref Matrix matrixToTransformBy)
        {

            Vector3 transformed = Vector3.Zero;

            transformed.X =
                matrixToTransformBy.M11 * vectorToTransform.X +
                matrixToTransformBy.M21 * vectorToTransform.Y +
                matrixToTransformBy.M31 * vectorToTransform.Z +
                matrixToTransformBy.M41;

            transformed.Y =
                matrixToTransformBy.M12 * vectorToTransform.X +
                matrixToTransformBy.M22 * vectorToTransform.Y +
                matrixToTransformBy.M32 * vectorToTransform.Z +
                matrixToTransformBy.M42;

            transformed.Z =
                matrixToTransformBy.M13 * vectorToTransform.X +
                matrixToTransformBy.M23 * vectorToTransform.Y +
                matrixToTransformBy.M33 * vectorToTransform.Z +
                matrixToTransformBy.M43;

            vectorToTransform = transformed;
        }

        public static float GetWorldX(this Cursor cursor, SystemManagers managers = null)
        {
            if (cursor.PrimaryDown)
            {
                int m = 3;
            }

            Renderer renderer = null;
            if (managers == null)
            {
                renderer = Renderer.Self;
            }
            else
            {
                renderer = managers.Renderer;
            }

            Vector3 transformed = new Vector3(cursor.X, cursor.Y, 0);
            Matrix matrix = renderer.Camera.GetTransformationMatrix();
            matrix = Matrix.Invert(matrix);

            TransformVector(ref transformed, ref matrix);


            return transformed.X;
        }

        public static float GetWorldY(this Cursor cursor, SystemManagers managers = null)
        {
            Vector3 transformed = new Vector3(cursor.X, cursor.Y, 0);

            Renderer renderer = null;
            if (managers == null)
            {
                renderer = Renderer.Self;
            }
            else
            {
                renderer = managers.Renderer;
            }


            Matrix matrix = renderer.Camera.GetTransformationMatrix();
            matrix = Matrix.Invert(matrix);

            TransformVector(ref transformed, ref matrix);


            return transformed.Y;

        }
    }
}
