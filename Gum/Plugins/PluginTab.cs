using System;

namespace Gum.Plugins
{
    public class PluginTab
    {
        public string Title
        {
            get => (string)TabItem.Header;
            set => TabItem.Header = value;
        }

        public TabLocation SuggestedLocation
        {
            get; set;
        } = TabLocation.RightBottom;

        PluginTabItem tabItem;
        internal PluginTabItem TabItem
        {
            get => tabItem;
            set
            {
                if (tabItem != value)
                {
                    tabItem = value;

                    if(tabItem != null)
                    {
                        tabItem.MiddleMouseClicked += (_, _) => HandleMiddleMouseClicked();
                        tabItem.GotFocus += (_, _) => GotFocus?.Invoke();
                    }
                }
            }
        }
        public event Action GotFocus;

        private void HandleMiddleMouseClicked()
        {
            if(CanClose)
            {
                Hide();
            }
        }

        public void Show(bool focus = true) => GumCommands.Self.GuiCommands.ShowTab(this, focus);
        public void Hide() => GumCommands.Self.GuiCommands.HideTab(this);

        public void RaiseTabShown() => TabShown?.Invoke();
        public event Action TabShown;

        public void RaiseTabHidden() => TabHidden?.Invoke();
        public event Action TabHidden;


        public void Focus() => TabItem.Focus();

        public bool IsFocused =>
            GumCommands.Self.GuiCommands.IsTabFocused(this);

        public bool CanClose { get; set; }
    }
}
