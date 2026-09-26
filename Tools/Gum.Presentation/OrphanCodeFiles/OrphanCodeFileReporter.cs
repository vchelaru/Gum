using CommunityToolkit.Mvvm.Input;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services;
using Gum.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrphanCodeFilePlugin;

/// <summary>
/// Business logic behind the tool's orphaned-code-file reporting, kept out of the
/// <c>MainOrphanCodeFilePlugin</c> (mirrors <see cref="ConvertToJsonPlugin.ConvertToJsonLogic"/>)
/// so it can be unit tested headlessly. Holds the most recent scan result, turns it into Errors tab
/// entries, and resolves an entry by moving the file to the Recycle Bin.
/// </summary>
public class OrphanCodeFileReporter
{
    /// <summary>
    /// Error code for an orphaned code file, registered in the docs registry so the Errors tab can
    /// link to the explanation of what orphans are and why the scan is best effort.
    /// </summary>
    public const string ErrorCode = "GUM0005";

    private readonly IOrphanCodeFileScanService _scanService;
    private readonly IFileCommands _fileCommands;
    private readonly IDialogService _dialogService;
    private readonly IDispatcher _dispatcher;
    private readonly IOutputManager _outputManager;
    private readonly List<OrphanCodeFile> _orphans;
    private CancellationTokenSource? _currentScan;

    public OrphanCodeFileReporter(
        IOrphanCodeFileScanService scanService,
        IFileCommands fileCommands,
        IDialogService dialogService,
        IDispatcher dispatcher,
        IOutputManager outputManager)
    {
        _scanService = scanService;
        _fileCommands = fileCommands;
        _dialogService = dialogService;
        _dispatcher = dispatcher;
        _outputManager = outputManager;
        _orphans = new List<OrphanCodeFile>();
    }

    /// <summary>
    /// The orphans found by the most recent <see cref="RefreshAsync"/>, minus any already resolved.
    /// </summary>
    public IReadOnlyList<OrphanCodeFile> Orphans => _orphans;

    /// <summary>
    /// Raised on the UI thread whenever <see cref="Orphans"/> changes, so the host can refresh
    /// whatever displays it.
    /// </summary>
    public event Action? OrphansChanged;

    /// <summary>
    /// Re-runs the scan and replaces <see cref="Orphans"/>; clears the list when no project is loaded.
    /// Call it on the UI thread: the project is read there, the disk walk runs on the thread pool, and
    /// the result is posted back through <see cref="IDispatcher"/>, where <paramref name="onApplied"/>
    /// also runs. Starting a new refresh cancels the previous one and discards its result. The
    /// returned task completes once the result has been posted, not applied. Read-only — nothing is
    /// deleted until <see cref="Resolve"/> is called.
    /// </summary>
    public async Task RefreshAsync(GumProjectSave? project, CodeOutputProjectSettings projectSettings,
        Action<OrphanCodeFileScanResult>? onApplied = null)
    {
        _currentScan?.Cancel();
        CancellationTokenSource scan = new CancellationTokenSource();
        _currentScan = scan;

        if (project == null)
        {
            _orphans.Clear();
            OrphansChanged?.Invoke();
            return;
        }

        OrphanCodeFileScanPlan plan = _scanService.CreatePlan(project, projectSettings);

        OrphanCodeFileScanResult result;
        try
        {
            result = await Task.Run(() => _scanService.Execute(plan, scan.Token)).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception exception)
        {
            _dispatcher.Post(() => _outputManager.AddError(
                $"The orphaned code file scan failed: {exception.Message}"));
            return;
        }

        _dispatcher.Post(() => Apply(scan, result, onApplied));
    }

    private void Apply(CancellationTokenSource scan, OrphanCodeFileScanResult result,
        Action<OrphanCodeFileScanResult>? onApplied)
    {
        ///////////////////Early Out///////////////////
        if (scan != _currentScan)
        {
            // A newer refresh started while this one ran.
            return;
        }
        /////////////////End Early Out/////////////////

        _orphans.Clear();
        _orphans.AddRange(result.Orphans);

        if (result.IsTruncated)
        {
            _outputManager.AddError(OrphanCodeFileScanService.GetTruncatedMessage(result.CodeRoot));
        }

        OrphansChanged?.Invoke();
        onApplied?.Invoke(result);
    }

    /// <summary>
    /// Builds one Errors tab entry per orphan, each carrying an action that resolves it.
    /// </summary>
    public IEnumerable<ErrorViewModel> CreateErrors() =>
        _orphans.ToList().Select(orphan => new ErrorViewModel
        {
            Code = ErrorCode,
            ElementName = orphan.ElementName ?? string.Empty,
            Message = $"Orphaned {GetKindDescription(orphan.Kind)}, no matching element in the project: " +
                $"{orphan.FilePath.FullPath}",
            ActionName = "Delete File",
            ActionCommand = new RelayCommand(() => Resolve(orphan))
        });

    /// <summary>
    /// Moves an orphaned file to the Recycle Bin and drops it from <see cref="Orphans"/>. A generated
    /// file goes immediately — it is a pure function of its element, so removing it is lossless.
    /// Anything else is user-authored or hand-configured, so it is confirmed first.
    /// </summary>
    public void Resolve(OrphanCodeFile orphan)
    {
        if (orphan.Kind != OrphanCodeFileKind.Generated)
        {
            bool confirmed = _dialogService.ShowYesNoMessage(
                $"{orphan.FilePath.FullPath}\n\nThis file is not regenerated by Gum, so removing it loses " +
                "anything it contains. Move it to the Recycle Bin?",
                "Delete Orphaned File?");

            if (!confirmed)
            {
                return;
            }
        }

        _fileCommands.MoveToRecycleBin(orphan.FilePath);
        _orphans.Remove(orphan);
        OrphansChanged?.Invoke();
    }

    private static string GetKindDescription(OrphanCodeFileKind kind) => kind switch
    {
        OrphanCodeFileKind.Generated => "generated code file",
        OrphanCodeFileKind.CustomCode => "custom code file",
        _ => "element code settings file"
    };
}
