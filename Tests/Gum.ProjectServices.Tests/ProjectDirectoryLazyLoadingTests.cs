using Gum.DataTypes;
using Gum.ProjectServices.CodeGeneration;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Guards against services capturing ProjectDirectory at construction time.
/// These services must be usable as singletons across project switches,
/// so each must resolve the directory on every call via IProjectDirectoryProvider.
/// </summary>
public class ProjectDirectoryLazyLoadingTests
{
    [Fact]
    public void CodeOutputProjectSettingsManager_UsesCurrentProjectDirectory_AfterSwitch()
    {
        MutableProjectDirectoryProvider provider = new MutableProjectDirectoryProvider();
        provider.ProjectDirectory = "C:/ProjectA/";

        CodeOutputProjectSettingsManager manager = new CodeOutputProjectSettingsManager(
            new NullCodeGenLogger(), provider);

        provider.ProjectDirectory = "C:/ProjectB/";

        string? path = manager.GetProjectCodeSettingsFilePath()?.FullPath;
        path.ShouldNotBeNull();
        path.ShouldContain("ProjectB");
        path.ShouldNotContain("ProjectA");
    }

    [Fact]
    public void CodeOutputElementSettingsManager_UsesCurrentProjectDirectory_AfterSwitch()
    {
        MutableProjectDirectoryProvider provider = new MutableProjectDirectoryProvider();
        provider.ProjectDirectory = "C:/ProjectA/";

        CodeOutputElementSettingsManager manager = new CodeOutputElementSettingsManager(provider);

        provider.ProjectDirectory = "C:/ProjectB/";

        ScreenSave screen = new ScreenSave();
        screen.Name = "MyScreen";
        string? path = manager.GetCodeSettingsFilePath(screen)?.FullPath;
        path.ShouldNotBeNull();
        path.ShouldContain("ProjectB");
        path.ShouldNotContain("ProjectA");
    }

    private class MutableProjectDirectoryProvider : IProjectDirectoryProvider
    {
        public string? ProjectDirectory { get; set; }
    }

    private class NullCodeGenLogger : ICodeGenLogger
    {
        public void PrintOutput(string message) { }
        public void PrintError(string message) { }
    }
}
