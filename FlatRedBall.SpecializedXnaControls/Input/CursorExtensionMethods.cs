using RenderingLibrary;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Matrix = System.Numerics.Matrix4x4;

namespace InputLibrary
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


        public static void TransformVector(ref Vector2 vectorToTransform, ref Matrix matrixToTransformBy)
        {
            Vector2 transformed = Vector2.Zero;

            transformed.X =
                matrixToTransformBy.M11 * vectorToTransform.X +
                matrixToTransformBy.M21 * vectorToTransform.Y;

            transformed.Y =
                matrixToTransformBy.M12 * vectorToTransform.X +
                matrixToTransformBy.M22 * vectorToTransform.Y;

            vectorToTransform.X = transformed.X;
            vectorToTransform.Y = transformed.Y;
        }


        public static float GetWorldX(this Cursor cursor, SystemManagers managers)
        {
            Vector3 transformed = new Vector3(cursor.X, cursor.Y, 0);
            Matrix matrix = managers.Renderer.Camera.GetTransformationMatrix();
            Matrix.Invert(matrix, out matrix);

            TransformVector(ref transformed, ref matrix);


            return transformed.X;
        }

        public static float GetWorldY(this Cursor cursor, SystemManagers managers)
        {
            Vector3 transformed = new Vector3(cursor.X, cursor.Y, 0);
            Matrix matrix = managers.Renderer.Camera.GetTransformationMatrix();
            Matrix.Invert(matrix, out matrix);

            TransformVector(ref transformed, ref matrix);


            return transformed.Y;

        }
    }
}
