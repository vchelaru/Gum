using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Shouldly;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Gum.Cli.Tests;

/// <summary>
/// What a beginner does after adding a Gum project to an existing MonoGame game: names things the way
/// people name things, turns on code generation, generates, edits in the tool and generates again.
/// Each test builds the game folder the tool would leave (<c>MyGame.csproj</c> and
/// <c>Content/GumProject/</c>), runs <c>gumcli codegen</c> and compiles the output against the real
/// MonoGame runtime, so a generated class that does not build fails here instead of in the game.
/// </summary>
public class BeginnerToolCodegenTests : BaseTestClass
{
    #region Naming

    [Theory]
    [InlineData("Main Menu")]
    [InlineData("2PlayerScreen")]
    [InlineData("event")]
    [InlineData("MyGame")]
    [InlineData("Screen")]
    public void Codegen_ScreenNamedTheWayABeginnerNamesIt_GeneratesCompilableClass(string screenName)
    {
        string gumx = CreateGame();
        EditProject(gumx, project => AddScreen(project, screenName, ("Title", "Text")));

        Codegen(gumx).ExitCode.ShouldBe(0);

        Compile().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Score Panel")]
    [InlineData("3DView")]
    [InlineData("object")]
    [InlineData("Menus/Main Menu")]
    [InlineData("Menus/2Player")]
    public void Codegen_ComponentNamedTheWayABeginnerNamesIt_GeneratesCompilableClass(string componentName)
    {
        string gumx = CreateGame();
        EditProject(gumx, project =>
        {
            AddComponent(project, componentName, "Container", ("Title", "Text"));
            AddScreen(project, "Hud", ("PanelInstance", componentName));
        });

        Codegen(gumx).ExitCode.ShouldBe(0);

        Compile().ShouldBeEmpty();
    }

    [Fact]
    public void Codegen_InstancesNamedLikeMembersOfTheFormsBaseClass_GeneratesCompilableClass()
    {
        // The tool rejects names of standard variables (Width, Visible, Parent, Name); these it accepts.
        string gumx = CreateGame();
        EditProject(gumx, project => AddComponent(project, "Header", "Container",
            ("Visual", "Container"), ("IsEnabled", "Text"), ("IsFocused", "Text"), ("Dispose", "Text"),
            ("ToString", "Text"), ("GetHashCode", "Text"), ("Tag", "Text"), ("Children", "Container")));

        Codegen(gumx).ExitCode.ShouldBe(0);

        Compile().ShouldBeEmpty();
    }

    [Fact]
    public void Codegen_ScreenAndComponentShareAName_InstanceOfTheComponentGetsTheComponentType()
    {
        string gumx = CreateGame();
        EditProject(gumx, project =>
        {
            AddComponent(project, "Inventory", "Container", ("Slot", "Text"));
            AddScreen(project, "Inventory", ("InventoryInstance", "Inventory"));
        });

        Codegen(gumx).ExitCode.ShouldBe(0);

        Compile().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Button")]
    [InlineData("Label")]
    [InlineData("TextBox")]
    [InlineData("Text")]
    public void Codegen_ComponentNamedLikeAFormsControl_InstanceOfItCompiles(string componentName)
    {
        string gumx = CreateGame();
        EditProject(gumx, project =>
        {
            AddComponent(project, componentName, "Container", ("Caption", "Text"));
            AddScreen(project, "Menu", ("First", componentName), ("Second", "Text"));
        });

        Codegen(gumx).ExitCode.ShouldBe(0);

        Compile().ShouldBeEmpty();
    }

    [Fact]
    public void Codegen_StateNamedLikeAnInstance_GeneratesCompilableClass()
    {
        string gumx = CreateGame();
        EditProject(gumx, project =>
        {
            ComponentSave component = AddComponent(project, "Card", "Container", ("Label", "Text"), ("Icon", "Sprite"));
            AddCategory(component, "Mode", "Label", "Icon", "Default2");
        });

        Codegen(gumx).ExitCode.ShouldBe(0);

        Compile().ShouldBeEmpty();
    }

    #endregion

    #region Empty and minimal elements

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Codegen_EmptyScreenAndComponentWithNoChildren_GenerateCompilableClasses(int instantiationType)
    {
        string gumx = CreateGame(instantiationType: instantiationType);
        EditProject(gumx, project =>
        {
            AddScreen(project, "Blank");
            AddComponent(project, "Spacer", "Container");
        });

        Codegen(gumx).ExitCode.ShouldBe(0);

        Compile().ShouldBeEmpty();
        File.Exists(Path.Combine(TempDirectory, "Screens", "Blank.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(TempDirectory, "Components", "Spacer.Generated.cs")).ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Codegen_ElementInASubfolderWithStates_GeneratesCompilableClassInTheFolderNamespace(int instantiationType)
    {
        string gumx = CreateGame(instantiationType: instantiationType);
        EditProject(gumx, project =>
        {
            ComponentSave panel = AddComponent(project, "Menus/Panel", "Container", ("Title", "Text"));
            AddCategory(panel, "Size", "Small", "Big");
            AddScreen(project, "Menus/Pause", ("PanelInstance", "Menus/Panel"));
        });

        Codegen(gumx).ExitCode.ShouldBe(0);

        Compile().ShouldBeEmpty();
        File.ReadAllText(Path.Combine(TempDirectory, "Components", "Menus", "Panel.Generated.cs"))
            .ShouldContain("namespace MyGame.Components.Menus");
        File.ReadAllText(Path.Combine(TempDirectory, "Screens", "Menus", "Pause.Generated.cs"))
            .ShouldContain("namespace MyGame.Screens.Menus");
    }

    #endregion

    #region Regenerating

    [Fact]
    public void Codegen_RunTwice_RewritesNoGeneratedFileAndNeverTouchesTheHandWrittenHalf()
    {
        string gumx = CreateGame();
        EditProject(gumx, project => AddScreen(project, "MainMenu", ("PlayText", "Text")));
        Codegen(gumx).ExitCode.ShouldBe(0);
        string custom = Path.Combine(TempDirectory, "Screens", "MainMenu.cs");
        File.WriteAllText(custom, "namespace MyGame.Screens\n{\n    partial class MainMenu\n    {\n        partial void CustomInitialize()\n        {\n            System.Console.WriteLine(\"mine\");\n        }\n    }\n}\n");
        Dictionary<string, byte[]> before = Snapshot(TempDirectory);

        CliTestHelper second = Codegen(gumx);

        second.ExitCode.ShouldBe(0);
        Dictionary<string, byte[]> after = Snapshot(TempDirectory);
        after.Keys.OrderBy(key => key).ShouldBe(before.Keys.OrderBy(key => key));
        foreach (string path in before.Keys)
        {
            after[path].ShouldBe(before[path], $"{path} changed on the second run");
        }
        Compile().ShouldBeEmpty();
    }

    [Fact]
    public void Codegen_AfterAddingAnInstance_GeneratedFileHasItAndTheHandWrittenHalfIsUntouched()
    {
        string gumx = CreateGame();
        EditProject(gumx, project => AddScreen(project, "MainMenu", ("PlayText", "Text")));
        Codegen(gumx).ExitCode.ShouldBe(0);
        string custom = Path.Combine(TempDirectory, "Screens", "MainMenu.cs");
        string handWritten = "namespace MyGame.Screens\n{\n    partial class MainMenu\n    {\n        partial void CustomInitialize()\n        {\n            var x = PlayText;\n        }\n    }\n}\n";
        File.WriteAllText(custom, handWritten);

        EditProject(gumx, project => project.GetScreenSave("MainMenu")!.Instances.Add(
            new InstanceSave { Name = "QuitText", BaseType = "Text", ParentContainer = project.GetScreenSave("MainMenu")! }));
        Codegen(gumx).ExitCode.ShouldBe(0);

        File.ReadAllText(Path.Combine(TempDirectory, "Screens", "MainMenu.Generated.cs")).ShouldContain("QuitText");
        File.ReadAllText(custom).ShouldBe(handWritten);
        Compile().ShouldBeEmpty();
    }

    [Fact]
    public void Codegen_AfterRenamingAnInstance_GeneratedFileUsesTheNewNameOnly()
    {
        string gumx = CreateGame();
        EditProject(gumx, project => AddScreen(project, "MainMenu", ("ButtonInstance", "Text")));
        Codegen(gumx).ExitCode.ShouldBe(0);

        EditProject(gumx, project =>
        {
            InstanceSave instance = project.GetScreenSave("MainMenu")!.Instances.Single();
            instance.Name = "PlayButton";
        });
        Codegen(gumx).ExitCode.ShouldBe(0);

        string generated = File.ReadAllText(Path.Combine(TempDirectory, "Screens", "MainMenu.Generated.cs"));
        generated.ShouldContain("PlayButton");
        generated.ShouldNotContain("ButtonInstance");
        Compile().ShouldBeEmpty();
    }

    [Fact]
    public void Codegen_AfterRenamingAnElementInTheTool_NewClassIsGeneratedAndTheGameStillBuilds()
    {
        string gumx = CreateGame();
        EditProject(gumx, project => AddComponent(project, "ScorePanel", "Container", ("Value", "Text")));
        Codegen(gumx).ExitCode.ShouldBe(0);

        RenameComponent(gumx, "ScorePanel", "ScoreBox");
        CliTestHelper second = Codegen(gumx);

        second.ExitCode.ShouldBe(0, second.StandardError);
        File.Exists(Path.Combine(TempDirectory, "Components", "ScoreBox.Generated.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(TempDirectory, "Components", "ScoreBox.cs")).ShouldBeTrue();
        Compile().ShouldBeEmpty("the stale ScorePanel files are still in the game and must not stop it from building");
    }

    [Fact]
    public void Codegen_AfterRenamingAnElementWithPrune_DeletesOnlyTheStaleGeneratedFileAndListsTheStaleCustomFile()
    {
        string gumx = CreateGame();
        EditProject(gumx, project => AddComponent(project, "ScorePanel", "Container", ("Value", "Text")));
        Codegen(gumx).ExitCode.ShouldBe(0);

        RenameComponent(gumx, "ScorePanel", "ScoreBox");
        CliTestHelper pruned = Codegen(gumx, "--prune");

        pruned.ExitCode.ShouldBe(0, pruned.StandardError);
        File.Exists(Path.Combine(TempDirectory, "Components", "ScorePanel.Generated.cs")).ShouldBeFalse();
        File.Exists(Path.Combine(TempDirectory, "Components", "ScorePanel.cs")).ShouldBeTrue("custom code is never deleted by codegen");
        pruned.StandardOutput.ShouldContain("ScorePanel.cs", Case.Sensitive, "the leftover custom file is named so the beginner can delete it");
    }

    [Fact]
    public void Codegen_AfterDeletingAVariableTheCustomCodeUsedNothingFrom_StillBuilds()
    {
        string gumx = CreateGame();
        EditProject(gumx, project =>
        {
            ComponentSave component = AddComponent(project, "Card", "Container", ("Title", "Text"));
            component.DefaultState!.Variables.Add(new VariableSave { Name = "Title.Width", Type = "float", Value = 80f, SetsValue = true });
        });
        Codegen(gumx).ExitCode.ShouldBe(0);

        EditProject(gumx, project => project.GetComponentSave("Card")!.DefaultState!.Variables.RemoveAll(variable => variable.Name == "Title.Width"));
        Codegen(gumx).ExitCode.ShouldBe(0);

        File.ReadAllText(Path.Combine(TempDirectory, "Components", "Card.Generated.cs")).ShouldNotContain("Width = 80");
        Compile().ShouldBeEmpty();
    }

    [Fact]
    public void Codegen_HandWrittenHalfStillUsesARenamedInstance_BuildFailsOnTheOldName()
    {
        string gumx = CreateGame();
        EditProject(gumx, project => AddScreen(project, "MainMenu", ("ButtonInstance", "Text")));
        Codegen(gumx).ExitCode.ShouldBe(0);
        File.WriteAllText(Path.Combine(TempDirectory, "Screens", "MainMenu.cs"),
            "namespace MyGame.Screens\n{\n    partial class MainMenu\n    {\n        partial void CustomInitialize()\n        {\n            var text = ButtonInstance;\n        }\n    }\n}\n");
        EditProject(gumx, project => project.GetScreenSave("MainMenu")!.Instances.Single().Name = "PlayButton");
        Codegen(gumx).ExitCode.ShouldBe(0);

        List<string> errors = Compile();

        errors.Count.ShouldBe(1, string.Join("\n", errors));
        errors[0].ShouldContain("ButtonInstance", Case.Sensitive, "the error names the instance the beginner renamed");
    }

    #endregion

    #region Paths and files

    [Fact]
    public void Codegen_GumProjectFolderWithASpace_WritesCodeNextToTheGameProject()
    {
        string gumx = CreateGame(projectFolder: "Gum Project");
        EditProject(gumx, project => AddScreen(project, "MainMenu", ("PlayText", "Text")));

        CliTestHelper result = Codegen(gumx);

        result.ExitCode.ShouldBe(0, result.StandardError);
        File.Exists(Path.Combine(TempDirectory, "Screens", "MainMenu.Generated.cs")).ShouldBeTrue();
        Compile().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("GeneratedCodeFolder", "Gum\\Generated", "Gum/Generated/Screens/MainMenu.Generated.cs")]
    [InlineData("CustomCodeFolder", "Gum\\Mine", "Gum/Mine/Screens/MainMenu.cs")]
    [InlineData("GeneratedCodeFolderPrefix", "Gum\\", "Gum/Screens/MainMenu.Generated.cs")]
    public void Codegen_FolderSettingSavedWithWindowsBackslashes_WritesIntoTheNestedFolder(string setting, string value, string expectedFile)
    {
        string gumx = CreateGame();
        EditProject(gumx, project => AddScreen(project, "MainMenu", ("PlayText", "Text")));
        SetCodeSetting(gumx, setting, value);

        CliTestHelper result = Codegen(gumx);

        result.ExitCode.ShouldBe(0, result.StandardError);
        File.Exists(Path.Combine(TempDirectory, expectedFile.Replace('/', Path.DirectorySeparatorChar)))
            .ShouldBeTrue($"{setting} = {value}; files: {string.Join(", ", Directory.GetFiles(TempDirectory, "MainMenu*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(TempDirectory, path)))}");
    }

    [Fact]
    public void Codegen_CodeProjectRootSavedWithBackslashes_WritesCodeNextToTheGameProject()
    {
        string gumx = CreateGame();
        EditProject(gumx, project => AddScreen(project, "MainMenu", ("PlayText", "Text")));
        SetCodeSetting(gumx, "CodeProjectRoot", "..\\..\\");

        CliTestHelper result = Codegen(gumx);

        result.ExitCode.ShouldBe(0, result.StandardError);
        File.Exists(Path.Combine(TempDirectory, "Screens", "MainMenu.Generated.cs")).ShouldBeTrue();
    }

    [Fact]
    public void Codegen_CodeProjectRootSavedAsAnAbsolutePath_WritesThereAndNotUnderTheGumProject()
    {
        string gumx = CreateGame();
        EditProject(gumx, project => AddScreen(project, "MainMenu", ("PlayText", "Text")));
        SetCodeSetting(gumx, "CodeProjectRoot", TempDirectory.Replace('\\', '/') + "/");

        CliTestHelper result = Codegen(gumx);

        result.ExitCode.ShouldBe(0, result.StandardError);
        File.Exists(Path.Combine(TempDirectory, "Screens", "MainMenu.Generated.cs")).ShouldBeTrue();
    }

    [Fact]
    public void Resave_ProjectWithCodeSettings_KeepsTheSettingsFileByteForByte()
    {
        string gumx = CreateGame();
        SetCodeSetting(gumx, "GeneratedCodeFolder", "Gum/Generated");
        string settings = Path.Combine(Path.GetDirectoryName(gumx)!, "ProjectCodeSettings.codsj");
        byte[] before = File.ReadAllBytes(settings);

        CliTestHelper.Run("resave", gumx).ExitCode.ShouldBe(0);

        File.ReadAllBytes(settings).ShouldBe(before);
    }

    #endregion

    #region Forms template

    [Fact]
    public void Codegen_FormsTemplateWithABeginnersScreenAndDerivedComponent_BuildsInBothInstantiationModes()
    {
        string gumx = CreateGame(template: "forms");
        EditProject(gumx, project =>
        {
            ComponentSave derived = AddComponent(project, "Controls/ButtonBig", "Controls/ButtonStandard");
            derived.DefaultState!.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 300f, SetsValue = true });
            ComponentSave panel = AddComponent(project, "Controls/StartPanel", "Container",
                ("StartButton", "Controls/ButtonStandard"), ("NameBox", "Controls/TextBox"), ("Hint", "Text"), ("Big", "Controls/ButtonBig"));
            AddCategory(panel, "Look", "Plain", "Fancy");
            AddScreen(project, "MainMenu", ("StartPanelInstance", "Controls/StartPanel"), ("QuitButton", "Controls/ButtonStandard"));
        });

        foreach (int instantiationType in new[] { 1, 0 })
        {
            SetCodeSetting(gumx, "ObjectInstantiationType", instantiationType.ToString());
            CliTestHelper result = Codegen(gumx);
            result.ExitCode.ShouldBe(0, result.StandardError);
            Compile().ShouldBeEmpty($"ObjectInstantiationType {instantiationType}");
        }
        File.ReadAllText(Path.Combine(TempDirectory, "Components", "Controls", "ButtonBig.Generated.cs"))
            .ShouldContain("ButtonBig");
    }

    [Fact]
    public void AddForms_RunTwiceOnAnEmptyProject_SecondRunAddsNothingAndStillExitsZero()
    {
        string gumx = CreateGame(template: "empty");
        CliTestHelper.Run("add-forms", gumx).ExitCode.ShouldBe(0);
        Dictionary<string, byte[]> before = Snapshot(Path.GetDirectoryName(gumx)!);

        CliTestHelper second = CliTestHelper.Run("add-forms", gumx);

        second.ExitCode.ShouldBe(0, second.StandardError);
        second.StandardOutput.ShouldContain("0 component(s)");
        Snapshot(Path.GetDirectoryName(gumx)!).Keys.OrderBy(key => key).ShouldBe(before.Keys.OrderBy(key => key));
    }

    [Fact]
    public void AddForms_ProjectWithABeginnersOwnButtonComponent_KeepsTheirComponentUntouched()
    {
        string gumx = CreateGame(template: "empty");
        EditProject(gumx, project => AddComponent(project, "Controls/ButtonStandard", "Container", ("Mine", "Text")));
        string theirs = Path.Combine(Path.GetDirectoryName(gumx)!, "Components", "Controls", "ButtonStandard.gucx");
        byte[] before = File.ReadAllBytes(theirs);

        CliTestHelper result = CliTestHelper.Run("add-forms", gumx);

        result.ExitCode.ShouldBe(0, result.StandardError);
        File.ReadAllBytes(theirs).ShouldBe(before);
    }

    [Fact]
    public void Check_FormsProjectAfterAddingABeginnersScreen_ReportsNoErrors()
    {
        string gumx = CreateGame(template: "forms");
        EditProject(gumx, project => AddScreen(project, "MainMenu", ("StartButton", "Controls/ButtonStandard"), ("NameBox", "Controls/TextBox")));

        CliTestHelper result = CliTestHelper.Run("check", gumx);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
    }

    #endregion

    #region Harness

    private string CreateGame(string template = "empty", string projectFolder = "GumProject", int instantiationType = 1, string csprojName = "MyGame")
    {
        File.WriteAllText(Path.Combine(TempDirectory, csprojName + ".csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><RootNamespace>MyGame</RootNamespace></PropertyGroup>" +
            "<ItemGroup><PackageReference Include=\"MonoGame.Framework.DesktopGL\" Version=\"3.8.4\" /></ItemGroup></Project>");
        string gumx = Path.Combine(TempDirectory, "Content", projectFolder, "GumProject.gumx");
        CliTestHelper.Run("new", gumx, "--template", template).ExitCode.ShouldBe(0);
        CliTestHelper.Run("codegen-init", gumx).ExitCode.ShouldBe(0);
        if (instantiationType != 1)
        {
            SetCodeSetting(gumx, "ObjectInstantiationType", instantiationType.ToString());
        }
        return gumx;
    }

    private CliTestHelper Codegen(string gumx, params string[] extra) =>
        CliTestHelper.Run(new[] { "codegen", gumx }.Concat(extra).ToArray());

    private static void SetCodeSetting(string gumx, string key, string value)
    {
        string path = Path.Combine(Path.GetDirectoryName(gumx)!, "ProjectCodeSettings.codsj");
        string json = File.ReadAllText(path);
        string literal = int.TryParse(value, out int number) ? number.ToString() : "\"" + value.Replace("\\", "\\\\") + "\"";
        Regex existing = new Regex("\"" + key + "\"\\s*:\\s*(\"(?:[^\"\\\\]|\\\\.)*\"|\\d+|true|false)");
        json = existing.IsMatch(json)
            ? existing.Replace(json, "\"" + key + "\": " + literal.Replace("$", "$$"), 1)
            : json.Replace("{", "{\n  \"" + key + "\": " + literal + ",", StringComparison.Ordinal);
        File.WriteAllText(path, json);
    }

    private static void EditProject(string gumx, Action<GumProjectSave> edit)
    {
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        edit(project);
        project.Save(gumx, saveElements: true);
    }

    private static void RenameComponent(string gumx, string oldName, string newName)
    {
        string folder = Path.GetDirectoryName(gumx)!;
        EditProject(gumx, project =>
        {
            ComponentSave component = project.GetComponentSave(oldName)!;
            component.Name = newName;
            project.ComponentReferences.Single(reference => reference.Name == oldName).Name = newName;
        });
        File.Delete(Path.Combine(folder, "Components", oldName + ".gucx"));
    }

    private static ScreenSave AddScreen(GumProjectSave project, string name, params (string Name, string BaseType)[] instances)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        foreach ((string instanceName, string baseType) in instances)
        {
            screen.Instances.Add(new InstanceSave { Name = instanceName, BaseType = baseType, ParentContainer = screen });
        }
        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference { Name = name, ElementType = ElementType.Screen });
        return screen;
    }

    private static ComponentSave AddComponent(GumProjectSave project, string name, string baseType, params (string Name, string BaseType)[] instances)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        ComponentSave? baseComponent = project.Components.SingleOrDefault(item => item.Name == baseType);
        foreach (InstanceSave baseInstance in baseComponent?.Instances ?? new List<InstanceSave>())
        {
            component.Instances.Add(new InstanceSave { Name = baseInstance.Name, BaseType = baseInstance.BaseType, DefinedByBase = true, ParentContainer = component });
        }
        foreach ((string instanceName, string instanceBaseType) in instances)
        {
            component.Instances.Add(new InstanceSave { Name = instanceName, BaseType = instanceBaseType, ParentContainer = component });
        }
        project.Components.Add(component);
        project.ComponentReferences.Add(new ElementReference { Name = name, ElementType = ElementType.Component });
        return component;
    }

    private static void AddCategory(ElementSave element, string categoryName, params string[] stateNames)
    {
        StateSaveCategory category = new StateSaveCategory { Name = categoryName };
        foreach (string stateName in stateNames)
        {
            category.States.Add(new StateSave { Name = stateName, ParentContainer = element });
        }
        element.Categories.Add(category);
    }

    private static Dictionary<string, byte[]> Snapshot(string folder) =>
        Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
            .ToDictionary(file => Path.GetRelativePath(folder, file).Replace('\\', '/'), File.ReadAllBytes);

    /// <summary>Compiles every .cs under the game folder the way the game's build would; returns the errors.</summary>
    private List<string> Compile()
    {
        CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        List<SyntaxTree> trees = Directory.GetFiles(TempDirectory, "*.cs", SearchOption.AllDirectories)
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), parseOptions, path))
            .ToList();

        // Only the framework and the MonoGame runtime: RaylibGum and the Forms placeholders declare the same type names.
        string frameworkDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        HashSet<string> referencePaths = new HashSet<string>(
            Directory.GetFiles(frameworkDirectory, "*.dll").Where(IsManagedAssembly),
            StringComparer.OrdinalIgnoreCase)
        {
            Assembly.Load("GumCommon").Location,
            Assembly.Load("MonoGameGum").Location,
            Assembly.Load("MonoGame.Framework").Location,
        };

        CSharpCompilation compilation = CSharpCompilation.Create(
            "BeginnerGame_" + Guid.NewGuid().ToString("N"),
            trees,
            referencePaths.Where(File.Exists).Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Annotations));

        using MemoryStream stream = new MemoryStream();
        EmitResult result = compilation.Emit(stream);
        return result.Diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString())
            .Take(10)
            .ToList();
    }

    private static bool IsManagedAssembly(string path)
    {
        try
        {
            AssemblyName.GetAssemblyName(path);
            return true;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
    }

    #endregion
}
