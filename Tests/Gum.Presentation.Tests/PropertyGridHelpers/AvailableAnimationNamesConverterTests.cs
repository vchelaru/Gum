using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.PropertyGridHelpers.Converters;
using Gum.Services;
using Gum.ToolStates;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Gum.Presentation.Tests.PropertyGridHelpers;

public class AvailableAnimationNamesConverterTests : BaseTestClass
{
    private readonly IServiceProvider _testServiceProvider;
    private readonly string _projectDirectory;

    public AvailableAnimationNamesConverterTests()
    {
        _projectDirectory = Path.Combine(Path.GetTempPath(), "AvailableAnimationNames_" + Guid.NewGuid().ToString("N"))
            + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(_projectDirectory);

        Mock<IProjectState> projectState = new Mock<IProjectState>();
        projectState.SetupGet(x => x.ProjectDirectory).Returns(_projectDirectory);

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(projectState.Object);
        _testServiceProvider = services.BuildServiceProvider();
        Locator.Register(_testServiceProvider);
    }

    public override void Dispose()
    {
        PropertyInfo prop = typeof(Locator).GetProperty(
            "ServiceProviders", BindingFlags.NonPublic | BindingFlags.Static)!;
        List<IServiceProvider> providers = (List<IServiceProvider>)prop.GetValue(null)!;
        providers.Remove(_testServiceProvider);

        try
        {
            Directory.Delete(_projectDirectory, recursive: true);
        }
        catch
        {
            // Best effort cleanup
        }

        base.Dispose();
    }

    [Fact]
    public void GetAvailableValues_BackslashSourceFile_ListsChainsOnEveryOS()
    {
        // A project saved on Windows stores SourceFile with backslashes. On macOS/Linux a
        // backslash is a file-name character, so the file was not found and the list was empty.
        Directory.CreateDirectory(Path.Combine(_projectDirectory, "Animations"));
        File.WriteAllText(Path.Combine(_projectDirectory, "Animations", "hero.achx"), """
            <AnimationChainArraySave>
              <AnimationChain>
                <Name>Walk</Name>
              </AnimationChain>
            </AnimationChainArraySave>
            """);

        ComponentSave component = new ComponentSave { Name = "Hero" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = component };
        state.Variables.Add(new VariableSave
        {
            Name = "SourceFile",
            Type = "string",
            Value = "Animations\\hero.achx",
            SetsValue = true,
            IsFile = true,
        });
        component.States.Add(state);

        List<string> values = AvailableAnimationNamesConverter.GetAvailableValues(component, instance: null, state);

        values.ShouldContain("Walk");
    }
}
