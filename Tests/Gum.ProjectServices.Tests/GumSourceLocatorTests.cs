using Shouldly;

namespace Gum.ProjectServices.Tests;

public class GumSourceLocatorTests : IDisposable
{
    private readonly string _tempDirectory;

    public GumSourceLocatorTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumSourceLocatorTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Find_FromNestedFolderOfCheckout_ShouldReturnCheckoutRoot()
    {
        string root = Path.Combine(_tempDirectory, "Gum");
        Directory.CreateDirectory(Path.Combine(root, "GumCommon"));
        File.WriteAllText(Path.Combine(root, "GumCommon", "GumCommon.csproj"), "<Project />");
        string nested = Path.Combine(root, "Tools", "Gum.Cli", "bin", "Debug");
        Directory.CreateDirectory(nested);

        string? found = GumSourceLocator.Find(nested);

        found.ShouldBe(root);
    }

    [Fact]
    public void Find_OutsideAnyCheckout_ShouldReturnNull()
    {
        string folder = Path.Combine(_tempDirectory, "elsewhere");
        Directory.CreateDirectory(folder);

        string? found = GumSourceLocator.Find(folder);

        found.ShouldBeNull();
    }
}
