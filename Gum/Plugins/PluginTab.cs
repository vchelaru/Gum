using System;
using Gum.Commands;
using Gum.Services;

namespace Gum.Plugins
{
    public class PluginTab
    {
        private readonly GuiCommands _guiCommands;
        
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

        public PluginTab()
        {
            _guiCommands = Locator.GetRequiredService<GuiCommands>();
        }

        private void HandleMiddleMouseClicked()
        {
            if(CanClose)
            {
                Hide();
            }
        }

        public void Show(bool focus = true) => _guiCommands.ShowTab(this, focus);
        public void Hide() => _guiCommands.HideTab(this);

        public void RaiseTabShown() => TabShown?.Invoke();
        public event Action TabShown;

        public void RaiseTabHidden() => TabHidden?.Invoke();
        public event Action TabHidden;


        public void Focus() => TabItem.Focus();

        public bool IsFocused =>
            _guiCommands.IsTabFocused(this);

        public bool CanClose { get; set; }
    }
}
