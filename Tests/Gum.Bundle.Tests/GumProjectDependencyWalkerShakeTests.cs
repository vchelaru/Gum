using System;
using System.Collections.Generic;
using System.IO;
using Gum.Bundle;
using Gum.DataTypes;
using Shouldly;

namespace Gum.Bundle.Tests;

public class GumProjectDependencyWalkerShakeTests : IDisposable
{
    private readonly List<string> _tempDirectories = new List<string>();
    private static readonly byte[] EmptyContent = TestProjectBuilder.EmptyXmlBytes;

    [Fact]
    public void Walk_with_kept_elements_leaves_out_trimmed_elements_and_their_files()
    {
        ComponentSave kept = TestProjectBuilder.BuildComponent("Kept");
        TestProjectBuilder.AddSpriteInstance(kept, "Sprite", "Textures/kept.png");
        ComponentSave trimmed = TestProjectBuilder.BuildComponent("Trimmed");
        TestProjectBuilder.AddSpriteInstance(trimmed, "Sprite", "Textures/trimmed.png");
        StandardElementSave sprite = TestProjectBuilder.BuildStandard("Sprite");
        GumProjectSave project = TestProjectBuilder.BuildProject(
            components: new[] { kept, trimmed }, standards: new[] { sprite });
        string root = CreateProjectRoot(new[]
        {
            ("Components/Kept.gucx", EmptyContent),
            ("Components/Trimmed.gucx", EmptyContent),
            ("Standards/Sprite.gutx", EmptyContent),
            ("Textures/kept.png", EmptyContent),
            ("Textures/trimmed.png", EmptyContent),
        });

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root,
            GumBundleInclusion.Core | GumBundleInclusion.ExternalFiles,
            keptElementNames: new HashSet<string> { "Kept" });

        result.CoreFiles.ShouldContain("Components/Kept.gucx");
        result.CoreFiles.ShouldContain("Standards/Sprite.gutx");
        result.CoreFiles.ShouldNotContain("Components/Trimmed.gucx");
        result.ExternalFiles.ShouldBe(new[] { "Textures/kept.png" });
    }

    private string CreateProjectRoot(IEnumerable<(string relativePath, byte[] content)> files)
    {
        string root = TestProjectBuilder.CreateTempProjectDirectory(files);
        _tempDirectories.Add(root);
        return root;
    }

    public void Dispose()
    {
        foreach (string directory in _tempDirectories)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
