#nullable enable
using System;
using System.IO;
using Shouldly;
using ToolsUtilities;

namespace Gum.Tests.Shared;

/// <summary>
/// The check each test project's base-class tests run against its own base class: a test that
/// changes the <see cref="FileManager"/> settings must not leave them changed for the next test.
/// </summary>
public static class FileManagerStateAssert
{
    /// <param name="startTest">Constructs the base class, as xUnit does at the start of a test.</param>
    public static void DisposeRestores(Func<IDisposable> startTest)
    {
        FileManagerState before = FileManagerState.Capture();
        string relativeDirectory = FileManager.RelativeDirectory;
        bool? isMobileOverride = FileManager.IsMobileOverride;
        string? userApplicationDataFolderOverride = FileManager.UserApplicationDataFolderOverride;
        try
        {
            IDisposable test = startTest();
            FileManager.RelativeDirectory = Path.Combine(Path.GetTempPath(), "LeakedRelativeDirectory") + Path.DirectorySeparatorChar;
            FileManager.IsMobileOverride = !(isMobileOverride ?? false);
            FileManager.UserApplicationDataFolderOverride = Path.Combine(Path.GetTempPath(), "LeakedUserData");
            FileManager.CustomGetStreamFromFile = _ => Stream.Null;

            test.Dispose();

            FileManager.RelativeDirectory.ShouldBe(relativeDirectory);
            FileManager.IsMobileOverride.ShouldBe(isMobileOverride);
            FileManager.UserApplicationDataFolderOverride.ShouldBe(userApplicationDataFolderOverride);
            FileManager.CustomGetStreamFromFile.ShouldBeNull();
        }
        finally
        {
            before.Restore();
        }
    }
}
