using Gum.Commands;
using System.Windows;

namespace Gum.Controls;

/// <summary>
/// A progress dialog shown during font generation. Exposed to logic as the framework-neutral
/// <see cref="ISpinner"/>.
/// </summary>
public partial class Spinner : Window, ISpinner
{
    private int _completed;
    private int _total;

    public Spinner()
    {
        try
        {
            Window? mainWindow = Application.Current.MainWindow;
            if (mainWindow != null && mainWindow.IsLoaded)
            {
                Owner = mainWindow;
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }
        catch
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        InitializeComponent();
    }

    /// <inheritdoc/>
    public void SetTotal(int total)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _total = total;
            _completed = 0;
            FontProgressBar.Maximum = total;
            FontProgressBar.Value = 0;
            CountLabel.Text = $"0/{total}";
        });
    }

    /// <inheritdoc/>
    public void IncrementProgress()
    {
        Dispatcher.BeginInvoke(() =>
        {
            _completed++;
            FontProgressBar.Value = _completed;
            CountLabel.Text = $"{_completed}/{_total}";
        });
    }
}
