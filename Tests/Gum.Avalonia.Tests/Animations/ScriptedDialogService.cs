using Gum.Services.Dialogs;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>
/// An <see cref="IDialogService"/> a scenario scripts ahead of time: each dialog the code under test
/// opens is answered by a handler the test queued, the way a user would fill it in and press OK or
/// Cancel. A dialog nothing answers fails the test instead of blocking the headless run, since the
/// real service parks in a nested main loop until a person closes the window.
/// </summary>
internal sealed class ScriptedDialogService : IDialogService
{
    private readonly Queue<Func<DialogViewModel, bool>> _dialogAnswers;
    private readonly Queue<MessageDialogResult> _messageAnswers;
    private readonly Queue<string?> _userStrings;
    private readonly List<string> _messages;

    public ScriptedDialogService()
    {
        _dialogAnswers = new Queue<Func<DialogViewModel, bool>>();
        _messageAnswers = new Queue<MessageDialogResult>();
        _userStrings = new Queue<string?>();
        _messages = new List<string>();
    }

    /// <summary>Every message the code under test showed, in order.</summary>
    public IReadOnlyList<string> Messages => _messages;

    /// <summary>
    /// Queues the answer to the next dialog of type <typeparamref name="T"/>: the handler fills the
    /// view model in and returns whether the user presses OK (true) or Cancel.
    /// </summary>
    public void AnswerNext<T>(Func<T, bool> fill) where T : DialogViewModel
    {
        _dialogAnswers.Enqueue(viewModel => viewModel is T typed
            ? fill(typed)
            : throw new InvalidOperationException($"Expected a {typeof(T).Name} dialog but the code opened a {viewModel.GetType().Name}."));
    }

    /// <summary>Queues the button the user presses on the next message box.</summary>
    public void AnswerNextMessage(MessageDialogResult result) => _messageAnswers.Enqueue(result);

    /// <summary>Queues the text the user types into the next text prompt, or null for Cancel.</summary>
    public void AnswerNextUserString(string? value) => _userStrings.Enqueue(value);

    /// <inheritdoc/>
    public MessageDialogResult ShowMessage(string message, string? title = null, MessageDialogStyle? style = null)
    {
        _messages.Add(message);
        if (_messageAnswers.Count == 0)
        {
            throw new InvalidOperationException($"No answer was queued for the message \"{message}\".");
        }
        return _messageAnswers.Dequeue();
    }

    /// <inheritdoc/>
    public bool Show<T>(T dialogViewModel) where T : DialogViewModel
    {
        if (_dialogAnswers.Count == 0)
        {
            throw new InvalidOperationException($"No answer was queued for the {typeof(T).Name} dialog.");
        }

        // The real dialog window closes with the value OnAffirmative/OnNegative raise, so the
        // affirmative command's CanExecute gate applies here too.
        bool? closedWith = null;
        dialogViewModel.RequestClose += (_, result) => closedWith = result;
        bool pressOk = _dialogAnswers.Dequeue()(dialogViewModel);
        if (pressOk)
        {
            if (!dialogViewModel.AffirmativeCommand.CanExecute(null))
            {
                throw new InvalidOperationException($"The {typeof(T).Name} dialog's OK button is disabled with the scripted values.");
            }
            dialogViewModel.AffirmativeCommand.Execute(null);
        }
        else
        {
            dialogViewModel.NegativeCommand.Execute(null);
        }
        return closedWith == true;
    }

    /// <inheritdoc/>
    public bool Show<T>(Action<T>? initializer, out T viewModel) where T : DialogViewModel
    {
        viewModel = Activator.CreateInstance<T>();
        initializer?.Invoke(viewModel);
        return Show(viewModel);
    }

    /// <inheritdoc/>
    public string? GetUserString(string message, string? title = null, GetUserStringOptions? options = null)
    {
        if (_userStrings.Count == 0)
        {
            throw new InvalidOperationException($"No answer was queued for the text prompt \"{message}\".");
        }
        string? answer = _userStrings.Dequeue();
        // The real prompt keeps OK disabled while the validator objects, so a script cannot get past it.
        if (answer != null && options?.Validator?.Invoke(answer) is { } objection)
        {
            throw new InvalidOperationException($"The text prompt \"{message}\" rejects \"{answer}\": {objection}");
        }
        return answer;
    }

    /// <inheritdoc/>
    public List<string>? OpenFile(OpenFileDialogOptions? options = null) =>
        throw new InvalidOperationException("The animation editor scenarios never open a file picker.");

    /// <inheritdoc/>
    public string? SaveFile(SaveFileDialogOptions? options = null) =>
        throw new InvalidOperationException("The animation editor scenarios never open a save picker.");

    /// <inheritdoc/>
    public string? OpenFolder(OpenFolderDialogOptions? options = null) =>
        throw new InvalidOperationException("The animation editor scenarios never open a folder picker.");
}
