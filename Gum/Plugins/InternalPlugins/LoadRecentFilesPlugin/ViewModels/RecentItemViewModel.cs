using Gum.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.ViewModels
{
    internal class RecentItemViewModel : ViewModel
    {
        public string FullPath
        {
            get => Get<string>();
            set => Set(value);
        }

        [DependsOn(nameof(FullPath))]
        public string StrippedName => !string.IsNullOrEmpty(FullPath)
            ? FileManager.RemovePath(FullPath)
            : "";

        public bool IsFavorite
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsFavorite))]
        public BitmapImage FavoriteImage
        {
            get
            {
                string sourceName;
                if (IsFavorite)
                {
                    sourceName = "/Content/Icons/RecentFiles/StarFilled.png";
                }
                else
                {
                    sourceName = "/Content/Icons/RecentFiles/StarOutline.png";
                }

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(sourceName, UriKind.Relative);
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
    }
}
