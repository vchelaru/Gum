namespace RenderingLibrary.Graphics {
	public static class XNAExtensions {
		public static Microsoft.Xna.Framework.Color ToXNA(this System.Drawing.Color value) {
			return new Microsoft.Xna.Framework.Color(value.R, value.G, value.B, value.A);
		}

		public static Microsoft.Xna.Framework.Point ToXNA(this System.Drawing.Point value) {
			return new Microsoft.Xna.Framework.Point(value.X, value.Y);
		}

		public static Microsoft.Xna.Framework.Rectangle ToXNA(this System.Drawing.Rectangle value) {
			return new Microsoft.Xna.Framework.Rectangle(value.X, value.Y, value.Width, value.Height);
		}

		public static Microsoft.Xna.Framework.Vector2 ToXNA(this System.Numerics.Vector2 value) {
			return new Microsoft.Xna.Framework.Vector2(value.X, value.Y);
		}

		public static Microsoft.Xna.Framework.Vector3 ToXNA(this System.Numerics.Vector3 value) {
			return new Microsoft.Xna.Framework.Vector3(value.X, value.Y, value.Z);
		}

		public static Microsoft.Xna.Framework.Matrix ToXNA(this System.Numerics.Matrix4x4 value) {
			return new Microsoft.Xna.Framework.Matrix(
				value.M11, value.M12, value.M13, value.M14,
				value.M21, value.M22, value.M23, value.M24,
				value.M31, value.M32, value.M33, value.M34,
				value.M41, value.M42, value.M43, value.M44
			);
		}
	}
}