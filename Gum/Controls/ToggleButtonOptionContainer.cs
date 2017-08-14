using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;
using static WpfDataUi.Controls.ToggleButtonOptionDisplay;

namespace Gum.Controls
{
    /// <summary>
    /// A container for ToggleButtonOptionDisplay, which can be extended
    /// to create custom toggle button option views. See remakrs for details on why
    /// this exists.
    /// </summary>
    /// <remarks>
    /// The WpfDataUi project contains the core implementation class
    /// ToggleButtonOptionDisplay. This class handles most of the implementation
    /// of the IDataUi interface. Originally I wrote it with the intent of inheriting
    /// from it. However, it seems like that's not easy to do with the way WPF works according
    /// to this StackOverflow post:
    /// https://stackoverflow.com/questions/7646331/the-component-does-not-have-a-resource-identified-by-the-uri
    /// One of the answers suggested has-a instead of inheritance, so that's what I decided to do.
    /// Therefore, this class contains an instance of the ToggleButtonOptionDisplay, but it exists in 
    /// the Gum project. It can be inherited from to do specific implementations
    /// </remarks>
    public abstract class ToggleButtonOptionContainer : UserControl, IDataUi
    {
        #region Fields/Properties
        ToggleButtonOptionDisplay internalDisplay;

        protected abstract Option[] GetOptions();

        public InstanceMember InstanceMember
        {
            get => internalDisplay.InstanceMember; set => internalDisplay.InstanceMember = value;
        }
        public bool SuppressSettingProperty
        {
            get => internalDisplay.SuppressSettingProperty;
            set => internalDisplay.SuppressSettingProperty = value;
        }

        #endregion

        public ToggleButtonOptionContainer()
        {
            internalDisplay = new ToggleButtonOptionDisplay(GetOptions());
            this.AddChild(internalDisplay);
        }

        public void Refresh(bool forceRefreshEvenIfFocused = false) => internalDisplay.Refresh(forceRefreshEvenIfFocused);

        public ApplyValueResult TryGetValueOnUi(out object result)
        {
            return internalDisplay.TryGetValueOnUi(out result);
        }

        public ApplyValueResult TrySetValueOnUi(object value) => internalDisplay.TrySetValueOnUi(value);



        protected static BitmapImage CreateBitmapFromFile(string resourceName)
        {
            BitmapImage centerAlignBitmap = new BitmapImage();
            centerAlignBitmap.BeginInit();
            centerAlignBitmap.UriSource = new Uri(resourceName, UriKind.Relative);
            centerAlignBitmap.EndInit();
            // Accessing the Width property force loads the bitmap.
            var throwaway = centerAlignBitmap.Width;
            return centerAlignBitmap;
        }
    }
}
