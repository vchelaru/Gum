using static WpfDataUi.Controls.ToggleButtonOptionDisplay;

namespace Gum.Controls
{
    class XOriginControl : ToggleButtonOptionContainer
    {
        static Option[] cachedOptions;

        protected override Option[] GetOptions()
        {
            if (cachedOptions == null)
            {
                cachedOptions = new Option[]
                {
                    new Option
                    {
                        Name = "Left",
                        Value = global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                        GumIconName = "XOriginStart"
                    },
                    new Option
                    {
                        Name = "Center",
                        Value = global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
                        GumIconName = "XOriginCenter"
                    },
                    new Option
                    {
                        Name = "Right",
                        Value = global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                        GumIconName = "XOriginEnd"
                    }
                };
            }

            return cachedOptions;
        }
    }
}
