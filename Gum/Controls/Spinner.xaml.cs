using System.Windows;

namespace Gum.Controls;

/// <summary>
/// A progress dialog shown during font generation.
/// </summary>
public partial class Spinner : Window
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

    /// <summary>
    /// Sets the total number of fonts to generate and resets the progress bar.
    /// Must be called from the UI thread.
    /// </summary>
    public void SetTotal(int total)
    {
        _total = total;
        _completed = 0;
        FontProgressBar.Maximum = total;
        FontProgressBar.Value = 0;
        CountLabel.Text = $"0/{total}";
    }

    /// <summary>
    /// Increments the completed count by one and updates the progress bar and label.
    /// Safe to call from any thread; dispatches to the UI thread automatically.
    /// </summary>
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
