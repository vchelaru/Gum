using Gum.DataTypes;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.ProjectServices;
using System.Collections.Generic;

namespace Gum.Managers;

/// <summary>
/// Tool-side error checker that delegates core error-checking logic to
/// <see cref="IHeadlessErrorChecker"/> and appends plugin-contributed errors.
/// </summary>
public class ErrorChecker : IErrorChecker
{
    private readonly IHeadlessErrorChecker _headlessErrorChecker;
    private readonly IPluginManager _pluginManager;
    private readonly IErrorDocsRegistry _errorDocsRegistry;

    public ErrorChecker(
        IHeadlessErrorChecker headlessErrorChecker,
        IPluginManager pluginManager,
        IErrorDocsRegistry errorDocsRegistry)
    {
        _headlessErrorChecker = headlessErrorChecker;
        _pluginManager = pluginManager;
        _errorDocsRegistry = errorDocsRegistry;
    }

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

            _pluginManager.FillWithErrors(list);

            foreach (var vm in list)
            {
                if (vm.Code != null && vm.HelpUrl == null)
                {
                    vm.HelpUrl = _errorDocsRegistry.GetUrl(vm.Code);
                }
            }

            ApplyDefaultElementName(list, element);
        }

        return list.ToArray();
    }

    public ErrorViewModel[] GetErrorsFor(ElementSave? element, PluginBase plugin)
    {
        var list = new List<ErrorViewModel>();

        if (element != null)
        {
            ObjectFinder.Self.EnableCache();
            try
            {
                _pluginManager.FillWithErrors(list, plugin);
            }
            finally
            {
                ObjectFinder.Self.DisableCache();
            }

            ApplyDefaultElementName(list, element);
        }

        return list.ToArray();
    }

    // Plugin-contributed errors don't always know which element they belong to (e.g. a check that
    // isn't scoped to the selected element). Falling back to the selected element keeps every row
    // in the tab equally useful to copy, even when the plugin itself set no ElementName.
    private static void ApplyDefaultElementName(List<ErrorViewModel> list, ElementSave element)
    {
        foreach (var vm in list)
        {
            if (string.IsNullOrEmpty(vm.ElementName))
            {
                vm.ElementName = element.Name;
            }
        }
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
        return vm;
    }
}
