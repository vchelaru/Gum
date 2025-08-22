using System.Drawing;
using System.Windows.Forms;
using Gum.Mvvm;

namespace Gum.ViewModels;

public class MainWindowViewModel : ViewModel
{
    public string? Title
    {
        get => Get<string?>();
        set => Set(value);   
    }

    public Rectangle? Bounds
    {
        get => Get<Rectangle?>();
        set => Set(value);
    }

    public FormWindowState? WindowState
    {
        get => Get<FormWindowState?>();
        set => Set(value);
    }
    
    public void LoadWindowSettings()
    {
        var settings = ProjectManager.Self.GeneralSettingsFile;

        // Apply the window position and size settings only if a large enough portion of the
        // window would end up on the screen.
        var workingArea = Screen.GetWorkingArea(settings.MainWindowBounds);
        var intersection = Rectangle.Intersect(settings.MainWindowBounds, workingArea);
        
        if (intersection.Width > 100 && intersection.Height > 100)
        {
            Bounds = settings.MainWindowBounds;
            WindowState = settings.MainWindowState;
        }
    }

    public void SaveWindowSettings(Rectangle bounds, FormWindowState windowState)
    {
        var settings = ProjectManager.Self.GeneralSettingsFile;

        settings.MainWindowBounds = bounds;
        settings.MainWindowState = windowState;

        settings.Save();
    }
}