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
            return TrySetCustomVariableOnFormsControl(propertyName, value);
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
                string instanceName = underlyingProperty.Substring(0, indexOfDot);
                GraphicalUiElement? containedGue = GetGraphicalUiElementByName(instanceName);
                string variable = underlyingProperty.Substring(indexOfDot + 1);

                // Children may not have been created yet
                if (containedGue != null)
                {
                    containedGue.SetProperty(variable, value);
                }
            }
        }
        else if (ToolsUtilities.StringFunctions.ContainsNoAlloc(propertyName, '.'))
        {
            int indexOfDot = propertyName.IndexOf('.');
            string instanceName = propertyName.Substring(0, indexOfDot);
            GraphicalUiElement? containedGue = GetGraphicalUiElementByName(instanceName);
            string variable = propertyName.Substring(indexOfDot + 1);

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

                    string nameWithoutState = propertyName.Substring(0, propertyName.Length - "State".Length);

                    if (string.IsNullOrEmpty(nameWithoutState))
                    {
                        // This is an uncategorized state
                        if (mStates.ContainsKey(valueAsString))
                        {
                            ApplyState(mStates[valueAsString]);
                            toReturn = true;
                        }
                    }
                    else if (mCategories.ContainsKey(nameWithoutState))
                    {

                        var category = mCategories[nameWithoutState];

                        var state = category.States.FirstOrDefault(item => item.Name == valueAsString);
                        if (state != null)
                        {
                            ApplyState(state);
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

            var state = category.States.FirstOrDefault(item => item.Name == stateName);
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
            var foundState = category.States.FirstOrDefault(item => item.Name == name);

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

            var state = category.States.FirstOrDefault(item => item.Name == stateName);

            if (state != null)
            {
                ApplyState(state);
            }
        }
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

        if (state.Apply != null)
        {
            state.Apply();
        }
        else
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

            var variablesWithoutStatesOnParent =
                state.Variables.Where(item =>
                {
                    if (item.SetsValue)
                    {
                        // We can set the variable if it's not setting a state (to prevent recursive setting).
                        // Update May 4, 2023 - But if you have a base element that defines a state, and the derived
                        // element sets that state, then we want to allow it.  But should we just allow all states?
                        // Or should we check if it's defined by the base...
                        //return (item.IsState(state.ParentContainer) == false ||
                        //    // If it is setting a state we'll allow it if it's on a child.
                        //    !string.IsNullOrEmpty(item.SourceObject));
                        // let's test this out:
                        return true;

                    }
                    return false;
                }).ToArray();


            var parentSettingVariables =
                variablesWithoutStatesOnParent
                    .Where(item => item.GetRootName() == "Parent")
                    .OrderBy(item => GetOrderedIndexForParentVariable(item))
                    .ToArray();

            var nonParentSettingVariables =
                variablesWithoutStatesOnParent
                    .Except(parentSettingVariables)
                    // Even though we removed state-setting variables on the parent, we still allow setting
                    // states on the contained objects
                    .OrderBy(item => state.ParentContainer == null || !item.IsState(state.ParentContainer))
                    .ToArray();

            var variablesToConsider =
                parentSettingVariables.Concat(nonParentSettingVariables)
                .ToArray();

            int variableCount = variablesToConsider.Length;
            for (int i = 0; i < variableCount; i++)
            {
                var variable = variablesToConsider[i];
                if (variable.SetsValue && variable.Value != null)
                {
                    this.SetProperty(variable.Name, variable.Value);
                }
            }

            foreach (var variableList in state.VariableLists)
            {
                this.SetProperty(variableList.Name, variableList.ValueAsIList);
            }

            if (didSuspend)
            {
                this.ResumeLayout(true);

            }
        }

        statesInStack.Remove(state);

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

        foreach (var variable in variableSaveValues)
        {
            if (variable.Value != null)
            {
                this.SetProperty(variable.Name, variable.Value);
            }
        }

        if (didSuspend)
        {
            this.ResumeLayout(true);
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
