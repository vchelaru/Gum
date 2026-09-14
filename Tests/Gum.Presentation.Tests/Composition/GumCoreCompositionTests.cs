using Gum.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.Composition;

/// <summary>
/// Proves the headless service graph composes without any UI framework: <c>AddGumCore()</c> plus a
/// stub for each contract a head must provide, then every registered service resolves. A core
/// service that quietly grows a dependency on a WPF-only type fails here rather than at the
/// Avalonia head's startup.
/// </summary>
public class GumCoreCompositionTests
{
    [Fact]
    public void AddGumCore_ResolvesEveryRegisteredService_WithHeadContractsStubbed()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddGumCore();
        int coreDescriptorCount = services.Count;

        AddHeadStubs(services);
        services.AddOptions();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = false,
            ValidateScopes = false,
        });

        List<string> failures = new List<string>();
        foreach (ServiceDescriptor descriptor in services.Take(coreDescriptorCount))
        {
            // Transient ViewModels are swept in by the assembly scan and are created through
            // Func<> factories with runtime arguments, or by plugins; nothing resolves them bare.
            // The singleton graph is the service wiring this test guards.
            if (descriptor.ServiceType.IsGenericTypeDefinition || descriptor.Lifetime != ServiceLifetime.Singleton)
            {
                continue;
            }

            try
            {
                object? resolved = provider.GetService(descriptor.ServiceType);
                if (resolved == null)
                {
                    failures.Add($"{descriptor.ServiceType.Name}: resolved to null");
                }
            }
            catch (Exception exception)
            {
                failures.Add($"{descriptor.ServiceType.Name}: {exception.GetBaseException().Message}");
            }
        }

        failures.ShouldBeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void HeadProvidedContracts_AreNotRegisteredByCore()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddGumCore();

        foreach (Type contract in GumCoreServiceCollectionExtensions.HeadProvidedContracts)
        {
            services.ShouldNotContain(
                descriptor => descriptor.ServiceType == contract,
                $"{contract.Name} is a head-provided contract and must not be registered by AddGumCore");
        }
    }

    private static void AddHeadStubs(IServiceCollection services)
    {
        foreach (Type contract in GumCoreServiceCollectionExtensions.HeadProvidedContracts)
        {
            Type mockType = typeof(Mock<>).MakeGenericType(contract);
            Mock mock = (Mock)Activator.CreateInstance(mockType)!;
            services.AddSingleton(contract, mock.Object);
        }
    }
}
