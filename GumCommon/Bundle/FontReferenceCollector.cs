using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
/// <para>
/// Known limitation: a conditional (ternary) <c>VariableReferences</c> row on the element
/// itself or a direct instance has every branch pregenerated (see #4042), but a conditional
/// reference reached only through a nested component instance's inner Text instances
/// (<c>CollectFontsFromNestedTextInstances</c>) still resolves a single value via
/// <see cref="RecursiveVariableFinder"/> - only the currently-active branch is collected there.
/// </para>
/// </remarks>
public class FontReferenceCollector
{
    /// <summary>
    /// Font-affecting properties whose <c>VariableReferences</c> row is branch-enumerated
    /// (all ternary branches collected, not just the one active now). See #4042.
    /// </summary>
    /// <remarks>
    /// This is every input to <see cref="BmfcSave.GetFontCacheFileNameFor"/>, which is what makes
    /// two Texts need two baked atlases - <c>UseCustomFont</c> included, since it decides whether
    /// <c>Font</c> or <c>CustomFontFile</c> is the identity. A property that reaches
    /// <see cref="BuildBmfcSave"/> but not the cache name (the dropshadow offset and color, applied
    /// at draw time) deliberately stays out: its branches all bake the same file. Anything added to
    /// the cache name belongs here too, or only its currently-active branch gets pregenerated (#4936).
    /// </remarks>
    private static readonly string[] FontAffectingVariableNames =
    {
        "Font", "FontSize", "OutlineThickness", "UseFontSmoothing", "IsItalic", "IsBold",
        "UseCustomFont", "CustomFontFile", "HasDropshadow", "DropshadowBlur"
    };

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

                // A non-default state that sets nothing for a given owner resolves that owner's
                // font properties identically to the default state, which is always collected -
                // so it can only produce an already-deduplicated entry. Skipping those pairs is
                // what keeps collection from costing (states x instances) recursive lookups on a
                // project whose category states mostly touch one instance each.
                bool isDefaultState = state == element.DefaultState;

                if (isDefaultState || StateSetsAnythingFor(state, ownerName: null))
                {
                    foreach (BmfcSave bmfcSave in CollectAllBmfcSavesFor(instance: null, state, fontRanges, spacingHorizontal, spacingVertical))
                    {
                        bitmapFonts[bmfcSave.FontCacheFileName] = bmfcSave;
                    }
                }

                foreach (InstanceSave instance in element.Instances)
                {
                    if (!isDefaultState && !StateSetsAnythingFor(state, instance.Name))
                    {
                        continue;
                    }

                    foreach (BmfcSave bmfcSaveInner in CollectAllBmfcSavesFor(instance, state, fontRanges, spacingHorizontal, spacingVertical))
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

    /// <summary>
    /// Whether <paramref name="stateSave"/> sets any variable belonging to <paramref name="ownerName"/>
    /// - an instance name, or null for the element itself. Deliberately checks membership rather than
    /// font-specific names: a font property can reach an owner under an arbitrary name through an
    /// exposed variable, so only "sets nothing at all for this owner" is safe to skip.
    /// </summary>
    private static bool StateSetsAnythingFor(StateSave stateSave, string? ownerName)
    {
        if (ownerName == null)
        {
            return stateSave.Variables.Any(v => !v.Name.Contains('.'))
                || stateSave.VariableLists.Any(vl => string.IsNullOrEmpty(vl.SourceObject));
        }

        string prefix = ownerName + ".";
        return stateSave.Variables.Any(v => v.Name.StartsWith(prefix, StringComparison.Ordinal))
            || stateSave.VariableLists.Any(vl => vl.SourceObject == ownerName);
    }

    private static BmfcSave? TryGetBmfcSaveFor(InstanceSave? instance, StateSave stateSave,
        string fontRanges, int spacingHorizontal, int spacingVertical)
    {
        string prefix = instance != null ? instance.Name + "." : "";
        return BuildBmfcSave(name => stateSave.GetValueRecursive(prefix + name),
            fontRanges, spacingHorizontal, spacingVertical);
    }

    /// <summary>
    /// Collects every <see cref="BmfcSave"/> combination for <paramref name="instance"/> (or the
    /// element itself when null), enumerating all branches of any conditional (ternary)
    /// <c>VariableReferences</c> row that targets a <see cref="FontAffectingVariableNames"/>
    /// entry, instead of only the branch <c>ApplyVariableReferences</c> already
    /// baked into <paramref name="stateSave"/>. Yields a single combo (the baseline already
    /// applied) when none of the owner's font-affecting properties are set via a conditional
    /// reference. See #4042.
    /// </summary>
    private static IEnumerable<BmfcSave> CollectAllBmfcSavesFor(InstanceSave? instance, StateSave stateSave,
        string fontRanges, int spacingHorizontal, int spacingVertical)
    {
        string prefix = instance != null ? instance.Name + "." : "";
        string? ownerName = instance?.Name;

        VariableListSave? variableList = stateSave.VariableLists
            .FirstOrDefault(vl => vl.GetRootName() == "VariableReferences"
                && vl.ValueAsIList.Count > 0
                && string.Equals(vl.SourceObject, ownerName, StringComparison.Ordinal));

        Dictionary<string, List<object>> branchValuesByProperty = new();

        if (variableList != null)
        {
            foreach (string referenceString in variableList.ValueAsIList.Cast<string>())
            {
                var parsed = ElementSaveExtensions.GetAllVariableReferenceBranches(instance, referenceString, stateSave);
                if (parsed == null)
                {
                    continue;
                }

                (string variableName, IEnumerable<object> values) = parsed.Value;
                string unqualified = variableName.Contains('.')
                    ? variableName.Substring(variableName.LastIndexOf('.') + 1)
                    : variableName;

                if (!FontAffectingVariableNames.Contains(unqualified))
                {
                    continue;
                }

                List<object> distinctValues = values.Where(v => v != null).Distinct().ToList();
                if (distinctValues.Count > 1)
                {
                    branchValuesByProperty[unqualified] = distinctValues;
                }
            }
        }

        if (branchValuesByProperty.Count == 0)
        {
            BmfcSave? baseline = TryGetBmfcSaveFor(instance, stateSave, fontRanges, spacingHorizontal, spacingVertical);
            if (baseline != null)
            {
                yield return baseline;
            }
            yield break;
        }

        foreach (Dictionary<string, object> combo in CrossProduct(branchValuesByProperty))
        {
            object? Lookup(string name) => combo.TryGetValue(name, out object? overridden)
                ? overridden
                : stateSave.GetValueRecursive(prefix + name);

            BmfcSave? bmfcSave = BuildBmfcSave(Lookup, fontRanges, spacingHorizontal, spacingVertical);
            if (bmfcSave != null)
            {
                yield return bmfcSave;
            }
        }
    }

    /// <summary>
    /// All combinations of one value per key in <paramref name="branchValuesByProperty"/>
    /// (Cartesian product), used to cross-multiply multiple independently-conditional
    /// font-affecting properties (e.g. both <c>Font</c> and <c>FontSize</c> set via ternaries).
    /// </summary>
    private static IEnumerable<Dictionary<string, object>> CrossProduct(Dictionary<string, List<object>> branchValuesByProperty)
    {
        IEnumerable<Dictionary<string, object>> combos = new[] { new Dictionary<string, object>() };

        foreach (KeyValuePair<string, List<object>> property in branchValuesByProperty)
        {
            combos = combos.SelectMany(existing => property.Value.Select(value =>
            {
                Dictionary<string, object> next = new(existing) { [property.Key] = value };
                return next;
            }));
        }

        return combos;
    }

    private static BmfcSave? BuildBmfcSave(Func<string, object?> getValue,
        string fontRanges, int spacingHorizontal, int spacingVertical)
    {
        // Font and FontSize gate the result, so read them first and stop on a miss. Every
        // non-Text instance misses on Font, and each recursive lookup walks the state, base
        // and instance chains, so the style lookups below only run for real text (#4865).
        string? fontValue = getValue("Font") as string;
        if (fontValue == null)
        {
            return null;
        }

        int? fontSize = getValue("FontSize") as int?;
        if (fontSize == null)
        {
            return null;
        }

        // UseCustomFont makes CustomFontFile the font's identity and leaves Font and the style
        // variables inert - the variables tab hides them rather than clearing them, so they keep
        // whatever system font was last picked. A .ttf/.otf custom file still needs baking (the same
        // ResolveTtfSourcePath decision every backend's font-loading path makes), but a pre-baked
        // .fnt is loaded straight off disk and needs nothing generated for it. See #4922.
        if (getValue("UseCustomFont") as bool? == true)
        {
            string? customFontFile = BmfcSave.ResolveTtfSourcePath(
                useCustomFont: true, getValue("CustomFontFile") as string, fontValue);

            if (customFontFile == null)
            {
                return null;
            }

            fontValue = customFontFile;
        }

        int outlineValue = getValue("OutlineThickness") as int? ?? 0;

        // default to true to match how old behavior worked
        bool fontSmoothing = getValue("UseFontSmoothing") as bool? ?? true;
        bool isItalic = getValue("IsItalic") as bool? ?? false;
        bool isBold = getValue("IsBold") as bool? ?? false;

        // The shadow is baked into the atlas as a blurred silhouette variant, so a dropshadow Text
        // needs a different file than the same font without one - BmfcSave.FontCacheFileName gives it
        // a "_ds{blur}" suffix. Offset and color are applied at draw time and don't change the bake,
        // but they're carried along so the BmfcSave stays a full description of the font - read only
        // when the shadow is on, so a plain Text doesn't pay seven more recursive lookups (#4929).
        bool hasDropshadow = getValue("HasDropshadow") as bool? ?? false;

        BmfcSave bmfcSave = new BmfcSave();
        bmfcSave.FontSize = fontSize.Value;
        bmfcSave.OutlineThickness = outlineValue;
        bmfcSave.UseSmoothing = fontSmoothing;
        bmfcSave.IsItalic = isItalic;
        bmfcSave.IsBold = isBold;
        bmfcSave.Ranges = fontRanges;
        bmfcSave.SpacingHorizontal = spacingHorizontal;
        bmfcSave.SpacingVertical = spacingVertical;
        bmfcSave.HasDropshadow = hasDropshadow;

        if (hasDropshadow)
        {
            bmfcSave.DropshadowOffsetX = getValue("DropshadowOffsetX") as float? ?? 0f;
            bmfcSave.DropshadowOffsetY = getValue("DropshadowOffsetY") as float? ?? 0f;
            bmfcSave.DropshadowBlur = getValue("DropshadowBlur") as float? ?? 0f;
            bmfcSave.DropshadowRed = (byte)(getValue("DropshadowRed") as int? ?? 0);
            bmfcSave.DropshadowGreen = (byte)(getValue("DropshadowGreen") as int? ?? 0);
            bmfcSave.DropshadowBlue = (byte)(getValue("DropshadowBlue") as int? ?? 0);
            bmfcSave.DropshadowAlpha = (byte)(getValue("DropshadowAlpha") as int? ?? 0);
        }

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

        return BuildBmfcSave(name => rfv.GetValueByBottomName(name), fontRanges, spacingHorizontal, spacingVertical);
    }
}
