using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Mvvm;
using Gum.Services.Dialogs;

namespace Gum.Dialogs;

/// <summary>
/// Backs the single-prompt "Expose color" dialog. The user edits one base name; each channel is exposed as
/// <c>BaseName + channelRootName</c>, and the resulting names update live as a preview (<see cref="ExposedNames"/>).
/// An empty base name is allowed and exposes the channels under their raw root names (e.g.
/// FillRed/FillGreen/FillBlue), which is how a user gets unprefixed exposed variables.
/// </summary>
public class ExposeColorDialogViewModel : DialogViewModel
{
    private readonly IReadOnlyList<string> _channelRootNames;
    private readonly Func<string, string?> _validateExposedName;

    public string? Message { get => Get<string>(); set => Set(value); }

    public string? BaseName { get => Get<string>(); set => Set(value); }

    /// <summary>
    /// The exposed name for each channel, in channel order, derived as <c>BaseName + channelRootName</c>. Shown
    /// in the dialog as a live preview and read by the caller to perform the exposure.
    /// </summary>
    [DependsOn(nameof(BaseName))]
    public IReadOnlyList<string> ExposedNames =>
        _channelRootNames.Select(rootName => (BaseName ?? "") + rootName).ToList();

    [DependsOn(nameof(BaseName))]
    public string? Error
    {
        get
        {
            foreach (string exposedName in ExposedNames)
            {
                if (_validateExposedName(exposedName) is { } whyNot)
                {
                    return whyNot;
                }
            }
            return null;
        }
    }

    public ExposeColorDialogViewModel(
        string defaultBaseName,
        IReadOnlyList<string> channelRootNames,
        Func<string, string?> validateExposedName)
    {
        _channelRootNames = channelRootNames;
        _validateExposedName = validateExposedName;

        AffirmativeText = "OK";
        NegativeText = "Cancel";
        Message = "Enter a base name for the exposed variables. Leave blank to expose them under their existing channel names.";

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Error))
            {
                AffirmativeCommand.NotifyCanExecuteChanged();
            }
        };

        BaseName = defaultBaseName;
    }

    public override bool CanExecuteAffirmative() => Error is null;
}
