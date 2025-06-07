using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Services;

public static class Locator
{
    private static List<IServiceProvider> ServiceProviders { get; } = [];
    public static void Register(IServiceProvider serviceProvider)
    {
        ServiceProviders.Insert(0, serviceProvider);
    }

    public static T GetRequiredService<T>() => ServiceProviders.Select(isp => isp.GetService<T>()).FirstOrDefault() ??
                                               throw new InvalidOperationException($"No service for {typeof(T)} has been registered.");
}