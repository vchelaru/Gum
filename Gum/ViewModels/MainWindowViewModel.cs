using Gum.Mvvm;

namespace Gum.ViewModels;

public class MainWindowViewModel : ViewModel
{
    public string? Title
    {
        get => Get<string?>();
        set => Set(value);   
    }
}