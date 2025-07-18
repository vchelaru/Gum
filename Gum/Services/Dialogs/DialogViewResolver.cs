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
    
    private readonly HashSet<Assembly> _checkedAssemblies = [];
    private readonly Dictionary<Type, Type> _vmViewPaires = [];

    public DialogViewResolver(ILogger<DialogViewResolver> logger)
    {
        _logger = logger;
    }
    
    public Type? GetDialogViewType(Type viewModelType)
    {
        if (_vmViewPaires.TryGetValue(viewModelType, out Type viewType))
        {
            return viewType;
        }

        if (!_checkedAssemblies.Contains(viewModelType.Assembly))
        {
            Scan(viewModelType.Assembly);
            if (_vmViewPaires.TryGetValue(viewModelType, out Type addedViewType))
            {
                return addedViewType;
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
                    if (viewType.GetCustomAttribute<DialogAttribute>() is { } attr)
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