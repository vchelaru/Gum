using Microsoft.Extensions.DependencyInjection;

namespace GumCommon;

public static class Locator
{
    private static List<IServiceProvider> ServiceProviders { get; } = [];
    public static void Register(IServiceProvider serviceProvider)
    {
        if (ServiceProviders.Contains(serviceProvider))
        {
            throw new InvalidOperationException("Trying to register a provider that is already registered.");
        }
        ServiceProviders.Insert(0, serviceProvider);
    }

    public static T GetRequiredService<T>() => ServiceProviders
        .Select(isp => isp.GetService<T>())
        .FirstOrDefault(x => x is not null) ?? throw new InvalidOperationException($"No service for {typeof(T)} has been registered.");
}