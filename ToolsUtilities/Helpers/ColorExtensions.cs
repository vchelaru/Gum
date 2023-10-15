using System.Drawing;

namespace ToolsUtilitiesStandard.Helpers {
	public static class ColorExtensions {
		public static Color WithAlpha(this Color color, byte value) {
			return Color.FromArgb(value, color.R, color.G, color.B);
		}

		public static Color WithRed(this Color color, byte value) {
			return Color.FromArgb(color.A, value, color.G, color.B);
		}

		public static Color WithGreen(this Color color, byte value) {
			return Color.FromArgb(color.A, color.R, value, color.B);
		}

		public static Color WithBlue(this Color color, byte value) {
			return Color.FromArgb(color.A, color.R, color.G, value);
		}
	}
}