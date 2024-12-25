using System.Numerics;

namespace ToolsUtilitiesStandard.Helpers 
{
	public static class Vector3Extensions {
		private static readonly Vector3 _up = new Vector3(0.0f, 1f, 0.0f);
		private static readonly Vector3 _down = new Vector3(0.0f, -1f, 0.0f);
		private static readonly Vector3 _right = new Vector3(1f, 0.0f, 0.0f);
		private static readonly Vector3 _left = new Vector3(-1f, 0.0f, 0.0f);
		private static readonly Vector3 _forward = new Vector3(0.0f, 0.0f, -1f);
		private static readonly Vector3 _backward = new Vector3(0.0f, 0.0f, 1f);

		public static Vector3 Up => _up;
		public static Vector3 Down => _down;
		public static Vector3 Left => _left;
		public static Vector3 Right => _right;
		public static Vector3 Forward => _forward;
		public static Vector3 Backward => _backward;

		public static Vector2 ToVector2(this Vector3 vector3) =>
			new Vector2(vector3.X, vector3.Y);
	}
}