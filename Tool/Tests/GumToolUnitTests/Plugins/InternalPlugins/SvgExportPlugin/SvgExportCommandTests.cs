using Gum.Commands;
using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.SvgExportPlugin;
using Gum.Services.Dialogs;
using Moq;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.SvgExportPlugin;

public class SvgExportCommandTests : BaseTestClass
{
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IGuiCommands> _guiCommands;

    public SvgExportCommandTests()
    {
        _dialogService = new Mock<IDialogService>();
        _guiCommands = new Mock<IGuiCommands>();
    }

    [Fact]
    public void BuildSvgExportArguments_quotes_all_arguments()
    {
        SvgExportCommand command = new(_dialogService.Object, _guiCommands.Object);

        string arguments = command.BuildSvgExportArguments(
            "c:/my projects/Game.gumx",
            "Main Screen",
            "c:/out dir/Main Screen.svg");

        arguments.ShouldBe(
            "svg \"c:/my projects/Game.gumx\" \"Main Screen\" --output \"c:/out dir/Main Screen.svg\"");
    }

    [Fact]
    public void ExportElementToSvg_does_not_run_gumcli_when_save_cancelled()
    {
        _dialogService
            .Setup(d => d.SaveFile(It.IsAny<SaveFileDialogOptions?>()))
            .Returns((string?)null);

        TestableSvgExportCommand command = new(_dialogService.Object, _guiCommands.Object)
        {
            GumCliPathToReturn = "c:/gum/GumCli/gumcli.exe",
        };

        command.ExportElementToSvg(new ScreenSave { Name = "MyScreen" }, new GumProjectSave());

        command.RunCount.ShouldBe(0);
    }

    [Fact]
    public void ExportElementToSvg_offers_svg_filename_and_filter_in_save_dialog()
    {
        SaveFileDialogOptions? captured = null;
        _dialogService
            .Setup(d => d.SaveFile(It.IsAny<SaveFileDialogOptions?>()))
            .Callback<SaveFileDialogOptions?>(options => captured = options)
            .Returns((string?)null);

        TestableSvgExportCommand command = new(_dialogService.Object, _guiCommands.Object);

        command.ExportElementToSvg(new ScreenSave { Name = "MyScreen" }, new GumProjectSave());

        captured.ShouldNotBeNull();
        captured!.FileName.ShouldBe("MyScreen.svg");
        captured.Filter.ShouldBe("SVG Files (*.svg)|*.svg");
    }

    [Fact]
    public void ExportElementToSvg_prints_error_and_skips_run_when_gumcli_missing()
    {
        _dialogService
            .Setup(d => d.SaveFile(It.IsAny<SaveFileDialogOptions?>()))
            .Returns("c:/out/MyScreen.svg");

        TestableSvgExportCommand command = new(_dialogService.Object, _guiCommands.Object)
        {
            GumCliPathToReturn = null,
        };

        command.ExportElementToSvg(new ScreenSave { Name = "MyScreen" }, new GumProjectSave());

        command.RunCount.ShouldBe(0);
        _guiCommands.Verify(g => g.PrintOutput(It.Is<string>(s => s.Contains("gumcli"))), Times.Once);
    }

    [Fact]
    public void ExportElementToSvg_runs_gumcli_with_chosen_path_when_available()
    {
        _dialogService
            .Setup(d => d.SaveFile(It.IsAny<SaveFileDialogOptions?>()))
            .Returns("c:/out/MyScreen.svg");

        TestableSvgExportCommand command = new(_dialogService.Object, _guiCommands.Object)
        {
            GumCliPathToReturn = "c:/gum/GumCli/gumcli.exe",
        };

        command.ExportElementToSvg(
            new ScreenSave { Name = "MyScreen" },
            new GumProjectSave { FullFileName = "c:/proj/Game.gumx" });

        command.RunCount.ShouldBe(1);
        command.LastGumCliPath.ShouldBe("c:/gum/GumCli/gumcli.exe");
        command.LastProjectPath.ShouldBe("c:/proj/Game.gumx");
        command.LastElementName.ShouldBe("MyScreen");
        command.LastOutputPath.ShouldBe("c:/out/MyScreen.svg");
    }

    // Stubs the two environment-bound seams (gumcli path resolution and the actual
    // subprocess launch) so the export decision flow can be exercised deterministically
    // without a gumcli.exe present in the test bin or a real process being spawned.
    private sealed class TestableSvgExportCommand : SvgExportCommand
    {
        public TestableSvgExportCommand(IDialogService dialogService, IGuiCommands guiCommands)
            : base(dialogService, guiCommands)
        {
        }

        public string? GumCliPathToReturn { get; set; }
        public int RunCount { get; private set; }
        public string? LastGumCliPath { get; private set; }
        public string? LastProjectPath { get; private set; }
        public string? LastElementName { get; private set; }
        public string? LastOutputPath { get; private set; }

        protected override string? FindGumCliPath()
        {
            return GumCliPathToReturn;
        }

        protected override void RunGumCliSvgExport(
            string gumCliPath, string projectPath, string elementName, string outputPath)
        {
            RunCount++;
            LastGumCliPath = gumCliPath;
            LastProjectPath = projectPath;
            LastElementName = elementName;
            LastOutputPath = outputPath;
        }
    }
}
