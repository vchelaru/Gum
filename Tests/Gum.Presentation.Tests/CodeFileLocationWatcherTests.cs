using CodeOutputPlugin;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Messages;
using Gum.ProjectServices.CodeGeneration;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using Gum.Localization;
using Gum.Managers;
using Moq;

namespace Gum.Presentation.Tests;

/// <summary>
/// Tests for <see cref="CodeFileLocationWatcher"/>, which tells the migration when a Code tab edit
/// moved where code files belong (issue #5846).
/// </summary>
public class CodeFileLocationWatcherTests
{

    private static GeneratedFolderEditorConfig CreateEditorConfig(string projectDirectory)
    {
        CodeGenerationNameVerifier nameVerifier = new CodeGenerationNameVerifier(new Mock<INameVerifier>().Object);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory);
        CodeGenerator codeGenerator = new CodeGenerator(
            nameVerifier, new LocalizationService(), new CodeOutputElementSettingsManager(directoryProvider), directoryProvider);
        return new GeneratedFolderEditorConfig(
            new CodeGenerationFileLocationsService(codeGenerator, nameVerifier, directoryProvider), Mock.Of<ICodeGenLogger>());
    }

    [Fact]
    public void CheckAfterEdit_RemovesGumsEditorConfig_WhenTheOptionIsTurnedOff_AndWritesItWhenTurnedOn()
    {
        string root = Path.Combine(Path.GetTempPath(), "GumWatcherEditorConfig_" + System.Guid.NewGuid().ToString("N")) + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(root);
        try
        {
            CodeFileLocationWatcher watcher = new CodeFileLocationWatcher(
                new WeakReferenceMessenger(), new CodeFileLocationChange(), new CustomCodeHeaderChange(), CreateEditorConfig(root));
            CodeOutputProjectSettings settings = new CodeOutputProjectSettings
            {
                OutputLibrary = OutputLibrary.GumForms, CodeProjectRoot = root, GeneratedCodeFolder = "Gen", CustomCodeFolder = "Mine",
            };
            watcher.Reset(settings);

            settings.MarkGeneratedCodeFolder = true;
            watcher.CheckAfterEdit(settings);
            File.Exists(Path.Combine(root, "Gen", ".editorconfig")).ShouldBeTrue();

            settings.MarkGeneratedCodeFolder = false;
            watcher.CheckAfterEdit(settings);
            File.Exists(Path.Combine(root, "Gen", ".editorconfig")).ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CheckAfterEdit_MovesGumsEditorConfig_WhenTheGeneratedFolderChanges()
    {
        string root = Path.Combine(Path.GetTempPath(), "GumWatcherEditorConfig_" + System.Guid.NewGuid().ToString("N")) + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(root);
        try
        {
            CodeFileLocationWatcher watcher = new CodeFileLocationWatcher(
                new WeakReferenceMessenger(), new CodeFileLocationChange(), new CustomCodeHeaderChange(), CreateEditorConfig(root));
            CodeOutputProjectSettings settings = new CodeOutputProjectSettings
            {
                OutputLibrary = OutputLibrary.GumForms, CodeProjectRoot = root, GeneratedCodeFolder = "Gen", CustomCodeFolder = "Mine",
                MarkGeneratedCodeFolder = true,
            };
            watcher.Reset(settings);
            watcher.CheckAfterEdit(settings);

            settings.GeneratedCodeFolder = "Gen2";
            watcher.CheckAfterEdit(settings);

            File.Exists(Path.Combine(root, "Gen", ".editorconfig")).ShouldBeFalse();
            File.Exists(Path.Combine(root, "Gen2", ".editorconfig")).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
    [Fact]
    public void CheckAfterEdit_SendsTheOldAndNewSettings_OnlyWhenAnEditMovesFiles()
    {
        WeakReferenceMessenger messenger = new WeakReferenceMessenger();
        List<CodeFileLocationsChangedMessage> sent = new List<CodeFileLocationsChangedMessage>();
        messenger.Register<CodeFileLocationsChangedMessage>(this, (_, message) => sent.Add(message));
        CodeFileLocationWatcher watcher = new CodeFileLocationWatcher(messenger, new CodeFileLocationChange(), new CustomCodeHeaderChange(), CreateEditorConfig(Path.GetTempPath()));
        // The tab edits this one object in place, so the watcher has to keep its own copy.
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGame, RootNamespace = "Game" };
        watcher.Reset(settings);

        settings.RootNamespace = "Renamed";
        watcher.CheckAfterEdit(settings);
        settings.OutputLibrary = OutputLibrary.GumForms;
        watcher.CheckAfterEdit(settings);
        watcher.CheckAfterEdit(settings);

        CodeFileLocationsChangedMessage message = sent.ShouldHaveSingleItem();
        message.Previous.OutputLibrary.ShouldBe(OutputLibrary.MonoGame);
        message.Current.ShouldBeSameAs(settings);
        message.Description.ShouldBe("Output Library from MonoGame (deprecated) to Gum Forms (recommended)");
    }

    [Fact]
    public void CheckAfterEdit_SendsALocationChange_ButNoHeaderChange_WhenOnlyTheCustomCodeFolderChanges()
    {
        WeakReferenceMessenger messenger = new WeakReferenceMessenger();
        List<CodeFileLocationsChangedMessage> locations = new List<CodeFileLocationsChangedMessage>();
        List<CustomCodeHeadersChangedMessage> headers = new List<CustomCodeHeadersChangedMessage>();
        messenger.Register<CodeFileLocationsChangedMessage>(this, (_, message) => locations.Add(message));
        messenger.Register<CustomCodeHeadersChangedMessage>(this, (_, message) => headers.Add(message));
        CodeFileLocationWatcher watcher = new CodeFileLocationWatcher(messenger, new CodeFileLocationChange(), new CustomCodeHeaderChange(), CreateEditorConfig(Path.GetTempPath()));
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.GumForms, RootNamespace = "Game" };
        watcher.Reset(settings);

        settings.CustomCodeFolder = "Mine/";
        watcher.CheckAfterEdit(settings);

        CodeFileLocationsChangedMessage message = locations.ShouldHaveSingleItem();
        message.Previous.CustomCodeFolder.ShouldBe(string.Empty);
        message.Description.ShouldBe("Custom Code Folder from (none) to Mine/");
        headers.ShouldBeEmpty("a custom folder moves files; namespaces are unaffected");
    }

    [Fact]
    public void CheckAfterEdit_SendsALocationChange_ButNoHeaderChange_WhenOnlyThePrefixChanges()
    {
        WeakReferenceMessenger messenger = new WeakReferenceMessenger();
        List<CodeFileLocationsChangedMessage> locations = new List<CodeFileLocationsChangedMessage>();
        List<CustomCodeHeadersChangedMessage> headers = new List<CustomCodeHeadersChangedMessage>();
        messenger.Register<CodeFileLocationsChangedMessage>(this, (_, message) => locations.Add(message));
        messenger.Register<CustomCodeHeadersChangedMessage>(this, (_, message) => headers.Add(message));
        CodeFileLocationWatcher watcher = new CodeFileLocationWatcher(messenger, new CodeFileLocationChange(), new CustomCodeHeaderChange(), CreateEditorConfig(Path.GetTempPath()));
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.GumForms, RootNamespace = "Game" };
        watcher.Reset(settings);

        settings.GeneratedCodeFolderPrefix = "GumCodeGen/";
        watcher.CheckAfterEdit(settings);

        CodeFileLocationsChangedMessage message = locations.ShouldHaveSingleItem();
        message.Previous.GeneratedCodeFolderPrefix.ShouldBe(string.Empty);
        message.Description.ShouldBe("Generated Code Folder Prefix from (none) to GumCodeGen/");
        headers.ShouldBeEmpty("the prefix moves files; namespaces are unaffected");
    }

    [Fact]
    public void CheckAfterEdit_SendsAHeaderChange_OnlyWhenAnEditChangesANamespaceSetting()
    {
        WeakReferenceMessenger messenger = new WeakReferenceMessenger();
        List<CustomCodeHeadersChangedMessage> sent = new List<CustomCodeHeadersChangedMessage>();
        messenger.Register<CustomCodeHeadersChangedMessage>(this, (_, message) => sent.Add(message));
        CodeFileLocationWatcher watcher = new CodeFileLocationWatcher(messenger, new CodeFileLocationChange(), new CustomCodeHeaderChange(), CreateEditorConfig(Path.GetTempPath()));
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGame, RootNamespace = "Game" };
        watcher.Reset(settings);

        settings.OutputLibrary = OutputLibrary.GumForms;
        watcher.CheckAfterEdit(settings);
        settings.RootNamespace = "Renamed";
        settings.AppendFolderToNamespace = true;
        watcher.CheckAfterEdit(settings);
        watcher.CheckAfterEdit(settings);

        CustomCodeHeadersChangedMessage message = sent.ShouldHaveSingleItem();
        message.Current.ShouldBeSameAs(settings);
        message.Description.ShouldBe("Root Namespace from Game to Renamed, Append Folder to Namespace from False to True");
    }
}
