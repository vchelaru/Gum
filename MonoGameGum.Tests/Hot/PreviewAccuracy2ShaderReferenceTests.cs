#pragma warning disable CS0618
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Shouldly;
using System.IO;
using Xunit;
using static MonoGameGum.Tests.Hot.PreviewAccuracy2World;

namespace MonoGameGum.Tests.Hot;

// Shader parameters, shader files and variable references that cross instances.
public partial class PreviewAccuracy2Tests
{
    private static void Shaded(GumProjectSave project, float thickness = 2f, string file = "Content/Outline.fx")
    {
        ScreenSave screen = MainScreen(project);
        Std(screen, "Wrap", "IsRenderTarget", true);
        Std(screen, "Wrap", "SourceShaderFile", file);
        Set(screen, "Wrap.Outline_Thickness", "float", thickness);
    }

    private PreviewAccuracy2World BootShaded()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World();
        _worlds.Add(world);
        world.Boot(editBaseline: project => Shaded(project));
        return world;
    }

    [Fact]
    public void Shader_ScalarParameterEdited()
    {
        PreviewVerdict2 verdict = BootShaded().Save(project => Shaded(project, thickness: 5f));
        ShouldBeCorrect(verdict);
        verdict.Hot.ShouldContain("Thickness:");
    }

    [Fact]
    public void Shader_VectorParameterEdited()
    {
        PreviewAccuracy2World world = BootShaded();
        world.Save(project =>
        {
            Shaded(project);
            foreach (string component in new[] { "X", "Y", "Z", "W" })
            {
                Set(MainScreen(project), "Wrap.Outline_Tint__" + component, "float", 0.25f);
            }
        });
        ShouldBeCorrect(world.Save(project =>
        {
            Shaded(project);
            foreach (string component in new[] { "X", "Y", "Z", "W" })
            {
                Set(MainScreen(project), "Wrap.Outline_Tint__" + component, "float", 0.75f);
            }
        }));
    }

    [Fact]
    public void Shader_ParameterRemovedFromTheFile()
    {
        PreviewVerdict2 verdict = BootShaded().Save(project =>
        {
            Std(MainScreen(project), "Wrap", "IsRenderTarget", true);
            Std(MainScreen(project), "Wrap", "SourceShaderFile", "Content/Outline.fx");
        });
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Shader_FileSwitchedToAnotherShader()
    {
        PreviewVerdict2 verdict = BootShaded().Save(
            project => Shaded(project, file: "Content/Glow.fx"),
            dir => WriteShader(dir, "Glow.fx"));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Shader_FileSetToAMissingFile()
    {
        ShouldBeCorrect(BootShaded().Save(project => Shaded(project, file: "Content/Missing.fx")));
    }

    [Fact]
    public void Shader_FileEditedOnDisk_ReassignsTheEffect()
    {
        PreviewAccuracy2World world = BootShaded();
        File.WriteAllText(Content(world.Dir, "Outline.fx"), "// edited");
        PreviewVerdict2 verdict = world.Reload(Content(world.Dir, "Outline.fx"));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Shader_RenderTargetTurnedOff()
    {
        ShouldBeCorrect(BootShaded().Save(project =>
        {
            Shaded(project);
            Std(MainScreen(project), "Wrap", "IsRenderTarget", false);
        }));
    }

    // ---- Variable references ----

    private static void Follow(GumProjectSave project, string reader, string row)
    {
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = reader + ".VariableReferences" };
        list.Value.Add(row);
        MainScreen(project).DefaultState!.VariableLists.Add(list);
    }

    [Fact]
    public void Reference_SourceInstanceValueEdited_FollowerUpdates()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World();
        _worlds.Add(world);
        world.Boot(editBaseline: project => Follow(project, "Follower", "Red = Swatch.Red"));
        ShouldBeCorrect(world.Save(project => Std(MainScreen(project), "Swatch", "Red", 200)));
    }

    [Fact]
    public void Reference_RetargetedToAnotherInstance()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World();
        _worlds.Add(world);
        world.Boot(editBaseline: project => Follow(project, "Follower", "Red = Swatch.Red"));
        PreviewVerdict2 verdict = world.Save(project =>
        {
            project.Screens[0].DefaultState!.VariableLists.RemoveAll(list => list.Name == "Follower.VariableReferences");
            Follow(project, "Follower", "Red = Box.Red");
        });
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Reference_RemovedFromTheFollower_ValueGoesBackToItsOwn()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World();
        _worlds.Add(world);
        world.Boot(editBaseline: project => Follow(project, "Follower", "Red = Swatch.Red"));
        ShouldBeCorrect(world.Save(project => Std(MainScreen(project), "Swatch", "Red", 77)));
        PreviewVerdict2 verdict = world.Save(project => Std(MainScreen(project), "Swatch", "Red", 78));
        ShouldBeCorrect(verdict);
    }


    [Fact]
    public void Reference_SourceIsAComponentInstanceVariable_ComponentEdited()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World();
        _worlds.Add(world);
        world.Boot(editBaseline: project => Follow(project, "Follower", "Red = Badge1.Pic.Red"));
        ShouldNotBeStaleSilently(world.Save(project =>
        {
            Follow(project, "Follower", "Red = Badge1.Pic.Red");
            Std(Component(project, "Widgets/Badge"), "Pic", "Red", 99);
        }));
    }
}
