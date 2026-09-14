using Gum.Mvvm;
using System;
using CommunityToolkit.Mvvm.Input;

namespace Gum.Managers;

public interface IOutputManager
{
    void AddOutput(string value);
    void AddError(string value);

    /// <summary>
    /// Raised when <see cref="AddError"/> appends a line, so the Output tab can bring itself to the
    /// front. Errors written here are otherwise easy to miss, since nothing else signals them.
    /// </summary>
    event Action? ErrorAdded;
}

public partial class MainOutputViewModel : ViewModel, IOutputManager
{
    const int MaxCharacterLength = 50000;

    /// <summary>
    /// Environment variable that, when "1", also writes every Output line to standard error, so an
    /// unattended run (the head's <c>--exit-after</c> mode, CI) records what the Output tab showed.
    /// </summary>
    public const string EchoEnvironmentVariable = "GUM_ECHO_OUTPUT";

    private readonly bool _echoToConsole;

    public string OutputText
    {
        get => Get<string>();
        private set => Set(value);
    }

    public MainOutputViewModel() : this(Environment.GetEnvironmentVariable(EchoEnvironmentVariable) == "1")
    {
    }

    /// <summary>Creates the view model; <paramref name="echoToConsole"/> also writes each line to standard error.</summary>
    public MainOutputViewModel(bool echoToConsole)
    {
        _echoToConsole = echoToConsole;
        OutputText = string.Empty;
    }

    public void AddOutput(string value)
    {
        Echo(value);
        OutputText += "\n[" + DateTime.Now.ToShortTimeString() + "] " + value;

        if (OutputText.Length > MaxCharacterLength)
        {
            OutputText = OutputText.Substring(MaxCharacterLength / 2);
        }
    }

    /// <inheritdoc/>
    public event Action? ErrorAdded;

    public void AddError(string value)
    {
        Echo("ERROR:  " + value);
        OutputText += "\n[" + DateTime.Now.ToShortTimeString() + "] ERROR:  " + value;

        if (OutputText.Length > MaxCharacterLength)
        {
            OutputText = OutputText.Substring(MaxCharacterLength / 2);
        }

        ErrorAdded?.Invoke();
    }

    [RelayCommand]
    private void ClearOutput()
    {
        OutputText = string.Empty;
    }

    private void Echo(string line)
    {
        if (_echoToConsole)
        {
            Console.Error.WriteLine("[Gum output] " + line);
        }
    }
}
