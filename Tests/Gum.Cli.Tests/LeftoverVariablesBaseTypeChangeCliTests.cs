using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Gum.ProjectServices.CodeGeneration;
using Gum.Tests.Shared;
using Shouldly;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Gum.Cli.Tests;

/// <summary>
/// A project saved after base types changed (Text to Container) still holds the Text-only variables the
/// tool keeps on purpose (<see cref="LeftoverVariablesProject"/>). Those leftovers must never be reported
/// as errors or warnings, must not block <c>check</c> or <c>codegen</c>, and must survive a save and reload.
/// A new check that reports leftovers, or a save that drops them, turns these red.
/// </summary>
public class LeftoverVariablesBaseTypeChangeCliTests : BaseTestClass
{
    private string CreateProject(bool typeChanged, bool withReader = true)
    {
        string gumx = Path.Combine(TempDirectory, "Leftovers" + typeChanged, "Leftovers.gumx");
        CliTestHelper.Run("new", gumx, "--template", "empty").ExitCode.ShouldBe(0);
        GeneratedGameHarness.EditProject(gumx, project =>
        {
            LeftoverVariablesProject.AddComponents(project, typeChanged);
            if (!withReader)
            {
                project.Components.RemoveAll(component => component.Name == "Reader");
                project.ComponentReferences.RemoveAll(reference => reference.Name == "Reader");
            }
        });
        return gumx;
    }

    private static List<(string Element, string? Code, string Severity)> CheckResults(string gumx)
    {
        CliTestHelper result = CliTestHelper.Run("check", gumx, "--json");
        result.ExitCode.ShouldBe(0, result.StandardOutput);
        using JsonDocument document = JsonDocument.Parse(result.StandardOutput);
        return document.RootElement.EnumerateArray()
            .Select(entry => (
                entry.GetProperty("element").GetString()!,
                entry.TryGetProperty("code", out JsonElement code) ? code.GetString() : null,
                entry.GetProperty("severity").GetString()!))
            .ToList();
    }

    [Fact]
    public void Check_LeftoverVariables_ReportNothingAndExitZero()
    {
        // Wrapper is the component that was Text-based, kept Text and FontSize, and now has a Text instance named Inner.
        string gumx = CreateProject(typeChanged: true, withReader: false);

        CheckResults(gumx).ShouldBeEmpty();

        CliTestHelper human = CliTestHelper.Run("check", gumx);
        human.ExitCode.ShouldBe(0);
        human.StandardOutput.ShouldContain("No errors found.");
        human.StandardOutput.ShouldNotContain("warning:");
    }

    [Fact]
    public void Check_ReferenceToExposedLeftover_NamesInstanceTypeInWarning_Issue7151()
    {
        // The only result on a project with leftovers: GUM0009 for the reference that no longer applies. It
        // names the real cause (Caption's type lost Text), not that Card lacks CaptionText, which is still in
        // Card's file and in the Variables tab. Nothing else may start to be reported.
        CheckResults(CreateProject(typeChanged: false)).ShouldBeEmpty();

        string gumx = CreateProject(typeChanged: true);

        List<(string Element, string? Code, string Severity)> results = CheckResults(gumx);
        results.ShouldBe(new[] { ("Reader", (string?)"GUM0009", "Warning") });
        CliTestHelper.Run("check", gumx).StandardOutput
            .ShouldContain("which exposes Caption.Text, but Caption's type (Container) has no Text variable");
        GumProjectSave.Load(gumx, out _)!.Components.Single(c => c.Name == "Card")
            .DefaultState!.Variables.ShouldContain(variable => variable.ExposedAsName == "CaptionText");
    }

    [Theory]
    [InlineData(ObjectInstantiationType.FindByName)]
    [InlineData(ObjectInstantiationType.FullyInCode)]
    public void Codegen_LeftoverVariables_GeneratesEveryElementAndSkipsVariablesTheTypeLacks(ObjectInstantiationType instantiationType)
    {
        string gumx = GeneratedGameHarness.CreateGame(TempDirectory, instantiationType);
        GeneratedGameHarness.EditProject(gumx, project =>
        {
            LeftoverVariablesProject.AddComponents(project, typeChanged: true);
            RemoveConsumerOfExposedLeftover(project);
        });

        CliTestHelper result = GeneratedGameHarness.Codegen(gumx);

        result.ExitCode.ShouldBe(0, result.StandardError);
        string[] generated = Directory.GetFiles(TempDirectory, "*.Generated.cs", SearchOption.AllDirectories);
        foreach (string name in new[] { "Label", "Card", "Outer", "Reader", "Wrapper" })
        {
            generated.ShouldContain(path => Path.GetFileName(path) == name + ".Generated.cs", $"{name} was skipped");
        }
        File.ReadAllText(generated.Single(path => Path.GetFileName(path) == "Label.Generated.cs"))
            .ShouldNotContain(LeftoverVariablesProject.LabelText);
        File.ReadAllText(generated.Single(path => Path.GetFileName(path) == "Wrapper.Generated.cs"))
            .ShouldNotContain(LeftoverVariablesProject.LabelText);
        GeneratedGameHarness.Compile(TempDirectory).ShouldBeEmpty();
    }

    [Fact]
    public void Codegen_ConsumerOfExposedLeftover_FullyInCodeDoesNotCompile_Issue7152()
    {
        // Pins today's output: the only error is the consumer's assignment. When #7152 is fixed this
        // expectation changes to an empty list.
        string gumx = GeneratedGameHarness.CreateGame(TempDirectory, ObjectInstantiationType.FullyInCode);
        GeneratedGameHarness.EditProject(gumx, project => LeftoverVariablesProject.AddComponents(project, typeChanged: true));

        GeneratedGameHarness.Codegen(gumx).ExitCode.ShouldBe(0);

        List<string> errors = GeneratedGameHarness.Compile(TempDirectory);
        errors.Count.ShouldBe(1);
        errors[0].ShouldContain("Outer.Generated.cs");
        errors[0].ShouldContain("CS1061");
        errors[0].ShouldContain("CaptionText");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resave_LeftoverVariables_KeepEveryVariableBlockAndSurviveReload(bool raw)
    {
        string gumx = CreateProject(typeChanged: true);
        string[] files = Directory.GetFiles(Path.GetDirectoryName(gumx)!, "*.gucx", SearchOption.AllDirectories);
        files.Length.ShouldBe(5);
        Dictionary<string, string> saved = files.ToDictionary(file => file, File.ReadAllText);

        CliTestHelper result = CliTestHelper.Run(raw ? new[] { "resave", gumx, "--raw" } : new[] { "resave", gumx });

        result.ExitCode.ShouldBe(0, result.StandardError);
        foreach (KeyValuePair<string, string> file in saved)
        {
            // The default resave also back-fills the State variable the tool writes; every variable the
            // project had before must come out character for character.
            string resaved = File.ReadAllText(file.Key);
            foreach (Match block in Regex.Matches(file.Value, @"<(Variable|VariableList)[ >].*?</\1>", RegexOptions.Singleline))
            {
                resaved.ShouldContain(block.Value, Case.Sensitive, file.Key);
            }
        }
        GumProjectSave expected = new GumProjectSave();
        LeftoverVariablesProject.AddComponents(expected, typeChanged: true);
        LeftoverVariablesProject.Snapshot(expected).ShouldBeSubsetOf(LeftoverVariablesProject.Snapshot(GumProjectSave.Load(gumx, out _)!));
    }

    private static void RemoveConsumerOfExposedLeftover(GumProjectSave project)
    {
        project.Components.Single(component => component.Name == "Outer").DefaultState!.Variables.Clear();
    }
}
