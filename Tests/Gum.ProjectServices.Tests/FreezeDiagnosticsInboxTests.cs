using System;
using System.Collections.Generic;
using System.IO;
using Gum.Diagnostics;
using Shouldly;
using Xunit;

namespace Gum.ProjectServices.Tests;

public class FreezeDiagnosticsInboxTests : IDisposable
{
    private readonly string _directory;

    public FreezeDiagnosticsInboxTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "GumFreezeDiagnosticsInboxTests_" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void BeginSession_FirstEverLaunch_IsNotDirty()
    {
        FreezeDiagnosticsInbox inbox = new FreezeDiagnosticsInbox(_directory);

        inbox.BeginSession().ShouldBeFalse();
    }

    [Fact]
    public void BeginSession_AfterCleanExit_IsNotDirty()
    {
        FreezeDiagnosticsInbox inbox = new FreezeDiagnosticsInbox(_directory);
        inbox.BeginSession();
        inbox.EndSessionCleanly();

        inbox.BeginSession().ShouldBeFalse();
    }

    [Fact]
    public void BeginSession_WithoutCleanExit_IsDirty()
    {
        FreezeDiagnosticsInbox inbox = new FreezeDiagnosticsInbox(_directory);
        inbox.BeginSession();

        new FreezeDiagnosticsInbox(_directory).BeginSession().ShouldBeTrue();
    }

    [Fact]
    public void GetUnreportedFiles_ListsOnlyDiagnosticFiles()
    {
        FreezeDiagnosticsInbox inbox = new FreezeDiagnosticsInbox(_directory);
        inbox.BeginSession();
        string dump = Path.Combine(_directory, "freeze-2026-09-18_10-00-00.dmp");
        string note = Path.Combine(_directory, "freeze-2026-09-18_10-00-00.txt");
        File.WriteAllText(dump, "");
        File.WriteAllText(note, "");
        Directory.CreateDirectory(Path.Combine(_directory, "Reported"));
        File.WriteAllText(Path.Combine(_directory, "Reported", "freeze-old.dmp"), "");

        IReadOnlyList<string> unreported = inbox.GetUnreportedFiles();

        unreported.ShouldBe(new[] { dump, note }, ignoreOrder: true);
    }

    [Fact]
    public void GetUnreportedFiles_MissingDirectory_IsEmpty()
    {
        FreezeDiagnosticsInbox inbox = new FreezeDiagnosticsInbox(_directory);

        inbox.GetUnreportedFiles().ShouldBeEmpty();
    }

    [Fact]
    public void MarkReported_MovesFilesOutOfTheUnreportedSet()
    {
        FreezeDiagnosticsInbox inbox = new FreezeDiagnosticsInbox(_directory);
        Directory.CreateDirectory(_directory);
        string dump = Path.Combine(_directory, "freeze-2026-09-18_10-00-00.dmp");
        File.WriteAllText(dump, "");

        inbox.MarkReported(new[] { dump });

        inbox.GetUnreportedFiles().ShouldBeEmpty();
        File.Exists(Path.Combine(_directory, "Reported", "freeze-2026-09-18_10-00-00.dmp")).ShouldBeTrue();
    }

    [Fact]
    public void SuppressPrompts_PersistsAcrossInstances()
    {
        FreezeDiagnosticsInbox inbox = new FreezeDiagnosticsInbox(_directory);
        inbox.ArePromptsSuppressed.ShouldBeFalse();

        inbox.SuppressPrompts();

        new FreezeDiagnosticsInbox(_directory).ArePromptsSuppressed.ShouldBeTrue();
        inbox.GetUnreportedFiles().ShouldBeEmpty();
    }
}
