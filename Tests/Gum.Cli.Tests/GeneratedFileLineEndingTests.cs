using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Gum.ProjectServices.CodeGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using System.Text.RegularExpressions;

namespace Gum.Cli.Tests;

/// <summary>
/// Every file <c>gumcli codegen</c> writes uses one line ending, the same on every OS (CRLF, which is what
/// Windows has always produced), so editors and git never see a half-converted file.
/// </summary>
public class GeneratedFileLineEndingTests : BaseTestClass
{
    [Fact]
    public void Codegen_GeneratedFilesAndStubs_UseOneLineEndingEach()
    {
        string gumx = CreateGame();
        EditProject(gumx, project =>
        {
            AddScreen(project, "MainMenu", ("PlayText", "Text"));
            AddComponent(project, "Card", "Container", ("Title", "Text"));
        });

        CliTestHelper.Run("codegen", gumx).ExitCode.ShouldBe(0);

        string[] files = Directory.GetFiles(TempDirectory, "*.cs", SearchOption.AllDirectories);
        files.Length.ShouldBeGreaterThanOrEqualTo(5);
        List<string> notCrLf = new List<string>();
        foreach (string path in files)
        {
            string text = File.ReadAllText(path);
            int crlf = Regex.Matches(text, "\r\n").Count;
            int lf = Regex.Matches(text, "(?<!\r)\n").Count;
            if (crlf == 0 || lf > 0)
            {
                notCrLf.Add($"{Path.GetRelativePath(TempDirectory, path)}: {crlf} CRLF, {lf} LF");
            }
        }
        notCrLf.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("A\nB")]
    [InlineData("say \"hi\"\nC:\\dir")]
    public void Codegen_TextWithLineBreaks_KeepsItsValueAndPutsNoRawNewlineInAStringLiteral(string text)
    {
        string gumx = CreateGame();
        string gumFolder = Path.GetDirectoryName(gumx)! + Path.DirectorySeparatorChar;
        CodeOutputProjectSettingsManager settingsManager = new CodeOutputProjectSettingsManager(
            new SilentLogger(), new FixedProjectDirectoryProvider(gumFolder));
        CodeOutputProjectSettings settings = settingsManager.CreateOrLoadSettingsForProject();
        settings.OutputLibrary = OutputLibrary.GumForms;
        settings.ObjectInstantiationType = ObjectInstantiationType.FullyInCode;
        settingsManager.WriteSettingsForProject(settings);
        EditProject(gumx, project =>
        {
            AddScreen(project, "MainMenu", ("PlayText", "Text"));
            project.GetScreenSave("MainMenu")!.DefaultState!.Variables.Add(new VariableSave
            {
                Name = "PlayText.Text", Type = "string", Value = text, SetsValue = true,
            });
        });

        CliTestHelper.Run("codegen", gumx).ExitCode.ShouldBe(0);

        string generated = File.ReadAllText(Directory.GetFiles(TempDirectory, "MainMenu.Generated.cs", SearchOption.AllDirectories).Single());
        List<string> literals = CSharpSyntaxTree.ParseText(generated).GetRoot().DescendantTokens()
            .Where(token => token.IsKind(SyntaxKind.StringLiteralToken))
            .Select(token => token.ValueText)
            .ToList();
        literals.ShouldContain(text);
        foreach (SyntaxToken token in CSharpSyntaxTree.ParseText(generated).GetRoot().DescendantTokens()
            .Where(token => token.IsKind(SyntaxKind.StringLiteralToken)))
        {
            token.Text.ShouldNotContain("\n");
            token.Text.ShouldNotContain("\r");
        }
    }

    private class SilentLogger : ICodeGenLogger
    {
        public void PrintOutput(string message) { }

        public void PrintError(string message) { }
    }

    private string CreateGame()
    {
        File.WriteAllText(Path.Combine(TempDirectory, "MyGame.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><RootNamespace>MyGame</RootNamespace></PropertyGroup></Project>");
        string gumx = Path.Combine(TempDirectory, "Content", "GumProject", "GumProject.gumx");
        CliTestHelper.Run("new", gumx, "--template", "empty").ExitCode.ShouldBe(0);
        CliTestHelper.Run("codegen-init", gumx).ExitCode.ShouldBe(0);
        return gumx;
    }

    private static void EditProject(string gumx, Action<GumProjectSave> edit)
    {
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        edit(project);
        project.Save(gumx, saveElements: true);
    }

    private static void AddScreen(GumProjectSave project, string name, params (string Name, string BaseType)[] instances)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        foreach ((string instanceName, string baseType) in instances)
        {
            screen.Instances.Add(new InstanceSave { Name = instanceName, BaseType = baseType, ParentContainer = screen });
        }
        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference { Name = name, ElementType = ElementType.Screen });
    }

    private static void AddComponent(GumProjectSave project, string name, string baseType, params (string Name, string BaseType)[] instances)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        foreach ((string instanceName, string instanceBaseType) in instances)
        {
            component.Instances.Add(new InstanceSave { Name = instanceName, BaseType = instanceBaseType, ParentContainer = component });
        }
        project.Components.Add(component);
        project.ComponentReferences.Add(new ElementReference { Name = name, ElementType = ElementType.Component });
    }
}
