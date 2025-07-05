namespace Gum.Services.Dialogs;

public class MessageDialogViewModel : DialogViewModel
{
    public string? Title { get => Get<string>(); set => Set(value); }
    public string Message { get => Get<string>(); set => Set(value); }
}