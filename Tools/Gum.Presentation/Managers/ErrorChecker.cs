using Gum.DataTypes;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.Errors;
using Gum.Plugins.InternalPlugins.VariableGrid;
using CommunityToolkit.Mvvm.Input;
using Gum.ProjectServices;
using Gum.Services;
using System;
using System.Collections.Generic;

namespace Gum.Managers;

/// <summary>
/// Tool-side error checker that delegates core error-checking logic to
/// <see cref="IHeadlessErrorChecker"/> and collects the project-wide plugin-contributed errors.
/// </summary>
public class ErrorChecker : IErrorChecker
{
    private readonly IHeadlessErrorChecker _headlessErrorChecker;
    private readonly IPluginManager _pluginManager;
    private readonly IErrorDocsRegistry _errorDocsRegistry;
    private readonly IFileSystemRevealService _fileSystemRevealService;
    private readonly IVariableReferenceConflictFixer _variableReferenceConflictFixer;
    private readonly IFileCaseMismatchFixer _fileCaseMismatchFixer;

    public ErrorChecker(
        IHeadlessErrorChecker headlessErrorChecker,
        IPluginManager pluginManager,
        IErrorDocsRegistry errorDocsRegistry,
        IFileSystemRevealService fileSystemRevealService,
        IVariableReferenceConflictFixer variableReferenceConflictFixer,
        IFileCaseMismatchFixer fileCaseMismatchFixer)
    {
        _headlessErrorChecker = headlessErrorChecker;
        _pluginManager = pluginManager;
        _errorDocsRegistry = errorDocsRegistry;
        _fileSystemRevealService = fileSystemRevealService;
        _variableReferenceConflictFixer = variableReferenceConflictFixer;
        _fileCaseMismatchFixer = fileCaseMismatchFixer;
    }

    /// <inheritdoc/>
    public event Action<ElementSave, ErrorViewModel[]>? ErrorsChecked;

    public ErrorViewModel[] GetErrorsFor(ElementSave? element, GumProjectSave project)
    {
        var list = new List<ErrorViewModel>();

        if (element != null)
        {
            var errorResults = _headlessErrorChecker.GetErrorsFor(element, project);

            foreach (var errorResult in errorResults)
            {
                list.Add(ToViewModel(errorResult));
            }

            ErrorViewModel[] errors = list.ToArray();
            ErrorsChecked?.Invoke(element, errors);
            return errors;
        }

        return list.ToArray();
    }

    public ErrorViewModel[] GetProjectErrors(GumProjectSave project) =>
        ToViewModels(_headlessErrorChecker.GetProjectErrors(project));

    /// <inheritdoc/>
    public ProjectErrorCheck BeginProjectErrors(GumProjectSave project) => _headlessErrorChecker.BeginProjectErrors(project);

    /// <inheritdoc/>
    public ErrorViewModel[] ToViewModels(IReadOnlyList<ErrorResult> rows)
    {
        var list = new List<ErrorViewModel>();
        foreach (var errorResult in rows)
        {
            list.Add(ToViewModel(errorResult));
        }
        return list.ToArray();
    }

    public ErrorViewModel[] GetPluginErrors(PluginBase? plugin = null)
    {
        var list = new List<ErrorViewModel>();

        ObjectFinder.Self.EnableCache();
        try
        {
            _pluginManager.FillWithErrors(list, plugin);
        }
        finally
        {
            ObjectFinder.Self.DisableCache();
        }

        foreach (var vm in list)
        {
            if (vm.Code != null && vm.HelpUrl == null)
            {
                vm.HelpUrl = _errorDocsRegistry.GetUrl(vm.Code);
            }
        }

        return list.ToArray();
    }

    private ErrorViewModel ToViewModel(ErrorResult errorResult)
    {
        ErrorViewModel vm = new ErrorViewModel
        {
            Message = errorResult.Message,
            Code = errorResult.Code,
            ElementName = errorResult.ElementName
        };
        if (errorResult.Code != null)
        {
            vm.HelpUrl = _errorDocsRegistry.GetUrl(errorResult.Code);
        }
        if (errorResult.FilePath is { } filePath)
        {
            vm.ActionName = "Show File";
            vm.ActionCommand = new RelayCommand(() => _fileSystemRevealService.RevealFile(filePath));
        }
        if (errorResult.Details is FileNameCaseMismatchDetails caseMismatch && _fileCaseMismatchFixer.CanFix(caseMismatch))
        {
            // Replaces Show File: the reference is the thing to change, and the file is the one already named.
            vm.ActionName = "Fix";
            vm.ActionToolTip = "Change the reference to match the file on disk";
            vm.ActionCommand = new RelayCommand(() => _fileCaseMismatchFixer.Fix(caseMismatch));
        }
        if (errorResult.ReferenceConflict is { } conflict)
        {
            // Two lines: what it sets, then why that value.
            vm.ActionName = $"{_variableReferenceConflictFixer.GetActionLabel(conflict)}\n(match reference)";
            vm.ActionCommand = new RelayCommand(() => _variableReferenceConflictFixer.Fix(conflict));
        }
        return vm;
    }
}
