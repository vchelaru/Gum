using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.RenderingLibrary;
using GumDataTypes.Variables;

using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.ComponentModel;
using ToolsUtilitiesStandard.Helpers;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;
using GumRuntime;
using Gum.Collections;

#if FRB

using InteractiveGue = Gum.Wireframe.GraphicalUiElement;
#endif


#if !FRB
using Gum.StateAnimation.Runtime;
#endif

namespace Gum.Wireframe;

public partial class GraphicalUiElement
{

    private int GetOrderedIndexForParentVariable(VariableSave item)
    {
        var objectName = item.SourceObject;
        // An element created in code has no instances to order by.
        if (ElementSave == null)
        {
            return -1;
        }
        for (int i = 0; i < ElementSave.Instances.Count; i++)
        {
            if (objectName == ElementSave.Instances[i].Name)
            {
                return i;
            }
        }
        return -1;
    }


    public void AddCategory(DataTypes.Variables.StateSaveCategory category)
    {
#if FULL_DIAGNOSTICS
        if (string.IsNullOrEmpty(category.Name))
        {
            throw new ArgumentException("The category must have its Name set before being added to this");
        }
#endif
        //mCategories[category.Name] = category;
        // Why call "Add"? This makes Gum crash if there are duplicate catgories...
        //mCategories.Add(category.Name, category);
        mCategories[category.Name] = category;
    }

    public void AddStates(List<DataTypes.Variables.StateSave> list)
    {
        foreach (var state in list)
        {
#if FULL_DIAGNOSTICS
            if (state.Name == null)
            {
                throw new ArgumentException("One of the states being added has a null name - be sure to set the name of all states");
            }
#endif
            // Right now this doesn't support inheritance
            // Need to investigate this....at some point:
            mStates[state.Name] = state;
        }
    }

    // When interpolating between two states,
    // the code is goign to merge the values from
    // the two states to create a 3rd set of (merged)
    // values. Interpolation can happen in complex animations
    // resulting in lots of merged lists being created. This allocates
    // tons of memory. Therefore we create a static set of variable lists
    // to store the merged values. We don't know how deep the stack will go
    // (animations within animations) so we need to support a dynamically growing
    // list. The numberOfUsedInterpolationLists stores how many times this is being
    // called so it knows if it needs to add more lists.
    static List<List<Gum.DataTypes.Variables.VariableSaveValues>> listOfListsForReducingAllocInInterpolation = new List<List<Gum.DataTypes.Variables.VariableSaveValues>>();
    int numberOfUsedInterpolationLists = 0;

    public void InterpolateBetween(Gum.DataTypes.Variables.StateSave first, Gum.DataTypes.Variables.StateSave second, float interpolationValue)
    {
        if (numberOfUsedInterpolationLists >= listOfListsForReducingAllocInInterpolation.Count)
        {
            const int capacity = 20;
            var newList = new List<DataTypes.Variables.VariableSaveValues>(capacity);
            listOfListsForReducingAllocInInterpolation.Add(newList);
        }

        List<Gum.DataTypes.Variables.VariableSaveValues> values = listOfListsForReducingAllocInInterpolation[numberOfUsedInterpolationLists];
        values.Clear();
        numberOfUsedInterpolationLists++;

        Gum.DataTypes.Variables.StateSaveExtensionMethods.Merge(first, second, interpolationValue, values);

        this.ApplyState(values);
        numberOfUsedInterpolationLists--;
    }



    #region Set Values/States


    // This is made public so that specific implementations can fall back to it if needed:
    public static bool SetPropertyThroughReflection(IRenderableIpso mContainedObjectAsIpso, GraphicalUiElement graphicalUiElement, string propertyName, object? value) =>
        TrySetPropertyThroughReflection(mContainedObjectAsIpso.GetType(), mContainedObjectAsIpso, propertyName, value);

    // Shared by SetPropertyThroughReflection (targets the contained renderable) and
    // TrySetCustomVariableOnThis (targets this GUE/generated runtime class itself, issue #4891) so
    // both get the same enum/Nullable<T> coercion tolerance.
    //
    // targetObjectType is passed separately from target so the trimmer can see it carries public
    // properties: each caller gets it from GetType() on a statically typed value whose type is
    // annotated with DynamicallyAccessedMembers(PublicProperties) (IRenderableIpso, GraphicalUiElement),
    // which a GetType() on a plain object cannot express.
    private static bool TrySetPropertyThroughReflection(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type targetObjectType,
        object target, string propertyName, object? value)
    {
        System.Reflection.PropertyInfo? propertyInfo = targetObjectType.GetProperty(propertyName);

        if (propertyInfo == null || !propertyInfo.CanWrite)
        {
            return false;
        }

        if (value != null && value.GetType() != propertyInfo.PropertyType)
        {
            // This is the data-driven path (ApplyState → SetProperty → reflection); strongly-typed
            // C# setters never reach it. .gumx serializes enums as their underlying int (and
            // hand-written files sometimes by name), and many renderable properties are declared
            // Nullable<T> (e.g. Blend? on Sprite/NineSlice). Convert.ChangeType handles neither: it
            // throws on int/string → enum and cannot produce a Nullable<T>. So convert against the
            // underlying T — routing enums through Enum.ToObject / Enum.Parse — and let the boxed T
            // assign cleanly into a Nullable<T> property via SetValue.
            Type targetType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
            if (value.GetType() != targetType)
            {
                try
                {
                    if (targetType.IsEnum)
                    {
                        value = value is string enumName
                            ? Enum.Parse(targetType, enumName, ignoreCase: true)
                            : Enum.ToObject(targetType, value);
                    }
                    else
                    {
                        value = System.Convert.ChangeType(value, targetType);
                    }
                }
                catch
                {
                    // One bad variable (incompatible type, undefined enum name, out-of-range value)
                    // must not tear down the entire screen load — skip this assignment.
                    return false;
                }
            }
        }

        propertyInfo.SetValue(target, value, null);
        return true;
    }

    /// <summary>
    /// Last-resort fallback for a variable name that neither TrySetValueOnThis nor the contained
    /// renderable's dispatch (SetPropertyOnRenderable) claims. Covers a Component's custom ("new")
    /// variable (issue #4891): CodeGenerator.FillWithNewVariables emits it as a bare auto-property
    /// on the generated partial class with no wiring back into the Gum variable system, so without
    /// this, a .gumx-authored value for it never reaches the property under FindByName
    /// instantiation. Scoped to properties declared on a type more derived than GraphicalUiElement
    /// itself - GraphicalUiElement's own properties are already owned by TrySetValueOnThis, and
    /// this must never compete with it or with a renderable-dispatched name.
    /// </summary>
    private bool TrySetCustomVariableOnThis(string propertyName, object? value)
    {
        var propertyInfo = this.GetType().GetProperty(propertyName);
        if (propertyInfo == null)
        {
            if (TrySetCustomVariableOnFormsControl(propertyName, value))
            {
                return true;
            }
            // No property holds the value, so keep it where a reference reads it from. Without this a
            // keyframe's X = OffsetX (#7645) would never see an OffsetX set on a plain element, since no
            // row of the element's own default state reads it. Only a variable the element declares is kept,
            // so a misspelled name is not stored.
            if (ElementSave?.DefaultState?.GetVariableRecursive(propertyName) != null)
            {
                RecordReferenceInput(propertyName, value);
            }
            return false;
        }

        if (propertyInfo.DeclaringType == typeof(GraphicalUiElement))
        {
            return false;
        }

        return TrySetPropertyThroughReflection(this.GetType(), this, propertyName, value);
    }

    /// <summary>
    /// Second half of the custom-variable fallback, for MonoGameForms codegen (issue #4947): there
    /// the generated property lives on the Forms class rather than on the visual, so the reflection
    /// above cannot find it. The generated template applies the element's state before creating the
    /// Forms control, so a value that arrives first is held until one is assigned.
    /// </summary>
    private bool TrySetCustomVariableOnFormsControl(string propertyName, object? value)
    {
        InteractiveGue? interactiveGue = this as InteractiveGue;
        if (interactiveGue == null)
        {
            return false;
        }

        object? formsControl = interactiveGue.FormsControlAsObject;
        if (formsControl != null)
        {
            return TrySetPropertyThroughReflection(GetFormsControlType(formsControl), formsControl, propertyName, value);
        }

        if (_pendingCustomVariables == null)
        {
            _pendingCustomVariables = new Dictionary<string, object?>();
        }
        _pendingCustomVariables[propertyName] = value;
        return false;
    }

    // FormsControlAsObject is typed object because this file also compiles into FlatRedBall, which
    // has its own FrameworkElement. The value is always a FrameworkElement, whose
    // DynamicallyAccessedMembers(PublicProperties) type annotation keeps the public properties of
    // every subclass.
    [UnconditionalSuppressMessage("Trimming", "IL2073",
        Justification = "Forms controls derive from FrameworkElement, which is annotated with DynamicallyAccessedMembers(PublicProperties).")]
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    private static Type GetFormsControlType(object formsControl) => formsControl.GetType();

    /// <summary>
    /// Applies the custom variables that arrived before a Forms control existed, then discards
    /// them so a later Forms control does not inherit stale values. Called by
    /// <see cref="InteractiveGue.FormsControlAsObject"/>'s setter.
    /// </summary>
    private protected void ApplyPendingCustomVariables(object formsControl)
    {
        if (_pendingCustomVariables == null)
        {
            return;
        }

        Dictionary<string, object?> pending = _pendingCustomVariables;
        _pendingCustomVariables = null;

        foreach (KeyValuePair<string, object?> variable in pending)
        {
            TrySetPropertyThroughReflection(GetFormsControlType(formsControl), formsControl, variable.Key, variable.Value);
        }
    }

    /// <summary>
    /// Sets a variable on this object (such as "X") to the argument value
    /// (such as 100.0f). This can be a primitive property like Height, or it can be
    /// a state.
    /// </summary>
    /// <param name="propertyName">The name of the variable on this object such as X or Height. If the property is a state, then the name should be "{CategoryName}State".</param>
    /// <param name="value">The value, casted to the correct type.</param>
    public void SetProperty(string propertyName, object? value)
    {
        if (propertyName == "SourceFile")
        {
            ReleaseChainsFromPreviousSourceFile(value as string);
        }

        SetPropertyCore(propertyName, value);

        if (propertyName == "SourceFile")
        {
            RecordAssignedSourceFile(value as string);
        }

        // These names are reported by their own property setter.
        if (!ReportsFromTypedSetter(propertyName))
        {
            ReportReferenceSourceChanged(propertyName, value);
        }
    }

    // The file last assigned through the SourceFile variable. A Sprite or NineSlice shows the texture or
    // animation chains the file produced, not the path, so hot reload needs this to find who shows a
    // file that changed on disk.
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<GraphicalUiElement, string> _assignedSourceFiles =
        new System.Runtime.CompilerServices.ConditionalWeakTable<GraphicalUiElement, string>();

    internal string? AssignedSourceFile => _assignedSourceFiles.TryGetValue(this, out string? file) ? file : null;

    private void RecordAssignedSourceFile(string? file)
    {
        _assignedSourceFiles.Remove(this);
        _chainsFromSourceFile.Remove(this);
        if (!string.IsNullOrEmpty(file))
        {
            _assignedSourceFiles.Add(this, file!);
            if (IsChainFile(file!)
                && (mContainedObjectAsIpso as IAnimationChainRenderable)?.AnimationChainList is { } chains)
            {
                _chainsFromSourceFile.Add(this, new WeakReference(chains));
            }
        }
    }

    private static bool IsChainFile(string file) => file.EndsWith(".achx") || file.EndsWith(".achj");

    // The chain list the last chain file assigned through SourceFile produced. Chains a game assigns
    // afterward are not this list and are never dropped when the SourceFile changes.
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<GraphicalUiElement, WeakReference> _chainsFromSourceFile =
        new System.Runtime.CompilerServices.ConditionalWeakTable<GraphicalUiElement, WeakReference>();

    // A SourceFile that is empty or not a chain file shows no animation, as a restart does, so the
    // chains the previous chain file produced go and the texture area the frames set goes back to the
    // element's variables.
    private void ReleaseChainsFromPreviousSourceFile(string? newFile)
    {
        if (!string.IsNullOrEmpty(newFile) && IsChainFile(newFile!))
        {
            return;
        }

        if (!_chainsFromSourceFile.TryGetValue(this, out WeakReference? owned)
            || mContainedObjectAsIpso is not IAnimationChainRenderable renderable
            || !ReferenceEquals(renderable.AnimationChainList, owned.Target))
        {
            return;
        }

        renderable.ClearAnimationChains();
        _chainsFromSourceFile.Remove(this);

        bool wasSuspended = IsLayoutSuspended;
        SuspendLayout();
        try
        {
            RestoreTextureVariables();
        }
        finally
        {
            if (!wasSuspended)
            {
                ResumeLayout();
            }
        }
    }

    private static readonly string[] _chainDrivenVariables = { "TextureAddress", "TextureLeft", "TextureTop", "TextureWidth", "TextureHeight", "FlipHorizontal" };

    // Sets the variables a chain frame writes to what the saved element gives them: the default
    // state, then the category states applied (or being applied) on the element that contains this one.
    // The chain name is not among them: the renderable keeps the name asked for.
    private void RestoreTextureVariables()
    {
        InstanceSave? instance = Tag as InstanceSave;
        string prefix = instance == null ? "" : instance.Name + ".";
        foreach (string name in _chainDrivenVariables)
        {
            object? fromProject = instance?.ParentContainer?.DefaultState?.GetValueRecursive(prefix + name);
            SetProperty(name, fromProject ?? name switch
            {
                "TextureAddress" => TextureAddress.EntireTexture,
                "FlipHorizontal" => false,
                _ => 0,
            });
        }

        GraphicalUiElement? container = ElementGueContainingThis;
        if (instance == null || container == null)
        {
            return;
        }

        List<StateSave> states = new List<StateSave>();
        if (container._appliedCategoryStates != null)
        {
            foreach (KeyValuePair<string, string> applied in container._appliedCategoryStates)
            {
                StateSave? state = container.mCategories.TryGetValue(applied.Key, out StateSaveCategory? category)
                    ? category.States.FindByName(applied.Value)
                    : null;
                if (state != null)
                {
                    states.Add(state);
                }
            }
        }
        states.AddRange(container.statesInStack);

        foreach (StateSave state in states)
        {
            foreach (VariableSave variable in state.Variables)
            {
                if (variable.SetsValue && variable.Value != null && variable.Name.StartsWith(prefix, StringComparison.Ordinal)
                    && Array.IndexOf(_chainDrivenVariables, variable.Name.Substring(prefix.Length)) >= 0)
                {
                    SetProperty(variable.Name.Substring(prefix.Length), variable.Value);
                }
            }
        }
    }

    // "Background.Blue" splits into "Background" and "Blue" on every state change that sets it, so the
    // two strings are kept per name. The split depends only on the name, so nothing invalidates it; the
    // cap bounds a program that builds dotted names on the fly.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (string Instance, string Variable)> _dottedNameSplits =
        new System.Collections.Concurrent.ConcurrentDictionary<string, (string Instance, string Variable)>(StringComparer.Ordinal);
    private const int MaxCachedDottedNameSplits = 4096;
    // Counted separately: ConcurrentDictionary.Count takes every lock, and past the cap each miss would pay it.
    private static int _dottedNameSplitCount;

    private static (string Instance, string Variable) SplitAtFirstDot(string name, int indexOfDot)
    {
        if (_dottedNameSplits.TryGetValue(name, out (string Instance, string Variable) split))
        {
            return split;
        }
        split = (name.Substring(0, indexOfDot), name.Substring(indexOfDot + 1));
        if (System.Threading.Volatile.Read(ref _dottedNameSplitCount) < MaxCachedDottedNameSplits
            && _dottedNameSplits.TryAdd(name, split))
        {
            System.Threading.Interlocked.Increment(ref _dottedNameSplitCount);
        }
        return split;
    }

    private void SetPropertyCore(string propertyName, object? value)
    {

        if (mExposedVariables.ContainsKey(propertyName))
        {
            string underlyingProperty = mExposedVariables[propertyName];
            int indexOfDot = underlyingProperty.IndexOf('.');

            // ExposedAsName forwards to a named child instance's property, so the
            // underlying property must be instance-qualified (e.g. "Background.ColorCategoryState").
            // A malformed/unqualified underlying property has nowhere to forward to - skip it
            // rather than crash the whole load (mirrors SetPropertyThroughReflection's handling
            // of bad values below).
            if (indexOfDot >= 0)
            {
                (string instanceName, string variable) = SplitAtFirstDot(underlyingProperty, indexOfDot);
                GraphicalUiElement? containedGue = GetGraphicalUiElementByName(instanceName);

                // Children may not have been created yet
                if (containedGue != null)
                {
                    // SetProperty reports the write under the exposed name, so the instance must not
                    // report that name for this element as well (it would evaluate the rows twice).
                    _exposedForwardDepth++;
                    try
                    {
                        containedGue.SetProperty(variable, value);
                    }
                    finally
                    {
                        _exposedForwardDepth--;
                    }
                }
            }
        }
        else if (ToolsUtilities.StringFunctions.ContainsNoAlloc(propertyName, '.'))
        {
            int indexOfDot = propertyName.IndexOf('.');
            (string instanceName, string variable) = SplitAtFirstDot(propertyName, indexOfDot);
            GraphicalUiElement? containedGue = GetGraphicalUiElementByName(instanceName);

            // instances may not have been set yet
            if (containedGue != null)
            {
                containedGue.SetProperty(variable, value);
            }


        }
        else if (TrySetValueOnThis(propertyName, value))
        {
            // success, do nothing, but it's in an else if to prevent the following else if's from evaluating
        }
        else if (this.mContainedObjectAsIpso != null)
        {
#if FULL_DIAGNOSTICS
            if (SetPropertyOnRenderable == null)
            {
                throw new Exception($"{nameof(SetPropertyOnRenderable)} must be set on GraphicalUiElement");
            }
#endif
            bool handledByRenderable;
            try
            {
                handledByRenderable = SetPropertyOnRenderable(mContainedObjectAsIpso, this, propertyName, value);
            }
            catch (InvalidCastException invalidCastException)
            {
                throw new InvalidCastException($"Error trying to set {propertyName} to {value} on {mContainedObjectAsIpso}", invalidCastException);
            }

            if (!handledByRenderable)
            {
                TrySetCustomVariableOnThis(propertyName, value);
            }
        }
        else
        {
            TrySetCustomVariableOnThis(propertyName, value);
        }
    }

    #region Variable reference re-evaluation (ADR 0022)

    private VariableReferenceGraph? _referenceGraph;

    // Greater than zero while SetVariablesRecursively applies this element's own default state. That
    // call applies the element's rows itself, in order, so what it writes to the element reports to
    // nobody. What it writes to the element's children does report: a child's rows were evaluated
    // when the child was created, against its defaults, and need the values the parent overrides.
    private int _referenceLoadDepth;

    internal void BeginReferenceLoad() => _referenceLoadDepth++;

    internal void EndReferenceLoad() => _referenceLoadDepth--;

    [ThreadStatic]
    private static List<ReferenceRow>? _affectedRowsScratch;

    /// <summary>
    /// Re-evaluates the same-component variable references that read one of the given variables, so a
    /// variable such as <c>Y = Math.Sin(Progress)</c> follows <c>Progress</c> when it changes. Rows that read
    /// a result of an affected row are re-evaluated after it. References to other elements are not
    /// touched; those stay explicit (see <see cref="RefreshStyles"/>).
    /// </summary>
    /// <remarks>
    /// Animations and state application call this for you. Call it directly after changing a variable
    /// through a typed property, such as <c>element.X = 5</c>, when a reference reads it. Rows read every other
    /// variable at the value the element holds now, falling back to the authored value for one it cannot read.
    /// </remarks>
    /// <param name="changedVariables">The variables that changed, with their new values. Names are relative to this element,
    /// such as <c>Progress</c> or <c>Instance.Width</c>.</param>
    public void NotifyVariablesChanged(IReadOnlyList<DataTypes.Variables.VariableSave> changedVariables)
    {
        ElementSave? element = ElementSave;
        VariableReferenceGraph? graph = GetReferenceGraph();
        if (element == null || graph == null || !graph.HasRows || changedVariables.Count == 0)
        {
            return;
        }

        List<ReferenceRow> affected = _affectedRowsScratch ??= new List<ReferenceRow>();
        graph.FindAffectedRows(changedVariables, affected);
        if (affected.Count == 0)
        {
            return;
        }

        // Rows read their inputs from a state, so the new values are put in one. It is owned by the
        // element only to resolve names; nothing else sees it.
        DataTypes.Variables.StateSave evaluationState = new DataTypes.Variables.StateSave
        {
            Name = "ReferenceChanges",
            ParentContainer = element
        };
        for (int i = 0; i < changedVariables.Count; i++)
        {
            evaluationState.Variables.Add(changedVariables[i]);
            RecordReferenceInput(changedVariables[i].Name, changedVariables[i].Value);
        }

        // Every other input a row reads is the value the object holds now, not the authored one (#6411).
        AddLiveInputs(affected, evaluationState);

        VariableReferenceGraph.Evaluate(affected, evaluationState, this);
    }

    /// <summary>
    /// Evaluates the rows that read a laid-out value (<c>AbsoluteWidth</c> and so on) again on this element
    /// and everything it contains. A build applies references while layout is suspended, so those rows read
    /// zero; the build calls this once after its final layout (8026). Elements without such a row cost one
    /// cached graph lookup.
    /// </summary>
    internal void ReevaluateLiveLayoutReferences()
    {
        VariableReferenceGraph? graph = GetReferenceGraph();
        ElementSave? element = ElementSave;
        if (graph != null && element != null && graph.HasLiveLayoutRows)
        {
            List<ReferenceRow> rows = new List<ReferenceRow>();
            graph.FindLiveLayoutRows(rows);
            DataTypes.Variables.StateSave evaluationState = new DataTypes.Variables.StateSave
            {
                Name = "ReferenceLayout",
                ParentContainer = element
            };
            AddLiveInputs(rows, evaluationState);
            VariableReferenceGraph.Evaluate(rows, evaluationState, this);
        }

        IList<GraphicalUiElement> contained = ContainedElements;
        for (int i = 0; i < contained.Count; i++)
        {
            contained[i].ReevaluateLiveLayoutReferences();
        }
    }

    // Variables that have no property on the visual (a component's custom variables) are not held
    // anywhere on the live object, so the last value reported for each is kept here. Read only when the
    // object cannot be asked, and only for names some row reads. Holds the names a row of the default state reads
    // that were reported, and the custom variables the element declares (a keyframe's rows read those).
    private Dictionary<string, object?>? _referenceInputs;

    // Keeps the value of a variable some row reads. For an instance-qualified name the instance holds it.
    internal void RecordReferenceInput(string name, object? value)
    {
        GraphicalUiElement? owner = this;
        string propertyName = name;
        int dot = name.IndexOf('.');
        if (dot >= 0)
        {
            owner = GetGraphicalUiElementByName(name.Substring(0, dot));
            propertyName = name.Substring(dot + 1);
        }
        if (owner == null)
        {
            return;
        }
        owner._referenceInputs ??= new Dictionary<string, object?>();
        owner._referenceInputs[propertyName] = value;
    }

    private bool TryReadLiveInput(string name, out object? value)
    {
        value = null;
        GraphicalUiElement? owner = this;
        string propertyName = name;
        int dot = name.IndexOf('.');
        if (dot >= 0)
        {
            owner = GetGraphicalUiElementByName(name.Substring(0, dot));
            propertyName = name.Substring(dot + 1);
        }
        if (owner == null)
        {
            return false;
        }

        // Laid-out values are resolved by the evaluator from the live tree.
        if (propertyName.StartsWith("Absolute", StringComparison.Ordinal))
        {
            return false;
        }

#if !FRB
        // The property reader is not part of the FlatRedBall build.
        if (owner.TryGetPropertyExact(propertyName, out value) && value != null)
        {
            return true;
        }

        // A Forms-only property lives on the Forms control, which holds the effective value: the behavior
        // default, what code set, and the control's own clamping.
        if (owner.TryReadFormsControlProperty(propertyName, out value, out _))
        {
            return true;
        }
#endif
        // A null cannot be seeded: the state would fall back to the authored value for it.
        return owner._referenceInputs != null && owner._referenceInputs.TryGetValue(propertyName, out value) && value != null;
    }

#if !FRB
    /// <summary>
    /// Reads a behavior-declared Forms property (such as <c>Slider.Minimum</c>) from the live Forms control
    /// of this element. Only a name some behavior on the element's type chain declares is read, so a
    /// reference resolves exactly the properties the tool and the checker know about. False when the
    /// element has no Forms control (the tool's canvas never creates one) or no such property.
    /// </summary>
    internal bool TryReadFormsControlProperty(string propertyName, out object? value, out VariableSave? declaration)
    {
        value = null;
        declaration = null;
        object? formsControl = (this as InteractiveGue)?.FormsControlAsObject;
        ElementSave? element = ElementSave;
        if (formsControl == null || element == null)
        {
            return false;
        }

        declaration = Gum.ProjectServices.FormsBehaviorChain.FindFormsPropertyDeclaration(element, propertyName);
        if (declaration == null)
        {
            return false;
        }

        System.Reflection.PropertyInfo? propertyInfo = GetReadableFormsProperty(formsControl, propertyName);
        if (propertyInfo == null)
        {
            return false;
        }
        value = propertyInfo.GetValue(formsControl, null);
        return value != null;
    }

    /// <summary>
    /// Reports a Forms-only property that code just assigned on this element's Forms control to the references
    /// that read it, with the value the control ends with (after clamping). Nothing is read or boxed unless a
    /// reference reads the property.
    /// </summary>
    internal void ReportFormsPropertyChanged(string propertyName)
    {
        if (GetReferenceAudience(propertyName, forFormsProperty: true) != ReferenceAudience.None &&
            TryReadFormsControlProperty(propertyName, out object? value, out _))
        {
            ReportReferenceSourceChanged(propertyName, value);
        }
    }

    // A live update reads the same few properties on every change, so the reflection lookup is kept per type.
    // Like the other static reflection caches here, it is never cleared: types are kept for the process lifetime.
    private static readonly Dictionary<(Type, string), System.Reflection.PropertyInfo?> _readableFormsProperties =
        new Dictionary<(Type, string), System.Reflection.PropertyInfo?>();

    private static System.Reflection.PropertyInfo? GetReadableFormsProperty(object formsControl, string propertyName)
    {
        Type type = GetFormsControlType(formsControl);
        lock (_readableFormsProperties)
        {
            if (_readableFormsProperties.TryGetValue((type, propertyName), out System.Reflection.PropertyInfo? cached))
            {
                return cached;
            }
        }

        System.Reflection.PropertyInfo? found = null;
        try
        {
            System.Reflection.PropertyInfo? propertyInfo = GetFormsControlType(formsControl).GetProperty(propertyName);
            if (propertyInfo != null && propertyInfo.CanRead && propertyInfo.GetIndexParameters().Length == 0)
            {
                found = propertyInfo;
            }
        }
        catch (System.Reflection.AmbiguousMatchException)
        {
        }
        lock (_readableFormsProperties)
        {
            _readableFormsProperties[(type, propertyName)] = found;
        }
        return found;
    }
#endif

    private static bool StateHasVariable(DataTypes.Variables.StateSave state, string name)
    {
        for (int i = 0; i < state.Variables.Count; i++)
        {
            if (state.Variables[i].Name == name)
            {
                return true;
            }
        }
        return false;
    }

    private static bool WrittenByAny(IReadOnlyList<ReferenceRow> rows, string name)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].WrittenName == name)
            {
                return true;
            }
        }
        return false;
    }

    // Puts the live value of every input the rows read into the state they evaluate against. The changed
    // variables are already there, and a result of one of the rows is computed in order, so neither is read.
    private void AddLiveInputs(IReadOnlyList<ReferenceRow> rows, DataTypes.Variables.StateSave state)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            string[] reads = rows[i].ReadNames;
            for (int j = 0; j < reads.Length; j++)
            {
                string read = reads[j];
                if (StateHasVariable(state, read) || WrittenByAny(rows, read))
                {
                    continue;
                }
                if (TryReadLiveInput(read, out object? value))
                {
                    state.Variables.Add(new DataTypes.Variables.VariableSave { Name = read, Value = value, SetsValue = true });
                }
            }
        }
    }

    /// <summary>
    /// A copy of the animation keyframe state with each of its variable-reference rows evaluated against
    /// this element's live values (#7645), so a keyframe's <c>X = OffsetX</c> is the offset the element holds
    /// now. The state itself is not changed, since it is shared by every element playing the animation.
    /// A row that cannot be evaluated (unknown name, a type that does not fit, an expression without
    /// Gum.Expressions) leaves the variable at its stored value and is reported once, like any other row.
    /// </summary>
    internal DataTypes.Variables.StateSave ResolveKeyframeReferences(ElementSave element, DataTypes.Variables.StateSave keyframeState)
    {
        VariableReferenceGraph graph = VariableReferenceGraph.GetForState(element, keyframeState);
        if (!graph.HasRows)
        {
            return keyframeState;
        }

        DataTypes.Variables.StateSave resolved = keyframeState.Clone();

        // Rows read the keyframe's own variables first, then what this element holds now. The evaluation
        // state shares the variables of the copy, so a result lands in the copy; a result for a variable
        // the keyframe does not store is added to the copy below.
        DataTypes.Variables.StateSave evaluationState = new DataTypes.Variables.StateSave
        {
            Name = "KeyframeReferences",
            ParentContainer = element
        };
        evaluationState.Variables.AddRange(resolved.Variables);
        IReadOnlyList<ReferenceRow> rows = graph.LoadOrderRows;
        AddLiveInputs(rows, evaluationState);

        for (int i = 0; i < rows.Count; i++)
        {
            ReferenceRow row = rows[i];
            ElementSaveExtensions.ApplyVariableReferencesOnSpecificOwner(row.Instance, row.Line, evaluationState, this, propagateNull: false);

            DataTypes.Variables.VariableSave? result = null;
            for (int j = 0; j < evaluationState.Variables.Count; j++)
            {
                if (evaluationState.Variables[j].Name == row.WrittenName)
                {
                    result = evaluationState.Variables[j];
                    break;
                }
            }
            if (result != null && result.Value != null && !StateHasVariable(resolved, row.WrittenName))
            {
                resolved.Variables.Add(result);
            }
        }

        return resolved;
    }

    /// <summary>
    /// Re-evaluates the same-component variable references that read the variable. See
    /// <see cref="NotifyVariablesChanged"/>.
    /// </summary>
    /// <param name="variableName">The variable relative to this element, such as <c>Progress</c>.</param>
    /// <param name="value">The variable's new value.</param>
    public void NotifyVariableChanged(string variableName, object? value)
    {
        VariableReferenceGraph? graph = GetReferenceGraph();
        if (graph == null || !graph.HasRows)
        {
            return;
        }

        NotifyVariablesChanged(new[]
        {
            new DataTypes.Variables.VariableSave { Name = variableName, Value = value, SetsValue = true }
        });
    }

    private struct PendingReferenceChange
    {
        public GraphicalUiElement Root;
        public string Name;
        public object? Value;
    }

    // While a state is being applied, changes are collected and reported once at the end, so a state
    // that sets several variables re-evaluates each affected row once.
    [ThreadStatic]
    private static int _stateBatchDepth;

    [ThreadStatic]
    private static List<PendingReferenceChange>? _pendingReferenceChanges;

    private static bool ReportsFromTypedSetter(string propertyName)
    {
        switch (propertyName)
        {
            case "X":
            case "Y":
            case "Width":
            case "Height":
            case "Rotation":
            case "Visible":
                return true;
            default:
                return false;
        }
    }

    // Which elements have a reference that reads the variable: this element's own, and the one
    // containing this instance. Allocates nothing, so a typed property can ask before boxing its value.
    private ReferenceAudience GetReferenceAudience(string propertyName, bool forFormsProperty = false)
    {
        ReferenceAudience audience = ReferenceAudience.None;
        // A row writing its result, or this element applying its own default state: the load applies
        // its rows right after, so a report would only evaluate them twice.
        if (VariableReferenceGraph.IsSuppressed || _referenceLoadDepth > 0)
        {
            return audience;
        }

        // The element's own rows read plain names. A dotted name was forwarded to the instance, which
        // reports it to this element itself.
        if (ElementSave != null && IsFullyCreated && propertyName.IndexOf('.') < 0)
        {
            VariableReferenceGraph? graph = GetReferenceGraph(forFormsProperty);
            if (graph != null && graph.IsAffectedBy(propertyName, includeLiveLayout: !forFormsProperty))
            {
                audience |= ReferenceAudience.Self;
            }
        }

        GraphicalUiElement? container = mWhatContainsThis;
        // A container that is loading applies its own rows after the state that wrote this value.
        if (container != null && container.IsFullyCreated && container._referenceLoadDepth == 0 && Tag is InstanceSave instance)
        {
            VariableReferenceGraph? containerGraph = container.GetReferenceGraph(forFormsProperty);
            if (containerGraph != null && containerGraph.IsAffectedBy(instance.Name, propertyName, includeLiveLayout: !forFormsProperty))
            {
                audience |= ReferenceAudience.Container;
            }

            // The container may expose this variable under its own name (IsMyButtonVisible for
            // MyButton.Visible). That is the same value, so rows reading the exposed name follow a write
            // through the instance. A write made through the exposed name is reported under it already.
            if (container.mExposedVariables.Count > 0 && container._exposedForwardDepth == 0 &&
                container.FindExposedNameOf(instance.Name, propertyName) is string exposedName &&
                container.GetReferenceAudience(exposedName, forFormsProperty) != ReferenceAudience.None)
            {
                audience |= ReferenceAudience.ExposedName;
            }
        }
        return audience;
    }

    // Greater than zero while this element forwards a write made through an exposed name to an instance.
    private int _exposedForwardDepth;

    // The name this element exposes instanceName.memberName under, or null. Allocates nothing.
    private string? FindExposedNameOf(string instanceName, string memberName)
    {
        foreach (KeyValuePair<string, string> exposed in mExposedVariables)
        {
            string underlying = exposed.Value;
            if (underlying.Length == instanceName.Length + 1 + memberName.Length
                && underlying[instanceName.Length] == '.'
                && string.CompareOrdinal(underlying, 0, instanceName, 0, instanceName.Length) == 0
                && string.CompareOrdinal(underlying, instanceName.Length + 1, memberName, 0, memberName.Length) == 0)
            {
                return exposed.Key;
            }
        }
        return null;
    }

    [Flags]
    private enum ReferenceAudience
    {
        None = 0,
        Self = 1,
        Container = 2,
        ExposedName = 4
    }

    // The typed properties report their own changes so that code such as element.X = 5 updates the
    // references that read X. Nothing is boxed unless a reference reads the property.
    /// <summary>
    /// Reports a typed property that was just assigned, reading its new value, to the variable references
    /// that read it. Nothing is read or boxed unless a reference reads the property. Subclasses call this
    /// from a setter that does not raise <see cref="PropertyChanged"/>.
    /// </summary>
    /// <param name="propertyName">The variable name, such as <c>Text</c>.</param>
    protected void ReportTypedPropertyChanged(string propertyName)
    {
#if !FRB
        // The property reader is not part of the FlatRedBall build.
        if (GetReferenceAudience(propertyName) != ReferenceAudience.None &&
            this.TryGetPropertyExact(propertyName, out object? value))
        {
            ReportReferenceSourceChanged(propertyName, value);
        }
#endif
    }

    /// <summary>
    /// Reports a typed property that was just assigned, with the value the caller already has, to the variable
    /// references that read it. For a property whose value cannot be read back by name (a file path whose runtime
    /// holds only the loaded document).
    /// </summary>
    protected void ReportTypedPropertyChanged(string propertyName, string? value)
    {
        if (GetReferenceAudience(propertyName) != ReferenceAudience.None)
        {
            ReportReferenceSourceChanged(propertyName, value);
        }
    }

    private void ReportTypedPropertyChanged(string propertyName, float value)
    {
        if (GetReferenceAudience(propertyName) != ReferenceAudience.None)
        {
            ReportReferenceSourceChanged(propertyName, value);
        }
    }

    private void ReportTypedPropertyChanged(string propertyName, bool value)
    {
        if (GetReferenceAudience(propertyName) != ReferenceAudience.None)
        {
            ReportReferenceSourceChanged(propertyName, value);
        }
    }

    // Reports a variable that was just written to the elements whose references read it.
    private void ReportReferenceSourceChanged(string propertyName, object? value)
    {
        ReferenceAudience audience = GetReferenceAudience(propertyName);
        if ((audience & ReferenceAudience.Self) != 0)
        {
            QueueReferenceChange(this, propertyName, value);
        }
        if ((audience & ReferenceAudience.Container) != 0)
        {
            QueueReferenceChange(mWhatContainsThis!, ((InstanceSave)Tag!).Name + "." + propertyName, value);
        }
        if ((audience & ReferenceAudience.ExposedName) != 0)
        {
            GraphicalUiElement container = mWhatContainsThis!;
            string exposedName = container.FindExposedNameOf(((InstanceSave)Tag!).Name, propertyName)!;

            // SetProperty reports even when the value is the same, and a Forms control sets its visual that
            // way, so a value the exposed name was last reported with is not reported again. Not inside a state batch, where the last report is still queued.
            bool unchanged = _stateBatchDepth == 0 && container._referenceInputs != null &&
                container._referenceInputs.TryGetValue(exposedName, out object? last) && Equals(last, value);
            if (!unchanged)
            {
                container.ReportReferenceSourceChanged(exposedName, value);
            }
        }
    }

    private static void QueueReferenceChange(GraphicalUiElement root, string name, object? value)
    {
        if (_stateBatchDepth > 0)
        {
            List<PendingReferenceChange> pending = _pendingReferenceChanges ??= new List<PendingReferenceChange>();
            pending.Add(new PendingReferenceChange { Root = root, Name = name, Value = value });
        }
        else
        {
            root.NotifyVariableChanged(name, value);
        }
    }

    private static void FlushPendingReferenceChanges()
    {
        List<PendingReferenceChange>? pending = _pendingReferenceChanges;
        while (pending != null && pending.Count > 0)
        {
            GraphicalUiElement root = pending[0].Root;
            List<DataTypes.Variables.VariableSave> changed = new List<DataTypes.Variables.VariableSave>();
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (pending[i].Root != root)
                {
                    continue;
                }

                // A variable set twice in one state keeps its last value, which comes last in pending.
                bool alreadyListed = false;
                for (int j = 0; j < changed.Count; j++)
                {
                    if (changed[j].Name == pending[i].Name)
                    {
                        alreadyListed = true;
                        break;
                    }
                }
                if (!alreadyListed)
                {
                    changed.Add(new DataTypes.Variables.VariableSave { Name = pending[i].Name, Value = pending[i].Value, SetsValue = true });
                }
                pending.RemoveAt(i);
            }
            changed.Reverse();
            root.NotifyVariablesChanged(changed);
        }
    }

    private VariableReferenceGraph? GetReferenceGraph(bool trustCached = false)
    {
        ElementSave? element = ElementSave;
        // Standard elements (Sprite, Text, ...) hold the styling rows ApplyAllVariableReferences
        // applies explicitly, not component-local ones.
        if (element == null || element is StandardElementSave)
        {
            return null;
        }

        VariableReferenceGraph? graph = _referenceGraph;
        // A Forms property setter can run on every frame and almost never has a reader, and validating the graph walks every
        // reference list of the element. It trusts the graph it has; a row added since is found by the next path that validates.
        if (trustCached && graph != null)
        {
            return graph;
        }
        if (graph == null || !graph.IsCurrent(element))
        {
            graph = VariableReferenceGraph.GetFor(element);
            _referenceGraph = graph;
        }
        return graph;
    }

    #endregion

    private bool TrySetValueOnThis(string propertyName, object? value)
    {
        bool toReturn = false;
        try
        {
            switch (propertyName)
            {
                case "AutoGridHorizontalCells":
                    this.AutoGridHorizontalCells = (int)value!;
                    break;
                case "AutoGridVerticalCells":
                    this.AutoGridVerticalCells = (int)value!;
                    break;
                case "ChildrenLayout":
                case "Children Layout":
                    this.ChildrenLayout = ToEnum<ChildrenLayout>(value);
                    toReturn = true;
                    break;
                case "ClipsChildren":
                case "Clips Children":
                    this.ClipsChildren = (bool)value!;
                    toReturn = true;
                    break;
#if !FRB && (NET6_0_OR_GREATER || NETSTANDARD2_1)
                case "ExposeChildrenEvents":
                    {
                        if (this is InteractiveGue interactiveGue)
                        {
                            interactiveGue.ExposeChildrenEvents = (bool)value!;
                            toReturn = true;
                        }
                    }
                    break;
#endif
                case "FlipHorizontal":
                    this.FlipHorizontal = (bool)value!;
                    toReturn = true;
                    break;
#if !FRB && (NET6_0_OR_GREATER || NETSTANDARD2_1)
                case "HasEvents":
                    {
                        if (this is InteractiveGue interactiveGue)
                        {
                            interactiveGue.HasEvents = (bool)value!;
                            toReturn = true;
                        }
                    }
                    break;
#endif
                case "Height":
                    this.Height = (float)value!;
                    toReturn = true;
                    break;
                case "HeightUnits":
                case "Height Units":
                    this.HeightUnits = ToEnum<DimensionUnitType>(value);
                    toReturn = true;
                    break;
                case nameof(IgnoredByParentSize):
                    this.IgnoredByParentSize = (bool)value!;
                    toReturn = true;
                    break;
                case nameof(MaxHeight):
                    this.MaxHeight = (float?)value;
                    toReturn = true;
                    break;
                case nameof(MaxWidth):
                    this.MaxWidth = (float?)value;
                    toReturn = true;
                    break;
                case nameof(MinHeight):
                    this.MinHeight = (float?)value;
                    toReturn = true;
                    break;
                case nameof(MinWidth):
                    this.MinWidth = (float?)value;
                    toReturn = true;
                    break;
                case "Parent":
                    {
                        string valueAsString = (string)value!;

                        if (!string.IsNullOrEmpty(valueAsString) && mWhatContainsThis != null)
                        {
                            var newParent = this.mWhatContainsThis.GetGraphicalUiElementByName(valueAsString);
                            if (newParent != null)
                            {
                                Parent = newParent;
                            }
                        }
                        toReturn = true;
                    }
                    break;
                case "Rotation":
                    this.Rotation = (float)value!;
                    toReturn = true;
                    break;
                case "StackSpacing":
                    this.StackSpacing = (float)value!;
                    toReturn = true;
                    break;
                case "TextureLeft":
                case "Texture Left":
                    this.TextureLeft = (int)value!;
                    toReturn = true;
                    break;
                case "TextureTop":
                case "Texture Top":
                    this.TextureTop = (int)value!;
                    toReturn = true;
                    break;
                case "TextureWidth":
                case "Texture Width":
                    this.TextureWidth = (int)value!;
                    toReturn = true;
                    break;
                case "TextureHeight":
                case "Texture Height":
                    this.TextureHeight = (int)value!;
                    toReturn = true;

                    break;
                case "TextureWidthScale":
                case "Texture Width Scale":
                    this.TextureWidthScale = (float)value!;
                    toReturn = true;
                    break;
                case "TextureHeightScale":
                case "Texture Height Scale":
                    this.TextureHeightScale = (float)value!;
                    toReturn = true;
                    break;
                case "TextureAddress":
                case "Texture Address":
                    this.TextureAddress = ToEnum<Gum.Managers.TextureAddress>(value);
                    toReturn = true;
                    break;
                case "Visible":
                    this.Visible = (bool)value!;
                    toReturn = true;
                    break;
                case "Width":
                    this.Width = (float)value!;
                    toReturn = true;
                    break;
                case "WidthUnits":
                case "Width Units":
                    this.WidthUnits = ToEnum<DimensionUnitType>(value);
                    toReturn = true;
                    break;
                case "X":
                    this.X = (float)value!;
                    toReturn = true;
                    break;
                case "XOrigin":
                case "X Origin":
                    this.XOrigin = ToEnum<HorizontalAlignment>(value);
                    toReturn = true;
                    break;
                case "XUnits":
                case "X Units":
                    this.XUnits = UnitConverter.ConvertToGeneralUnit(value!);
                    toReturn = true;
                    break;
                case "Y":
                    this.Y = (float)value!;
                    toReturn = true;
                    break;
                case "YOrigin":
                case "Y Origin":
                    this.YOrigin = ToEnum<VerticalAlignment>(value);
                    toReturn = true;
                    break;
                case "YUnits":
                case "Y Units":

                    this.YUnits = UnitConverter.ConvertToGeneralUnit(value!);
                    toReturn = true;
                    break;
                case "Wrap":
                    this.Wrap = (bool)value!;
                    toReturn = true;
                    break;
                case "WrapsChildren":
                case "Wraps Children":
                    this.WrapsChildren = (bool)value!;
                    toReturn = true;
                    break;

                // No Font/FontSize/CustomFontFile/UseCustomFont cases here by design: those properties
                // exist on GraphicalUiElement only under #if FRB (see the Font/Text region below). Every
                // other backend owns them on TextRuntime and dispatches via CustomSetPropertyOnRenderable,
                // so there is no backend-agnostic property here to route them to (see issue #4088).
            }

            if (!toReturn)
            {
                var propertyNameLength = propertyName.Length;
                if (propertyNameLength > 5
                    && propertyName[propertyNameLength - 1] == 'e'
                    && propertyName[propertyNameLength - 2] == 't'
                    && propertyName[propertyNameLength - 3] == 'a'
                    && propertyName[propertyNameLength - 4] == 't'
                    && propertyName[propertyNameLength - 5] == 'S'
                    && value is string)
                {
                    var valueAsString = (string)value;

                    if (TryGetCategoryByPrefix(propertyName, propertyNameLength - "State".Length,
                        out string? categoryName, out StateSaveCategory? category))
                    {
                        var state = category.States.FindByName(valueAsString);
                        if (state != null)
                        {
                            ApplyState(state);
                            RecordAppliedCategoryState(categoryName, valueAsString);
                            toReturn = true;
                        }
                    }
                }
            }
        }
        catch (InvalidCastException innerException)
        {
            // There could be some rogue value set to the incorrect type, or maybe
            // a new type or plugin initialized the default to the wrong type. We don't
            // want to blow up if this happens
            // Update October 12, 2023
            // This swallowed exception caused
            // problems for myself and arcnor. I 
            // am concerned there may be other exceptions
            // being swallowed, but maybe we should push those
            // errors up and let the callers handle it.
#if FULL_DIAGNOSTICS
            throw new InvalidCastException($"Trying to set property {propertyName} to a value of {value} of type {value?.GetType()} on {Name}", innerException);
#endif
        }
        return toReturn;
    }

    /// <summary>
    /// Coerces a data-driven value to the enum type <typeparamref name="T"/> for the hardcoded
    /// property switch in <see cref="TrySetValueOnThis"/>. ApplyState/SetProperty feed values that
    /// may arrive already boxed as the enum, as a string name (variable references and hand-written
    /// .gumx files), or as the underlying int (.gumx serializes enums as int). A plain cast throws
    /// on the latter two, which under FULL_DIAGNOSTICS tears down the editor. This mirrors
    /// <see cref="SetPropertyThroughReflection"/>'s enum handling so the GUE-property path is as
    /// tolerant as the renderable path; a genuinely invalid value still throws (surfaced under
    /// FULL_DIAGNOSTICS, skipped in release by the surrounding catch).
    /// </summary>
    private static T ToEnum<T>(object? value) where T : struct, Enum =>
        value is T typed ? typed
        : value is string name ? (T)Enum.Parse(typeof(T), name, ignoreCase: true)
        : (T)Enum.ToObject(typeof(T), value!);

    public void ApplyStateRecursive(string categoryName, string stateName)
    {
        if (mCategories.ContainsKey(categoryName))
        {
            var category = mCategories[categoryName];

            var state = category.States.FindByName(stateName);
            if (state != null)
            {
                ApplyState(state);
            }
        }

        if (Children != null)
        {
            foreach (GraphicalUiElement child in this.Children)
            {
                child.ApplyStateRecursive(categoryName, stateName);
            }

        }
        else
        {
            foreach (var item in this.mWhatThisContains)
            {
                item.ApplyStateRecursive(categoryName, stateName);
            }
        }
    }

    public void ApplyState(string name)
    {
        if (mStates.ContainsKey(name))
        {
            var state = mStates[name];

            ApplyState(state);

        }


        // This is a little dangerous because it's ambiguous.
        // Technically categories could have same-named states.
        foreach (var category in mCategories.Values)
        {
            var foundState = category.States.FindByName(name);

            if (foundState != null)
            {
                ApplyState(foundState);
            }
        }
    }

    public void ApplyState(string categoryName, string stateName)
    {
        if (mCategories.ContainsKey(categoryName))
        {
            var category = mCategories[categoryName];

            var state = category.States.FindByName(stateName);

            if (state != null)
            {
                ApplyState(state);
                RecordAppliedCategoryState(categoryName, stateName);
            }
        }
    }

    // Finds the category whose key equals the first prefixLength characters of text, the same match
    // as mCategories.TryGetValue(text.Substring(0, prefixLength)) without building that string.
    // Forms sets "<Category>State" on every hover and press, so the Substring was a per-change allocation.
    private bool TryGetCategoryByPrefix(string text, int prefixLength,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? categoryName,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out StateSaveCategory? category)
    {
        foreach (KeyValuePair<string, StateSaveCategory> pair in mCategories)
        {
            string key = pair.Key;
            if (key.Length == prefixLength && string.CompareOrdinal(key, 0, text, 0, prefixLength) == 0)
            {
                categoryName = key;
                category = pair.Value;
                return true;
            }
        }
        categoryName = null;
        category = null;
        return false;
    }

    /// <summary>Greater than zero while <c>SetGraphicalUiElement</c> is building an element on this thread.</summary>
    [System.ThreadStatic]
    internal static int BuildDepth;

    private void RecordAppliedCategoryState(string categoryName, string stateName)
    {
        _appliedCategoryStates ??= new Dictionary<string, string>();
        bool sameAsBefore = _appliedCategoryStates.TryGetValue(categoryName, out string? before) && before == stateName;
        _appliedCategoryStates[categoryName] = stateName;
        // A state applied before the instance joined its container, or while an element is still being built
        // (a Forms control setting its ScrollBar's orientation), is part of the build, which the container's values then override.
        // Applying the same state again later (a control refreshing its orientation) is not a new decision.
        _buildTimeCategoryStates ??= new HashSet<string>();
        if (ElementGueContainingThis == null || BuildDepth > 0) { _buildTimeCategoryStates.Add(categoryName); }
        else if (!sameAsBefore) { _buildTimeCategoryStates.Remove(categoryName); }
    }

    /// <summary>
    /// Re-applies, on this element and its descendants, the state last applied in each category.
    /// States are looked up by name in the current categories, so a state the edited file changed
    /// applies its new values, and a state or category that no longer exists is skipped.
    /// </summary>
    internal void ReapplyAppliedCategoryStatesRecursive(bool skipCodeCreated = false)
    {
        for (int i = 0; i < Children.Count; i++)
        {
            Children[i].ReapplyAppliedCategoryStatesRecursive(skipCodeCreated);
        }

        // A visual made in code (no element, no instance) took its states from code, which a reload does not touch.
        // Re-applying one would replace the values its container gave it (a ScrollViewer sizing its bars), and no
        // project value can have changed for it (#8130, #8131).
        bool isCodeCreated = skipCodeCreated && ElementSave == null && Tag is not InstanceSave;
        if (_appliedCategoryStates != null && !isCodeCreated)
        {
            foreach (KeyValuePair<string, string> applied in _appliedCategoryStates.ToArray())
            {
                // Re-applying is not a new decision about the state, so a build-time state stays one.
                bool isBuildTime = _buildTimeCategoryStates?.Contains(applied.Key) == true;
                ApplyState(applied.Key, applied.Value);
                if (isBuildTime) { _buildTimeCategoryStates!.Add(applied.Key); }
                ReassertContainerOverrides(applied.Key, applied.Value);
            }
        }
    }

    // A state applied by name overwrites the values the containing elements gave this instance (a ListBox
    // sizing its ScrollBar, a screen sizing the ListBox). Building applies the state first and the
    // containers' values after it, so re-applying the state must put those values back, innermost
    // container first so the outermost one wins.
    private HashSet<string>? _buildTimeCategoryStates;

    private void ReassertContainerOverrides(string categoryName, string stateName)
    {
        if (Tag is not InstanceSave instance
            || _buildTimeCategoryStates?.Contains(categoryName) != true
            || !mCategories.TryGetValue(categoryName, out StateSaveCategory? category)
            || category.States.FindByName(stateName) is not { } state)
        {
            return;
        }

        string path = instance.Name;
        GraphicalUiElement? container = ElementGueContainingThis;
        ElementSave? containerElement = container?.ElementSave ?? instance.ParentContainer;
        while (containerElement != null)
        {
            foreach (VariableSave variable in state.Variables)
            {
                if (variable.SetsValue && variable.Value != null
                    && FindExplicitValue(containerElement, path + "." + variable.Name) is { } overridden)
                {
                    SetProperty(variable.Name, overridden);
                }
            }

            if (container?.Tag is not InstanceSave containerInstance)
            {
                break;
            }
            path = containerInstance.Name + "." + path;
            container = container.ElementGueContainingThis;
            containerElement = container?.ElementSave ?? containerInstance.ParentContainer;
        }
    }

    // Only a value an element (or one of its base types) writes for the path counts as an override. The
    // instance's own base component default, which GetValueRecursive falls back to, is not one: putting it
    // back would replace what the instance's state set (a standalone ScrollBar's Vertical Height).
    private static object? FindExplicitValue(ElementSave element, string name)
    {
        for (ElementSave? current = element; current != null; current = ObjectFinder.Self.GetBaseElement(current))
        {
            foreach (VariableSave variable in current.DefaultState?.Variables ?? new List<VariableSave>())
            {
                if (variable.SetsValue && variable.Value != null && variable.Name == name)
                {
                    return variable.Value;
                }
            }
        }
        return null;
    }

    HashSet<StateSave> statesInStack = new HashSet<StateSave>();
    public virtual void ApplyState(DataTypes.Variables.StateSave state)
    {
        if (statesInStack.Contains(state))
        {
            return; // don't do anything, this would cause infinite recursion
        }
#if FULL_DIAGNOSTICS
        // Dynamic states can be applied in code. It is cumbersome for the user to
        // specify the ParentContainer, especially if the state is to be reused. 
        // I'm removing this to see if it causes problems:
        //if (state.ParentContainer == null)
        //{
        //    throw new InvalidOperationException("State.ParentContainer is null - did you remember to initialize the state?");
        //}
#endif
        statesInStack.Add(state);
        _stateBatchDepth++;
        try
        {

            if (state.Apply != null)
            {
                state.Apply();
            }
            else
            {
                ApplyStateVariables(state);
            }

        }
        finally
        {
            statesInStack.Remove(state);
            _stateBatchDepth--;
        }

        if (_stateBatchDepth == 0)
        {
            FlushPendingReferenceChanges();
        }

    }

    // Kept out of ApplyState so the closure capturing state is allocated only on this path; a state
    // with an Apply delegate (every Forms control's default visual) then allocates nothing.
    private void ApplyStateVariables(DataTypes.Variables.StateSave state)
    {
        bool didSuspend = false;
        // Also check this.IsLayoutSuspended: a state's own Variables can include a category-state
        // assignment (e.g. "ButtonCategoryState" = "Highlighted"), which TrySetValueOnThis resolves
        // by calling ApplyState AGAIN on this same instance, nested inside this loop. Without this
        // check, that nested call would see IsAllLayoutSuspended still false, suspend/resume on its
        // own, and its ResumeLayout would prematurely clear mIsLayoutSuspended -- and flush any
        // deferred font load -- before THIS (outer) call finishes applying its own remaining
        // variables (#4567). Skipping suspend/resume when already suspended lets the nested call's
        // variables apply under the outer suspension, so only the outermost ApplyState flushes.
        if (GraphicalUiElement.IsAllLayoutSuspended == false && this.IsLayoutSuspended == false)
        {
            didSuspend = true;
            this.SuspendLayout(true);
        }

        // SuspendLayout(true) suspended this element and every descendant. If a variable throws
        // (a custom setter, an event handler), none of them may stay suspended: every later
        // UpdateLayout early-outs for a suspended element, so it would never lay out again.
        bool appliedAllVariables = false;
        ApplyStateScratch scratch = ApplyStateScratch.Rent();
        try
        {
            List<VariableSave> ordered = scratch.Ordered;
            FillInApplyOrder(state, scratch);

            int variableCount = ordered.Count;
            for (int i = 0; i < variableCount; i++)
            {
                var variable = ordered[i];
                if (variable.Value != null)
                {
                    this.SetProperty(variable.Name, variable.Value);
                }
            }

            foreach (var variableList in state.VariableLists)
            {
                this.SetProperty(variableList.Name, variableList.ValueAsIList);
            }

            appliedAllVariables = true;
        }
        finally
        {
            ApplyStateScratch.Return(scratch);
            if (didSuspend)
            {
                if (appliedAllVariables)
                {
                    this.ResumeLayout(true);
                }
                else
                {
                    // Lift the suspension without flushing: the flush would run layout (and the same
                    // throwing setter) while the original exception is unwinding. The dirty state is
                    // kept, so the next layout of these elements catches up.
                    this.ClearLayoutSuspensionRecursive();
                }
            }
        }
    }

    // Fills scratch.Ordered with the state's value-setting variables in the order they apply: Parent
    // variables first, by the index of the instance they move (stable), so children are added in
    // instance order; then every other variable once (a variable object listed twice applies at its
    // first position), with category-state variables ahead of the rest (stable), so a plain variable
    // overrides what a state it is listed with sets.
    private void FillInApplyOrder(StateSave state, ApplyStateScratch scratch)
    {
        List<VariableSave> ordered = scratch.Ordered;
        List<int> parentKeys = scratch.ParentKeys;
        List<VariableSave> variables = state.Variables;

        for (int i = 0; i < variables.Count; i++)
        {
            VariableSave variable = variables[i];
            if (variable.SetsValue && variable.GetRootName() == "Parent")
            {
                // Insertion sort keeps equal keys in list order, as OrderBy did. Each key is an
                // O(instances) scan, so it is computed once and kept beside its variable.
                int key = GetOrderedIndexForParentVariable(variable);
                int insertAt = ordered.Count;
                while (insertAt > 0 && parentKeys[insertAt - 1] > key)
                {
                    insertAt--;
                }
                ordered.Insert(insertAt, variable);
                parentKeys.Insert(insertAt, key);
            }
        }

        // Even though state-setting variables on the parent are allowed, states on the contained
        // objects are set too; both go before the plain variables.
        ElementSave? container = state.ParentContainer;
        List<VariableSave> plain = scratch.Plain;
        HashSet<VariableSave> seen = scratch.Seen;
        for (int i = 0; i < variables.Count; i++)
        {
            VariableSave variable = variables[i];
            if (!variable.SetsValue || variable.GetRootName() == "Parent" || !seen.Add(variable))
            {
                continue;
            }
            if (container != null && variable.IsState(container))
            {
                ordered.Add(variable);
            }
            else
            {
                plain.Add(variable);
            }
        }

        for (int i = 0; i < plain.Count; i++)
        {
            ordered.Add(plain[i]);
        }
    }

    // ApplyState re-enters through a state variable (a category state that sets another category
    // state), so each nesting level takes its own scratch. ThreadStatic because screens can be
    // built on a loading thread while the game thread changes states.
    private sealed class ApplyStateScratch
    {
        public readonly List<VariableSave> Ordered = new List<VariableSave>();
        public readonly List<VariableSave> Plain = new List<VariableSave>();
        public readonly List<int> ParentKeys = new List<int>();
        public readonly HashSet<VariableSave> Seen = new HashSet<VariableSave>();

        [ThreadStatic]
        private static List<ApplyStateScratch>? _pool;
        [ThreadStatic]
        private static int _depth;

        public static ApplyStateScratch Rent()
        {
            List<ApplyStateScratch> pool = _pool ??= new List<ApplyStateScratch>();
            if (_depth == pool.Count)
            {
                pool.Add(new ApplyStateScratch());
            }
            return pool[_depth++];
        }

        public static void Return(ApplyStateScratch scratch)
        {
            // Cleared so the pool does not keep a removed element's variables alive.
            scratch.Ordered.Clear();
            scratch.Plain.Clear();
            scratch.ParentKeys.Clear();
            scratch.Seen.Clear();
            _depth--;
        }
    }

    public void ApplyState(List<DataTypes.Variables.VariableSaveValues> variableSaveValues)
    {
        // Same rule as ApplyState(StateSave): under an outer suspension, leave the flush to it.
        bool didSuspend = false;
        if (GraphicalUiElement.IsAllLayoutSuspended == false && this.IsLayoutSuspended == false)
        {
            didSuspend = true;
            this.SuspendLayout(true);
        }

        bool appliedAllVariables = false;
        try
        {
            foreach (var variable in variableSaveValues)
            {
                if (variable.Value != null)
                {
                    this.SetProperty(variable.Name, variable.Value);
                }
            }

            appliedAllVariables = true;
        }
        finally
        {
            if (didSuspend)
            {
                if (appliedAllVariables)
                {
                    this.ResumeLayout(true);
                }
                else
                {
                    // See ApplyState(StateSave): lift the suspension without flushing while unwinding.
                    this.ClearLayoutSuspensionRecursive();
                }
            }
        }
    }


    public void SetGueValues(IVariableFinder rvf)
    {

        this.SuspendLayout();

        this.Width = rvf.GetValue<float>("Width");
        this.Height = rvf.GetValue<float>("Height");

        this.HeightUnits = rvf.GetValue<DimensionUnitType>("HeightUnits");
        this.WidthUnits = rvf.GetValue<DimensionUnitType>("WidthUnits");

        this.XOrigin = rvf.GetValue<HorizontalAlignment>("XOrigin");
        this.YOrigin = rvf.GetValue<VerticalAlignment>("YOrigin");

        this.X = rvf.GetValue<float>("X");
        this.Y = rvf.GetValue<float>("Y");

        this.XUnits = UnitConverter.ConvertToGeneralUnit(rvf.GetValue<PositionUnitType>("XUnits"));
        this.YUnits = UnitConverter.ConvertToGeneralUnit(rvf.GetValue<PositionUnitType>("YUnits"));

        this.TextureWidth = rvf.GetValue<int>("TextureWidth");
        this.TextureHeight = rvf.GetValue<int>("TextureHeight");
        this.TextureLeft = rvf.GetValue<int>("TextureLeft");
        this.TextureTop = rvf.GetValue<int>("TextureTop");

        this.TextureWidthScale = rvf.GetValue<float>("TextureWidthScale");
        this.TextureHeightScale = rvf.GetValue<float>("TextureHeightScale");

        this.Wrap = rvf.GetValue<bool>("Wrap");

        this.TextureAddress = rvf.GetValue<TextureAddress>("TextureAddress");

        this.ChildrenLayout = rvf.GetValue<ChildrenLayout>("ChildrenLayout");
        this.WrapsChildren = rvf.GetValue<bool>("WrapsChildren");
        this.ClipsChildren = rvf.GetValue<bool>("ClipsChildren");

        if (this.ElementSave != null)
        {
            foreach (var category in ElementSave.Categories)
            {
                string? valueOnThisState = rvf.GetValue<string>(category.Name + "State");

                if (!string.IsNullOrEmpty(valueOnThisState))
                {
                    this.ApplyState(valueOnThisState);
                }
            }
        }

        this.ResumeLayout();
    }

    #endregion
}
