using System;
using System.IO;
using System.Threading.Tasks;
using Gum.Services;
using Gum.Services.Dialogs;

namespace Gum.Diagnostics;

/// <summary>
/// Help > Create diagnostics file. Saving builds <c>GumDiagnostics-date.zip</c> in the Documents folder on a
/// background thread, reveals it in the file manager and closes; a failure stays in the dialog. Nothing is uploaded.
/// </summary>
public class CreateDiagnosticsViewModel : DialogViewModel
{
    /// <summary>The note under the disabled Gum project check box.</summary>
    public const string NoGumProjectNote = "Open a project to include it.";

    /// <summary>The note under the disabled C# project check box.</summary>
    public const string NoCSharpProjectNote = "Gum could not find the game's C# project.";

    private readonly IDiagnosticsEnvironment _environment;
    private readonly IDiagnosticsContentSource _contentSource;
    private readonly IFileSystemRevealService _revealService;

    /// <summary>Creates the dialog's view model.</summary>
    public CreateDiagnosticsViewModel(
        IDiagnosticsEnvironment environment,
        IDiagnosticsContentSource contentSource,
        IFileSystemRevealService revealService)
    {
        _environment = environment;
        _contentSource = contentSource;
        _revealService = revealService;

        Title = "Create diagnostics file";
        AffirmativeText = "Save zip";
        NegativeText = "Cancel";
        Description = string.Empty;
        CanIncludeGumProject = contentSource.GumProjectFile != null;
        CanIncludeCSharpProject = contentSource.CSharpProjectFolder != null;
        FileName = DiagnosticsFileName.Next(environment.SaveFolder, environment.Today);
    }

    /// <summary>The window title.</summary>
    public string Title { get; }

    /// <summary>What the user says went wrong; saved as description.txt when not empty.</summary>
    public string Description { get => Get<string>(); set => Set(value); }

    /// <summary>Whether the Gum project is added to the zip.</summary>
    public bool IncludeGumProject { get => Get<bool>(); set => Set(value); }

    /// <summary>Whether the C# project is added to the zip.</summary>
    public bool IncludeCSharpProject { get => Get<bool>(); set => Set(value); }

    /// <summary>False when no project is loaded.</summary>
    public bool CanIncludeGumProject { get; }

    /// <summary>False when the tool cannot locate the game's C# project.</summary>
    public bool CanIncludeCSharpProject { get; }

    /// <summary>The reason the Gum project box is disabled, or null.</summary>
    public string? GumProjectNote => CanIncludeGumProject ? null : NoGumProjectNote;

    /// <summary>The reason the C# project box is disabled, or null.</summary>
    public string? CSharpProjectNote => CanIncludeCSharpProject ? null : NoCSharpProjectNote;

    /// <summary>The name the zip will have.</summary>
    public string FileName { get => Get<string>(); private set => Set(value); }

    /// <summary>True while the zip is being built.</summary>
    public bool IsSaving
    {
        get => Get<bool>();
        private set
        {
            if (Set(value))
            {
                AffirmativeCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>The failure to show in the dialog, or null.</summary>
    public string? ErrorMessage
    {
        get => Get<string?>();
        private set
        {
            if (Set(value))
            {
                NotifyPropertyChanged(nameof(HasError));
            }
        }
    }

    /// <summary>True when <see cref="ErrorMessage"/> has text.</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <inheritdoc/>
    public override bool CanExecuteAffirmative() => !IsSaving;

    /// <inheritdoc/>
    public override void OnAffirmative()
    {
        _ = SaveAsync();
    }

    /// <summary>Builds the zip, reveals it and closes the dialog; on failure shows the error and stays open.</summary>
    public async Task SaveAsync()
    {
        if (IsSaving)
        {
            return;
        }

        ErrorMessage = null;
        IsSaving = true;
        try
        {
            DiagnosticsContent content = _contentSource.Collect();
            DiagnosticsSystemInfo info = _environment.GetSystemInfo();
            DiagnosticsRequest request = new DiagnosticsRequest(
                Description,
                CanIncludeGumProject && IncludeGumProject,
                CanIncludeCSharpProject && IncludeCSharpProject);
            PathScrubber scrubber = new PathScrubber(_environment.UserName, _environment.HomeFolder);
            string folder = _environment.SaveFolder;
            string path = Path.Combine(folder, DiagnosticsFileName.Next(folder, _environment.Today));

            await Task.Run(() => DiagnosticsZipWriter.Write(path, request, info, content, scrubber));

            _revealService.RevealFile(path);
            base.OnAffirmative();
        }
        catch (Exception exception)
        {
            ErrorMessage = "Could not save the diagnostics file: " + exception.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }
}
