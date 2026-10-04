using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins <see cref="GumProjectRepairLogic"/> after its extraction from <c>ProjectManager</c> (#3863,
/// part of the ADR-0005 headless-relocation effort) — a behavior-preserving move of the four
/// load-time normalization passes that only ever operated on an explicit <see cref="GumProjectSave"/>
/// parameter, so they carried no WPF/WinForms dependency in the first place.
/// </summary>
public class GumProjectRepairLogicTests
{
    private readonly GumProjectRepairLogic _repairLogic = new();

    [Fact]
    public void FixRecursiveAssignments_ForcesRecursiveInstanceToContainer()
    {
        ComponentSave component = new ComponentSave { Name = "MyComponent" };
        InstanceSave instance = new InstanceSave { Name = "SelfReferencing", BaseType = "MyComponent" };
        component.Instances.Add(instance);
        GumProjectSave gumProjectSave = new GumProjectSave();
        gumProjectSave.Components.Add(component);

        bool didChange = _repairLogic.FixRecursiveAssignments(gumProjectSave);

        didChange.ShouldBeTrue();
        instance.BaseType.ShouldBe("Container");
    }

    [Fact]
    public void FixRecursiveAssignments_ReturnsFalse_WhenNoInstanceIsRecursive()
    {
        ComponentSave component = new ComponentSave { Name = "MyComponent" };
        InstanceSave instance = new InstanceSave { Name = "Child", BaseType = "SomeOtherComponent" };
        component.Instances.Add(instance);
        GumProjectSave gumProjectSave = new GumProjectSave();
        gumProjectSave.Components.Add(component);

        bool didChange = _repairLogic.FixRecursiveAssignments(gumProjectSave);

        didChange.ShouldBeFalse();
        instance.BaseType.ShouldBe("SomeOtherComponent");
    }

    [Fact]
    public void FixSlashesInNames_ReplacesBackslashesWithForwardSlashes_InComponentAndInstanceNames()
    {
        ComponentSave component = new ComponentSave { Name = "Folder\\MyComponent" };
        InstanceSave instance = new InstanceSave { Name = "Child", BaseType = "Folder\\Base" };
        component.Instances.Add(instance);
        GumProjectSave gumProjectSave = new GumProjectSave();
        gumProjectSave.Components.Add(component);

        bool didChange = _repairLogic.FixSlashesInNames(gumProjectSave);

        didChange.ShouldBeTrue();
        component.Name.ShouldBe("Folder/MyComponent");
        instance.BaseType.ShouldBe("Folder/Base");
    }

    [Fact]
    public void FixSlashesInNames_ReturnsFalse_WhenNoNameContainsABackslash()
    {
        ComponentSave component = new ComponentSave { Name = "Folder/MyComponent" };
        GumProjectSave gumProjectSave = new GumProjectSave();
        gumProjectSave.Components.Add(component);

        bool didChange = _repairLogic.FixSlashesInNames(gumProjectSave);

        didChange.ShouldBeFalse();
    }

    [Fact]
    public void RemoveDuplicateVariables_RemovesLaterDuplicate_KeepingFirstOccurrence()
    {
        ComponentSave component = new ComponentSave { Name = "MyComponent" };
        StateSave state = new StateSave();
        component.States.Add(state);
        state.Variables.Add(new VariableSave { Name = "X", Value = 1f });
        state.Variables.Add(new VariableSave { Name = "X", Value = 2f });
        GumProjectSave gumProjectSave = new GumProjectSave();
        gumProjectSave.Components.Add(component);

        bool didChange = _repairLogic.RemoveDuplicateVariables(gumProjectSave);

        didChange.ShouldBeTrue();
        state.Variables.Count(v => v.Name == "X").ShouldBe(1);
    }

    [Fact]
    public void RemoveDuplicateVariables_ReturnsFalse_WhenNoVariableNameIsDuplicated()
    {
        ComponentSave component = new ComponentSave { Name = "MyComponent" };
        StateSave state = new StateSave();
        component.States.Add(state);
        state.Variables.Add(new VariableSave { Name = "X", Value = 1f });
        state.Variables.Add(new VariableSave { Name = "Y", Value = 2f });
        GumProjectSave gumProjectSave = new GumProjectSave();
        gumProjectSave.Components.Add(component);

        bool didChange = _repairLogic.RemoveDuplicateVariables(gumProjectSave);

        didChange.ShouldBeFalse();
    }

    [Fact]
    public void RemoveSpacesInVariables_StripsSpacesFromKnownLegacyVariableNames()
    {
        ComponentSave component = new ComponentSave { Name = "MyComponent" };
        StateSave state = new StateSave();
        component.States.Add(state);
        state.Variables.Add(new VariableSave { Name = "MyInstance.Base Type", Value = "Container" });
        GumProjectSave gumProjectSave = new GumProjectSave();
        gumProjectSave.Components.Add(component);

        bool didChange = _repairLogic.RemoveSpacesInVariables(gumProjectSave);

        didChange.ShouldBeTrue();
        state.Variables[0].Name.ShouldBe("MyInstance.BaseType");
    }

    [Fact]
    public void RemoveSpacesInVariables_ReturnsFalse_WhenNoVariableEndsWithALegacySpacedName()
    {
        ComponentSave component = new ComponentSave { Name = "MyComponent" };
        StateSave state = new StateSave();
        component.States.Add(state);
        state.Variables.Add(new VariableSave { Name = "MyInstance.Visible", Value = true });
        GumProjectSave gumProjectSave = new GumProjectSave();
        gumProjectSave.Components.Add(component);

        bool didChange = _repairLogic.RemoveSpacesInVariables(gumProjectSave);

        didChange.ShouldBeFalse();
    }

    [Fact]
    public void MakeFontFilePathsRelative_ShouldRewriteToRelative_WhenAbsolutePathIsAnExistingFileInsideProject()
    {
        // #5665: a project saved with an absolute Font path still resolves on the machine that
        // wrote it, so the rewrite is unambiguous.
        using TempFolder project = new TempFolder();
        string fontFile = project.CreateFile("Fonts/Sins 7.ttf");
        (GumProjectSave gumProjectSave, VariableSave variable) = CreateProjectWithFont(fontFile);

        bool didChange = _repairLogic.MakeFontFilePathsRelative(gumProjectSave, project.Path);

        didChange.ShouldBeTrue();
        variable.Value.ShouldBe("Fonts/Sins 7.ttf");
    }

    [Fact]
    public void MakeFontFilePathsRelative_ShouldLeavePathAlone_WhenAbsolutePathDoesNotExistOnThisMachine()
    {
        // The reporter's case: a path from another machine has no file here, so where it moved to
        // is a guess and must not be rewritten unprompted.
        using TempFolder project = new TempFolder();
        string missingFile = "C:/Users/Foo/Developer/Tiger/Content/Fonts/Sins 7.ttf";
        (GumProjectSave gumProjectSave, VariableSave variable) = CreateProjectWithFont(missingFile);

        bool didChange = _repairLogic.MakeFontFilePathsRelative(gumProjectSave, project.Path);

        didChange.ShouldBeFalse();
        variable.Value.ShouldBe(missingFile);
    }

    [Fact]
    public void MakeFontFilePathsRelative_ShouldLeavePathAlone_WhenExistingFileIsOutsideProject()
    {
        using TempFolder project = new TempFolder();
        using TempFolder elsewhere = new TempFolder();
        string fontFile = elsewhere.CreateFile("Sins 7.ttf");
        (GumProjectSave gumProjectSave, VariableSave variable) = CreateProjectWithFont(fontFile);

        bool didChange = _repairLogic.MakeFontFilePathsRelative(gumProjectSave, project.Path);

        didChange.ShouldBeFalse();
        variable.Value.ShouldBe(fontFile);
    }

    private static (GumProjectSave project, VariableSave variable) CreateProjectWithFont(string fontPath)
    {
        ComponentSave component = new ComponentSave { Name = "MyComponent" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(state);
        VariableSave variable = new VariableSave { Name = "TextInstance.Font", Value = fontPath };
        state.Variables.Add(variable);
        GumProjectSave gumProjectSave = new GumProjectSave();
        gumProjectSave.Components.Add(component);
        return (gumProjectSave, variable);
    }

    private sealed class TempFolder : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("gum-repair-").FullName;

        /// <summary>Creates an empty file and returns its absolute path, spelled with forward slashes.</summary>
        public string CreateFile(string relativePath)
        {
            string fullPath = System.IO.Path.Combine(Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
            File.WriteAllBytes(fullPath, []);
            return fullPath.Replace('\\', '/');
        }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
