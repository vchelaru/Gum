using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Shouldly;
using SkiaSharp;

namespace Gum.Cli.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on gumcli (inventory area GCLI): each runs the built gumcli as its own
/// process, as a script or CI job does, over a project it created in a temp folder, and checks the
/// exit code, what it printed and the files it left.
/// </summary>
[Trait("Category", "EndToEnd")]
public class GumCliScenarioTests : IDisposable
{
    private readonly string _folder;

    public GumCliScenarioTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "GumCliEndToEnd", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Theory]
    [InlineData("forms")]
    [InlineData("empty")]
    [Trait("Feature", "GCLI-001")]
    [Trait("Feature", "GCLI-002")]
    [Trait("Feature", "GCLI-014")]
    public void New_EachTemplate_ChecksClean_AndResavesByteForByte(string template)
    {
        string project = Path.Combine(_folder, "Game", "Game.gumx");

        GumCliProcess created = Cli("new", project, "--template", template);

        created.ExitCode.ShouldBe(0, created.Transcript);
        created.StandardOutput.ShouldContain("Created project");
        Directory.GetFiles(Path.Combine(_folder, "Game", "Standards"), "*.gutx").ShouldNotBeEmpty();
        Directory.Exists(Path.Combine(_folder, "Game", "Components", "Controls")).ShouldBe(template == "forms");
        GumCliProcess checkedProject = Cli("check", project);
        checkedProject.ExitCode.ShouldBe(0, checkedProject.Transcript);
        checkedProject.StandardOutput.ShouldContain("No errors found.");
        Dictionary<string, byte[]> asCreated = Snapshot(Path.Combine(_folder, "Game"));

        foreach (string[] resave in new[] { new[] { "resave", project }, new[] { "resave", project, "--raw" } })
        {
            GumCliProcess saved = Cli(resave);
            saved.ExitCode.ShouldBe(0, saved.Transcript);
            ShouldMatch(Snapshot(Path.Combine(_folder, "Game")), asCreated,
                $"`gumcli {string.Join(" ", resave.Take(1).Concat(resave.Skip(2)))}` of a new {template} project should change no byte");
        }
    }

    [Fact]
    [Trait("Feature", "GCLI-002")]
    public void Check_ReportsAnElementWhoseBaseTypeIsMissing_WithExitCode1()
    {
        string project = NewProject("Game", "empty");
        AddComponent(project, "Card", """
            <?xml version="1.0" encoding="utf-8"?>
            <ComponentSave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
              <Name>Card</Name>
              <BaseType>NoSuchBase</BaseType>
              <State>
                <Name>Default</Name>
              </State>
            </ComponentSave>
            """);

        GumCliProcess result = Cli("check", project);
        GumCliProcess json = Cli("check", project, "--json");

        result.ExitCode.ShouldBe(1, result.Transcript);
        result.StandardOutput.ShouldContain("Card");
        result.StandardOutput.ShouldContain("NoSuchBase");
        json.ExitCode.ShouldBe(1, json.Transcript);
        json.StandardOutput.TrimStart().ShouldStartWith("[");
        json.StandardOutput.ShouldContain("\"Card\"");
    }

    [Fact]
    [Trait("Feature", "GCLI-003")]
    public void CheckReferences_ReportsAHandWrittenReference_AndFixWritesTheValueItNames()
    {
        string project = NewProject("Game", "empty");
        string card = AddComponent(project, "Card", """
            <?xml version="1.0" encoding="utf-8"?>
            <ComponentSave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
              <Name>Card</Name>
              <BaseType>Container</BaseType>
              <State>
                <Name>Default</Name>
                <Variable Type="float" Name="Width" SetsValue="true">
                  <Value xsi:type="xsd:float">240</Value>
                </Variable>
                <VariableList xsi:type="VariableListSaveOfString">
                  <Type>string</Type>
                  <Name>VariableReferences</Name>
                  <IsFile>false</IsFile>
                  <IsHiddenInPropertyGrid>false</IsHiddenInPropertyGrid>
                  <Value>
                    <string>Height = Width</string>
                  </Value>
                </VariableList>
              </State>
            </ComponentSave>
            """);

        GumCliProcess detected = Cli("check-references", project);
        GumCliProcess fixedRun = Cli("check-references", project, "--fix");
        GumCliProcess after = Cli("check-references", project);

        detected.ExitCode.ShouldBe(1, detected.Transcript);
        detected.StandardOutput.ShouldContain("Card");
        fixedRun.ExitCode.ShouldBe(0, fixedRun.Transcript);
        fixedRun.StandardOutput.ShouldContain("fixed: Card");
        Regex.IsMatch(File.ReadAllText(card), """Name="Height"[^>]*>\s*<Value xsi:type="xsd:float">240</Value>""")
            .ShouldBeTrue("--fix writes the value the reference names into the file");
        after.ExitCode.ShouldBe(0, after.Transcript);
        after.StandardOutput.ShouldContain("No unpropagated references");
    }

    [Fact]
    [Trait("Feature", "GCLI-004")]
    [Trait("Feature", "GCLI-005")]
    public void CodegenInit_ThenCodegen_WritesCompilableFormsClasses_AndPruneRemovesADeletedElementsGeneratedFile()
    {
        string gameFolder = Path.Combine(_folder, "MyGame");
        Directory.CreateDirectory(gameFolder);
        string csproj = Path.Combine(gameFolder, "MyGame.csproj");
        File.WriteAllText(csproj, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <RootNamespace>MyGame</RootNamespace>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="MonoGame.Framework.DesktopGL" Version="3.8.*" />
              </ItemGroup>
            </Project>
            """);
        string project = Path.Combine(gameFolder, "Content", "GumProject", "GumProject.gumx");
        Cli("new", project).ExitCode.ShouldBe(0);

        GumCliProcess init = Cli("codegen-init", project, "--csproj", csproj);
        GumCliProcess initAgain = Cli("codegen-init", project, "--csproj", csproj);
        GumCliProcess one = Cli("codegen", project, "--element", "Controls/ButtonStandard");

        init.ExitCode.ShouldBe(0, init.Transcript);
        string settings = File.ReadAllText(Path.Combine(gameFolder, "Content", "GumProject", "ProjectCodeSettings.codsj"));
        settings.ShouldContain("\"RootNamespace\": \"MyGame\"");
        initAgain.ExitCode.ShouldBe(2, "existing settings are kept without --force");
        one.ExitCode.ShouldBe(0, one.Transcript);
        string button = File.ReadAllText(Path.Combine(gameFolder, "Components", "Controls", "ButtonStandard.Generated.cs"));
        button.ShouldContain("namespace MyGame.Components.Controls");
        button.ShouldContain("partial class ButtonStandard : global::Gum.Forms.Controls.Button");
        File.ReadAllText(Path.Combine(gameFolder, "Components", "Controls", "ButtonStandard.cs")).ShouldContain("partial void CustomInitialize()");

        GumCliProcess all = Cli("codegen", project);
        all.ExitCode.ShouldBe(0, all.Transcript);
        File.ReadAllText(Path.Combine(gameFolder, "Components", "Controls", "ListBox.Generated.cs"))
            .ShouldContain("partial class ListBox : global::Gum.Forms.Controls.ListBox");
        string keyboardScreen = Path.Combine(gameFolder, "Screens", "KeyboardScreenGum.Generated.cs");
        File.ReadAllText(keyboardScreen).ShouldContain("partial class KeyboardScreenGum");
        // Nothing references the demo screen, so dropping it leaves the rest of the project valid.
        File.WriteAllText(project, Regex.Replace(File.ReadAllText(project), """\s*<ScreenReference Name="KeyboardScreenGum" />""", ""));

        GumCliProcess pruned = Cli("codegen", project, "--prune");

        pruned.ExitCode.ShouldBe(0, pruned.Transcript);
        File.Exists(keyboardScreen).ShouldBeFalse("--prune removes the generated file of an element the project no longer has");
        File.Exists(Path.Combine(gameFolder, "Screens", "KeyboardScreenGum.cs")).ShouldBeTrue("custom code is only listed, never deleted");
        pruned.StandardOutput.ShouldContain("KeyboardScreenGum.cs");
        File.Exists(Path.Combine(gameFolder, "Components", "Controls", "ButtonStandard.Generated.cs")).ShouldBeTrue();
    }

    [Fact]
    [Trait("Feature", "GCLI-006")]
    public void Fonts_GeneratesTheFontFileTheDefaultTextNeeds()
    {
        string project = NewProject("Game", "empty");
        string fontCache = Path.Combine(_folder, "Game", "FontCache");

        GumCliProcess result = Cli("fonts", project);
        GumCliProcess again = Cli("fonts", project);

        result.ExitCode.ShouldBe(0, result.Transcript);
        string fnt = Directory.GetFiles(fontCache, "*.fnt").ShouldHaveSingleItem();
        File.ReadAllText(fnt).ShouldContain("size=18");
        Directory.GetFiles(fontCache, "*.png").ShouldNotBeEmpty();
        again.ExitCode.ShouldBe(0, again.Transcript);
        Directory.GetFiles(fontCache, "*.fnt").ShouldHaveSingleItem("a second run finds nothing missing");
    }

    [Fact]
    [Trait("Feature", "GCLI-007")]
    [Trait("Feature", "GCLI-011")]
    public void AddForms_ToAnEmptyProject_BringsTheControls_AndDiffStandardsReportsOnlyARealEdit()
    {
        string project = NewProject("Game", "empty");

        GumCliProcess cleanDiff = Cli("diff-standards", project);
        GumCliProcess added = Cli("add-forms", project);
        GumCliProcess checkedAfter = Cli("check", project);
        GumCliProcess diffAfterForms = Cli("diff-standards", project);

        cleanDiff.ExitCode.ShouldBe(0, cleanDiff.Transcript);
        cleanDiff.StandardOutput.ShouldContain("No drift found.");
        added.ExitCode.ShouldBe(0, added.Transcript);
        File.ReadAllText(project).ShouldContain("<ComponentReference Name=\"Controls/ButtonStandard\" />");
        File.Exists(Path.Combine(_folder, "Game", "Components", "Controls", "ButtonStandard.gucx")).ShouldBeTrue();
        File.Exists(Path.Combine(_folder, "Game", "Behaviors", "ButtonBehavior.behx")).ShouldBeTrue();
        checkedAfter.ExitCode.ShouldBe(0, checkedAfter.Transcript);
        diffAfterForms.ExitCode.ShouldBe(0, diffAfterForms.Transcript);

        string text = Path.Combine(_folder, "Game", "Standards", "Text.gutx");
        File.WriteAllText(text, Regex.Replace(File.ReadAllText(text),
            """(Name="FontSize"[^>]*>\s*<Value xsi:type="xsd:int">)18(</Value>)""", "${1}30${2}"));
        GumCliProcess drift = Cli("diff-standards", project);

        drift.ExitCode.ShouldBe(1, drift.Transcript);
        drift.StandardOutput.ShouldContain("FontSize");
    }

    [Fact]
    [Trait("Feature", "GCLI-008")]
    [Trait("Feature", "GCLI-013")]
    [Trait("Feature", "GCLI-016")]
    public void FormsProject_ConvertsToJson_Packs_AndStagesItsBehaviors()
    {
        string project = NewProject("Game", "forms");
        string game = Path.Combine(_folder, "Game");

        GumCliProcess converted = Cli("convert-to-json", project);
        GumCliProcess checkedJson = Cli("check", Path.ChangeExtension(project, ".gumj"));
        GumCliProcess packed = Cli("pack", project, "--output", Path.Combine(_folder, "Game.gumpkg"));
        GumCliProcess staged = Cli("stage-forms-behaviors", project, Path.Combine(_folder, "Staged"));

        converted.ExitCode.ShouldBe(0, converted.Transcript);
        File.Exists(Path.ChangeExtension(project, ".gumj")).ShouldBeTrue();
        File.Exists(Path.Combine(game, "Components", "Controls", "ButtonStandard.gucj")).ShouldBeTrue();
        File.Exists(Path.Combine(game, "Components", "Controls", "ButtonStandard.gucx")).ShouldBeTrue("the XML is left in place");
        checkedJson.ExitCode.ShouldBe(0, checkedJson.Transcript);
        packed.ExitCode.ShouldBe(0, packed.Transcript);
        new FileInfo(Path.Combine(_folder, "Game.gumpkg")).Length.ShouldBeGreaterThan(0);
        staged.ExitCode.ShouldBe(0, staged.Transcript);
        File.Exists(Path.Combine(_folder, "Staged", "ButtonBehavior.behx")).ShouldBeTrue();
        Directory.GetFiles(Path.Combine(_folder, "Staged")).ShouldAllBe(file => file.EndsWith(".behx", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Feature", "GCLI-012")]
    [Trait("Feature", "GCLI-015")]
    public void ImportScreen_AddsAScreenFileToTheProject_AndSvgDrawsIt()
    {
        string project = NewProject("Game", "empty");
        string screenFile = Path.Combine(_folder, "Title.gusx");
        File.WriteAllText(screenFile, RedBoxScreen("Title"));

        GumCliProcess imported = Cli("import-screen", project, screenFile, "--subfolder", "Menus");
        GumCliProcess checkedProject = Cli("check", project);
        GumCliProcess svg = Cli("svg", project, "Menus/Title", "--output", Path.Combine(_folder, "title.svg"));

        imported.ExitCode.ShouldBe(0, imported.Transcript);
        File.ReadAllText(project).ShouldContain("<ScreenReference Name=\"Menus/Title\" />");
        File.Exists(Path.Combine(_folder, "Game", "Screens", "Menus", "Title.gusx")).ShouldBeTrue();
        checkedProject.ExitCode.ShouldBe(0, checkedProject.Transcript);
        svg.ExitCode.ShouldBe(0, svg.Transcript);
        string drawn = File.ReadAllText(Path.Combine(_folder, "title.svg"));
        drawn.ShouldContain("<svg");
        drawn.ShouldContain("fill=\"red\"");
    }

    // Windows-only: raylib's window creation hangs on the macOS runners, and diff-screenshots
    // renders through raylib too (see ScreenshotCommandTests).
    [Fact]
    [Trait("Feature", "GCLI-009")]
    [Trait("Feature", "GCLI-010")]
    public void Screenshot_RendersTheScreensPixels_AndDiffScreenshotsFindsBothBackendsAgree()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }
        string project = NewProject("Game", "empty");
        string screenFile = Path.Combine(_folder, "Title.gusx");
        File.WriteAllText(screenFile, RedBoxScreen("Title"));
        Cli("import-screen", project, screenFile).ExitCode.ShouldBe(0);
        string png = Path.Combine(_folder, "title.png");

        GumCliProcess shot = Cli("screenshot", project, "Title", "--output", png, "--backend", "raylib");
        GumCliProcess diff = Cli("diff-screenshots", project, "--output", Path.Combine(_folder, "diff"));

        shot.ExitCode.ShouldBe(0, shot.Transcript);
        using (SKBitmap bitmap = SKBitmap.Decode(png))
        {
            bitmap.Width.ShouldBe(800);
            bitmap.Height.ShouldBe(600);
            bitmap.GetPixel(50, 50).ShouldBe(new SKColor(255, 0, 0, 255), "inside the red box");
            bitmap.GetPixel(400, 400).Alpha.ShouldBe((byte)0, "outside it the default background is transparent");
        }
        diff.ExitCode.ShouldBe(0, diff.Transcript);
        diff.StandardOutput.ShouldContain("All 1 element(s) matched.");
    }

    #region Helpers

    private GumCliProcess Cli(params string[] args) => GumCliProcess.Run(_folder, args);

    private string NewProject(string name, string template)
    {
        string project = Path.Combine(_folder, name, name + ".gumx");
        GumCliProcess created = Cli("new", project, "--template", template);
        created.ExitCode.ShouldBe(0, created.Transcript);
        return project;
    }

    /// <summary>Writes a component file by hand, as an agent or script does, and references it from the project.</summary>
    private static string AddComponent(string project, string name, string xml)
    {
        string file = Path.Combine(Path.GetDirectoryName(project)!, "Components", name + ".gucx");
        File.WriteAllText(file, xml);
        File.WriteAllText(project, File.ReadAllText(project)
            .Replace("</GumProjectSave>", $"  <ComponentReference Name=\"{name}\" />\n</GumProjectSave>"));
        return file;
    }

    /// <summary>A screen holding a filled red 100x100 rectangle at (10, 10).</summary>
    private static string RedBoxScreen(string name) => $"""
        <?xml version="1.0" encoding="utf-8"?>
        <ScreenSave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
          <Name>{name}</Name>
          <State>
            <Name>Default</Name>
            <Variable Type="float" Name="Box.X" SetsValue="true"><Value xsi:type="xsd:float">10</Value></Variable>
            <Variable Type="float" Name="Box.Y" SetsValue="true"><Value xsi:type="xsd:float">10</Value></Variable>
            <Variable Type="float" Name="Box.Width" SetsValue="true"><Value xsi:type="xsd:float">100</Value></Variable>
            <Variable Type="float" Name="Box.Height" SetsValue="true"><Value xsi:type="xsd:float">100</Value></Variable>
            <Variable Type="bool" Name="Box.IsFilled" SetsValue="true"><Value xsi:type="xsd:boolean">true</Value></Variable>
            <Variable Type="int" Name="Box.FillGreen" SetsValue="true"><Value xsi:type="xsd:int">0</Value></Variable>
            <Variable Type="int" Name="Box.FillBlue" SetsValue="true"><Value xsi:type="xsd:int">0</Value></Variable>
          </State>
          <Instance>
            <Name>Box</Name>
            <BaseType>Rectangle</BaseType>
          </Instance>
        </ScreenSave>
        """;

    private static Dictionary<string, byte[]> Snapshot(string folder) =>
        Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
            .ToDictionary(file => Path.GetRelativePath(folder, file).Replace('\\', '/'), File.ReadAllBytes);

    private static void ShouldMatch(Dictionary<string, byte[]> actual, Dictionary<string, byte[]> expected, string because)
    {
        List<string> differences = expected.Keys.Union(actual.Keys)
            .Where(path => !expected.TryGetValue(path, out byte[]? before) || !actual.TryGetValue(path, out byte[]? after) || !before.AsSpan().SequenceEqual(after))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
        differences.ShouldBeEmpty(because);
    }

    #endregion
}
