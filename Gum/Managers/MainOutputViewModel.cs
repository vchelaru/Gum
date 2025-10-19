using Gum.Mvvm;
using System;
using CommunityToolkit.Mvvm.Input;

namespace Gum.Managers;

public interface IOutputManager
{
    void AddOutput(string value);
    void AddError(string value);
}

public partial class MainOutputViewModel : ViewModel, IOutputManager
{
    const int MaxCharacterLength = 50000;

    public string OutputText
    {
        get => Get<string>();
        private set => Set(value);
    }

    public MainOutputViewModel()
    {
        OutputText = string.Empty;
    }

    public void AddOutput(string value)
    {
        OutputText += "\n[" + DateTime.Now.ToShortTimeString() + "] " + value;

        if (OutputText.Length > MaxCharacterLength)
        {
            OutputText = OutputText.Substring(MaxCharacterLength / 2);
        }
    }

    public void AddError(string value)
    {
        OutputText += "\n[" + DateTime.Now.ToShortTimeString() + "] ERROR:  " + value;

        if (OutputText.Length > MaxCharacterLength)
        {
            OutputText = OutputText.Substring(MaxCharacterLength / 2);
        }
    }

    [RelayCommand]
    private void ClearOutput()
    {
        OutputText = string.Empty;
    }
}