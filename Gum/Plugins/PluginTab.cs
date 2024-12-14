using System;

namespace Gum.Plugins
{
    public class PluginTab
    {
        public string Title
        {
            get => (string)Page.Header;
            set => Page.Header = value;
        }

        public TabLocation SuggestedLocation
        {
            get; set;
        } = TabLocation.RightBottom;

        PluginTabItem page;
        internal PluginTabItem Page
        {
            get => page;
            set
            {
                if (page != value)
                {
                    page = value;

                    if(page != null)
                    {
                        page.MiddleMouseClicked += (_, _) => HandleMiddleMouseClicked();
                    }


                    //page.TabSelected = RaiseTabShown;
                }
            }
        }

        private void HandleMiddleMouseClicked()
        {
            if(CanClose)
            {
                Hide();
            }
        }

        //TabPage page;
        //internal TabPage Page
        //{
        //    get => page;
        //    set
        //    {
        //        if (page != value)
        //        {
        //            page = value;
        //            //page.TabSelected = RaiseTabShown;
        //        }
        //    }
        //}

        public void Show() => GumCommands.Self.GuiCommands.ShowTab(this);
        public void Hide() => GumCommands.Self.GuiCommands.HideTab(this);

        public void RaiseTabShown() => TabShown?.Invoke();
        public event Action TabShown;

        public void RaiseTabHidden() => TabHidden?.Invoke();
        public event Action TabHidden;

        //public void Hide()
        //{
        //    var items = Page.ParentTabControl as ObservableCollection<PluginTabPage>;
        //    items?.Remove(Page);
        //    Page.ParentTabControl = null;

        //}

        //public void Show()
        //{
        //    if (Page.ParentTabControl == null)
        //    {
        //        var items = PluginBase.GetTabContainerFromLocation(SuggestedLocation);
        //        items.Add(Page);
        //        Page.ParentTabControl = items;
        //    }
        //}

        public void Focus()
        {
            Page.Focus();
        }

        public bool CanClose { get; set; }

        //public void ForceLocation(TabLocation tabLocation)
        //{
        //    var desiredTabControl = PluginBase.GetTabContainerFromLocation(SuggestedLocation);
        //    var parentTabControl = Page.ParentTabControl as ObservableCollection<PluginTabPage>;

        //    if (desiredTabControl != parentTabControl)
        //    {
        //        if (parentTabControl != null)
        //        {
        //            desiredTabControl.Remove(Page);
        //        }

        //        parentTabControl.Add(Page);
        //        Page.ParentTabControl = desiredTabControl;
        //    }
        //}
    }
}
