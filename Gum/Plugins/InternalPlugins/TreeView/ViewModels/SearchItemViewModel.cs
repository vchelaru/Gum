using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using System;
using System.Windows.Media.Imaging;

namespace Gum.Plugins.InternalPlugins.TreeView.ViewModels
{
    public class SearchItemViewModel
    {

        public static BitmapImage ComponentIcon;
        public static BitmapImage ScreenIcon;
        public static BitmapImage StandardIcon;
        public static BitmapImage InstanceIcon;
        public static BitmapImage BehaviorIcon;

        static SearchItemViewModel()
        {
            ComponentIcon = LoadIcon("Component");
            ScreenIcon = LoadIcon("screen");
            StandardIcon = LoadIcon("StandardElement");
            InstanceIcon = LoadIcon("Instance");
            BehaviorIcon = LoadIcon("behavior");

            BitmapImage LoadIcon(string iconName)
            {
                var location = $"pack://application:,,,/Plugins/InternalPlugins/TreeView/Content/{iconName}.png";
                var bitmapImage = new BitmapImage(new Uri(location, UriKind.Absolute));
                return bitmapImage;
            }
        }

        public object BackingObject { get; set; }

        public string CustomText { get; set; }

        public string Display => !string.IsNullOrWhiteSpace(CustomText) ? CustomText : BackingObject?.ToString();

        public BitmapImage Image =>
            BackingObject is ScreenSave ? ScreenIcon
            : BackingObject is ComponentSave ? ComponentIcon
            : BackingObject is InstanceSave ? InstanceIcon
            : BackingObject is BehaviorSave ? BehaviorIcon
            : StandardIcon;

    }
}
