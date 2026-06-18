using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.Wireframe;

/// <summary>
/// Builds Gum save data (states/variables) from a live <see cref="GraphicalUiElement"/> tree, for the
/// runtime inspector snapshot feature (issue #3070). Which variables exist for a given node is decided
/// by the standard-element catalog (what Gum understands), and each value is read from the live element
/// via <see cref="GraphicalUiElementPropertyReadExtensions.TryGetProperty"/>.
/// </summary>
public interface IRuntimeSnapshotSerializer
{
    /// <summary>
    /// Resolves the Gum standard-element type name (e.g. "Container", "Text") for a live element by
    /// walking its runtime type hierarchy, or null if no standard type applies.
    /// </summary>
    string? GetStandardTypeName(GraphicalUiElement element);

    /// <summary>
    /// Creates a state holding the element's current values for every variable in its standard type's
    /// catalog. When <paramref name="shake"/> is false the result is unshaken — every catalog value is
    /// emitted (heavy but always correct). When true, values equal to the standard-element default are
    /// pruned: an omitted variable falls back to that same default in the snapshot's embedded standards,
    /// so the shaken state is equivalent but lighter and reads as "unedited" in the tool.
    /// </summary>
    StateSave CreateStateForNode(GraphicalUiElement element, string stateName, bool shake = false);

    /// <summary>
    /// Builds a flattened <see cref="ScreenSave"/> snapshot of the live tree rooted at
    /// <paramref name="root"/>. Each descendant becomes a standard-element <see cref="InstanceSave"/>; the
    /// screen's default state holds each instance's values as instance-qualified variables, and
    /// child/parent structure is captured via the qualified "Parent" variable. The root itself maps to the
    /// screen, not to an instance. When <paramref name="shake"/> is true, equal-to-default values are pruned
    /// (see <see cref="CreateStateForNode"/>).
    /// </summary>
    ScreenSave CreateScreenSave(GraphicalUiElement root, string screenName, bool shake = false);

    /// <summary>
    /// The components synthesized during the most recent <see cref="CreateScreenSave"/> call — one per
    /// distinct Forms-control type that was collapsed from a flattened subtree into a reusable component
    /// (e.g. a "Button" component shared by every button instance). Empty when no baseline provider was
    /// supplied or no Forms-control subtree qualified. A caller assembling the snapshot project must add
    /// these to <c>GumProjectSave.Components</c> (and their references).
    /// </summary>
    IReadOnlyList<ComponentSave> SynthesizedComponents { get; }

    /// <summary>
    /// Returns the distinct, non-empty file paths referenced by "SourceFile" variables in the snapshot
    /// (e.g. Sprite/NineSlice textures), so a caller can bundle those files alongside the project. Includes
    /// files referenced by <see cref="SynthesizedComponents"/>, not just the screen.
    /// </summary>
    IEnumerable<string> GetReferencedFiles(ScreenSave screen);
}

/// <inheritdoc cref="IRuntimeSnapshotSerializer" />
public class RuntimeSnapshotSerializer : IRuntimeSnapshotSerializer
{
    private const string RuntimeSuffix = "Runtime";

    // Synthesized Forms-control components are emitted as Container-based components, and their control root
    // is read against the Container catalog (its visual root may be an InteractiveGue subclass that does not
    // resolve to a standard type on its own).
    private const string ComponentBaseType = "Container";

    private readonly IReadOnlyDictionary<string, StateSave> _defaultStates;
    private readonly Func<Type, GraphicalUiElement?>? _formsBaselineProvider;

    // Synthesized components produced by CreateScreenSave (one per Forms-control type), a per-type cache so
    // repeated instances of a control share one component, and the set of component names already taken (so
    // two control types with the same simple name -- e.g. a Button in two different namespaces -- do not
    // collide on one element name). All reset at the start of every CreateScreenSave so a reused serializer
    // does not leak state between snapshots.
    private readonly List<ComponentSave> _synthesizedComponents;
    private readonly Dictionary<Type, ComponentEntry?> _componentCache;
    private readonly HashSet<string> _usedComponentNames;

    /// <summary>
    /// Creates a serializer that reads runtime values against the supplied standard-element catalog.
    /// </summary>
    /// <param name="defaultStates">
    /// The standard-element default states keyed by type name — the authority on which variables Gum
    /// understands per type. Typically <c>StandardElementsManager.Self.DefaultStates</c>.
    /// </param>
    /// <param name="formsBaselineProvider">
    /// Optional. Given a Forms-control type (e.g. <c>typeof(Button)</c>), returns a pristine baseline visual
    /// for that control — typically <c>FrameworkElement.GetGraphicalUiElementForFrameworkElement</c>, which
    /// instantiates the registered <c>DefaultFormsTemplates</c> entry. When supplied, Forms-control subtrees
    /// are collapsed into synthesized components diffed against this baseline; when null, the tree is
    /// flattened exactly as before (every node becomes a standard-element instance).
    /// </param>
    public RuntimeSnapshotSerializer(IReadOnlyDictionary<string, StateSave> defaultStates,
        Func<Type, GraphicalUiElement?>? formsBaselineProvider = null)
    {
        _defaultStates = defaultStates;
        _formsBaselineProvider = formsBaselineProvider;
        _synthesizedComponents = new List<ComponentSave>();
        _componentCache = new Dictionary<Type, ComponentEntry?>();
        _usedComponentNames = new HashSet<string>();
    }

    /// <inheritdoc />
    public IReadOnlyList<ComponentSave> SynthesizedComponents => _synthesizedComponents;

    /// <inheritdoc />
    public string? GetStandardTypeName(GraphicalUiElement element)
    {
        // Standard runtimes follow the "<StandardName>Runtime" convention (ContainerRuntime ->
        // "Container"). Walking the base chain lets custom subclasses resolve to their nearest
        // standard ancestor; matching against the catalog keeps the result grounded in what Gum
        // actually understands rather than trusting the type name blindly.
        Type? type = element.GetType();
        while (type != null)
        {
            string candidate = StripRuntimeSuffix(type.Name);
            if (_defaultStates.ContainsKey(candidate))
            {
                return candidate;
            }
            type = type.BaseType;
        }
        return null;
    }

    /// <inheritdoc />
    public StateSave CreateStateForNode(GraphicalUiElement element, string stateName, bool shake = false)
    {
        return CreateStateForType(element, stateName, shake, GetStandardTypeName(element));
    }

    // Reads an element's values against an explicitly chosen standard type's catalog. CreateStateForNode
    // uses the element's own resolved type; the component root is read against the component base type
    // (Container) so a control root that does not itself resolve to a standard (e.g. an InteractiveGue
    // subclass like DefaultButtonRuntime) still contributes its geometry/visibility.
    private StateSave CreateStateForType(GraphicalUiElement element, string stateName, bool shake, string? typeName)
    {
        StateSave state = new StateSave { Name = stateName };

        if (typeName != null && _defaultStates.TryGetValue(typeName, out StateSave? defaultState))
        {
            foreach (VariableSave defaultVariable in defaultState.Variables)
            {
                // The catalog defines the name/type/category; the live element supplies the value.
                // Variables the element cannot read (no base-set case and no reflectable property)
                // are skipped rather than emitted with a wrong value.
                if (element.TryGetProperty(defaultVariable.Name, out object? value))
                {
                    // Only values Gum can serialize belong in a snapshot. The reflection fallback can
                    // return runtime-only objects (e.g. a Texture2D for "SourceFile") that the XML
                    // serializer cannot write; skip those rather than crash the whole save.
                    if (value != null && !IsSaveSafeValue(value))
                    {
                        continue;
                    }

                    // The shake prunes values equal to the standard-element default (the in-document
                    // baseline): an omitted variable resolves to that same default via the snapshot's
                    // embedded standards, so the result is equivalent but lighter.
                    if (shake && AreValuesEqual(value, defaultVariable.Value))
                    {
                        continue;
                    }

                    state.Variables.Add(new VariableSave
                    {
                        Name = defaultVariable.Name,
                        Type = defaultVariable.Type,
                        Value = value,
                        SetsValue = true,
                        Category = defaultVariable.Category,
                    });
                }
            }
        }

        return state;
    }

    /// <inheritdoc />
    public ScreenSave CreateScreenSave(GraphicalUiElement root, string screenName, bool shake = false)
    {
        _synthesizedComponents.Clear();
        _componentCache.Clear();
        _usedComponentNames.Clear();

        ScreenSave screen = new ScreenSave { Name = screenName };
        StateSave defaultState = new StateSave { Name = "Default" };
        screen.States.Add(defaultState);

        GraphicalUiElement screenRoot = ResolveScreenRoot(root);

        bool allowComponentization = _formsBaselineProvider != null;
        HashSet<string> usedNames = new HashSet<string>();
        foreach (GraphicalUiElement child in screenRoot.Children)
        {
            AddInstanceRecursive(child, parentInstanceName: null, screen, defaultState, usedNames, shake,
                allowComponentization);
        }

        return screen;
    }

    // Decides which element's children become the screen's top-level instances. When the supplied root
    // holds exactly one *authored* element (a custom type, not a standard layout element) that has
    // children, that element is the screen the user built -- the code-only equivalent of a Gum Screen --
    // so it is elided and its children are promoted. In every other case (multiple children at the root,
    // a single standard-type wrapper, or a custom leaf) the root's own children are the top level.
    private GraphicalUiElement ResolveScreenRoot(GraphicalUiElement root)
    {
        if (root.Children.Count == 1
            && root.Children[0] is GraphicalUiElement onlyChild
            && onlyChild.Children.Count > 0
            && IsAuthoredScreenRoot(onlyChild))
        {
            return onlyChild;
        }
        return root;
    }

    // A single root child is treated as the authored screen when either it is a custom type, or it
    // carries a Forms control identity. The latter matters because a Forms screen's visual is a plain
    // ContainerRuntime (a standard type) with FormsControlAsObject set -- its "screen-ness" lives in the
    // Forms control, not the runtime type. A plain anonymous container is neither, so it stays an instance.
    private bool IsAuthoredScreenRoot(GraphicalUiElement element) =>
        IsCustomType(element)
        || (element is InteractiveGue interactiveGue && interactiveGue.FormsControlAsObject != null);

    // A custom (game-authored) type's own name is not a standard-element name. Standard runtimes follow
    // the "<StandardName>Runtime" convention and resolve directly into the catalog; a subclass such as
    // "MainMenu : ContainerRuntime" does not. (This deliberately checks the concrete type only -- unlike
    // GetStandardTypeName, which walks the base chain to find the nearest standard ancestor.)
    private bool IsCustomType(GraphicalUiElement element)
    {
        string candidate = StripRuntimeSuffix(element.GetType().Name);
        return !_defaultStates.ContainsKey(candidate);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetReferencedFiles(ScreenSave screen)
    {
        HashSet<string> files = new HashSet<string>();
        AddReferencedFiles(screen, files);
        foreach (ComponentSave component in _synthesizedComponents)
        {
            AddReferencedFiles(component, files);
        }
        return files;
    }

    private static void AddReferencedFiles(ElementSave element, HashSet<string> files)
    {
        foreach (StateSave state in element.States)
        {
            foreach (VariableSave variable in state.Variables)
            {
                if (IsSourceFileVariable(variable.Name)
                    && variable.Value is string path
                    && !string.IsNullOrEmpty(path))
                {
                    files.Add(path);
                }
            }
        }
    }

    // "SourceFile" appears instance-qualified in the screen's default state (e.g. "Sprite.SourceFile").
    private static bool IsSourceFileVariable(string variableName)
    {
        int lastDot = variableName.LastIndexOf('.');
        string unqualified = lastDot >= 0 ? variableName.Substring(lastDot + 1) : variableName;
        return unqualified == "SourceFile" || unqualified == "Source File";
    }

    private void AddInstanceRecursive(GraphicalUiElement element, string? parentInstanceName,
        ElementSave targetElement, StateSave defaultState, HashSet<string> usedNames, bool shake,
        bool allowComponentization, Dictionary<GraphicalUiElement, string>? nodeToInstanceName = null)
    {
        // A Forms-control subtree can collapse into a single synthesized-component instance instead of a
        // flattened soup of standards -- but only when doing so reproduces the live visuals exactly (see
        // TryComponentize). Disabled while building a component's own internals so a control's baseline is
        // not itself re-componentized.
        if (allowComponentization
            && element is InteractiveGue formsElement
            && formsElement.FormsControlAsObject != null
            && TryComponentize(formsElement, parentInstanceName, targetElement, defaultState, usedNames, shake))
        {
            return;
        }

        string typeName = GetStandardTypeName(element) ?? "Container";
        string instanceName = GenerateUniqueName(element, typeName, usedNames);
        if (nodeToInstanceName != null)
        {
            nodeToInstanceName[element] = instanceName;
        }

        targetElement.Instances.Add(new InstanceSave { Name = instanceName, BaseType = typeName });

        // The node's catalog values become instance-qualified variables in the element's default state.
        // Read against the resolved BaseType (with the same "Container" fallback used above), not the node's
        // own GetStandardTypeName -- an InteractiveGue-rooted Forms visual (StackPanel/ScrollViewer/Panel)
        // resolves to null there, which would emit no geometry and leave the instance at the default size.
        StateSave nodeState = CreateStateForType(element, "Default", shake, typeName);
        foreach (VariableSave variable in nodeState.Variables)
        {
            defaultState.Variables.Add(new VariableSave
            {
                Name = instanceName + "." + variable.Name,
                Type = variable.Type,
                Value = variable.Value,
                SetsValue = true,
                Category = variable.Category,
            });
        }

        // Non-top-level instances record their parent; top-level instances are children of the element.
        if (parentInstanceName != null)
        {
            defaultState.Variables.Add(new VariableSave
            {
                Name = instanceName + ".Parent",
                Type = "string",
                Value = parentInstanceName,
                SetsValue = true,
            });
        }

        foreach (GraphicalUiElement child in element.Children)
        {
            AddInstanceRecursive(child, instanceName, targetElement, defaultState, usedNames, shake,
                allowComponentization, nodeToInstanceName);
        }
    }

    // Attempts to emit a Forms-control subtree as a thin instance of a synthesized component plus its
    // per-instance overrides. Returns false (so the caller flattens instead) when there is no baseline for
    // the control type, or when the live subtree does not structurally match the baseline -- in which case
    // componentizing would lose visuals, and the always-correct flattened form is used.
    private bool TryComponentize(InteractiveGue element, string? parentInstanceName,
        ElementSave targetElement, StateSave defaultState, HashSet<string> usedNames, bool shake)
    {
        Type controlType = element.FormsControlAsObject!.GetType();

        ComponentEntry? entry = GetOrBuildComponentEntry(controlType, shake);
        if (entry == null)
        {
            return false;
        }

        // Fidelity gate: only componentize when the live subtree aligns 1:1 with the pristine baseline.
        if (!StructuralMatches(element, entry.PristineRoot))
        {
            return false;
        }

        // The component is only added to the output once it is actually used by an instance, so a baseline
        // that every instance ends up rejecting never produces an orphan component.
        if (!entry.Emitted)
        {
            _synthesizedComponents.Add(entry.Component);
            entry.Emitted = true;
        }

        // BaseType uses the component's (possibly de-collided) name, not the raw type name, so the instance
        // resolves to the component even when two control types share a simple name.
        string instanceName = GenerateUniqueName(element, controlType.Name, usedNames);
        targetElement.Instances.Add(new InstanceSave { Name = instanceName, BaseType = entry.Component.Name });

        EmitOverridesRecursive(element, entry.PristineRoot, entry, instanceName, defaultState);

        if (parentInstanceName != null)
        {
            defaultState.Variables.Add(new VariableSave
            {
                Name = instanceName + ".Parent",
                Type = "string",
                Value = parentInstanceName,
                SetsValue = true,
            });
        }

        return true;
    }

    // Builds (and caches) the component for a control type from its pristine baseline, or caches a miss
    // (null) when no baseline is available. The build reads the baseline only -- never a live instance --
    // so the component stays neutral and every instance diffs against the same canonical values.
    private ComponentEntry? GetOrBuildComponentEntry(Type controlType, bool shake)
    {
        if (_componentCache.TryGetValue(controlType, out ComponentEntry? cached))
        {
            return cached;
        }

        GraphicalUiElement? pristine = _formsBaselineProvider?.Invoke(controlType);
        ComponentEntry? entry = pristine == null ? null : BuildComponentEntry(controlType, pristine, shake);
        _componentCache[controlType] = entry;
        return entry;
    }

    private ComponentEntry BuildComponentEntry(Type controlType, GraphicalUiElement pristine, bool shake)
    {
        ComponentSave component = new ComponentSave
        {
            Name = MakeUniqueComponentName(controlType.Name),
            BaseType = ComponentBaseType,
        };
        StateSave componentDefault = new StateSave { Name = "Default" };
        component.States.Add(componentDefault);

        Dictionary<GraphicalUiElement, string> nodeToInstanceName = new Dictionary<GraphicalUiElement, string>();

        // The baseline root maps to the component element itself: its catalog values become element-level
        // (unqualified) default variables, exactly how a hand-authored component holds its own values. Read
        // against the component base type so an InteractiveGue-rooted control still emits its root geometry.
        StateSave rootState = CreateStateForType(pristine, "Default", shake, ComponentBaseType);
        foreach (VariableSave variable in rootState.Variables)
        {
            componentDefault.Variables.Add(new VariableSave
            {
                Name = variable.Name,
                Type = variable.Type,
                Value = variable.Value,
                SetsValue = true,
                Category = variable.Category,
            });
        }

        // Seed Component-base variables the live visual cannot supply -- notably the "State" selector, which
        // is save-time metadata with no runtime property to read. An authored component carries these, so
        // without them the tool back-fills them on load and force-saves the whole project. Sourcing from the
        // catalog keeps a single source of truth and auto-covers any future Component-base meta-variable.
        if (_defaultStates.TryGetValue("Component", out StateSave? componentBaseState))
        {
            foreach (VariableSave baseVariable in componentBaseState.Variables)
            {
                if (FindVariable(componentDefault, baseVariable.Name) == null)
                {
                    componentDefault.Variables.Add(new VariableSave
                    {
                        Name = baseVariable.Name,
                        Type = baseVariable.Type,
                        Value = baseVariable.Value,
                        SetsValue = baseVariable.SetsValue,
                        Category = baseVariable.Category,
                    });
                }
            }
        }

        // The baseline's descendants become the component's (flat, parent-linked) instances.
        HashSet<string> usedNames = new HashSet<string>();
        foreach (GraphicalUiElement child in pristine.Children)
        {
            AddInstanceRecursive(child, parentInstanceName: null, component, componentDefault, usedNames,
                shake, allowComponentization: false, nodeToInstanceName);
        }

        return new ComponentEntry(component, pristine, componentDefault, nodeToInstanceName);
    }

    // Walks the live and pristine subtrees in lockstep (StructuralMatches guarantees they align), emitting
    // each value that differs from the baseline as an instance override. Root deltas map straight to the
    // component element and become direct instance variables; deltas on inner nodes require a synthesized
    // exposed variable on the component, since Gum resolves an instance variable only one level deep.
    private void EmitOverridesRecursive(GraphicalUiElement liveNode, GraphicalUiElement pristineNode,
        ComponentEntry entry, string instanceName, StateSave screenDefaultState)
    {
        entry.NodeToInstanceName.TryGetValue(pristineNode, out string? componentInstanceName);

        // The control root (no instance-name entry) is read against the component base type to match how it
        // was emitted in BuildComponentEntry; inner nodes use their own resolved standard type.
        string? typeName = componentInstanceName == null ? ComponentBaseType : GetStandardTypeName(liveNode);
        if (typeName != null && _defaultStates.TryGetValue(typeName, out StateSave? defaultStateForType))
        {
            foreach (VariableSave defaultVariable in defaultStateForType.Variables)
            {
                if (!liveNode.TryGetProperty(defaultVariable.Name, out object? liveValue))
                {
                    continue;
                }
                if (liveValue != null && !IsSaveSafeValue(liveValue))
                {
                    continue;
                }

                bool pristineHas = pristineNode.TryGetProperty(defaultVariable.Name, out object? pristineValue);
                if (pristineHas && AreValuesEqual(liveValue, pristineValue))
                {
                    // Equal to the baseline -> already represented by the component; nothing to override.
                    continue;
                }

                string overrideName;
                if (componentInstanceName == null)
                {
                    overrideName = instanceName + "." + defaultVariable.Name;
                }
                else
                {
                    string internalPath = componentInstanceName + "." + defaultVariable.Name;
                    string exposedName = EnsureExposed(entry, internalPath, defaultVariable.Type,
                        pristineHas, pristineValue);
                    overrideName = instanceName + "." + exposedName;
                }

                screenDefaultState.Variables.Add(new VariableSave
                {
                    Name = overrideName,
                    Type = defaultVariable.Type,
                    Value = liveValue,
                    SetsValue = true,
                    Category = defaultVariable.Category,
                });
            }
        }

        for (int i = 0; i < liveNode.Children.Count && i < pristineNode.Children.Count; i++)
        {
            if (liveNode.Children[i] is GraphicalUiElement liveChild
                && pristineNode.Children[i] is GraphicalUiElement pristineChild)
            {
                EmitOverridesRecursive(liveChild, pristineChild, entry, instanceName, screenDefaultState);
            }
        }
    }

    // Ensures the component exposes the inner variable at <paramref name="internalPath"/> (e.g.
    // "TextInstance.Text") and returns the exposed name an instance uses to override it. The exposure is
    // declared once per path and shared by every instance of the control type.
    private string EnsureExposed(ComponentEntry entry, string internalPath, string type,
        bool pristineHas, object? pristineValue)
    {
        if (entry.ExposedNamesByPath.TryGetValue(internalPath, out string? existing))
        {
            return existing;
        }

        string exposedName = GenerateExposedName(internalPath, entry.UsedExposedNames);

        // The exposure resolves through a component default-state variable for the inner path. It is usually
        // already present (emitted while building the component); if the shake pruned it (value equal to the
        // standard default), re-add it carrying the baseline value so the component still renders pristine.
        VariableSave? componentVariable = FindVariable(entry.DefaultState, internalPath);
        if (componentVariable == null)
        {
            componentVariable = new VariableSave
            {
                Name = internalPath,
                Type = type,
                Value = pristineHas ? pristineValue : null,
                SetsValue = pristineHas,
            };
            entry.DefaultState.Variables.Add(componentVariable);
        }
        componentVariable.ExposedAsName = exposedName;

        entry.ExposedNamesByPath[internalPath] = exposedName;
        return exposedName;
    }

    // Component names must be unique within the snapshot. Two distinct control types can share a simple type
    // name (e.g. a Button in two namespaces), so the second is de-collided with a numeric suffix.
    private string MakeUniqueComponentName(string typeName)
    {
        string candidate = typeName;
        int suffix = 1;
        while (!_usedComponentNames.Add(candidate))
        {
            candidate = typeName + suffix;
            suffix++;
        }
        return candidate;
    }

    // Exposed names must be a single token (an instance variable resolves only one level deep), so the
    // dotted inner path is flattened to underscores and de-duplicated within the component.
    private static string GenerateExposedName(string internalPath, HashSet<string> usedNames)
    {
        string baseName = internalPath.Replace('.', '_');
        string candidate = baseName;
        int suffix = 1;
        while (!usedNames.Add(candidate))
        {
            candidate = baseName + suffix;
            suffix++;
        }
        return candidate;
    }

    // The fidelity gate. The live and pristine subtrees match when every node shares the same standard type
    // and the same child count/order. A mismatch (most commonly runtime-added children a control's template
    // lacks, such as ListBox items) means componentizing would lose visuals, so the caller flattens instead.
    private bool StructuralMatches(GraphicalUiElement live, GraphicalUiElement pristine)
    {
        if (GetStandardTypeName(live) != GetStandardTypeName(pristine))
        {
            return false;
        }
        if (live.Children.Count != pristine.Children.Count)
        {
            return false;
        }
        for (int i = 0; i < live.Children.Count; i++)
        {
            if (live.Children[i] is GraphicalUiElement liveChild
                && pristine.Children[i] is GraphicalUiElement pristineChild)
            {
                if (!StructuralMatches(liveChild, pristineChild))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    private static VariableSave? FindVariable(StateSave state, string name)
    {
        foreach (VariableSave variable in state.Variables)
        {
            if (variable.Name == name)
            {
                return variable;
            }
        }
        return null;
    }

    // Equality for the shake. Same boxed type compares directly (covers bool/string/enum/int); numeric
    // cross-type (e.g. an int default vs a float read) compares by value so it is not treated as "edited".
    private static bool AreValuesEqual(object? a, object? b)
    {
        if (a == null)
        {
            return b == null;
        }
        if (b == null)
        {
            return false;
        }
        if (a.GetType() == b.GetType())
        {
            return a.Equals(b);
        }
        if (IsNumeric(a) && IsNumeric(b))
        {
            return Convert.ToDouble(a) == Convert.ToDouble(b);
        }
        return a.Equals(b);
    }

    private static bool IsNumeric(object value) =>
        value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    // The value kinds Gum's save format (and its XML serializer) can represent: strings, bools, the
    // numeric primitives, and enums. Anything else (a Texture2D, a renderable, ...) must not be emitted.
    private static bool IsSaveSafeValue(object value) =>
        value is string || value is bool || value.GetType().IsEnum || IsNumeric(value);

    private static string GenerateUniqueName(GraphicalUiElement element, string typeName, HashSet<string> usedNames)
    {
        string baseName = string.IsNullOrEmpty(element.Name) ? typeName : element.Name;
        string candidate = baseName;
        int suffix = 1;
        while (!usedNames.Add(candidate))
        {
            candidate = baseName + suffix;
            suffix++;
        }
        return candidate;
    }

    private static string StripRuntimeSuffix(string typeName)
    {
        if (typeName.Length > RuntimeSuffix.Length && typeName.EndsWith(RuntimeSuffix, StringComparison.Ordinal))
        {
            return typeName.Substring(0, typeName.Length - RuntimeSuffix.Length);
        }
        return typeName;
    }

    // The cached state for one synthesized component: the component itself, the pristine baseline tree it was
    // built from (kept so every instance diffs against the same canonical values), the map from each baseline
    // node to its instance name within the component (the root maps to no entry -> element-level), and the
    // bookkeeping for exposed variables synthesized on demand.
    private class ComponentEntry
    {
        public ComponentSave Component { get; }
        public GraphicalUiElement PristineRoot { get; }
        public StateSave DefaultState { get; }
        public Dictionary<GraphicalUiElement, string> NodeToInstanceName { get; }
        public Dictionary<string, string> ExposedNamesByPath { get; }
        public HashSet<string> UsedExposedNames { get; }
        public bool Emitted { get; set; }

        public ComponentEntry(ComponentSave component, GraphicalUiElement pristineRoot, StateSave defaultState,
            Dictionary<GraphicalUiElement, string> nodeToInstanceName)
        {
            Component = component;
            PristineRoot = pristineRoot;
            DefaultState = defaultState;
            NodeToInstanceName = nodeToInstanceName;
            ExposedNamesByPath = new Dictionary<string, string>();
            UsedExposedNames = new HashSet<string>();
        }
    }
}
