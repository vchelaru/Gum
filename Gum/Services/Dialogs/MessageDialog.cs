namespace Gum.Services.Dialogs;

public enum MessageDialogResult
{
    Canceled = -1,
    Negative = 0,
    Affirmative = 1,
}

public class MessageDialogStyle
{
    public string? AffirmativeText { get; set; }
    public string? NegativeText { get; set; }

    public static MessageDialogStyle Ok => new()
    {
        AffirmativeText = "Ok",
    };

    public static MessageDialogStyle OkCancel => new()
    {
        AffirmativeText = "Ok",
        NegativeText = "Cancel",
    };

    public static MessageDialogStyle YesNo => new()
    {
        AffirmativeText = "Yes",
        NegativeText = "No",
    };
}
