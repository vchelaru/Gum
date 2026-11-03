using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// A JSON file's problem is described in JSON terms: no "XML" for a <c>.gucj</c>, no <c>.gucx</c> fix for a JSON project (#7059).
/// </summary>
public class CheckJsonMessageWordingTests : CheckHandWrittenJsonFileHarness
{
    [Fact]
    public void Check_MalformedJsonComponent_ShouldNotCallTheFileXml()
    {
        string project = CreateJsonProject("MalformedComponent");
        WriteFile(project, "Components", "Holder.gucj", "{\"Name\":\"Holder\",\"BaseType\":\"Container\",}");
        AddJsonReference(project, "ComponentReferences", "Holder");

        CheckEntry entry = Check(project).Entries.First(e => e.Element == "Holder");

        entry.Message.ShouldNotContain("XML");
    }

    [Fact]
    public void Check_MalformedJsonBehavior_ShouldNotCallTheFileXml()
    {
        string project = CreateJsonProject("MalformedBehavior");
        WriteFile(project, "Behaviors", "Needy.behj", "{ not json");
        AddJsonReference(project, "BehaviorReferences", "Needy");

        CheckEntry entry = Check(project).Entries.First(e => e.Element == "Needy");

        entry.Message.ShouldNotContain("XML");
    }

    [Fact]
    public void Check_JsonComponentWhoseNameDiffersFromItsReference_ShouldNotCallTheFileXml()
    {
        string project = CreateJsonProject("NameMismatch");
        WriteFile(project, "Components", "Holder.gucj", JElement("Other", "Container", JDefault()));
        AddJsonReference(project, "ComponentReferences", "Holder");

        CheckEntry entry = Check(project).Entries.First(e => e.Message.Contains("Holder"));

        entry.Message.ShouldNotContain("XML");
    }

    [Fact]
    public void Check_JsonComponentWithoutBaseType_ShouldNotTellTheUserToEditAGucxFile()
    {
        string project = CreateJsonProject("NoBaseType");
        AddJsonComponent(project, "Orphan", null, JDefault());

        CheckEntry entry = Check(project).Entries.First(e => e.Code == "GUM0015");

        entry.Message.ShouldNotContain(".gucx");
    }
}
