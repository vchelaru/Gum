using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        System.Windows.Controls.TabItem page;
        internal System.Windows.Controls.TabItem Page
        {
            get => page;
            set
            {
                if (page != value)
                {
                    page = value;
                    //page.TabSelected = RaiseTabShown;
                }
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



        public void RaiseTabShown() => TabShown?.Invoke();

        public event Action TabShown;

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

        //public void Focus()
        //{
        //    Page.Focus();
        //    Page.LastTimeClicked = DateTime.Now;
        //}

        //public bool CanClose
        //{
        //    get => Page.DrawX;
        //    set => Page.DrawX = value;
        //}

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
