using System;
using System.Collections.Generic;
using System.IO;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics.Fonts;

namespace Gum.Bundle;

/// <summary>
/// Single source of truth for "what fonts does this project need bitmap-rendered?".
/// Consumed by both <c>HeadlessFontGenerationService.CollectRequiredFonts</c> (used by
/// <c>gumcli fonts</c> and the WPF tool) and <see cref="GumProjectDependencyWalker"/>
/// (used by <c>gumcli pack</c>).
/// </summary>
/// <remarks>
/// Decoupled from the <c>ObjectFinder.Self</c> singleton via a constructor-injected
/// element resolver so callers that don't (or shouldn't) populate the singleton can still
/// use the collector.
/// <para>
/// Known limitation: this collector does NOT yet resolve <c>StyleCategoryState</c>
/// indirection (where a TextInstance's <c>StyleCategoryState</c> property points at a
/// state in a Styles component whose <c>Strong.*</c> variables should be pulled in as
/// the resolved font for the referencing TextInstance). That's a deeper runtime-resolution
/// gap that affects both gumcli fonts and gumcli pack equally; tracking separately.
/// </para>
/// </remarks>
public class FontReferenceCollector
{
    private readonly Func<InstanceSave, ElementSave?> _resolveInstanceElement;

    /// <summary>
    /// Creates a collector that resolves an instance's referenced element via
    /// <paramref name="resolveInstanceElement"/> (typically
    /// <c>i =&gt; ObjectFinder.Self.GetElementSave(i)</c>).
    /// </summary>
    public FontReferenceCollector(Func<InstanceSave, ElementSave?> resolveInstanceElement)
    {
        _resolveInstanceElement = resolveInstanceElement ?? throw new ArgumentNullException(nameof(resolveInstanceElement));
    }

    /// <summary>
    /// Collects all unique fonts required by the given elements without performing any I/O.
    /// Keyed by <see cref="BmfcSave.FontCacheFileName"/> so duplicate font+size+style combinations
    /// are automatically deduplicated.
    /// </summary>
    public Dictionary<string, BmfcSave> Collect(GumProjectSave project, IEnumerable<ElementSave> elements)
    {
        string fontRanges = project.FontRanges;
        int spacingHorizontal = project.FontSpacingHorizontal;
        int spacingVertical = project.FontSpacingVertical;

        Dictionary<string, BmfcSave> bitmapFonts = new Dictionary<string, BmfcSave>();

        foreach (ElementSave element in elements)
        {
            foreach (StateSave state in element.AllStates)
            {
                // Resolve variable references so font properties set via references
                // (e.g. "FontSize = HeaderText.FontSize") are baked into the state before
                // we read them. In the tool this happens on every edit, but in headless
                // paths the references may not have been applied yet.
                element.ApplyVariableReferences(state);

                BmfcSave? bmfcSave = TryGetBmfcSaveFor(instance: null, state, fontRanges, spacingHorizontal, spacingVertical);
                if (bmfcSave != null)
                {
                    bitmapFonts[bmfcSave.FontCacheFileName] = bmfcSave;
                }

                foreach (InstanceSave instance in element.Instances)
                {
                    BmfcSave? bmfcSaveInner = TryGetBmfcSaveFor(instance, state, fontRanges, spacingHorizontal, spacingVertical);
                    if (bmfcSaveInner != null)
                    {
                        bitmapFonts[bmfcSaveInner.FontCacheFileName] = bmfcSaveInner;
                    }

                    // Direct read on the instance only finds font properties set on this element
                    // (e.g., "MyComponentInstance.Font"). For component instances, font properties
                    // live on inner Text instances and may be partially exposed. Use
                    // RecursiveVariableFinder to resolve through the component hierarchy.
                    CollectFontsFromNestedTextInstances(element, state, instance,
                        bitmapFonts, fontRanges, spacingHorizontal, spacingVertical);
                }
            }
        }

        return bitmapFonts;
    }

    private static BmfcSave? TryGetBmfcSaveFor(InstanceSave? instance, StateSave stateSave,
        string fontRanges, int spacingHorizontal, int spacingVertical)
    {
        string prefix = "";
        if (instance != null)
        {
            prefix = instance.Name + ".";
        }

        int? fontSize = stateSave.GetValueRecursive(prefix + "FontSize") as int?;
        string? fontValue = stateSave.GetValueRecursive(prefix + "Font") as string;
        int outlineValue = stateSave.GetValueRecursive(prefix + "OutlineThickness") as int? ?? 0;

        // default to true to match how old behavior worked
        bool fontSmoothing = stateSave.GetValueRecursive(prefix + "UseFontSmoothing") as bool? ?? true;
        bool isItalic = stateSave.GetValueRecursive(prefix + "IsItalic") as bool? ?? false;
        bool isBold = stateSave.GetValueRecursive(prefix + "IsBold") as bool? ?? false;

        if (fontValue == null || fontSize == null)
        {
            return null;
        }

        BmfcSave bmfcSave = new BmfcSave();
        bmfcSave.FontSize = fontSize.Value;
        bmfcSave.OutlineThickness = outlineValue;
        bmfcSave.UseSmoothing = fontSmoothing;
        bmfcSave.IsItalic = isItalic;
        bmfcSave.IsBold = isBold;
        bmfcSave.Ranges = fontRanges;
        bmfcSave.SpacingHorizontal = spacingHorizontal;
        bmfcSave.SpacingVertical = spacingVertical;

        if (BmfcSave.IsFontFilePath(fontValue))
        {
            bmfcSave.FontFile = fontValue;
            bmfcSave.FontName = Path.GetFileNameWithoutExtension(fontValue);
        }
        else
        {
            bmfcSave.FontName = fontValue;
        }

        return bmfcSave;
    }

    private void CollectFontsFromNestedTextInstances(ElementSave outerElement, StateSave outerState,
        InstanceSave componentInstance, Dictionary<string, BmfcSave> bitmapFonts,
        string fontRanges, int spacingHorizontal, int spacingVertical)
    {
        ElementSave? componentElement = _resolveInstanceElement(componentInstance);
        if (componentElement == null)
        {
            return;
        }

        foreach (InstanceSave innerInstance in componentElement.Instances)
        {
            ElementSave? innerElement = _resolveInstanceElement(innerInstance);
            if (innerElement == null)
            {
                continue;
            }

            if (innerElement is StandardElementSave standard && standard.Name == "Text")
            {
                List<ElementWithState> elementStack = new List<ElementWithState>
                {
                    new ElementWithState(outerElement) { StateName = outerState.Name, InstanceName = componentInstance.Name },
                    new ElementWithState(componentElement) { InstanceName = innerInstance.Name },
                    new ElementWithState(innerElement)
                };

                BmfcSave? bmfcSave = TryGetBmfcSaveFromStack(elementStack, fontRanges, spacingHorizontal, spacingVertical);
                if (bmfcSave != null)
                {
                    bitmapFonts[bmfcSave.FontCacheFileName] = bmfcSave;
                }
            }
            else
            {
                CollectFontsFromNestedTextInstances(outerElement, outerState, componentInstance,
                    componentElement, innerInstance, bitmapFonts, fontRanges, spacingHorizontal, spacingVertical);
            }
        }
    }

    private void CollectFontsFromNestedTextInstances(ElementSave outerElement, StateSave outerState,
        InstanceSave outerInstance, ElementSave parentComponent, InstanceSave innerInstance,
        Dictionary<string, BmfcSave> bitmapFonts,
        string fontRanges, int spacingHorizontal, int spacingVertical)
    {
        ElementSave? innerElement = _resolveInstanceElement(innerInstance);
        if (innerElement == null)
        {
            return;
        }

        if (innerElement is StandardElementSave standard && standard.Name == "Text")
        {
            List<ElementWithState> elementStack = new List<ElementWithState>
            {
                new ElementWithState(outerElement) { StateName = outerState.Name, InstanceName = outerInstance.Name },
                new ElementWithState(parentComponent) { InstanceName = innerInstance.Name },
                new ElementWithState(innerElement)
            };

            BmfcSave? bmfcSave = TryGetBmfcSaveFromStack(elementStack, fontRanges, spacingHorizontal, spacingVertical);
            if (bmfcSave != null)
            {
                bitmapFonts[bmfcSave.FontCacheFileName] = bmfcSave;
            }
        }
        else
        {
            foreach (InstanceSave deeperInstance in innerElement.Instances)
            {
                CollectFontsFromNestedTextInstances(outerElement, outerState, outerInstance,
                    innerElement, deeperInstance, bitmapFonts, fontRanges, spacingHorizontal, spacingVertical);
            }
        }
    }

    private static BmfcSave? TryGetBmfcSaveFromStack(List<ElementWithState> elementStack,
        string fontRanges, int spacingHorizontal, int spacingVertical)
    {
        RecursiveVariableFinder rfv = new RecursiveVariableFinder(elementStack);

        string? fontValue = rfv.GetValueByBottomName("Font") as string;
        int? fontSize = rfv.GetValueByBottomName("FontSize") as int?;

        if (fontValue == null || fontSize == null)
        {
            return null;
        }

        int outlineValue = rfv.GetValueByBottomName("OutlineThickness") as int? ?? 0;
        bool fontSmoothing = rfv.GetValueByBottomName("UseFontSmoothing") as bool? ?? true;
        bool isItalic = rfv.GetValueByBottomName("IsItalic") as bool? ?? false;
        bool isBold = rfv.GetValueByBottomName("IsBold") as bool? ?? false;

        BmfcSave bmfcSave = new BmfcSave();
        bmfcSave.FontSize = fontSize.Value;
        bmfcSave.FontName = fontValue;
        bmfcSave.OutlineThickness = outlineValue;
        bmfcSave.UseSmoothing = fontSmoothing;
        bmfcSave.IsItalic = isItalic;
        bmfcSave.IsBold = isBold;
        bmfcSave.Ranges = fontRanges;
        bmfcSave.SpacingHorizontal = spacingHorizontal;
        bmfcSave.SpacingVertical = spacingVertical;

        return bmfcSave;
    }
}
