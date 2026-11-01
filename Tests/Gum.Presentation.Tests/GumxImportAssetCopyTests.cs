using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic.FileWatch;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Plugins.ImportPlugin.Services;
using Gum.ProjectServices;
using Gum.ToolStates;
using ImportFromGumxPlugin.Services;
using ImportFromGumxPlugin.ViewModels;
using Moq;
using RenderingLibrary.Content;
using Shouldly;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Importing from another .gumx brings along every file the imported components reference,
/// whatever its format.
/// </summary>
public class GumxImportAssetCopyTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GumAssetCopy_" + Guid.NewGuid().ToString("N"));
    private readonly FakeDecoder _decoder = new();

    public GumxImportAssetCopyTests()
    {
        LoaderManager.Self.ImageDecoders.Add(_decoder);
    }

    public void Dispose()
    {
        LoaderManager.Self.ImageDecoders.Remove(_decoder);
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class FakeDecoder : IImageDecoder
    {
        public IEnumerable<string>? SupportedExtensions => new[] { "fake" };
        public bool TryDecode(string fileName, Stream stream, out DecodedImage? image)
        {
            image = null;
            return false;
        }
    }

    [Theory]
    [InlineData("icon.png")]
    [InlineData("icon.jpg")]
    [InlineData("icon.tga")]
    public async Task ImportAsync_ShouldCopyReferencedFile_WhenSourceFileHasThisExtension(string relativePath)
    {
        string sourceDirectory = Path.Combine(_root, "Source");
        string projectDirectory = Path.Combine(_root, "Project");
        Directory.CreateDirectory(projectDirectory);
        Directory.CreateDirectory(sourceDirectory);

        GumProjectSave source = new GumProjectSave
        {
            FullFileName = Path.Combine(sourceDirectory, "Source.gumx"),
            Version = (int)GumProjectSave.GumxVersions.AttributeVersion,
        };
        ComponentSave component = new ComponentSave { Name = "Textured" };
        component.States.Add(new StateSave { Name = "Default" });
        component.DefaultState!.Variables.Add(new VariableSave { Name = "SourceFile", Value = relativePath, Type = "string" });
        source.Components.Add(component);
        source.ComponentReferences.Add(new ElementReference { Name = "Textured", ElementType = ElementType.Component });
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(sourceDirectory, relativePath))!);
        File.WriteAllBytes(Path.Combine(sourceDirectory, relativePath), new byte[] { 1, 2, 3 });
        source.Save(source.FullFileName, saveElements: true);

        GumxImportService sut = CreateService(projectDirectory, source.Version);

        await sut.ImportAsync(
            new ImportSelections { DirectComponents = source.Components.ToList() },
            source, sourceDirectory, destinationSubfolder: string.Empty);

        File.Exists(Path.Combine(projectDirectory, relativePath)).ShouldBeTrue();
    }

    private static GumxImportService CreateService(string projectDirectory, int version)
    {
        GumProjectSave project = new GumProjectSave
        {
            FullFileName = Path.Combine(projectDirectory, "Test.gumx"),
            Version = version,
        };
        Mock<IProjectState> projectState = new();
        projectState.Setup(x => x.GumProjectSave).Returns(project);
        projectState.Setup(x => x.ProjectDirectory).Returns(projectDirectory + Path.DirectorySeparatorChar);

        Mock<IImportLogic> importLogic = new();
        importLogic.Setup(x => x.ImportComponent(It.IsAny<ComponentSave>(), It.IsAny<bool>()))
            .Returns((ComponentSave component, bool _) => component);

        return new GumxImportService(
            importLogic.Object, projectState.Object, new Mock<IFileCommands>().Object,
            new GumxSourceService(), new ImportVersionGate(), new Mock<IFileWatchIgnoreList>().Object);
    }
}
