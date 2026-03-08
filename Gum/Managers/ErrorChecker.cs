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

    public ErrorChecker(IHeadlessErrorChecker headlessErrorChecker, IPluginManager pluginManager)
    {
        _headlessErrorChecker = headlessErrorChecker;
        _pluginManager = pluginManager;
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
        }

        return list.ToArray();
    }

    private static ErrorViewModel ToViewModel(ErrorResult errorResult)
    {
        return new ErrorViewModel
        {
            Message = errorResult.Message
        };
    }
}
