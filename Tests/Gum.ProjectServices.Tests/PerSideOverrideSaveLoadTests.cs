using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;
using System.IO;
using System.Linq;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Save and load round trip of the nullable per-side/per-corner overrides (Rectangle's
/// <c>CustomRadius*</c>, NineSlice's <c>CustomBorderScale*</c>, #6983 coverage sweep): a set value
/// (including 0) comes back, and an unset override stays unset.
/// </summary>
public class PerSideOverrideSaveLoadTests : BaseTestClass
{
    private static ComponentSave RoundTrip(ComponentSave component, bool compact)
    {
        string folder = Path.Combine(Path.GetTempPath(), "gum-6983-" + Path.GetRandomFileName());
        Directory.CreateDirectory(folder);
        try
        {
            string path = Path.Combine(folder, "Panel." + GumProjectSave.ComponentExtension);
            component.Save(path, useCompactFormat: compact);
            return GumFileSerializer.DeserializeElementSave<ComponentSave>(
                File.ReadAllText(path), (int)GumProjectSave.GumxVersions.AttributeVersion)!;
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static ComponentSave CreatePanel(string instanceName, string baseType, params (string name, float? value)[] variables)
    {
        ComponentSave panel = new ComponentSave { Name = "Panel", BaseType = "Container" };
        panel.States.Add(new StateSave { Name = "Default", ParentContainer = panel });
        panel.Instances.Add(new InstanceSave { Name = instanceName, BaseType = baseType, ParentContainer = panel });
        foreach ((string name, float? value) in variables)
        {
            panel.DefaultState.Variables.Add(new VariableSave
            {
                Name = $"{instanceName}.{name}",
                Type = "float?",
                Value = value,
                SetsValue = true,
            });
        }
        return panel;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NineSliceCustomBorderScales_SetValuesIncludingZero_RoundTrip(bool compact)
    {
        ComponentSave loaded = RoundTrip(
            CreatePanel("Frame", "NineSlice", ("CustomBorderScaleLeft", 2.5f), ("CustomBorderScaleTop", 0f)), compact);

        loaded.DefaultState.Variables.Single(v => v.Name == "Frame.CustomBorderScaleLeft").Value.ShouldBe(2.5f);
        loaded.DefaultState.Variables.Single(v => v.Name == "Frame.CustomBorderScaleTop").Value.ShouldBe(0f);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RectangleCustomRadii_SetValuesIncludingZero_RoundTrip(bool compact)
    {
        ComponentSave loaded = RoundTrip(
            CreatePanel("Box", "Rectangle", ("CustomRadiusTopLeft", 12.5f), ("CustomRadiusBottomRight", 0f)), compact);

        loaded.DefaultState.Variables.Single(v => v.Name == "Box.CustomRadiusTopLeft").Value.ShouldBe(12.5f);
        loaded.DefaultState.Variables.Single(v => v.Name == "Box.CustomRadiusBottomRight").Value.ShouldBe(0f);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UnsetOverrides_StayUnset_AfterRoundTrip(bool compact)
    {
        ComponentSave loaded = RoundTrip(
            CreatePanel("Frame", "NineSlice", ("CustomBorderScaleLeft", null), ("CustomBorderScaleRight", 1f)), compact);

        VariableSave? left = loaded.DefaultState.Variables.SingleOrDefault(v => v.Name == "Frame.CustomBorderScaleLeft");
        (left == null || left.Value == null).ShouldBeTrue();
        loaded.DefaultState.Variables.Single(v => v.Name == "Frame.CustomBorderScaleRight").Value.ShouldBe(1f);
    }
}
