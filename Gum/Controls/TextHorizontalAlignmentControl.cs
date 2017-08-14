using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;
using static WpfDataUi.Controls.ToggleButtonOptionDisplay;

namespace Gum.Controls
{
    public class TextHorizontalAlignmentControl : UserControl, IDataUi
    {



        ToggleButtonOptionDisplay internalDisplay;

        public InstanceMember InstanceMember
        {
            get => internalDisplay.InstanceMember; set => internalDisplay.InstanceMember = value;
        }
        public bool SuppressSettingProperty
        { get => internalDisplay.SuppressSettingProperty;
            set => internalDisplay.SuppressSettingProperty = value; }

        static Option[] cachedOptions;

        private Option[] GetOptions()
        {
            if(cachedOptions == null)
            {
                BitmapImage centerAlignBitmap = CreateBitmapFromResource("Content/Icons/Alignment/CenterAlign.png");
                BitmapImage leftAlignBitmap = CreateBitmapFromResource("Content/Icons/Alignment/LeftAlign.png");
                BitmapImage rightAlignBitmap = CreateBitmapFromResource("Content/Icons/Alignment/RightAlign.png");

                cachedOptions = new Option[]
                {
                    new Option
                    {
                        Name = "Left",
                        Value = global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                        Image = leftAlignBitmap

                    },
                    new Option
                    {
                        Name = "Center",
                        Value = global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
                        Image = centerAlignBitmap
                    },
                    new Option
                    {
                        Name = "Right",
                        Value = global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                        Image = rightAlignBitmap
                    },
                };
            }

            return cachedOptions;
        }

        private static BitmapImage CreateBitmapFromResource(string resourceName)
        {
            BitmapImage centerAlignBitmap = new BitmapImage();
            centerAlignBitmap.BeginInit();
            centerAlignBitmap.UriSource = new Uri(resourceName, UriKind.Relative);
            centerAlignBitmap.EndInit();
            // force load it:
            var throwaway = centerAlignBitmap.Width;
            return centerAlignBitmap;
        }

        public void Refresh(bool forceRefreshEvenIfFocused = false) => internalDisplay.Refresh(forceRefreshEvenIfFocused);

        public ApplyValueResult TryGetValueOnUi(out object result)
        {
            return internalDisplay.TryGetValueOnUi(out result);
        }

        public ApplyValueResult TrySetValueOnUi(object value) => internalDisplay.TrySetValueOnUi(value);

        public TextHorizontalAlignmentControl()
        {
            

            internalDisplay = new ToggleButtonOptionDisplay(GetOptions());
            this.AddChild(internalDisplay);
        }
    }
}
