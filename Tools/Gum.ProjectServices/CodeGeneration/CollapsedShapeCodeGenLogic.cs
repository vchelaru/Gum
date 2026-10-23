using Gum.DataTypes;
using System.Collections.Generic;
using System.Linq;

namespace Gum.ProjectServices.CodeGeneration;

/// <inheritdoc cref="ICollapsedShapeCodeGenLogic"/>
public class CollapsedShapeCodeGenLogic : ICollapsedShapeCodeGenLogic
{
    /// <summary>
    /// The first runtime syntax version guaranteed to have the channel properties (<c>FillRed</c>,
    /// <c>StrokeRed</c>, ...) and <c>IsFilled</c> on <c>CircleRuntime</c> / <c>RectangleRuntime</c>.
    /// Both the collapsed shape emission (#2775) and the plain Circle/Rectangle stroke routing
    /// (#6594) assign them. They shipped after version 2 was stamped, so version 2 is not a safe
    /// floor (#6613). Below this version, codegen keeps the legacy types and variable names.
    /// </summary>
    public const int ShapeColorChannelSyntaxVersion = 3;

    /// <inheritdoc/>
    public bool ShouldCollapse(string? standardElementName, int resolvedSyntaxVersion, CodeOutputProjectSettings? projectSettings)
    {
        if (projectSettings == null)
        {
            return false;
        }

        bool isMonoGameFamily = projectSettings.OutputLibrary == OutputLibrary.MonoGame ||
            projectSettings.OutputLibrary == OutputLibrary.GumForms;

        // FindByName casts must match what the runtime .gumx loader instantiates, which is still
        // the legacy type — only fully-in-code instantiation may target the collapsed runtimes.
        return resolvedSyntaxVersion >= ShapeColorChannelSyntaxVersion &&
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
    public string TranslateOutlineShapeVariableRootName(string? standardElementName, string rootName, int resolvedSyntaxVersion)
    {
        if (resolvedSyntaxVersion < ShapeColorChannelSyntaxVersion ||
            standardElementName is not ("Circle" or "Rectangle"))
        {
            return rootName;
        }

        return TranslateVariableRootName(rootName, effectiveIsFilled: false);
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
