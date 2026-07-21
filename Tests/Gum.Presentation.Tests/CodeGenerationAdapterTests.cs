using CodeOutputPlugin.Manager;
using Gum.Managers;
using Gum.Reflection;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for the small tool-to-engine adapters relocated out of
/// Gum/CodeOutputPlugin/Manager (Gum.csproj) into the headless Gum.Presentation assembly (#3905) -
/// each just forwards to an already-headless dependency and had no WPF dependency of its own.
/// Bumped from internal to public so the plugin (still in Gum.csproj) can construct them across the
/// new assembly boundary.
/// </summary>
public class CodeGenerationAdapterTests
{
    [Fact]
    public void ToolCodeGenLogger_PrintOutput_ForwardsToOutputManager()
    {
        Mock<IOutputManager> outputManager = new();
        var logger = new ToolCodeGenLogger(outputManager.Object);

        logger.PrintOutput("hello");

        outputManager.Verify(x => x.AddOutput("hello"), Times.Once);
    }

    [Fact]
    public void ToolCodeGenLogger_PrintError_ForwardsToOutputManager()
    {
        Mock<IOutputManager> outputManager = new();
        var logger = new ToolCodeGenLogger(outputManager.Object);

        logger.PrintError("uh oh");

        outputManager.Verify(x => x.AddError("uh oh"), Times.Once);
    }

    [Fact]
    public void ToolTypeStringResolver_GetTypeFromString_ForwardsToTypeManager()
    {
        Mock<ITypeManager> typeManager = new();
        typeManager.Setup(x => x.GetTypeFromString("System.String")).Returns(typeof(string));
        var resolver = new ToolTypeStringResolver(typeManager.Object);

        var result = resolver.GetTypeFromString("System.String");

        result.ShouldBe(typeof(string));
    }

    [Fact]
    public void ProjectStateDirectoryProvider_ProjectDirectory_ReflectsCurrentProjectState()
    {
        Mock<IProjectState> projectState = new();
        projectState.SetupSequence(x => x.ProjectDirectory)
            .Returns("C:\\First\\")
            .Returns("C:\\Second\\");
        var provider = new ProjectStateDirectoryProvider(projectState.Object);

        // Reads live (not captured at construction), so it tracks project switches (issue driving
        // ProjectStateDirectoryProvider's own existence - see its doc comment).
        provider.ProjectDirectory.ShouldBe("C:\\First\\");
        provider.ProjectDirectory.ShouldBe("C:\\Second\\");
    }
}
