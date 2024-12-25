using System.Numerics;

namespace ToolsUtilitiesStandard.Helpers 
{
	public static class Matrix4x4Extensions 
	{
		public static Vector3 Up(this Matrix4x4 matrix) {
			Vector3 up;
			up.X = matrix.M21;
			up.Y = matrix.M22;
			up.Z = matrix.M23;
			return up;
		}
		public static Vector3 Down(this Matrix4x4 matrix) {
			Vector3 down;
			down.X = -matrix.M21;
			down.Y = -matrix.M22;
			down.Z = -matrix.M23;
			return down;
		}
		public static Vector3 Right(this Matrix4x4 matrix) {
			Vector3 right;
			right.X = matrix.M11;
			right.Y = matrix.M12;
			right.Z = matrix.M13;
			return right;
		}
		public static Vector3 Left(this Matrix4x4 matrix) {
			Vector3 left;
			left.X = -matrix.M11;
			left.Y = -matrix.M12;
			left.Z = -matrix.M13;
			return left;
		}
	}
}