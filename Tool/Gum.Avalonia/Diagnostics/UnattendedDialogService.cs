using System;
using System.Collections.Generic;
using System.IO;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Diagnostics;

/// <summary>
/// The <see cref="IDialogService"/> of an unattended (<c>--exit-after</c>) run: nobody is there to answer
/// a modal, so every dialog is cancelled without being shown and its text goes to the error writer (#8451).
/// A message with only an OK button cannot be told apart from an error report, so it counts as one:
/// <see cref="HasShownNotice"/> makes the run exit nonzero after its screenshot.
/// </summary>
public sealed class UnattendedDialogService : IDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TextWriter _error;

    /// <summary>Creates the service; <paramref name="serviceProvider"/> builds the view models callers read back.</summary>
    public UnattendedDialogService(IServiceProvider serviceProvider, TextWriter error)
    {
        _serviceProvider = serviceProvider;
        _error = error;
    }

    /// <summary>True once a message without a cancel or no option was shown: a notice, possibly an error.</summary>
    public bool HasShownNotice { get; private set; }

    /// <inheritdoc/>
    public MessageDialogResult ShowMessage(string message, string? title = null, MessageDialogStyle? style = null)
    {
        string text = string.IsNullOrEmpty(title) ? message : title + ": " + message;
        if (style?.NegativeText == null)
        {
            HasShownNotice = true;
            Write("the tool showed a message an unattended run cannot confirm: " + text);
        }
        else
        {
            Write("cancelled the question: " + text);
        }
        return MessageDialogResult.Canceled;
    }

    /// <inheritdoc/>
    public bool Show<T>(T dialogViewModel) where T : DialogViewModel
    {
        Write("cancelled the " + dialogViewModel.GetType().Name + " dialog.");
        return false;
    }

    /// <inheritdoc/>
    public bool Show<T>(Action<T>? initializer, out T viewModel) where T : DialogViewModel
    {
        viewModel = _serviceProvider.GetRequiredService<T>();
        initializer?.Invoke(viewModel);
        return Show(viewModel);
    }

    /// <inheritdoc/>
    public string? GetUserString(string message, string? title = null, GetUserStringOptions? options = null)
    {
        Write("cancelled the text prompt: " + message);
        return null;
    }

    /// <inheritdoc/>
    public List<string>? OpenFile(OpenFileDialogOptions? options = null)
    {
        Write("cancelled the open-file picker.");
        return null;
    }

    /// <inheritdoc/>
    public string? SaveFile(SaveFileDialogOptions? options = null)
    {
        Write("cancelled the save-file picker.");
        return null;
    }

    /// <inheritdoc/>
    public string? OpenFolder(OpenFolderDialogOptions? options = null)
    {
        Write("cancelled the folder picker.");
        return null;
    }

    private void Write(string text)
    {
        _error.WriteLine("Unattended run: " + text);
        _error.Flush();
    }
}
