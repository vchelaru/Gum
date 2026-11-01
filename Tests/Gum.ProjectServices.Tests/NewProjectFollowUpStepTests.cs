#pragma warning disable xUnit1031 // The helpers create projects synchronously; nothing here can deadlock (no synchronization context).
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>The restore, package add and code generation steps that run after the files are written: order, failure and cancellation.</summary>
public class NewProjectFollowUpStepTests : NewProjectServiceTestBase
{
    [Fact]
    public async Task Execute_WhenCodegenThrows_ReportsAFailedCodegenInsteadOfThrowing_AndKeepsTheFiles()
    {
        _codegen.Throw = true;

        NewProjectResult result = await Run(Request("raylib", restore: true, generate: true));

        result.Codegen!.Success.ShouldBeFalse();
        result.Codegen.OutputTail.ShouldContain("boom");
        File.Exists(Path.Combine(Game(), "Game.csproj")).ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_GenerateCode_RunsTheCodegenOnTheGumProjectItWrote_AfterTheRestore()
    {
        NewProjectResult result = await Run(Request("kni", restore: true, generate: true, xml: true));

        result.Codegen!.Success.ShouldBeTrue();
        _calls.Select(call => call.Split(' ')[0]).ToArray().ShouldBe(new[] { "restore", "codegen" });
        _calls.Last().ShouldBe("codegen GumProject.gumx");
        File.ReadAllText(Path.Combine(Game(), "Game1.cs")).ShouldContain("new DemoScreenGum()");
    }

    [Fact]
    public async Task Execute_Canceled_WhileRestoring_KeepsTheFilesAndSkipsCodegen()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        NewProjectService service = CreateService(restore: new CancelingRestore(source));

        NewProjectResult result = await service.ExecuteAsync(Request("monogame", restore: true, generate: true), null, source.Token);

        result.Restore!.Status.ShouldBe(RestoreStatus.Canceled);
        result.Codegen.ShouldBeNull();
        File.Exists(Path.Combine(Game(), "Game.csproj")).ShouldBeTrue();
        File.Exists(Path.Combine(Game(), "Content", "GumProject", "GumProject.gumj")).ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_Canceled_WhileAddingPackages_KeepsTheFilesAndSkipsTheRestore()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        NewProjectService service = CreateService(add: new CancelingAdd(source));

        NewProjectResult result = await service.ExecuteAsync(Request("monogame", packages: true, restore: true), null, source.Token);

        result.PackageAdd!.Status.ShouldBe(RestoreStatus.Canceled);
        result.Restore.ShouldBeNull();
        _calls.ShouldNotContain(call => call.StartsWith("restore"));
        File.Exists(Path.Combine(Game(), "Game.csproj")).ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_CanceledBeforeAnythingRuns_LeavesTheTargetFolderUntouched()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        NewProjectResult result = await Run(Request("monogame", restore: true), source.Token);

        result.Files.Success.ShouldBeFalse();
        Directory.Exists(Game()).ShouldBeFalse();
    }
}
