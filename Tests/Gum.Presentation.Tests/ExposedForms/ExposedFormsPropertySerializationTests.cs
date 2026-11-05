using Gum.DataTypes;
using Gum.DataTypes.Serialization;
using Gum.DataTypes.Variables;
using Shouldly;
using System.Globalization;

namespace Gum.Presentation.Tests.ExposedForms;

/// <summary>
/// An exposed Forms property, and the value a container sets on it, survive saving and loading the files,
/// in both the compact and the full format.
/// </summary>
public class ExposedFormsPropertySerializationTests : BaseTestClass
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "gum-exposed-forms-" + Guid.NewGuid().ToString("N"));

    public ExposedFormsPropertySerializationTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public override void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
        base.Dispose();
    }

    private T RoundTrip<T>(T element, bool compact) where T : ElementSave, new()
    {
        string path = Path.Combine(_folder, element.Name + (element is ScreenSave ? ".gusx" : ".gucx"));
        element.Save(path, compact);
        T loaded = GumFileSerializer.DeserializeElementSave<T>(File.ReadAllText(path), GumProjectSave.NativeVersion)!;
        foreach (StateSave state in loaded.AllStates)
        {
            state.FixEnumerations();
        }
        return loaded;
    }

    [Theory]
    [MemberData(nameof(ExposedFormsProject.AllPairs), MemberType = typeof(ExposedFormsProject))]
    public void ExposedFormsProperty_SurvivesSaveAndLoad(string behavior, string property)
    {
        foreach (bool compact in new[] { false, true })
        {
            ExposedFormsProject world = new ExposedFormsProject(behavior, property);
            world.ExposeProperty();

            ComponentSave loaded = RoundTrip(world.Row, compact);

            VariableSave exposed = loaded.DefaultState!.Variables.Single(item => item.Name == "Inner." + property);
            exposed.ExposedAsName.ShouldBe(ExposedFormsProject.ExposedName, $"compact={compact}");
            exposed.SourceObject.ShouldBe("Inner");
            exposed.Type.ShouldBe(world.Property.Type);
            exposed.SetsValue.ShouldBeFalse();
        }
    }

    [Theory]
    [MemberData(nameof(ExposedFormsProject.AllPairs), MemberType = typeof(ExposedFormsProject))]
    public void ValueSetOnAContainerInstance_SurvivesSaveAndLoad(string behavior, string property)
    {
        foreach (bool compact in new[] { false, true })
        {
            ExposedFormsProject world = new ExposedFormsProject(behavior, property);
            world.ExposeProperty();
            object value = world.SampleValue();
            world.Screen.DefaultState!.SetValue("RowInstance." + ExposedFormsProject.ExposedName, value, world.RowInstance, world.Property.Type);

            ScreenSave loaded = RoundTrip(world.Screen, compact);

            VariableSave stored = loaded.DefaultState!.Variables.Single(item => item.Name == "RowInstance." + ExposedFormsProject.ExposedName);
            stored.SetsValue.ShouldBeTrue();
            Convert.ToString(stored.Value, CultureInfo.InvariantCulture)
                .ShouldBe(Convert.ToString(value, CultureInfo.InvariantCulture), $"compact={compact}");
        }
    }

    [Theory]
    [MemberData(nameof(ExposedFormsProject.AllPairs), MemberType = typeof(ExposedFormsProject))]
    public void ExposedFormsProperty_ValueSetInsideTheRow_SurvivesSaveAndLoadAndStaysExposed(string behavior, string property)
    {
        ExposedFormsProject world = new ExposedFormsProject(behavior, property);
        world.ExposeProperty();
        world.Row.DefaultState!.SetValue("Inner." + property, world.SampleValue(), world.Inner, world.Property.Type);

        ComponentSave loaded = RoundTrip(world.Row, compact: false);

        VariableSave variable = loaded.DefaultState!.Variables.Single(item => item.Name == "Inner." + property);
        variable.ExposedAsName.ShouldBe(ExposedFormsProject.ExposedName);
        variable.Value.ShouldNotBeNull();
    }
}
