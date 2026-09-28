using System;
using System.IO;
using CodeOutputPlugin.Manager;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests.Plugins.CodeOutputPlugins;

/// <summary>
/// A new element starts from default code settings: a .codsj already sitting at its name is left
/// over from something else and is recycled, while a duplicate keeps the settings copied from its
/// source (#5396).
/// </summary>
public class NewElementCodeSettingsServiceTests : IDisposable
{
    private readonly string _projectDirectory;
    private readonly CodeOutputElementSettingsManager _settingsManager;
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly Mock<IOutputManager> _outputManager = new();
    private readonly NewElementCodeSettingsService _service;

    public NewElementCodeSettingsServiceTests()
    {
        _projectDirectory = Path.Combine(Path.GetTempPath(), "GumNewElementCodeSettingsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_projectDirectory, "Components"));
        _settingsManager = new CodeOutputElementSettingsManager(new FixedProvider(_projectDirectory + Path.DirectorySeparatorChar));
        _fileCommands.Setup(x => x.MoveToRecycleBin(It.IsAny<FilePath>()))
            .Callback<FilePath>(file => File.Delete(file.FullPath));
        _service = new NewElementCodeSettingsService(_settingsManager, _fileCommands.Object, _outputManager.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_projectDirectory))
        {
            Directory.Delete(_projectDirectory, recursive: true);
        }
    }

    [Fact]
    public void HandleElementAdd_RecyclesALeftoverSettingsFile_AndReportsIt()
    {
        ComponentSave component = new ComponentSave { Name = "Card" };
        _settingsManager.WriteSettingsForElement(component, new CodeOutputElementSettings
        {
            GeneratedFileName = "Other/OtherView.Generated.cs",
            GenerationBehavior = GenerationBehavior.NeverGenerate,
        });
        FilePath settingsFile = _settingsManager.GetCodeSettingsFilePath(component)!;

        _service.HandleElementAdd(component);

        _fileCommands.Verify(x => x.MoveToRecycleBin(settingsFile), Times.Once);
        _outputManager.Verify(x => x.AddOutput(It.Is<string>(line => line.Contains(settingsFile.FullPath))), Times.Once);
        CodeOutputElementSettings settings = _settingsManager.LoadOrCreateSettingsFor(component);
        settings.GeneratedFileName.ShouldBeNullOrEmpty();
        settings.GenerationBehavior.ShouldBe(GenerationBehavior.GenerateAutomaticallyOnPropertyChange);
    }

    [Fact]
    public void HandleElementAdd_KeepsSettingsCopiedFromTheDuplicateSource_AndReportsNothing()
    {
        ComponentSave source = new ComponentSave { Name = "Card" };
        ComponentSave copy = new ComponentSave { Name = "CardCopy" };
        _settingsManager.WriteSettingsForElement(source, new CodeOutputElementSettings { Namespace = "Cards.Ui" });

        _service.HandleElementDuplicate(source, copy);
        _service.HandleElementAdd(copy);

        _settingsManager.LoadOrCreateSettingsFor(copy).Namespace.ShouldBe("Cards.Ui");
        _fileCommands.Verify(x => x.MoveToRecycleBin(It.IsAny<FilePath>()), Times.Never);
        _outputManager.Verify(x => x.AddOutput(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void HandleElementAdd_RecyclesALeftoverFile_ForADuplicateWhoseSourceHasNoSettings()
    {
        ComponentSave source = new ComponentSave { Name = "Card" };
        ComponentSave copy = new ComponentSave { Name = "CardCopy" };
        _settingsManager.WriteSettingsForElement(copy, new CodeOutputElementSettings { Namespace = "Leftover" });
        FilePath copyFile = _settingsManager.GetCodeSettingsFilePath(copy)!;

        _service.HandleElementDuplicate(source, copy);
        _service.HandleElementAdd(copy);

        _fileCommands.Verify(x => x.MoveToRecycleBin(copyFile), Times.Once);
        _settingsManager.LoadOrCreateSettingsFor(copy).Namespace.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void HandleElementAdd_ReportsAnError_WhenTheLeftoverFileCannotBeRecycled()
    {
        ComponentSave component = new ComponentSave { Name = "Card" };
        _settingsManager.WriteSettingsForElement(component, new CodeOutputElementSettings());
        FilePath settingsFile = _settingsManager.GetCodeSettingsFilePath(component)!;
        _fileCommands.Setup(x => x.MoveToRecycleBin(It.IsAny<FilePath>())).Throws(new IOException("locked"));

        _service.HandleElementAdd(component);

        _outputManager.Verify(x => x.AddError(It.Is<string>(line => line.Contains(settingsFile.FullPath))), Times.Once);
        _outputManager.Verify(x => x.AddOutput(It.IsAny<string>()), Times.Never);
    }

    private class FixedProvider : IProjectDirectoryProvider
    {
        public FixedProvider(string directory) => ProjectDirectory = directory;
        public string? ProjectDirectory { get; }
    }
}
