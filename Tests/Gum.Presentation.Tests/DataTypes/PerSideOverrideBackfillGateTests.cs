using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Gum.Presentation.Tests.DataTypes;

/// <summary>
/// Version gating for the per-side/per-corner overrides: Rectangle's <c>CustomRadius*</c> (v3) and
/// NineSlice's <c>CustomBorderScale*</c> (v7). A project older than the gate must not have them
/// back-filled on load, and the variable grid must hide them (#6983 coverage sweep; the corner radius
/// had gate tests only for CornerRadius itself).
/// </summary>
public class PerSideOverrideBackfillGateTests : BaseTestClass
{
    private static readonly string[] CornerOverrides =
    {
        "CustomRadiusTopLeft", "CustomRadiusTopRight", "CustomRadiusBottomLeft", "CustomRadiusBottomRight"
    };

    private static readonly string[] SideOverrides =
    {
        "CustomBorderScaleLeft", "CustomBorderScaleTop", "CustomBorderScaleRight", "CustomBorderScaleBottom"
    };

    private static StateSave DefaultStateAfterInitialize(string standardName, int version, params string[] classicVariableNames)
    {
        StandardElementSave standard = new StandardElementSave
        {
            Name = standardName,
            States = new List<StateSave>
            {
                new StateSave
                {
                    Name = "Default",
                    Variables = classicVariableNames
                        .Select(name => new VariableSave { Name = name, Type = "float", Value = 0f, SetsValue = true })
                        .ToList(),
                },
            },
        };
        GumProjectSave project = new GumProjectSave { Version = version };
        project.StandardElements.Add(standard);

        project.Initialize();

        return project.StandardElements.Single(item => item.Name == standardName).DefaultState;
    }

    [Fact]
    public void Initialize_DoesNotInjectCustomRadiusOverrides_IntoPreV3Rectangle()
    {
        StateSave state = DefaultStateAfterInitialize("Rectangle",
            (int)GumProjectSave.GumxVersions.AttributeVersion, "Width", "Height", "Visible");

        foreach (string name in CornerOverrides)
        {
            state.Variables.Any(v => v.Name == name).ShouldBeFalse(name);
        }
    }

    [Fact]
    public void Initialize_InjectsCustomRadiusOverrides_IntoV3Rectangle()
    {
        StateSave state = DefaultStateAfterInitialize("Rectangle",
            (int)GumProjectSave.GumxVersions.ShapeVariableExpansion, "Width", "Height", "Visible");

        foreach (string name in CornerOverrides)
        {
            state.Variables.Any(v => v.Name == name).ShouldBeTrue(name);
        }
    }

    [Fact]
    public void Initialize_DoesNotInjectCustomBorderScaleOverrides_IntoPreV7NineSlice()
    {
        StateSave state = DefaultStateAfterInitialize("NineSlice",
            (int)GumProjectSave.GumxVersions.FontCachePathHash, "Width", "Height", "Visible");

        foreach (string name in SideOverrides)
        {
            state.Variables.Any(v => v.Name == name).ShouldBeFalse(name);
        }
    }

    [Fact]
    public void Initialize_InjectsCustomBorderScaleOverrides_IntoV7NineSlice()
    {
        StateSave state = DefaultStateAfterInitialize("NineSlice",
            (int)GumProjectSave.GumxVersions.ShaderParametersExpansion, "Width", "Height", "Visible");

        foreach (string name in SideOverrides)
        {
            state.Variables.Any(v => v.Name == name).ShouldBeTrue(name);
        }
    }

    [Fact]
    public void Initialize_KeepsBorderScaleItself_InPreV7NineSlice()
    {
        StateSave state = DefaultStateAfterInitialize("NineSlice",
            (int)GumProjectSave.GumxVersions.FontCachePathHash, "Width", "Height", "Visible");

        state.Variables.Any(v => v.Name == "BorderScale").ShouldBeTrue();
    }

    [Theory]
    [InlineData("CustomRadiusTopLeft")]
    [InlineData("CustomRadiusTopRight")]
    [InlineData("CustomRadiusBottomLeft")]
    [InlineData("CustomRadiusBottomRight")]
    public void VersionGate_HidesCustomRadiusOverrides_OnPreV3Rectangle(string variableName)
    {
        ShapeVariableVersionGate gate = new ShapeVariableVersionGate();

        gate.GetIfHiddenForProjectVersion(variableName, "Rectangle", (int)GumProjectSave.GumxVersions.AttributeVersion).ShouldBeTrue();
        gate.GetIfHiddenForProjectVersion(variableName, "Rectangle", (int)GumProjectSave.GumxVersions.ShapeVariableExpansion).ShouldBeFalse();
    }
}
