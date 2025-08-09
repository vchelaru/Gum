using System;
using Gum.Commands;
using Gum.Managers;
using Gum.Services;

namespace Gum.Plugins
{
    public class PluginTab
    {
        private readonly ITabManager _tabManager;
        
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
            _tabManager = Locator.GetRequiredService<ITabManager>();
        }

        private void HandleMiddleMouseClicked()
        {
            if(CanClose)
            {
                Hide();
            }
        }

        public void Show(bool focus = true) => _tabManager.ShowTab(this, focus);
        public void Hide() => _tabManager.HideTab(this);

        public void RaiseTabShown() => TabShown?.Invoke();
        public event Action TabShown;

        public void RaiseTabHidden() => TabHidden?.Invoke();
        public event Action TabHidden;


        public void Focus() => TabItem.Focus();

        public bool IsFocused =>
            _tabManager.IsTabFocused(this);

        public bool CanClose { get; set; }
    }
}
