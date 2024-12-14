using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Gum.Plugins;
internal class PluginTabItem : TabItem
{
    public event EventHandler MiddleMouseClicked;
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        if(e.MiddleButton == MouseButtonState.Pressed)
        {
            MiddleMouseClicked?.Invoke(this, EventArgs.Empty);
        }
        base.OnMouseDown(e);
    }
}
