using System.Windows.Controls;
using Gum.Services.Dialogs;
using HtmlToGumPlugin;

namespace Gum.PluginViews.HtmlToGum;

/// <summary>The WPF view of the dialog shown after an HTML import.</summary>
[Dialog(typeof(ImportHtmlResultViewModel))]
public partial class ImportHtmlResultView : UserControl
{
    public ImportHtmlResultView()
    {
        InitializeComponent();
    }
}
