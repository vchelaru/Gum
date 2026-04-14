using Gum.Managers;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace GumToolUnitTests.Managers;

public class FileChangeReactionLogicTests
{
    [Fact]
    public void IsLocalizationFileThatShouldTriggerReload_ShouldReturnTrue_WhenFileIsBaseLocalizationFile()
    {
        var baseFile = new FilePath(@"C:\project\Strings.resx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(baseFile, baseFile).ShouldBeTrue();
    }

    [Fact]
    public void IsLocalizationFileThatShouldTriggerReload_ShouldReturnTrue_WhenFileIsBaseLocalizationCsv()
    {
        var baseFile = new FilePath(@"C:\project\Localization.csv");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(baseFile, baseFile).ShouldBeTrue();
    }

    [Fact]
    public void IsLocalizationFileThatShouldTriggerReload_ShouldReturnTrue_WhenFileIsSatelliteResx()
    {
        var baseFile = new FilePath(@"C:\project\Strings.resx");
        var satellite = new FilePath(@"C:\project\Strings.es.resx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(satellite, baseFile).ShouldBeTrue();
    }

    [Fact]
    public void IsLocalizationFileThatShouldTriggerReload_ShouldReturnTrue_WhenFileIsAnySatellite()
    {
        var baseFile = new FilePath(@"C:\project\Strings.resx");
        var satellite = new FilePath(@"C:\project\Strings.zh-Hans.resx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(satellite, baseFile).ShouldBeTrue();
    }

    [Fact]
    public void IsLocalizationFileThatShouldTriggerReload_ShouldReturnFalse_WhenCsvHasNoSatellites()
    {
        var baseFile = new FilePath(@"C:\project\Localization.csv");
        var sibling = new FilePath(@"C:\project\Localization.es.resx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(sibling, baseFile).ShouldBeFalse();
    }

    [Fact]
    public void IsLocalizationFileThatShouldTriggerReload_ShouldReturnFalse_WhenSatelliteIsInDifferentDirectory()
    {
        var baseFile = new FilePath(@"C:\project\Strings.resx");
        var satellite = new FilePath(@"C:\other\Strings.es.resx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(satellite, baseFile).ShouldBeFalse();
    }

    [Fact]
    public void IsLocalizationFileThatShouldTriggerReload_ShouldReturnFalse_WhenResxNameDoesNotMatchBase()
    {
        var baseFile = new FilePath(@"C:\project\Strings.resx");
        var unrelated = new FilePath(@"C:\project\OtherStrings.es.resx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(unrelated, baseFile).ShouldBeFalse();
    }

    [Fact]
    public void IsLocalizationFileThatShouldTriggerReload_ShouldReturnFalse_WhenUnrelatedFileChanges()
    {
        var baseFile = new FilePath(@"C:\project\Strings.resx");
        var unrelated = new FilePath(@"C:\project\SomeScreen.gusx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(unrelated, baseFile).ShouldBeFalse();
    }
}
