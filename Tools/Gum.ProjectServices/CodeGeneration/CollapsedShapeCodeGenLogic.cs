using Gum.DataTypes;
using System.Collections.Generic;
using System.Linq;

namespace Gum.ProjectServices.CodeGeneration;

/// <inheritdoc cref="ICollapsedShapeCodeGenLogic"/>
public class CollapsedShapeCodeGenLogic : ICollapsedShapeCodeGenLogic
{
    /// <summary>
    /// The runtime syntax version that introduced the collapsed shape runtimes (#2774 / PR #2910).
    /// Below this version, codegen keeps emitting the legacy types so existing pipelines don't
    /// churn at runtime-upgrade time.
    /// </summary>
    public const int CollapsedShapeSyntaxVersion = 2;

    /// <inheritdoc/>
    public bool ShouldCollapse(string? standardElementName, int resolvedSyntaxVersion, CodeOutputProjectSettings? projectSettings)
    {
        if (projectSettings == null)
        {
            return false;
        }

        bool isMonoGameFamily = projectSettings.OutputLibrary == OutputLibrary.MonoGame ||
            projectSettings.OutputLibrary == OutputLibrary.MonoGameForms;

        // FindByName casts must match what the runtime .gumx loader instantiates, which is still
        // the legacy type — only fully-in-code instantiation may target the collapsed runtimes.
        return resolvedSyntaxVersion >= CollapsedShapeSyntaxVersion &&
            isMonoGameFamily &&
            projectSettings.ObjectInstantiationType == ObjectInstantiationType.FullyInCode &&
            TryGetCollapsedRuntimeName(standardElementName) != null;
    }

    /// <inheritdoc/>
    public string? TryGetCollapsedRuntimeName(string? standardElementName) => standardElementName switch
    {
        "ColoredCircle" => "CircleRuntime",
        "ColoredRectangle" => "RectangleRuntime",
        "RoundedRectangle" => "RectangleRuntime",
        _ => null,
    };

    /// <inheritdoc/>
    public bool GetEffectiveIsFilled(ElementSave? container, InstanceSave? instance, ElementSave shapeElement)
    {
        string variableName = instance == null ? "IsFilled" : $"{instance.Name}.IsFilled";
        object? explicitValue = container?.DefaultState?.Variables
            .FirstOrDefault(item => item.Name == variableName && item.SetsValue)?.Value;
        if (explicitValue is bool explicitIsFilled)
        {
            return explicitIsFilled;
        }

        object? shapeDefaultValue = shapeElement.DefaultState?.Variables
            .FirstOrDefault(item => item.Name == "IsFilled")?.Value;
        if (shapeDefaultValue is bool shapeDefaultIsFilled)
        {
            return shapeDefaultIsFilled;
        }

        // ColoredRectangle defines no IsFilled variable — it is always filled.
        return true;
    }

    /// <inheritdoc/>
    public string TranslateVariableRootName(string rootName, bool effectiveIsFilled)
    {
        string channelPrefix = effectiveIsFilled ? "Fill" : "Stroke";
        return rootName switch
        {
            "Red" => channelPrefix + "Red",
            "Green" => channelPrefix + "Green",
            "Blue" => channelPrefix + "Blue",
            "Alpha" => channelPrefix + "Alpha",
            _ => rootName,
        };
    }

    /// <inheritdoc/>
    public bool ShouldDropVariable(string rootName, bool effectiveIsFilled)
    {
        // Legacy standalone gradient start color: the collapsed shapes derive the gradient start
        // from the active body color (#3009), so these have no target property.
        if (rootName is "Red1" or "Green1" or "Blue1" or "Alpha1")
        {
            return true;
        }

        // Legacy filled shapes rendered no stroke, and the baseline block zeroes StrokeWidth.
        // Passing explicit stroke values through would draw an outline that never existed.
        if (effectiveIsFilled && rootName is "StrokeWidth" or "StrokeDashLength" or "StrokeGapLength")
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetBaselineAssignments(string standardElementName, bool effectiveIsFilled)
    {
        // The collapsed runtimes construct as a stroke-only outline with a transparent (but
        // opaque-white-valued) fill; the legacy shapes constructed as the inverse. Flipping
        // IsFilled paints the white fill, matching the legacy default disc, and zeroing
        // StrokeWidth removes the outline the legacy shapes never drew. These run after
        // SetInitialState(), whose legacy single-color variables route to the stroke slot
        // on the collapsed runtimes (#2938 semantics); explicit instance variables follow in
        // ApplyDefaultVariables and override per-channel values.
        if (effectiveIsFilled)
        {
            yield return "IsFilled = true;";
            yield return "StrokeWidth = 0f;";
        }
        else
        {
            yield return "IsFilled = false;";
            // Legacy outline default width.
            yield return "StrokeWidth = 2f;";
        }

        if (standardElementName == "RoundedRectangle")
        {
            // Legacy RoundedRectangle default; the collapsed RectangleRuntime defaults to 0.
            yield return "CornerRadius = 5f;";
        }
    }
}
