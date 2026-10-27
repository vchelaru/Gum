using Gum.Avalonia.Services;
using Shouldly;

namespace Gum.Avalonia.Tests;

public class StagedContentStalenessCheckTests : IDisposable
{
    private static readonly DateTime Old = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Newer = new(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Newest = new(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc);

    private readonly string _baseDirectory = Path.Combine(Path.GetTempPath(), "gum6873-" + Guid.NewGuid().ToString("N"));

    public StagedContentStalenessCheckTests()
    {
        Directory.CreateDirectory(_baseDirectory);
    }

    public void Dispose()
    {
        Directory.Delete(_baseDirectory, recursive: true);
    }

    private void Write(string relativePath, DateTime lastWriteUtc)
    {
        string path = Path.Combine(_baseDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "x");
        File.SetLastWriteTimeUtc(path, lastWriteUtc);
    }

    [Fact]
    public void FindWarning_NoPluginsOrThemes_ReturnsNull()
    {
        Write("Gum.Presentation.dll", Newer);

        StagedContentStalenessCheck.FindWarning(_baseDirectory).ShouldBeNull();
    }

    [Fact]
    public void FindWarning_PluginOlderThanSharedAssembly_NamesThePlugin()
    {
        Write("Gum.Presentation.dll", Newer);
        Write("Plugins/EventOutputPlugin/EventOutputPlugin.dll", Old);

        string? warning = StagedContentStalenessCheck.FindWarning(_baseDirectory);

        warning.ShouldNotBeNull();
        warning.ShouldContain("EventOutputPlugin");
    }

    [Theory]
    [InlineData("Gum.Presentation.dll")]
    [InlineData("Gum.ProjectServices.dll")]
    [InlineData("GumCommon.dll")]
    public void FindWarning_PluginOlderThanAnySharedAssembly_Warns(string sharedAssembly)
    {
        Write(sharedAssembly, Newer);
        Write("Plugins/EventOutputPlugin/EventOutputPlugin.dll", Old);

        StagedContentStalenessCheck.FindWarning(_baseDirectory).ShouldNotBeNull();
    }

    [Fact]
    public void FindWarning_PluginNewerThanSharedAssemblies_ReturnsNull()
    {
        Write("Gum.Presentation.dll", Old);
        Write("Gum.ProjectServices.dll", Old);
        Write("Plugins/EventOutputPlugin/EventOutputPlugin.dll", Newer);

        StagedContentStalenessCheck.FindWarning(_baseDirectory).ShouldBeNull();
    }

    [Fact]
    public void FindWarning_HeadAssemblyNewerThanPlugin_ReturnsNull()
    {
        // Editing only the head's own code must not flag plugins it does not depend on.
        Write("Gum.dll", Newest);
        Write("Gum.Presentation.dll", Old);
        Write("Plugins/EventOutputPlugin/EventOutputPlugin.dll", Newer);

        StagedContentStalenessCheck.FindWarning(_baseDirectory).ShouldBeNull();
    }

    [Fact]
    public void FindWarning_OldDependencyInsidePluginFolder_IsNotAPlugin()
    {
        Write("Gum.Presentation.dll", Newer);
        Write("Plugins/SkiaPlugin/SkiaPlugin.dll", Newest);
        Write("Plugins/SkiaPlugin/Svg.Skia.dll", Old);

        StagedContentStalenessCheck.FindWarning(_baseDirectory).ShouldBeNull();
    }

    [Fact]
    public void FindWarning_ThemesWithoutStamp_Warns()
    {
        Write("Gum.ProjectServices.dll", Old);
        Write("Content/FormsThemes/Standard/GumProject.gumx", Newest);

        string? warning = StagedContentStalenessCheck.FindWarning(_baseDirectory);

        warning.ShouldNotBeNull();
        warning.ShouldContain("Forms themes");
        warning.ShouldContain(".staged");
    }

    [Fact]
    public void FindWarning_ThemeStampOlderThanProjectServices_Warns()
    {
        Write("Gum.ProjectServices.dll", Newer);
        Write("Content/FormsThemes/Standard/GumProject.gumx", Old);
        Write("Content/FormsThemes/.staged", Old);

        string? warning = StagedContentStalenessCheck.FindWarning(_baseDirectory);

        warning.ShouldNotBeNull();
        warning.ShouldContain("Forms themes");
    }

    [Fact]
    public void FindWarning_ThemeStampNewerThanProjectServices_ReturnsNull()
    {
        Write("Gum.ProjectServices.dll", Old);
        Write("Content/FormsThemes/Standard/GumProject.gumx", Newer);
        Write("Content/FormsThemes/.staged", Newer);

        StagedContentStalenessCheck.FindWarning(_baseDirectory).ShouldBeNull();
    }

    [Fact]
    public void FindWarning_StalePluginsAndThemes_NamesBothAndTheFix()
    {
        Write("Gum.Presentation.dll", Newer);
        Write("Gum.ProjectServices.dll", Newer);
        Write("Plugins/EventOutputPlugin/EventOutputPlugin.dll", Old);
        Write("Content/FormsThemes/Standard/GumProject.gumx", Old);
        Write("Content/FormsThemes/.staged", Old);

        string? warning = StagedContentStalenessCheck.FindWarning(_baseDirectory);

        warning.ShouldNotBeNull();
        warning.ShouldContain("EventOutputPlugin");
        warning.ShouldContain("Forms themes");
        warning.ShouldContain("dotnet build Gum.slnx --no-incremental -p:SkipPreviewVariantPublish=true");
    }
}
