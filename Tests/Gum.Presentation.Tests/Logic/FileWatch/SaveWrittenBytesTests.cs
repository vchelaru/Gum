using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Shouldly;
using System.IO;

namespace Gum.Presentation.Tests.Logic.FileWatch;

/// <summary>
/// The bytes a save returns are what the file watch ignore list hashes, so they must be exactly what
/// reached the disk.
/// </summary>
public class SaveWrittenBytesTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "SaveWrittenBytes_" + Path.GetRandomFileName());

    public SaveWrittenBytesTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { /* best-effort */ }
    }

    [Theory]
    [InlineData("Title.gusx", false)]
    [InlineData("Title.gusx", true)]
    [InlineData("Title.gusj", false)]
    public void SaveAndGetBytes_ReturnsTheBytesOnDisk(string fileName, bool useCompact)
    {
        ScreenSave screen = new ScreenSave { Name = "Title" };
        screen.States.Add(new StateSave { Name = "Default" });
        screen.Instances.Add(new InstanceSave { Name = "Label", BaseType = "Text" });
        screen.DefaultState.SetValue("Label.X", 12.5f, "float");
        string path = Path.Combine(_directory, fileName);
        File.WriteAllText(path, "an older, longer file that the save must replace completely");

        byte[] written = screen.SaveAndGetBytes(path, useCompact);

        written.ShouldBe(File.ReadAllBytes(path));
    }

    [Theory]
    [InlineData("Clickable.behx", false)]
    [InlineData("Clickable.behx", true)]
    [InlineData("Clickable.behj", false)]
    public void BehaviorSaveAndGetBytes_ReturnsTheBytesOnDisk(string fileName, bool useCompact)
    {
        BehaviorSave behavior = new BehaviorSave { Name = "Clickable" };
        string path = Path.Combine(_directory, fileName);

        byte[] written = behavior.SaveAndGetBytes(path, useCompact);

        written.ShouldBe(File.ReadAllBytes(path));
    }
}
