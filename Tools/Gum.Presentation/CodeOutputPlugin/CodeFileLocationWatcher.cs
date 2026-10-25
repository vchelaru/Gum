using CommunityToolkit.Mvvm.Messaging;
using Gum.Messages;
using Gum.ProjectServices.CodeGeneration;
using Newtonsoft.Json;

namespace CodeOutputPlugin;

/// <summary>
/// Sends <see cref="CodeFileLocationsChangedMessage"/> when a Code tab edit changes a setting that
/// moves where code files belong, and <see cref="CustomCodeHeadersChangedMessage"/> when one changes
/// what custom code files must declare. The tab edits one settings object in place, so this keeps
/// its own copy of the settings as of the last check to compare against.
/// </summary>
public class CodeFileLocationWatcher
{
    private readonly IMessenger _messenger;
    private readonly CodeFileLocationChange _locationChange;
    private readonly CustomCodeHeaderChange _headerChange;
    private readonly GeneratedFolderEditorConfig _editorConfig;
    private CodeOutputProjectSettings _lastChecked = new CodeOutputProjectSettings();

    public CodeFileLocationWatcher(IMessenger messenger, CodeFileLocationChange locationChange, CustomCodeHeaderChange headerChange, GeneratedFolderEditorConfig editorConfig)
    {
        _messenger = messenger;
        _locationChange = locationChange;
        _headerChange = headerChange;
        _editorConfig = editorConfig;
    }

    /// <summary>
    /// Takes <paramref name="settings"/> as the baseline without sending anything: on project load,
    /// and when the settings file is reloaded from disk, whose files already match it.
    /// </summary>
    public void Reset(CodeOutputProjectSettings settings) => _lastChecked = Copy(settings);

    /// <summary>Call after each Code tab edit is written.</summary>
    public void CheckAfterEdit(CodeOutputProjectSettings settings)
    {
        CodeOutputProjectSettings previous = _lastChecked;
        _lastChecked = Copy(settings);

        string? description = _locationChange.Describe(previous, settings);
        if (description != null)
        {
            _messenger.Send(new CodeFileLocationsChangedMessage(previous, settings, description));
        }

        // Files already written follow the edit even when the user declines the migration prompt.
        _editorConfig.Sync(settings, previous);

        string? headerDescription = _headerChange.Describe(previous, settings);
        if (headerDescription != null)
        {
            _messenger.Send(new CustomCodeHeadersChangedMessage(settings, headerDescription));
        }
    }

    private static CodeOutputProjectSettings Copy(CodeOutputProjectSettings settings) =>
        JsonConvert.DeserializeObject<CodeOutputProjectSettings>(JsonConvert.SerializeObject(settings))!;
}
