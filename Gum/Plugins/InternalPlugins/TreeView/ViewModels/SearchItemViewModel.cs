using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using System;
using System.Windows.Media.Imaging;

namespace Gum.Plugins.InternalPlugins.TreeView.ViewModels
{
    public class SearchItemViewModel
    {
        public object BackingObject { get; set; }

        public string CustomText { get; set; }

        public string Display => !string.IsNullOrWhiteSpace(CustomText) ? CustomText : BackingObject?.ToString();

    }
}
