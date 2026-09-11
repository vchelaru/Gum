using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;
using static WpfDataUi.Controls.ToggleButtonOptionDisplay;

namespace Gum.Controls
{
    /// <summary>
    /// Hosts a <see cref="ToggleButtonOptionDisplay"/> over one of the shared
    /// <see cref="VariableGridToggleOptions"/> sets; subclasses pick the set. The WPF display cannot be
    /// inherited from (its XAML is loaded by class name), so it is composed instead.
    /// </summary>
    public abstract class ToggleButtonOptionContainer : UserControl, IDataUi
    {
        #region Fields/Properties

        // One WPF option per shared option, so repeated lookups hand the display the same instances
        // and it keeps its buttons instead of rebuilding them.
        static readonly Dictionary<ToggleButtonOption, Option> _wpfOptions = new();

        ToggleButtonOptionDisplay internalDisplay;

        /// <summary>The shared option sets.</summary>
        protected static VariableGridToggleOptions ToggleOptions => Locator.GetRequiredService<VariableGridToggleOptions>();

        protected abstract ToggleButtonOption[] GetToggleOptions();

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

        public ApplyValueResult TryGetValueOnUi(out object? result)
        {
            return internalDisplay.TryGetValueOnUi(out result);
        }

        public ApplyValueResult TrySetValueOnUi(object value)
        {
            return internalDisplay.TrySetValueOnUi(value);
        }

        private Option[] GetOptions() => GetToggleOptions().Select(ToWpfOption).ToArray();

        private static Option ToWpfOption(ToggleButtonOption option)
        {
            if (!_wpfOptions.TryGetValue(option, out Option? wpfOption))
            {
                wpfOption = new Option
                {
                    Name = option.Name,
                    Value = option.Value,
                    GumIconName = option.GumIconName,
                    IconName = option.IconName,
                    Image = option.ImagePath != null ? CreateBitmapFromFile(option.ImagePath) : null!,
                };
                _wpfOptions[option] = wpfOption;
            }
            return wpfOption;
        }

        protected static BitmapImage CreateBitmapFromFile(string resourceName)
        {
            // Absolute, so the app doesn't look in the current directory, which could be outside the
            // Gum.exe location. AppContext.BaseDirectory already ends with a separator (and is correct
            // in single-file published apps, where Assembly.Location is empty).
            var relativeDirectory = AppContext.BaseDirectory.ToLower();

            resourceName = relativeDirectory + resourceName;

            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(resourceName, UriKind.Relative);
            bitmap.EndInit();
            // Accessing the Width property force loads the bitmap.
            var throwaway = bitmap.Width;
            return bitmap;
        }
    }

    class XUnitsControl : ToggleButtonOptionContainer
    {
        protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.XUnits;
    }

    class YUnitsControl : ToggleButtonOptionContainer
    {
        protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.YUnits;
    }

    class XOriginControl : ToggleButtonOptionContainer
    {
        protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.XOrigin;
    }

    class YOriginControl : ToggleButtonOptionContainer
    {
        protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.GetYOrigins();
    }

    class WidthUnitsControl : ToggleButtonOptionContainer
    {
        public WidthUnitsControl()
        {
            this.RefreshButtonsOnSelection = true;
        }

        protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.GetWidthUnits();
    }

    class HeightUnitsControl : ToggleButtonOptionContainer
    {
        public HeightUnitsControl()
        {
            this.RefreshButtonsOnSelection = true;
        }

        protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.GetHeightUnits();
    }

    public class TextHorizontalAlignmentControl : ToggleButtonOptionContainer
    {
        protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.TextHorizontalAlignment;
    }

    class TextVerticalAlignmentControl : ToggleButtonOptionContainer
    {
        protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.TextVerticalAlignment;
    }

    class ChildrenLayoutControl : ToggleButtonOptionContainer
    {
        protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.ChildrenLayout;
    }

    class TextOverflowHorizontalModeControl : ToggleButtonOptionContainer
    {
        protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.TextOverflowHorizontalMode;
    }

    class TextOverflowVerticalModeControl : ToggleButtonOptionContainer
    {
        protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.TextOverflowVerticalMode;
    }
}
