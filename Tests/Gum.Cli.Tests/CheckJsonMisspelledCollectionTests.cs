using Gum.DataTypes;
using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// The XML path reports a misspelled collection (<c>&lt;Instances&gt;</c> for <c>&lt;Instance&gt;</c>) that the
/// serializer would silently drop. The JSON loader drops a misspelled <c>Instance</c> or <c>State</c> just as
/// silently, so a hand-written component loses its content with no message (#7059).
/// </summary>
public class CheckJsonMisspelledCollectionTests : CheckHandWrittenJsonFileHarness
{
    [Fact]
    public void Check_JsonComponentWithInstanceInsteadOfInstances_ShouldReportTheComponent()
    {
        string project = CreateJsonProject("InstanceSingular");
        AddJsonComponent(project, "Holder", "Container", JDefault(), "\"Instance\":[" + JInstance("Label", "Text") + "]");

        CheckRun run = Check(project);

        run.Entries.ShouldContain(e => e.Element == "Holder" && e.Message.Contains("Instance"));
    }

    [Fact]
    public void Check_JsonComponentWithStateInsteadOfStates_ShouldReportTheComponent()
    {
        string project = CreateJsonProject("StateSingular");
        AddJsonComponent(project, "Holder", "Container", "\"State\":[" + JState("Default", JVar("float", "Width", "\"ValueAsFloat\":4")) + "]");

        CheckRun run = Check(project);

        run.Entries.ShouldContain(e => e.Element == "Holder" && e.Message.Contains("State"));
    }

    [Fact]
    public void Check_JsonStateWithVariableInsteadOfVariables_ShouldReportTheComponent()
    {
        string project = CreateJsonProject("VariableSingular");
        AddJsonComponent(project, "Holder", "Container",
            "\"States\":[{\"Name\":\"Default\",\"Variable\":[" + JVar("float", "Width", "\"ValueAsFloat\":4") + "]}]");

        CheckRun run = Check(project);

        run.Entries.ShouldContain(e => e.Element == "Holder" && e.Message.Contains("Variable"));
    }

    [Fact]
    public void Check_JsonComponentWithLowercasePropertyNames_ShouldReportTheFile()
    {
        string project = CreateJsonProject("LowercaseNames");
        WriteFile(project, "Components", "Holder.gucj", "{\"name\":\"Holder\",\"baseType\":\"Container\",\"states\":[]}");
        AddJsonReference(project, "ComponentReferences", "Holder");

        CheckRun run = Check(project);

        run.Entries.ShouldContain(e => e.Message.Contains("\"name\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_JsonComponentWithInstanceInsteadOfInstances_ShouldLoseTheInstance()
    {
        string json = JElement("Holder", "Container", JDefault(), "\"Instance\":[" + JInstance("Label", "Text") + "]");

        ComponentSave component = Gum.DataTypes.Serialization.Json.GumJsonFileSerializer.DeserializeElement<ComponentSave>(json);

        component.Instances.Count.ShouldBe(0);
    }
}
