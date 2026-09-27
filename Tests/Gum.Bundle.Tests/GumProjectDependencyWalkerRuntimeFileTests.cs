using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Shouldly;

namespace Gum.Bundle.Tests;

/// <summary>
/// Files the runtime loads that no element variable names directly: localization files and their
/// RESX satellites, element animation files, the page textures of a custom .fnt, and the frame
/// textures of an animation chain. A bundle missing any of them loads a broken project.
/// </summary>
public class GumProjectDependencyWalkerRuntimeFileTests : IDisposable
{
    private readonly List<string> _tempDirectories = new List<string>();
    private static readonly byte[] EmptyContent = TestProjectBuilder.EmptyXmlBytes;

    [Fact]
    public void Walk_includes_animation_chain_frame_textures_resolved_against_the_achx()
    {
        const string achx =
            "<AnimationChainArraySave><FileRelativeTextures>true</FileRelativeTextures>" +
            "<AnimationChain><Name>Run</Name>" +
            "<Frame><TextureName>run.png</TextureName></Frame>" +
            "<Frame><TextureName>../Shared/dust.png</TextureName></Frame>" +
            "</AnimationChain></AnimationChainArraySave>";
        ComponentSave component = TestProjectBuilder.BuildComponent("Hero");
        TestProjectBuilder.AddSpriteInstance(component, "Sprite", "Anims/Hero.achx");
        GumProjectSave project = TestProjectBuilder.BuildProject(components: new[] { component });
        string root = CreateProjectRoot(new[]
        {
            ("Components/Hero.gucx", EmptyContent),
            ("Anims/Hero.achx", Encoding.UTF8.GetBytes(achx)),
            ("Anims/run.png", EmptyContent),
            ("Shared/dust.png", EmptyContent),
        });

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.Core | GumBundleInclusion.ExternalFiles);

        result.MissingFiles.ShouldBeEmpty();
        result.ExternalFiles.ShouldBe(new[] { "Anims/Hero.achx", "Anims/run.png", "Shared/dust.png" });
    }

    [Fact]
    public void Walk_includes_custom_font_page_textures_listed_in_the_fnt()
    {
        const string fnt = "info face=\"Pixel\" size=12\ncommon lineHeight=12 base=10 pages=2\n" +
            "page id=0 file=\"Pixel_0.png\"\npage id=1 file=\"Pixel_1.png\"\n";
        ComponentSave component = TestProjectBuilder.BuildComponent("Label");
        TestProjectBuilder.AddTextInstanceWithCustomFont(component, "Text", "Fonts/Pixel.fnt");
        GumProjectSave project = TestProjectBuilder.BuildProject(components: new[] { component });
        string root = CreateProjectRoot(new[]
        {
            ("Components/Label.gucx", EmptyContent),
            ("Fonts/Pixel.fnt", Encoding.UTF8.GetBytes(fnt)),
            ("Fonts/Pixel_0.png", EmptyContent),
            ("Fonts/Pixel_1.png", EmptyContent),
        });

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.ExternalFiles);

        result.MissingFiles.ShouldBeEmpty();
        result.ExternalFiles.ShouldBe(new[] { "Fonts/Pixel.fnt", "Fonts/Pixel_0.png", "Fonts/Pixel_1.png" });
    }

    [Fact]
    public void Walk_reports_a_custom_font_it_cannot_parse()
    {
        ComponentSave component = TestProjectBuilder.BuildComponent("Label");
        TestProjectBuilder.AddTextInstanceWithCustomFont(component, "Text", "Fonts/Broken.fnt");
        GumProjectSave project = TestProjectBuilder.BuildProject(components: new[] { component });
        string root = CreateProjectRoot(new[]
        {
            ("Components/Label.gucx", EmptyContent),
            ("Fonts/Broken.fnt", Encoding.UTF8.GetBytes("not a font")),
        });

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.ExternalFiles);

        result.MissingFiles.ShouldHaveSingleItem().ReferencedPath.ShouldBe("Fonts/Broken.fnt");
    }

    [Fact]
    public void Walk_includes_element_animation_files_matching_the_project_format()
    {
        ScreenSave screen = TestProjectBuilder.BuildScreen("Title");
        ComponentSave animated = TestProjectBuilder.BuildComponent("Buttons/Fancy");
        ComponentSave still = TestProjectBuilder.BuildComponent("Plain");
        GumProjectSave project = TestProjectBuilder.BuildProject(screens: new[] { screen }, components: new[] { animated, still });
        string root = CreateProjectRoot(new[]
        {
            ("Screens/Title.gusx", EmptyContent),
            ("Screens/TitleAnimations.ganx", EmptyContent),
            ("Components/Buttons/Fancy.gucx", EmptyContent),
            ("Components/Buttons/FancyAnimations.ganx", EmptyContent),
            ("Components/Buttons/FancyAnimations.ganj", EmptyContent),
            ("Components/Plain.gucx", EmptyContent),
        });

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.Core);

        result.MissingFiles.ShouldBeEmpty();
        result.CoreFiles.ShouldContain("Screens/TitleAnimations.ganx");
        result.CoreFiles.ShouldContain("Components/Buttons/FancyAnimations.ganx");
        result.CoreFiles.ShouldNotContain("Components/Buttons/FancyAnimations.ganj");
        result.CoreFiles.ShouldNotContain("Components/PlainAnimations.ganx");
    }

    [Fact]
    public void Walk_includes_localization_files_and_resx_satellites_only()
    {
        GumProjectSave project = TestProjectBuilder.BuildProject();
        project.LocalizationFiles.Add("Localization/Strings.resx");
        project.LocalizationFiles.Add("Localization/Buttons.csv");
        string root = CreateProjectRoot(new[]
        {
            ("Localization/Strings.resx", EmptyContent),
            ("Localization/Strings.es.resx", EmptyContent),
            ("Localization/Strings.fr.resx", EmptyContent),
            ("Localization/StringsExtra.resx", EmptyContent),
            ("Localization/Buttons.csv", EmptyContent),
            ("Localization/Buttons.es.csv", EmptyContent),
        });

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.Core);

        result.MissingFiles.ShouldBeEmpty();
        result.CoreFiles.Where(p => p.StartsWith("Localization/", StringComparison.Ordinal)).ShouldBe(new[]
        {
            "Localization/Buttons.csv",
            "Localization/Strings.es.resx",
            "Localization/Strings.fr.resx",
            "Localization/Strings.resx",
        });
    }

    [Fact]
    public void Walk_reports_a_missing_localization_file()
    {
        GumProjectSave project = TestProjectBuilder.BuildProject();
        project.LocalizationFiles.Add("Localization/Strings.csv");
        string root = CreateProjectRoot(Array.Empty<(string, byte[])>());

        WalkResult result = new GumProjectDependencyWalker().Walk(project, root, GumBundleInclusion.Core);

        result.MissingFiles.ShouldHaveSingleItem().ReferencedPath.ShouldBe("Localization/Strings.csv");
    }

    private string CreateProjectRoot(IEnumerable<(string, byte[])> files)
    {
        List<(string, byte[])> withProjectFile = new List<(string, byte[])>(files)
        {
            (TestProjectBuilder.DefaultProjectName + "." + GumProjectSave.ProjectExtension, EmptyContent)
        };
        string dir = TestProjectBuilder.CreateTempProjectDirectory(withProjectFile);
        _tempDirectories.Add(dir);
        return dir;
    }

    public void Dispose()
    {
        foreach (string dir in _tempDirectories)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
