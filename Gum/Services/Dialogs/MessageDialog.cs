namespace Gum.Services.Dialogs;

public enum MessageDialogResult
{
    Canceled = -1,
    Negative = 0,
    Affirmative = 1
}

public class MessageDialogStyle
{
    private MessageDialogStyle(){}
    public static MessageDialogStyle OK { get; } = new();
    public static MessageDialogStyle OKCancel { get; } = new();
    public static MessageDialogStyle YesNo { get; } = new();
}