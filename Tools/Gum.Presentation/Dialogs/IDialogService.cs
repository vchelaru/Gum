using System;
using System.Collections.Generic;

namespace Gum.Services.Dialogs;

public interface IDialogService
{
    MessageDialogResult ShowMessage(string message, string? title = null, MessageDialogStyle? style = null);
    public bool Show<T>(T dialogViewModel) where T : DialogViewModel;
    bool Show<T>(Action<T>? initializer, out T viewModel) where T : DialogViewModel;
    string? GetUserString(string message, string? title = null, GetUserStringOptions? options = null);
    List<string>? OpenFile(OpenFileDialogOptions? options = null);
    string? SaveFile(SaveFileDialogOptions? options = null);
}
