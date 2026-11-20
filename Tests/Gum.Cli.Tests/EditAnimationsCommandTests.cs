using Gum.DataTypes;
using Gum.StateAnimation.SaveClasses;
using Shouldly;

namespace Gum.Cli.Tests;

public class EditAnimationsCommandTests : BaseTestClass
{
    private const string PopupSetup = """
        { "op": "addComponent", "name": "Popup" },
        { "op": "addCategory", "element": "Popup", "name": "Visibility" },
        { "op": "addState", "element": "Popup", "category": "Visibility", "name": "Hidden" },
        { "op": "addState", "element": "Popup", "category": "Visibility", "name": "Shown" },
        { "op": "addState", "element": "Popup", "name": "Pressed" }
        """;

    private string CreateEmptyProject()
    {
        string projectPath = Path.Combine(TempDirectory, "MyProject", "MyProject.gumx");
        CliTestHelper.Run("new", projectPath, "--template", "empty").ExitCode.ShouldBe(0);
        return projectPath;
    }

    private string WriteOperations(string operations)
    {
        string path = Path.Combine(TempDirectory, "ops-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, "{ \"version\": 1, \"operations\": [" + operations + "] }");
        return path;
    }

    private static string AnimationFile(string projectPath) =>
        Path.Combine(Path.GetDirectoryName(projectPath)!, "Components", "PopupAnimations.ganx");

    private CliTestHelper Edit(string projectPath, string operations) =>
        CliTestHelper.Run("edit", projectPath, WriteOperations(operations));

    private Dictionary<string, string> ReadProjectFiles(string projectPath) =>
        Directory.GetFiles(Path.GetDirectoryName(projectPath)!, "*", SearchOption.AllDirectories)
            .ToDictionary(file => file, File.ReadAllText);

    [Fact]
    public void Edit_AddAnimationAndKeyframes_WritesTheSidecarTheToolWouldWrite()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, PopupSetup + """
            ,
            { "op": "addAnimation", "element": "Popup", "name": "FadeIn", "loops": true },
            { "op": "addKeyframe", "element": "Popup", "animation": "FadeIn", "state": "Visibility/Hidden", "time": 0 },
            { "op": "addKeyframe", "element": "Popup", "animation": "FadeIn", "state": "Visibility/Shown", "time": 0.5,
              "interpolation": "Exponential", "easing": "In" }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
        ElementAnimationsSave animations = ElementAnimationsSave.Load(AnimationFile(projectPath));
        animations.ElementName.ShouldBe("Popup");
        AnimationSave fadeIn = animations.Animations.Single();
        fadeIn.Name.ShouldBe("FadeIn");
        fadeIn.Loops.ShouldBeTrue();
        fadeIn.States.Select(item => (item.StateName, item.Time)).ShouldBe(new[] { ("Visibility/Hidden", 0f), ("Visibility/Shown", 0.5f) });
        fadeIn.States[0].InterpolationType.ShouldBe(FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear);
        fadeIn.States[0].Easing.ShouldBe(FlatRedBall.Glue.StateInterpolation.Easing.Out);
        fadeIn.States[1].InterpolationType.ShouldBe(FlatRedBall.Glue.StateInterpolation.InterpolationType.Exponential);
        fadeIn.States[1].Easing.ShouldBe(FlatRedBall.Glue.StateInterpolation.Easing.In);
    }

    [Fact]
    public void Edit_AnimationSidecar_IsByteIdenticalToWhatTheToolsSaveWritesForTheSameData()
    {
        string projectPath = CreateEmptyProject();
        Edit(projectPath, PopupSetup + """
            ,
            { "op": "addAnimation", "element": "Popup", "name": "Press" },
            { "op": "addKeyframe", "element": "Popup", "animation": "Press", "state": "Pressed", "time": 0.25 }
            """).ExitCode.ShouldBe(0);

        string rewritten = Path.Combine(TempDirectory, "Rewritten.ganx");
        ElementAnimationsSave.Load(AnimationFile(projectPath)).Save(rewritten);

        File.ReadAllText(AnimationFile(projectPath)).ShouldBe(File.ReadAllText(rewritten));
    }

    [Fact]
    public void Edit_Keyframes_AreKeptInTimeOrderWhateverOrderTheyAreAdded()
    {
        string projectPath = CreateEmptyProject();

        Edit(projectPath, PopupSetup + """
            ,
            { "op": "addAnimation", "element": "Popup", "name": "Pulse" },
            { "op": "addKeyframe", "element": "Popup", "animation": "Pulse", "state": "Visibility/Shown", "time": 1 },
            { "op": "addKeyframe", "element": "Popup", "animation": "Pulse", "state": "Visibility/Hidden", "time": 0 },
            { "op": "addKeyframe", "element": "Popup", "animation": "Pulse", "state": "Pressed", "time": 0.5 }
            """).ExitCode.ShouldBe(0);

        ElementAnimationsSave.Load(AnimationFile(projectPath)).Animations.Single().States.Select(item => item.Time)
            .ShouldBe(new[] { 0f, 0.5f, 1f });
    }

    [Fact]
    public void Edit_SecondRun_KeepsTheAnimationsAlreadyInTheSidecar()
    {
        string projectPath = CreateEmptyProject();
        Edit(projectPath, PopupSetup + """
            ,
            { "op": "addAnimation", "element": "Popup", "name": "FadeIn" },
            { "op": "addKeyframe", "element": "Popup", "animation": "FadeIn", "state": "Visibility/Hidden", "time": 0 }
            """).ExitCode.ShouldBe(0);

        Edit(projectPath, """
            { "op": "addAnimation", "element": "Popup", "name": "FadeOut" },
            { "op": "addKeyframe", "element": "Popup", "animation": "FadeIn", "state": "Visibility/Shown", "time": 1 }
            """).ExitCode.ShouldBe(0);

        ElementAnimationsSave animations = ElementAnimationsSave.Load(AnimationFile(projectPath));
        animations.Animations.Select(item => item.Name).ShouldBe(new[] { "FadeIn", "FadeOut" });
        animations.Animations[0].States.Count.ShouldBe(2);
    }

    [Fact]
    public void Edit_RemoveKeyframeAndSetLoops_ChangeTheSidecar()
    {
        string projectPath = CreateEmptyProject();
        Edit(projectPath, PopupSetup + """
            ,
            { "op": "addAnimation", "element": "Popup", "name": "FadeIn" },
            { "op": "addKeyframe", "element": "Popup", "animation": "FadeIn", "state": "Visibility/Hidden", "time": 0 },
            { "op": "addKeyframe", "element": "Popup", "animation": "FadeIn", "state": "Visibility/Shown", "time": 1 }
            """).ExitCode.ShouldBe(0);

        CliTestHelper result = Edit(projectPath, """
            { "op": "removeKeyframe", "element": "Popup", "animation": "FadeIn", "state": "Visibility/Hidden", "time": 0 },
            { "op": "setAnimationLoops", "element": "Popup", "animation": "FadeIn", "loops": true }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        AnimationSave fadeIn = ElementAnimationsSave.Load(AnimationFile(projectPath)).Animations.Single();
        fadeIn.Loops.ShouldBeTrue();
        fadeIn.States.Single().StateName.ShouldBe("Visibility/Shown");
    }

    [Fact]
    public void Edit_RemoveLastAnimation_DeletesTheSidecarLikeTheTool()
    {
        string projectPath = CreateEmptyProject();
        Edit(projectPath, PopupSetup + """
            ,
            { "op": "addAnimation", "element": "Popup", "name": "FadeIn" }
            """).ExitCode.ShouldBe(0);
        File.Exists(AnimationFile(projectPath)).ShouldBeTrue();

        Edit(projectPath, """
            { "op": "removeAnimation", "element": "Popup", "name": "FadeIn" }
            """).ExitCode.ShouldBe(0);

        File.Exists(AnimationFile(projectPath)).ShouldBeFalse();
    }

    [Theory]
    [InlineData("""{ "op": "addKeyframe", "element": "Popup", "animation": "FadeIn", "state": "Visibility/Gone", "time": 0 }""", "Visibility/Gone")]
    [InlineData("""{ "op": "addKeyframe", "element": "Popup", "animation": "Nope", "state": "Pressed", "time": 0 }""", "Nope")]
    [InlineData("""{ "op": "addKeyframe", "element": "Popup", "animation": "FadeIn", "state": "Pressed", "time": -1 }""", "time")]
    [InlineData("""{ "op": "addKeyframe", "element": "Popup", "animation": "FadeIn", "state": "Pressed", "time": "soon" }""", "time")]
    [InlineData("""{ "op": "addKeyframe", "element": "Popup", "animation": "FadeIn", "state": "Pressed", "time": 0, "interpolation": "Bouncy" }""", "Bouncy")]
    [InlineData("""{ "op": "addKeyframe", "element": "Popup", "animation": "FadeIn", "state": "Pressed", "time": 0, "easing": "Sideways" }""", "Sideways")]
    [InlineData("""{ "op": "addAnimation", "element": "Popup", "name": "FadeIn" }""", "FadeIn")]
    [InlineData("""{ "op": "addAnimation", "element": "Popup", "name": "Has Space" }""", "space")]
    [InlineData("""{ "op": "addAnimation", "element": "Popup", "name": "Visibility" }""", "Visibility")]
    [InlineData("""{ "op": "removeAnimation", "element": "Popup", "name": "Nope" }""", "Nope")]
    [InlineData("""{ "op": "removeKeyframe", "element": "Popup", "animation": "FadeIn", "state": "Pressed", "time": 3 }""", "Pressed")]
    [InlineData("""{ "op": "setAnimationLoops", "element": "Popup", "animation": "Nope", "loops": true }""", "Nope")]
    public void Edit_InvalidAnimationOperation_FailsNamingTheProblemAndWritesNothing(string operation, string expectedInMessage)
    {
        string projectPath = CreateEmptyProject();
        Edit(projectPath, PopupSetup + """
            ,
            { "op": "addAnimation", "element": "Popup", "name": "FadeIn" }
            """).ExitCode.ShouldBe(0);
        Dictionary<string, string> before = ReadProjectFiles(projectPath);

        CliTestHelper result = Edit(projectPath, operation);

        result.ExitCode.ShouldBe(1, result.StandardOutput + result.StandardError);
        result.StandardError.ShouldContain(expectedInMessage);
        ReadProjectFiles(projectPath).ShouldBe(before);
    }

    [Fact]
    public void Edit_KeyframeAtTheSameStateAndTime_IsRefused()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, PopupSetup + """
            ,
            { "op": "addAnimation", "element": "Popup", "name": "FadeIn" },
            { "op": "addKeyframe", "element": "Popup", "animation": "FadeIn", "state": "Pressed", "time": 0 },
            { "op": "addKeyframe", "element": "Popup", "animation": "FadeIn", "state": "Pressed", "time": 0 }
            """);

        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("already has");
        File.Exists(AnimationFile(projectPath)).ShouldBeFalse();
    }

    [Fact]
    public void Edit_RemovingAStateAKeyframeUses_IsReportedAsAnErrorAndWritesNothing()
    {
        string projectPath = CreateEmptyProject();
        Edit(projectPath, PopupSetup + """
            ,
            { "op": "addAnimation", "element": "Popup", "name": "FadeIn" },
            { "op": "addKeyframe", "element": "Popup", "animation": "FadeIn", "state": "Pressed", "time": 0 }
            """).ExitCode.ShouldBe(0);
        Dictionary<string, string> before = ReadProjectFiles(projectPath);

        CliTestHelper result = Edit(projectPath, """
            { "op": "removeState", "element": "Popup", "name": "Pressed" }
            """);

        result.ExitCode.ShouldBe(1, result.StandardOutput + result.StandardError);
        result.StandardOutput.ShouldContain("references state 'Pressed'");
        ReadProjectFiles(projectPath).ShouldBe(before);
    }

    [Fact]
    public void Edit_InstanceNamedLikeAnAnimation_IsRefused()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, PopupSetup + """
            ,
            { "op": "addAnimation", "element": "Popup", "name": "Spin" },
            { "op": "addInstance", "element": "Popup", "name": "Spin", "type": "Container" }
            """);

        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("Spin");
    }

    [Fact]
    public void Edit_JsonProject_WritesTheGanjSidecar()
    {
        string projectPath = CreateEmptyProject();
        Edit(projectPath, PopupSetup).ExitCode.ShouldBe(0);
        CliTestHelper.Run("convert-to-json", projectPath).ExitCode.ShouldBe(0);
        string jsonProject = Path.ChangeExtension(projectPath, ".gumj");

        CliTestHelper result = Edit(jsonProject, """
            { "op": "addAnimation", "element": "Popup", "name": "FadeIn" },
            { "op": "addKeyframe", "element": "Popup", "animation": "FadeIn", "state": "Pressed", "time": 0 }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        string ganj = Path.Combine(Path.GetDirectoryName(jsonProject)!, "Components", "PopupAnimations.ganj");
        ElementAnimationsSave.Load(ganj).Animations.Single().States.Single().StateName.ShouldBe("Pressed");
    }

    [Fact]
    public void EditSchema_ListsTheAnimationOperations()
    {
        CliTestHelper result = CliTestHelper.Run("edit", "--schema");

        result.ExitCode.ShouldBe(0);
        foreach (string op in new[] { "addAnimation", "removeAnimation", "setAnimationLoops", "addKeyframe", "removeKeyframe" })
        {
            result.StandardOutput.ShouldContain(op);
        }
    }
}
