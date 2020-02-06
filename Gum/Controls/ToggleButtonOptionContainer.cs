using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    /// to create custom toggle button option views. See remarks for details on why
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

        public bool RefreshButtonsOnSelection
        {
            get; set;
        } = false;

        public InstanceMember InstanceMember
        {
            get
            {
                return internalDisplay.InstanceMember;
            }
            set
            {
                if(internalDisplay.InstanceMember != value)
                {
                    internalDisplay.InstanceMember = value;

                    if(RefreshButtonsOnSelection)
                    {
                        Refresh();
                    }
                }
            }
        }

        private void RefreshButtons()
        {
            internalDisplay.RefreshButtonFromOptions(GetOptions());
        }

        public bool SuppressSettingProperty
        {
            get
            {
                return internalDisplay.SuppressSettingProperty;
            }
            set
            {
                internalDisplay.SuppressSettingProperty = value;
            }
        }

        #endregion

        public ToggleButtonOptionContainer()
        {
            internalDisplay = new ToggleButtonOptionDisplay(GetOptions());
            this.AddChild(internalDisplay);
        }

        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {
            if(RefreshButtonsOnSelection)
            {
                RefreshButtons();
            }
            internalDisplay.Refresh(forceRefreshEvenIfFocused);

        }

        public ApplyValueResult TryGetValueOnUi(out object result)
        {
            return internalDisplay.TryGetValueOnUi(out result);
        }

        public ApplyValueResult TrySetValueOnUi(object value)
        {
            return internalDisplay.TrySetValueOnUi(value);
        }

        protected static StandardElementSave GetRootElement()
        {
            StandardElementSave rootElement = null;

            if (SelectedState.Self.SelectedInstance != null)
            {
                rootElement =
                    ObjectFinder.Self.GetRootStandardElementSave(SelectedState.Self.SelectedInstance);
            }
            else if (SelectedState.Self.SelectedElement != null)
            {
                rootElement =
                    ObjectFinder.Self.GetRootStandardElementSave(SelectedState.Self.SelectedElement);
            }

            return rootElement;
        }

        protected static BitmapImage CreateBitmapFromFile(string resourceName)
        {
            // make it absolute so the app doesn't look for the files in the current directory, 
            // which could be outside the Gum.exe location
            var relativeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).ToLower() + "/";

            resourceName = relativeDirectory + resourceName;

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
