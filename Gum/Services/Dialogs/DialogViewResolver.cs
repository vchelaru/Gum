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
            List<Type> viewTypes = assembly.GetTypes()
                .Where(t => typeof(FrameworkElement).IsAssignableFrom(t) && t.Name.Contains("DialogView")).ToList();

            Dictionary<Type, Type> vmViewPairs = assembly.GetTypes()
                .Where(t => typeof(DialogViewModel).IsAssignableFrom(t))
                .Aggregate(new Dictionary<Type, Type>(),
                    (types, vmType) =>
                    {
                        if (viewTypes.FirstOrDefault(type => vmType.Name == $"{type.Name}Model") is { } viewType)
                        {
                            types[vmType] = viewType;
                        } 
                        else if (typeof(GetUserStringDialogBaseViewModel).IsAssignableFrom(vmType) && !vmType.IsAbstract)
                        {
                            types[vmType] = typeof(GetUserStringDialogView);
                        }

                        return types;
                    });
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