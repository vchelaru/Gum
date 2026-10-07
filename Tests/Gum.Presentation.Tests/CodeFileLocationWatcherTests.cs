using CodeOutputPlugin;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Messages;
using Gum.ProjectServices.CodeGeneration;
using Shouldly;
using System.Collections.Generic;

namespace Gum.Presentation.Tests;

/// <summary>
/// Tests for <see cref="CodeFileLocationWatcher"/>, which tells the migration when a Code tab edit
/// moved where code files belong (issue #5846).
/// </summary>
public class CodeFileLocationWatcherTests
{
    [Fact]
    public void CheckAfterEdit_SendsTheOldAndNewSettings_OnlyWhenAnEditMovesFiles()
    {
        WeakReferenceMessenger messenger = new WeakReferenceMessenger();
        List<CodeFileLocationsChangedMessage> sent = new List<CodeFileLocationsChangedMessage>();
        messenger.Register<CodeFileLocationsChangedMessage>(this, (_, message) => sent.Add(message));
        CodeFileLocationWatcher watcher = new CodeFileLocationWatcher(messenger, new CodeFileLocationChange());
        // The tab edits this one object in place, so the watcher has to keep its own copy.
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGame, RootNamespace = "Game" };
        watcher.Reset(settings);

        settings.RootNamespace = "Renamed";
        watcher.CheckAfterEdit(settings);
        settings.OutputLibrary = OutputLibrary.MonoGameForms;
        watcher.CheckAfterEdit(settings);
        watcher.CheckAfterEdit(settings);

        CodeFileLocationsChangedMessage message = sent.ShouldHaveSingleItem();
        message.Previous.OutputLibrary.ShouldBe(OutputLibrary.MonoGame);
        message.Current.ShouldBeSameAs(settings);
        message.Description.ShouldBe("Output Library from MonoGame (deprecated) to Gum Forms (recommended)");
    }
}
