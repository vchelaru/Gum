using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace Gum.Services.Dialogs;

internal interface IDialogViewResolver
{
    Type? GetDialogViewType(Type viewModelType);
}

/// <summary>
/// Supplies additional assemblies for <see cref="DialogViewResolver"/> to search when a view
/// model's own assembly doesn't contain its paired View. A relocated <see cref="DialogViewModel"/>
/// living in the headless Gum.Presentation assembly is the common case: it has no WPF types at
/// all, so its View necessarily lives in a different, WPF-capable assembly (the Gum tool itself,
/// or a dynamically-loaded plugin).
/// </summary>
internal interface IDialogViewAssemblyProvider
{
    IEnumerable<Assembly> GetCandidateAssemblies();
}

/// <summary>
/// Default <see cref="IDialogViewAssemblyProvider"/> backed by every assembly currently loaded in
/// the process. This is deliberately dynamic rather than a fixed list handed to
/// <see cref="DialogViewResolver"/> at DI-registration time: a plugin assembly (e.g.
/// ImportFromGumxPlugin) is loaded via reflection well after the DI container is built, so only a
/// live query of the app domain sees it.
/// </summary>
internal class AppDomainDialogViewAssemblyProvider : IDialogViewAssemblyProvider
{
    public IEnumerable<Assembly> GetCandidateAssemblies() => AppDomain.CurrentDomain.GetAssemblies();
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DialogAttribute : Attribute
{
    public Type DataContext { get; }
    public DialogAttribute(Type dataContext)
    {
        DataContext = dataContext;
    }
}

internal class DialogViewResolver : IDialogViewResolver
{
    private readonly ILogger _logger;
    private readonly IDialogViewAssemblyProvider _assemblyProvider;

    private readonly HashSet<Assembly> _checkedAssemblies = [];
    private readonly Dictionary<Type, Type> _vmViewPaires = [];

    public DialogViewResolver(ILogger<DialogViewResolver> logger, IDialogViewAssemblyProvider assemblyProvider)
    {
        _logger = logger;
        _assemblyProvider = assemblyProvider;
    }

    public Type? GetDialogViewType(Type viewModelType)
    {
        if (_vmViewPaires.TryGetValue(viewModelType, out Type viewType))
        {
            return viewType;
        }

        Scan(viewModelType.Assembly);
        if (_vmViewPaires.TryGetValue(viewModelType, out Type ownAssemblyViewType))
        {
            return ownAssemblyViewType;
        }

        // The view model's own assembly doesn't host its View. This is expected for view models
        // that live in the headless Gum.Presentation assembly (no WPF types at all) - their View
        // stays behind in a WPF-capable assembly (the Gum tool itself, or a dynamically-loaded
        // plugin), paired via [Dialog(typeof(...))] rather than same-assembly naming convention.
        foreach (Assembly candidate in _assemblyProvider.GetCandidateAssemblies())
        {
            if (_checkedAssemblies.Contains(candidate))
            {
                continue;
            }

            Scan(candidate);
            if (_vmViewPaires.TryGetValue(viewModelType, out Type fallbackViewType))
            {
                return fallbackViewType;
            }
        }

        _logger.LogError($"Unable to resolve view associated with viewmodel type {viewModelType.FullName}");
        return null;
    }

    private void Scan(Assembly assembly)
    {
        if (!_checkedAssemblies.Add(assembly))
        {
            return;
        }

        try
        {
            Dictionary<string, Type> viewModelTypes = assembly.GetTypes()
                .Where(t => typeof(DialogViewModel).IsAssignableFrom(t) && !t.IsAbstract)
                .ToDictionary(
                    x => x.Name.Replace("DialogViewModel", string.Empty), 
                    x => x);
            
            Dictionary<Type, Type> vmViewPairs = assembly.GetTypes()
                .Where(t => typeof(FrameworkElement).IsAssignableFrom(t))
                .Aggregate(new Dictionary<Type, Type>(), (types, viewType) =>
                {
                    if (viewType.GetCustomAttributes<DialogAttribute>().ToList() is { Count: > 0 } attributes)
                    {
                        foreach (var attr in attributes)
                        {
                            if (typeof(DialogViewModel).IsAssignableFrom(attr.DataContext) && !attr.DataContext.IsAbstract)
                            {
                                types[attr.DataContext] = viewType;
                            }
                            else
                            {
                                throw new ArgumentException("Type " + attr.DataContext + " does not derive from DialogViewModel");
                            }
                        }
                    }
                    else
                    {
                        string baseViewName = viewType.Name.Replace("DialogView",  string.Empty);

                        if (viewModelTypes.TryGetValue(baseViewName, out Type viewModelType))
                        {
                            types[viewModelType] = viewType;
                        }
                    }
                    
                    return types;
                });

            foreach (var kvp in viewModelTypes)
            {
                if (vmViewPairs.ContainsKey(kvp.Value) == false)
                {
                    // we didn't handle this VM type, we need to:
                    if (typeof(GetUserStringDialogBaseViewModel).IsAssignableFrom(kvp.Value))
                    {
                        vmViewPairs[kvp.Value] = typeof(GetUserStringDialogView);
                    }
                }
            }


            foreach (var pair in vmViewPairs)
            {
                _vmViewPaires[pair.Key] = pair.Value;
            }
        }
        catch
        {
            _checkedAssemblies.Remove(assembly);
        }
    }
}