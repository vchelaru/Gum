using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// The kept set of referenced files after an edit that can only add a reference through a variable's value (an
/// instance or variable edit, a canvas refresh): a file a variable now names still counts, and a file nothing
/// names is answered without walking the project again.
/// </summary>
public class ProjectReferencedFilesTests : BaseTestClass
{
    private readonly string _root;
    private readonly GumProjectSave _project;
    private readonly ScreenSave _screen;
    private readonly ProjectReferencedFiles _referencedFiles;
    private int _walks;

    public ProjectReferencedFilesTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "GumRefFiles_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_root);
        _project = new GumProjectSave { FullFileName = Path.Combine(_root, "Project.gumx") };
        ObjectFinder.Self.GumProjectSave = _project;

        _screen = new ScreenSave { Name = "Main" };
        _screen.States.Add(new StateSave { Name = "Default", ParentContainer = _screen });
        _project.Screens.Add(_screen);
        _project.ScreenReferences.Add(new ElementReference { Name = "Main", ElementType = ElementType.Screen });
        SetFile("Item", "SourceFile", "Textures/Old.png");

        _referencedFiles = new ProjectReferencedFiles(() =>
        {
            _walks++;
            return ObjectFinder.Self.GetAllFilesInProject(
                Gum.Bundle.GumBundleInclusion.Core | Gum.Bundle.GumBundleInclusion.ExternalFiles, resolveFontFiles: false);
        });
        _referencedFiles.Contains(PathOf("Textures/Old.png")).ShouldBeTrue();
    }

    public override void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best-effort */ }
        base.Dispose();
    }

    private FilePath PathOf(string relative) => new FilePath(Path.Combine(_root, relative));

    private void SetFile(string instance, string variable, string value)
    {
        if (_screen.GetInstance(instance) == null)
        {
            _screen.Instances.Add(new InstanceSave { Name = instance, BaseType = "Sprite", ParentContainer = _screen });
        }
        _screen.DefaultState!.Variables.Add(new VariableSave
        {
            Name = instance + "." + variable, Type = "string", Value = value, IsFile = true, SetsValue = true,
        });
    }

    private string WriteChain(string relative, string frameTexture)
    {
        string path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path,
            "<?xml version=\"1.0\"?><AnimationChainArraySave><FileRelativeTextures>false</FileRelativeTextures>" +
            "<AnimationChain><Name>Idle</Name><Frame><TextureName>" + frameTexture + "</TextureName>" +
            "<FrameLength>0.1</FrameLength></Frame></AnimationChain></AnimationChainArraySave>");
        return path;
    }

    [Fact]
    public void AfterAVariableEdit_AFileNoVariableNames_IsNotReferenced_WithoutWalkingAgain()
    {
        _screen.DefaultState!.Variables.Add(new VariableSave { Name = "Item.X", Type = "float", Value = 5f, SetsValue = true });
        _referencedFiles.InvalidateFilesNamedByVariables();

        _referencedFiles.Contains(PathOf("EventExport/gum_events.json")).ShouldBeFalse();
        _referencedFiles.Contains(PathOf("Textures/Old.png")).ShouldBeTrue();

        _walks.ShouldBe(1);
    }

    [Fact]
    public void AfterAVariableEdit_APictureTheEditNamed_IsReferenced()
    {
        SetFile("Added", "SourceFile", "Textures/New.png");
        _referencedFiles.InvalidateFilesNamedByVariables();

        _referencedFiles.Contains(PathOf("Textures/New.png")).ShouldBeTrue();
    }

    [Fact]
    public void AfterAVariableEdit_APictureOfTheSameNameInAnotherFolder_IsNotReferenced()
    {
        SetFile("Added", "SourceFile", "Textures/New.png");
        _referencedFiles.InvalidateFilesNamedByVariables();

        _referencedFiles.Contains(PathOf("Other/New.png")).ShouldBeFalse();
    }

    [Fact]
    public void AfterAVariableEdit_TheFramesOfAnAnimationChainTheEditNamed_AreReferenced()
    {
        WriteChain("Animations/Idle.achx", "Textures/Frame.png");
        SetFile("Added", "AnimationChains", "Animations/Idle.achx");
        _referencedFiles.InvalidateFilesNamedByVariables();

        _referencedFiles.Contains(PathOf("Textures/Frame.png")).ShouldBeTrue();
    }

    [Fact]
    public void AfterAFullInvalidation_AFileOnlyTheProjectLists_IsReferenced()
    {
        // An element added to the project is listed by the project, not named by a variable.
        _project.Components.Add(new ComponentSave { Name = "Card", BaseType = "Container" });
        _project.ComponentReferences.Add(new ElementReference { Name = "Card", ElementType = ElementType.Component });
        _referencedFiles.Invalidate();

        _referencedFiles.Contains(PathOf("Components/Card.gucx")).ShouldBeTrue();
    }
}
