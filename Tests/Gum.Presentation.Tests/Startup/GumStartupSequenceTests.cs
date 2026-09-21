using System.Reflection;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Reflection;
using Gum.Services;
using Gum.Startup;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Presentation.Tests.Startup;

/// <summary>
/// <see cref="GumStartupSequence"/> is the one place shared by both heads, so a step both heads
/// need (rather than a per-head <see cref="IHeadStartup"/> implementation) can't be forgotten in
/// just one of them - see #4899, where only the WPF head wired enum promotion.
/// </summary>
public class GumStartupSequenceTests : IDisposable
{
    private readonly IServiceProvider _testServiceProvider;
    private readonly Func<VariableSave, bool>? _originalCustomFixEnumerations;

    public GumStartupSequenceTests()
    {
        _originalCustomFixEnumerations = VariableSaveExtensionMethods.CustomFixEnumerations;

        TypeManager typeManager = new TypeManager();
        typeManager.Initialize();

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton<ITypeManager>(typeManager);
        _testServiceProvider = services.BuildServiceProvider();
        Locator.Register(_testServiceProvider);
    }

    public void Dispose()
    {
        // CustomFixEnumerations is declared non-nullable but is used as nullable throughout
        // (see its null check in FixEnumerations); restoring a possibly-null prior value is
        // intentional here, not a bug this test is introducing.
        VariableSaveExtensionMethods.CustomFixEnumerations = _originalCustomFixEnumerations!;

        PropertyInfo providersProperty = typeof(Locator).GetProperty(
            "ServiceProviders", BindingFlags.NonPublic | BindingFlags.Static)!;
        List<IServiceProvider> providers = (List<IServiceProvider>)providersProperty.GetValue(null)!;
        providers.Remove(_testServiceProvider);
    }

    [Fact]
    public void WireEnumFixups_PromotesIntValuedEnumVariableSave_ToBoxedEnum()
    {
        // Repro for #4899: a StackPanel instance's Orientation (or any other enum-typed
        // VariableSave, e.g. TextWrapping, ScrollBarVisibility, a StateSaveCategory value) is
        // written to disk as an int. Startup must promote it back to the real enum on load, or
        // the property grid shows it blank and reference comparisons against string literals
        // (e.g. "Orientation == \"Horizontal\"") silently fail.
        VariableSave variableSave = new VariableSave
        {
            Name = "Orientation",
            Type = "Orientation",
            Value = (int)Orientation.Horizontal,
            SetsValue = true
        };

        GumStartupSequence.WireEnumFixups();
        variableSave.FixEnumerations();

        variableSave.Value.ShouldBe(Orientation.Horizontal);
    }
}
