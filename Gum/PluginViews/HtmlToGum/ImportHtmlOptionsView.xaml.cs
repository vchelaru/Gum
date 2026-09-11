using System.Windows.Controls;
using Gum.Services.Dialogs;
using HtmlToGumPlugin;

namespace Gum.PluginViews.HtmlToGum;

/// <summary>The WPF view of the Import HTML options dialog.</summary>
[Dialog(typeof(ImportHtmlOptionsViewModel))]
public partial class ImportHtmlOptionsView : UserControl
{
    public ImportHtmlOptionsView()
    {
        InitializeComponent();
    }
}
