using Gum.Services.Dialogs;
using ImportFromGumxPlugin.ViewModels;
using System.Windows.Controls;

namespace ImportFromGumxPlugin.Views;

/// <summary>
/// Read-only modal showing per-variable / per-category diff rows for one Standard (#2779).
/// </summary>
[Dialog(typeof(StandardDiffDetailsViewModel))]
public partial class StandardDiffDetailsView : UserControl
{
    public StandardDiffDetailsView()
    {
        InitializeComponent();
    }
}
