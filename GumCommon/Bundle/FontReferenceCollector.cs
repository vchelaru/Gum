using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
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
/// A font set through a categorized state (for example <c>StyleCategoryState</c>) resolves the way
/// the tool does: <c>GetValueRecursive</c> for direct instances, and
/// <see cref="RecursiveVariableFinder.GetValueByBottomName"/> for Texts inside nested components,
/// including a state assigned by the container through an exposed variable.
/// </para>
/// <para>
/// A conditional (ternary) <c>VariableReferences</c> row has every branch pregenerated (see #4042),
/// including one on a component instance that drives a font variable inside the component
/// (<c>CollectFontsFromNestedTextInstancesAllBranches</c>).
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
                // paths the references may not have been applied yet. Collection is a read
                // (an error check runs it on every selection), so the applied values are
                // rolled back once this state is collected (#5736).
                List<VariableSave> originalVariables = state.Variables.ToList();
                List<(object? Value, bool SetsValue)> originalValues =
                    originalVariables.Select(v => (v.Value, v.SetsValue)).ToList();

                try
                {
                    element.ApplyVariableReferences(state, notifyChanges: false);
                    CollectForState(element, state, bitmapFonts, project, spacingHorizontal, spacingVertical);
                }
                finally
                {
                    state.Variables.Clear();
                    state.Variables.AddRange(originalVariables);
                    for (int i = 0; i < originalVariables.Count; i++)
                    {
                        originalVariables[i].Value = originalValues[i].Value;
                        originalVariables[i].SetsValue = originalValues[i].SetsValue;
                    }
                }
            }
        }

        return bitmapFonts;
    }

    private void CollectForState(ElementSave element, StateSave state, Dictionary<string, BmfcSave> bitmapFonts,
        GumProjectSave project, int spacingHorizontal, int spacingVertical)
    {
        // A non-default state that sets nothing for a given owner resolves that owner's
        // font properties identically to the default state, which is always collected -
        // so it can only produce an already-deduplicated entry. Skipping those pairs is
        // what keeps collection from costing (states x instances) recursive lookups on a
        // project whose category states mostly touch one instance each.
        bool isDefaultState = state == element.DefaultState;

        if (isDefaultState || StateSetsAnythingFor(state, ownerName: null))
        {
            foreach (BmfcSave bmfcSave in CollectAllBmfcSavesFor(instance: null, element, state, project, spacingHorizontal, spacingVertical))
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

            foreach (BmfcSave bmfcSaveInner in CollectAllBmfcSavesFor(instance, _resolveInstanceElement(instance), state, project, spacingHorizontal, spacingVertical))
            {
                bitmapFonts[bmfcSaveInner.FontCacheFileName] = bmfcSaveInner;
            }

            // Direct read on the instance only finds font properties set on this element
            // (e.g., "MyComponentInstance.Font"). For component instances, font properties
            // live on inner Text instances and may be partially exposed. Use
            // RecursiveVariableFinder to resolve through the component hierarchy.
            CollectFontsFromNestedTextInstancesAllBranches(element, state, instance,
                bitmapFonts, project, spacingHorizontal, spacingVertical);
        }
    }

    /// <summary>
    /// Runs <see cref="CollectFontsFromNestedTextInstances(ElementSave, StateSave, InstanceSave, Dictionary{string, BmfcSave}, GumProjectSave, int, int)"/>
    /// once per combination of branches of the conditional (ternary) <c>VariableReferences</c> rows
    /// on <paramref name="instance"/> that reach a font-affecting variable inside the component, by
    /// temporarily assigning each branch value on <paramref name="state"/>. Runs it once, as-is, when
    /// there are no such rows. See #6018.
    /// </summary>
    private void CollectFontsFromNestedTextInstancesAllBranches(ElementSave element, StateSave state,
        InstanceSave instance, Dictionary<string, BmfcSave> bitmapFonts,
        GumProjectSave project, int spacingHorizontal, int spacingVertical)
    {
        Dictionary<string, List<object>> branchValuesByProperty = GetNestedFontBranches(instance, state);

        if (branchValuesByProperty.Count == 0)
        {
            CollectFontsFromNestedTextInstances(element, state, instance,
                bitmapFonts, project, spacingHorizontal, spacingVertical);
            return;
        }

        foreach (Dictionary<string, object> combo in CrossProduct(branchValuesByProperty))
        {
            List<(VariableSave Variable, bool WasAdded, object? OriginalValue, bool OriginalSetsValue)> applied = new();

            try
            {
                foreach (KeyValuePair<string, object> assignment in combo)
                {
                    string fullName = instance.Name + "." + assignment.Key;
                    VariableSave? existing = state.Variables.FirstOrDefault(v => v.Name == fullName);

                    if (existing != null)
                    {
                        applied.Add((existing, false, existing.Value, existing.SetsValue));
                    }
                    else
                    {
                        existing = new VariableSave { Name = fullName };
                        state.Variables.Add(existing);
                        applied.Add((existing, true, null, false));
                    }

                    existing.Value = assignment.Value;
                    existing.SetsValue = true;
                }

                CollectFontsFromNestedTextInstances(element, state, instance,
                    bitmapFonts, project, spacingHorizontal, spacingVertical);
            }
            finally
            {
                foreach ((VariableSave variable, bool wasAdded, object? originalValue, bool originalSetsValue) in applied)
                {
                    if (wasAdded)
                    {
                        state.Variables.Remove(variable);
                    }
                    else
                    {
                        variable.Value = originalValue;
                        variable.SetsValue = originalSetsValue;
                    }
                }
            }
        }
    }

    /// <summary>
    /// The values each variable assigned by a conditional <c>VariableReferences</c> row on
    /// <paramref name="instance"/> can take, for rows whose target is (or is exposed from) a
    /// <see cref="FontAffectingVariableNames"/> entry on a Text inside the instance's component.
    /// Keyed by the variable name as seen on the instance (for example an exposed alias).
    /// </summary>
    private Dictionary<string, List<object>> GetNestedFontBranches(InstanceSave instance, StateSave state)
    {
        Dictionary<string, List<object>> branchValuesByProperty = new();

        VariableListSave? variableList = state.VariableLists
            .FirstOrDefault(vl => vl.GetRootName() == "VariableReferences"
                && vl.ValueAsIList.Count > 0
                && string.Equals(vl.SourceObject, instance.Name, StringComparison.Ordinal));

        ElementSave? componentElement = variableList == null ? null : _resolveInstanceElement(instance);
        if (variableList == null || componentElement == null)
        {
            return branchValuesByProperty;
        }

        foreach (string referenceString in variableList.ValueAsIList.Cast<string>())
        {
            var parsed = ElementSaveExtensions.GetAllVariableReferenceBranches(instance, referenceString, state);
            if (parsed == null)
            {
                continue;
            }

            (string variableName, IEnumerable<object> values) = parsed.Value;
            string unqualified = variableName.Contains('.')
                ? variableName.Substring(variableName.LastIndexOf('.') + 1)
                : variableName;

            if (!ReachesFontAffectingVariable(componentElement, unqualified))
            {
                continue;
            }

            List<object> distinctValues = values.Where(v => v != null).Distinct().ToList();
            if (distinctValues.Count > 1)
            {
                branchValuesByProperty[unqualified] = distinctValues;
            }
        }

        return branchValuesByProperty;
    }

    /// <summary>
    /// Whether <paramref name="variableName"/>, as seen on an instance of <paramref name="component"/>,
    /// is a <see cref="FontAffectingVariableNames"/> entry or an exposed variable that forwards to one,
    /// possibly through further nested components.
    /// </summary>
    private bool ReachesFontAffectingVariable(ElementSave component, string variableName, int depth = 0)
    {
        if (FontAffectingVariableNames.Contains(variableName))
        {
            return true;
        }

        // Exposure cycles are not valid, but a malformed project shouldn't hang collection.
        if (depth > 16)
        {
            return false;
        }

        foreach (VariableSave variable in component.DefaultState!.Variables)
        {
            if (variable.ExposedAsName != variableName)
            {
                continue;
            }

            string rootName = variable.GetRootName();
            if (FontAffectingVariableNames.Contains(rootName))
            {
                return true;
            }

            InstanceSave? innerInstance = component.GetInstance(variable.SourceObject);
            ElementSave? innerElement = innerInstance == null ? null : _resolveInstanceElement(innerInstance);
            if (innerElement != null && ReachesFontAffectingVariable(innerElement, rootName, depth + 1))
            {
                return true;
            }
        }

        return false;
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

    private static IEnumerable<BmfcSave> GetBmfcSavesFor(InstanceSave? instance, ElementSave? textElement,
        StateSave stateSave, GumProjectSave project, int spacingHorizontal, int spacingVertical)
    {
        string prefix = instance != null ? instance.Name + "." : "";
        Func<string, object?> getValue = name => stateSave.GetValueRecursive(prefix + name);
        return WithBbCodeRunFonts(BuildBmfcSave(getValue, project, spacingHorizontal, spacingVertical),
            getValue, textElement, project, spacingHorizontal, spacingVertical);
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
    private static IEnumerable<BmfcSave> CollectAllBmfcSavesFor(InstanceSave? instance, ElementSave? textElement,
        StateSave stateSave, GumProjectSave project, int spacingHorizontal, int spacingVertical)
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
            foreach (BmfcSave baseline in GetBmfcSavesFor(instance, textElement, stateSave, project, spacingHorizontal, spacingVertical))
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

            foreach (BmfcSave bmfcSave in WithBbCodeRunFonts(
                BuildBmfcSave(Lookup, project, spacingHorizontal, spacingVertical),
                Lookup, textElement, project, spacingHorizontal, spacingVertical))
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
        GumProjectSave project, int spacingHorizontal, int spacingVertical)
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

        return CreateBmfcSave(getValue, fontValue, fontSize.Value, outlineValue, fontSmoothing, isItalic, isBold,
            hasDropshadow, project, spacingHorizontal, spacingVertical);
    }

    /// <summary>
    /// Builds a <see cref="BmfcSave"/> from already-resolved font values. The dropshadow offset, blur and
    /// color come from <paramref name="getValue"/> (the Text's own variables) because no BBCode tag varies
    /// them mid-text, only the on/off flag. Shared by the Text's own font and each of its BBCode runs.
    /// </summary>
    private static BmfcSave CreateBmfcSave(Func<string, object?> getValue, string fontValue, float fontSize,
        int outlineThickness, bool useSmoothing, bool isItalic, bool isBold, bool hasDropshadow,
        GumProjectSave project, int spacingHorizontal, int spacingVertical)
    {
        BmfcSave bmfcSave = new BmfcSave();
        bmfcSave.FontSize = fontSize;
        bmfcSave.OutlineThickness = outlineThickness;
        bmfcSave.UseSmoothing = useSmoothing;
        bmfcSave.IsItalic = isItalic;
        bmfcSave.IsBold = isBold;
        bmfcSave.SpacingHorizontal = spacingHorizontal;
        bmfcSave.SpacingVertical = spacingVertical;
        bmfcSave.HasDropshadow = hasDropshadow;
        bmfcSave.IncludePathHashInCacheName = project.UsesPathHashedFontCacheNames;

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

        bmfcSave.Ranges = project.GetFontRanges(fontValue, out bool rangesAreFontSpecific) ?? BmfcSave.GetEffectiveDefaultRanges();
        bmfcSave.RangesAreFontSpecific = rangesAreFontSpecific;

        return bmfcSave;
    }

    /// <summary>
    /// <paramref name="baseSave"/> followed by every font the BBCode tags in the Text's value resolve to
    /// (#7201). The renderers create a run's font when they draw the text, so a project nobody has
    /// displayed yet has none of them. Replays <see cref="BbCodeFontStacks"/>, the same stack model the
    /// renderers use, seeded from the Text's own font. Nothing for a null <paramref name="baseSave"/>.
    /// </summary>
    private static IEnumerable<BmfcSave> WithBbCodeRunFonts(BmfcSave? baseSave, Func<string, object?> getValue,
        ElementSave? textElement, GumProjectSave project, int spacingHorizontal, int spacingVertical)
    {
        if (baseSave == null)
        {
            yield break;
        }

        yield return baseSave;

        if (getValue("Text") is not string text || !text.Contains('['))
        {
            yield break;
        }

        // Same normalization the renderer applies before parsing, so tag boundaries agree.
        string normalized = BbCodeParser.ReplaceGlyphTags(text.Replace("\r\n", "\n"));
        List<FoundTag> tags = BbCodeParser.Parse(normalized, BbCodeParser.KnownTags);
        if (tags.Count == 0)
        {
            yield break;
        }

        // A [state=Name] tag is expanded into the state's font variables, resolved on the Text's own
        // element like the runtime does (#7204).
        tags = BbCodeFontStacks.ExpandStateTags(tags, stateName => FindStateByName(textElement, stateName));

        BbCodeFontStacks stacks = new BbCodeFontStacks();
        stacks.Seed(baseSave.FontFile ?? baseSave.FontName, baseSave.FontSize, baseSave.OutlineThickness,
            baseSave.UseSmoothing, baseSave.IsItalic, baseSave.IsBold, useCustomFont: false, baseSave.HasDropshadow);

        List<TagInfo> allTags = new List<TagInfo>();
        allTags.AddRange(tags.Select(item => item.Open));
        allTags.AddRange(tags.Select(item => item.Close));
        allTags.Sort((a, b) => a.StartIndex - b.StartIndex);

        foreach (TagInfo tag in allTags)
        {
            if (!stacks.Apply(tag.Name, tag.Argument, out _))
            {
                continue;
            }

            string? fontName = stacks.FontName.Peek();
            if (string.IsNullOrEmpty(fontName))
            {
                continue;
            }

            // While a state has UseCustomFont on, the renderer loads a pre-baked "<name>.fnt" for the run
            // instead of generating one.
            if (stacks.UseCustomFont.Peek())
            {
                continue;
            }

            yield return CreateBmfcSave(getValue, fontName, stacks.FontSize.Peek(), stacks.OutlineThickness.Peek(),
                stacks.UseFontSmoothing.Peek(), stacks.IsItalic.Peek(), stacks.IsBold.Peek(),
                stacks.HasDropshadow.Peek(), project, spacingHorizontal, spacingVertical);
        }
    }

    /// <summary>
    /// Finds a state by its bare name the way the runtime does for a <c>[state=Name]</c> tag
    /// (<c>TryGetStateByName</c> on the Text's <c>GraphicalUiElement</c>): the uncategorized states first,
    /// then each category's states, across <paramref name="textElement"/> and its base types, where a
    /// derived element's state or category replaces the base's of the same name.
    /// </summary>
    private static StateSave? FindStateByName(ElementSave? textElement, string stateName)
    {
        Dictionary<string, StateSave> states = new Dictionary<string, StateSave>();
        Dictionary<string, StateSaveCategory> categories = new Dictionary<string, StateSaveCategory>();

        // Base first so the derived element overrides it. A base-type cycle is invalid, but a malformed
        // project shouldn't hang collection.
        List<ElementSave> chain = new List<ElementSave>();
        HashSet<ElementSave> visited = new HashSet<ElementSave>();
        for (ElementSave? current = textElement; current != null && visited.Add(current);
            current = ObjectFinder.Self.GetBaseElement(current))
        {
            chain.Insert(0, current);
        }

        foreach (ElementSave element in chain)
        {
            foreach (StateSaveCategory category in element.Categories)
            {
                categories[category.Name] = category;
            }

            foreach (StateSave state in element.States)
            {
                states[state.Name] = state;
            }
        }

        if (states.TryGetValue(stateName, out StateSave? found))
        {
            return found;
        }

        foreach (StateSaveCategory category in categories.Values)
        {
            found = category.States.FirstOrDefault(item => item.Name == stateName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void CollectFontsFromNestedTextInstances(ElementSave outerElement, StateSave outerState,
        InstanceSave componentInstance, Dictionary<string, BmfcSave> bitmapFonts,
        GumProjectSave project, int spacingHorizontal, int spacingVertical)
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

                foreach (BmfcSave bmfcSave in GetBmfcSavesFromStack(elementStack, project, spacingHorizontal, spacingVertical))
                {
                    bitmapFonts[bmfcSave.FontCacheFileName] = bmfcSave;
                }
            }
            else
            {
                CollectFontsFromNestedTextInstances(outerElement, outerState, componentInstance,
                    componentElement, innerInstance, bitmapFonts, project, spacingHorizontal, spacingVertical,
                    new HashSet<ElementSave> { outerElement, componentElement });
            }
        }
    }

    private void CollectFontsFromNestedTextInstances(ElementSave outerElement, StateSave outerState,
        InstanceSave outerInstance, ElementSave parentComponent, InstanceSave innerInstance,
        Dictionary<string, BmfcSave> bitmapFonts,
        GumProjectSave project, int spacingHorizontal, int spacingVertical,
        HashSet<ElementSave> elementsBeingWalked)
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

            foreach (BmfcSave bmfcSave in GetBmfcSavesFromStack(elementStack, project, spacingHorizontal, spacingVertical))
            {
                bitmapFonts[bmfcSave.FontCacheFileName] = bmfcSave;
            }
        }
        else if (elementsBeingWalked.Add(innerElement))
        {
            // An element that instantiates itself, directly or through other components, is an error
            // (GUM0018); skipping an element already on the walk's path ends the recursion (#7125).
            try
            {
                foreach (InstanceSave deeperInstance in innerElement.Instances)
                {
                    CollectFontsFromNestedTextInstances(outerElement, outerState, outerInstance,
                        innerElement, deeperInstance, bitmapFonts, project, spacingHorizontal, spacingVertical,
                        elementsBeingWalked);
                }
            }
            finally
            {
                elementsBeingWalked.Remove(innerElement);
            }
        }
    }

    private static IEnumerable<BmfcSave> GetBmfcSavesFromStack(List<ElementWithState> elementStack,
        GumProjectSave project, int spacingHorizontal, int spacingVertical)
    {
        RecursiveVariableFinder rfv = new RecursiveVariableFinder(elementStack);
        Func<string, object?> getValue = name => rfv.GetValueByBottomName(name);

        return WithBbCodeRunFonts(BuildBmfcSave(getValue, project, spacingHorizontal, spacingVertical),
            getValue, elementStack[elementStack.Count - 1].Element, project, spacingHorizontal, spacingVertical);
    }
}
