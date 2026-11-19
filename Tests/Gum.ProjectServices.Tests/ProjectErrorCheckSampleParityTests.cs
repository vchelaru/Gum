using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gum.DataTypes;
using Gum.Managers;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The tool checks a project's rows with <see cref="HeadlessErrorChecker.BeginProjectErrors"/>, its disk
/// half on a worker thread; on every checked-in sample project, and on a copy broken on purpose, that
/// gives the rows <see cref="HeadlessErrorChecker.GetProjectErrors"/> gives, row for row.
/// </summary>
public class ProjectErrorCheckSampleParityTests : BaseTestClass
{
    public static IEnumerable<object[]> SampleProjects() =>
        Directory.EnumerateFiles(Path.Combine(FindRepoRoot(), "Samples"), "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".gumx", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".gumj", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(part => part is "bin" or "obj"))
            .Select(path => new object[] { Path.GetRelativePath(FindRepoRoot(), path) });

    [Theory]
    [MemberData(nameof(SampleProjects))]
    public void BeginProjectErrors_ShouldMatchGetProjectErrors_OnASample(string relativeProjectPath)
    {
        GumProjectSave project = Load(Path.Combine(FindRepoRoot(), relativeProjectPath));

        AssertSameRows(project);
    }

    // A missing behavior file (GUM0031) and a behavior file spelled differently on disk (GUM0008, a project
    // row: element files and the files elements reference are the element checks' rows).
    [Fact]
    public void BeginProjectErrors_ShouldMatchGetProjectErrors_OnABrokenCopy()
    {
        string source = Path.Combine(FindRepoRoot(), "Samples", "GameUiSamples", "Content", "GumProject");
        string copy = Path.Combine(Path.GetTempPath(), "GumProjectErrorParity_" + Guid.NewGuid().ToString("N"));
        CopyDirectory(source, copy);
        try
        {
            File.Delete(Path.Combine(copy, "Behaviors", "ButtonBehavior.behx"));
            // Through a temporary name, since a rename that only changes case is a no-op on some file systems.
            File.Move(Path.Combine(copy, "Behaviors", "CheckBoxBehavior.behx"), Path.Combine(copy, "Behaviors", "moving.tmp"));
            File.Move(Path.Combine(copy, "Behaviors", "moving.tmp"), Path.Combine(copy, "Behaviors", "checkboxbehavior.behx"));
            GumProjectSave project = Load(Path.Combine(copy, "GameUiSamplesGumProject.gumx"));

            IReadOnlyList<ErrorResult> rows = AssertSameRows(project);

            rows.ShouldContain(row => row.Code == "GUM0031" && row.ElementName == "ButtonBehavior");
            rows.ShouldContain(row => row.Code == "GUM0008" && row.Message.Contains("checkboxbehavior.behx"));
        }
        finally
        {
            Directory.Delete(copy, recursive: true);
        }
    }

    private static IReadOnlyList<ErrorResult> AssertSameRows(GumProjectSave project)
    {
        HeadlessErrorChecker checker = new HeadlessErrorChecker(new Mock<ITypeResolver>().Object);
        IReadOnlyList<ErrorResult> expected = checker.GetProjectErrors(project);

        ProjectErrorCheck check = checker.BeginProjectErrors(project);
        while (!check.IsPrepared)
        {
            check.PrepareNext();
        }
        IReadOnlyList<ErrorResult> rows = Task.Run(check.Check).GetAwaiter().GetResult();

        rows.Select(Describe).ShouldBe(expected.Select(Describe));
        return rows;
    }

    private static string Describe(ErrorResult row) =>
        $"{row.Code}|{row.Severity}|{row.ElementName}|{row.Message}|{row.FilePath}";

    private static GumProjectSave Load(string projectPath)
    {
        StandardElementsManager.Self.Initialize();
        ProjectLoadResult result = new ProjectLoader().Load(projectPath);
        result.Success.ShouldBeTrue(projectPath);
        ObjectFinder.Self.GumProjectSave = result.Project;
        return result.Project!;
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (string file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        }
        foreach (string directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
        }
    }

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(current, "Samples")) && Directory.Exists(Path.Combine(current, "Tools", "Gum.ProjectServices")))
            {
                return current;
            }
            current = Path.GetDirectoryName(current) ?? current;
        }
        throw new DirectoryNotFoundException("Could not find the repo root from " + AppContext.BaseDirectory);
    }
}
