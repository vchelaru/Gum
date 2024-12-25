using System;

namespace ToolsUtilitiesStandard.Helpers 
{
	public static class MathHelper {
		public const float PiOver2 = (float)(Math.PI / 2);
		public const float PiOver4 = (float)(Math.PI / 4);

		/// <summary>Converts degrees to radians.</summary>
		/// <param name="degrees">The angle in degrees.</param>
		public static float ToRadians(float degrees) => degrees * ((float) Math.PI / 180f);

		/// <summary>Converts radians to degrees.</summary>
		/// <param name="radians">The angle in radians.</param>
		public static float ToDegrees(float radians) => radians * 57.295776f;
	}
}