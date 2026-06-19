using Gum.DataTypes;
using Gum.Managers;
using Gum.PropertyGridHelpers.Converters;
using Gum.Services;
using Gum.ToolStates;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace GumToolUnitTests.PropertyGridHelpers;

public class AvailableContainedTypeConverterTests : BaseTestClass
{
    private readonly IServiceProvider _testServiceProvider;
    private readonly Mock<ISelectedState> _selectedStateMock;
    private readonly Mock<IProjectManager> _projectManagerMock;

    public AvailableContainedTypeConverterTests()
    {
        GumProjectSave project = new GumProjectSave();

        _projectManagerMock = new Mock<IProjectManager>();
        _projectManagerMock.SetupGet(x => x.GumProjectSave).Returns(project);

        _selectedStateMock = new Mock<ISelectedState>();

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(_projectManagerMock.Object);
        services.AddSingleton(_selectedStateMock.Object);
        _testServiceProvider = services.BuildServiceProvider();
        Locator.Register(_testServiceProvider);
    }

    public override void Dispose()
    {
        PropertyInfo prop = typeof(Locator).GetProperty(
            "ServiceProviders", BindingFlags.NonPublic | BindingFlags.Static)!;
        List<IServiceProvider> providers = (List<IServiceProvider>)prop.GetValue(null)!;
        providers.Remove(_testServiceProvider);

        base.Dispose();
    }

    [Fact]
    public void GetStandardValues_SelectedInstanceNonNullSelectedElementNull_DoesNotThrow()
    {
        // Repro #3196: mid-transition during a project load/refresh, SelectedInstance can be
        // non-null while SelectedElement is null (a stale instance reference remains after the
        // element was cleared). The converter guarded SelectedInstance but then dereferenced
        // SelectedElement unconditionally (foreach over currentElement.Instances) => NRE.
        // SelectedElement is left at the mock default of null - that is the inconsistent state.
        _selectedStateMock.SetupGet(x => x.SelectedInstance)
            .Returns(new InstanceSave { Name = "StaleInstance" });

        AvailableContainedTypeConverter converter = new AvailableContainedTypeConverter();

        Should.NotThrow(() => converter.GetStandardValues(null));
    }
}
