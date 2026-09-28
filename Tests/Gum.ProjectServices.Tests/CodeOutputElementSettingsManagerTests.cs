using Gum.DataTypes;
using Gum.ProjectServices.CodeGeneration;
using Newtonsoft.Json;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class CodeOutputElementSettingsManagerTests : IDisposable
{
    private readonly string _projectDirectory;
    private readonly CodeOutputElementSettingsManager _manager;

    public CodeOutputElementSettingsManagerTests()
    {
        _projectDirectory = Path.Combine(Path.GetTempPath(), "GumElementSettingsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_projectDirectory);
        _manager = new CodeOutputElementSettingsManager(new FixedProvider(_projectDirectory + Path.DirectorySeparatorChar));
    }

    public void Dispose()
    {
        if (Directory.Exists(_projectDirectory))
        {
            Directory.Delete(_projectDirectory, recursive: true);
        }
    }

    [Fact]
    public void CopySettings_WritesTheSourcesSettingsForTheCopy_IntoItsFolder_WithoutTheGeneratedFileName()
    {
        ComponentSave source = new ComponentSave { Name = "Card" };
        ComponentSave copy = new ComponentSave { Name = "Controls/CardCopy" };
        Directory.CreateDirectory(Path.Combine(_projectDirectory, "Components"));
        _manager.WriteSettingsForElement(source, new CodeOutputElementSettings
        {
            UsingStatements = "using System.Numerics;",
            Namespace = "Cards.Ui",
            GeneratedFileName = "Special/CardView.Generated.cs",
            GenerationBehavior = GenerationBehavior.GenerateManually,
        });

        _manager.CopySettings(source, copy);

        string copyFile = Path.Combine(_projectDirectory, "Components", "Controls", "CardCopy.codsj");
        CodeOutputElementSettings copied = JsonConvert.DeserializeObject<CodeOutputElementSettings>(File.ReadAllText(copyFile))!;
        copied.UsingStatements.ShouldBe("using System.Numerics;");
        copied.Namespace.ShouldBe("Cards.Ui");
        copied.GenerationBehavior.ShouldBe(GenerationBehavior.GenerateManually);
        copied.GeneratedFileName.ShouldBe("");
        _manager.LoadOrCreateSettingsFor(source).GeneratedFileName.ShouldBe("Special/CardView.Generated.cs");
    }

    [Fact]
    public void CopySettings_WritesNothing_WhenTheSourceHasNoSettingsFile()
    {
        ComponentSave source = new ComponentSave { Name = "Card" };
        ComponentSave copy = new ComponentSave { Name = "CardCopy" };

        _manager.CopySettings(source, copy);

        File.Exists(Path.Combine(_projectDirectory, "Components", "CardCopy.codsj")).ShouldBeFalse();
    }

    private class FixedProvider : IProjectDirectoryProvider
    {
        public FixedProvider(string directory) => ProjectDirectory = directory;
        public string? ProjectDirectory { get; }
    }
}
