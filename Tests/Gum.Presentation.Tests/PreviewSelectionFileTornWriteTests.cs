using System;
using System.IO;
using System.Text;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Preview polls the selection file the tool rewrites. A read that lands mid-write, or on a file the
/// tool never finished, must give no selection rather than a wrong one or an exception.
/// </summary>
public class PreviewSelectionFileTornWriteTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"PreviewSelectionFileTornWrite_{Guid.NewGuid():N}.txt");

    public void Dispose()
    {
        File.Delete(_path);
    }

    [Fact]
    public void TryRead_MissingFile_IsNull()
    {
        PreviewSelectionFile.TryRead(_path).ShouldBeNull();
    }

    [Fact]
    public void TryRead_EmptyFile_IsNull()
    {
        File.WriteAllText(_path, "");

        PreviewSelectionFile.TryRead(_path).ShouldBeNull();
    }

    [Fact]
    public void TryRead_EveryPrefixOfAMessage_IsNullUntilTheEndLineArrives()
    {
        string full = new PreviewSelectionMessage("Screens/Main")
        {
            CategoryName = "Sizes",
            StateName = "Big",
            SortByBatchKey = true,
            Activate = true,
        }.Serialize();

        for (int length = 0; length < full.TrimEnd('\n').Length; length++)
        {
            File.WriteAllText(_path, full.Substring(0, length));
            PreviewSelectionFile.TryRead(_path).ShouldBeNull($"a {length} character prefix is not a whole message");
        }
        File.WriteAllText(_path, full);
        PreviewSelectionFile.TryRead(_path)!.ElementName.ShouldBe("Screens/Main");
    }

    [Fact]
    public void TryRead_WhileTheWriterHoldsTheFileOpenMidWrite_IsNullNotAnException()
    {
        using FileStream writer = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        writer.Write(Encoding.UTF8.GetBytes("element=Ma"));
        writer.Flush();

        PreviewSelectionFile.TryRead(_path).ShouldBeNull();
    }

    [Fact]
    public void WriteThenRead_UnicodeSpacesAndEqualsInNames_RoundTrips()
    {
        PreviewSelectionMessage message = new PreviewSelectionMessage("Menü/日本 語=1") { CategoryName = "A = B", StateName = "État 2" };

        PreviewSelectionFile.TryWrite(_path, message.Serialize()).ShouldBeTrue();

        PreviewSelectionMessage? read = PreviewSelectionFile.TryRead(_path);
        read.ShouldNotBeNull();
        read.ElementName.ShouldBe(message.ElementName);
        read.CategoryName.ShouldBe(message.CategoryName);
        read.StateName.ShouldBe(message.StateName);
    }
}
