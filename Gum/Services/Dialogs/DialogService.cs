using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Gum.Controls;
using MessageBox = System.Windows.MessageBox;

namespace Gum.Services.Dialogs;

public interface IDialogService
{
    MessageDialogResult ShowMessage(string message, string? title = null, MessageDialogStyle? style = null);
}

internal class DialogService : IDialogService
{
    public MessageDialogResult ShowMessage(string message, string? title = null, MessageDialogStyle? style = null)
    {
        MessageBoxButton buttons = style switch
        {
            { } when style == MessageDialogStyle.OK => MessageBoxButton.OK,
            { } when style == MessageDialogStyle.OKCancel => MessageBoxButton.OKCancel,
            { } when style == MessageDialogStyle.YesNo => MessageBoxButton.YesNo,
            _ => MessageBoxButton.OK
        };

        return MessageBox.Show(message, title, buttons) switch
        {
            MessageBoxResult.Yes or MessageBoxResult.OK => MessageDialogResult.Affirmative,
            MessageBoxResult.No => MessageDialogResult.Negative,
            _ => MessageDialogResult.Canceled
        };
    }
}

public static class IDialogServiceExt
{
    public static bool ShowYesNoMessage(this IDialogService dialogService, string message, string? title = null)
    {
        return dialogService.ShowMessage(message, title, MessageDialogStyle.YesNo) is MessageDialogResult.Affirmative;
    }
}