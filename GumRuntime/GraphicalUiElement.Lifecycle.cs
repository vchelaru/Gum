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

    public bool IsFullyCreated { get; private set; }
    /// <summary>
    /// Method which is called after a control is fully created when it is created from a FrameworkElement
    /// when ToGraphicalUiElement or SetGraphicalUiElement are called. 
    /// </summary>
    public virtual void AfterFullCreation()
    {
        IsFullyCreated = true;
    }

    /// <summary>
    /// Sets the default state.
    /// </summary>
    /// <remarks>
    /// This function is virtual so that derived classes can override it
    /// and provide a quicker method for setting default states
    /// </remarks>
    public virtual void SetInitialState()
    {
        var elementSave = this.Tag as ElementSave ?? this.ElementSave;
        // An element created in code has no ElementSave, so there is no initial state to apply.
        if (elementSave != null)
        {
            this.SetVariablesRecursively(elementSave, elementSave.DefaultState!);
        }
    }

    /// <summary>
    /// Optional delegate called by <see cref="RefreshStyles"/> before re-applying
    /// states, allowing Forms controls to save runtime property values (such as
    /// text content and caret position) that would be lost during state re-application.
    /// Wired by MonoGameGum to call <c>FrameworkElement.SaveRuntimeProperties()</c>.
    /// </summary>
    public static Action<object>? SaveFormsRuntimePropertiesAction;

    /// <summary>
    /// Optional delegate called by <see cref="RefreshStyles"/> to re-apply
    /// the current Forms visual state on an element. Wired by MonoGameGum
    /// to call <c>FrameworkElement.UpdateState()</c> and
    /// <c>FrameworkElement.ApplyRuntimeProperties()</c> since GumRuntime cannot
    /// reference Forms types directly.
    /// </summary>
    public static Action<object>? UpdateFormsStateAction;

    /// <summary>
    /// Re-applies all default state values and current Forms visual states
    /// recursively on this element and all children. Call this after modifying
    /// variable values on ElementSave states (e.g., after
    /// <see cref="GumRuntime.ElementSaveExtensions.ApplyAllVariableReferences"/>)
    /// to push those changes to the live visual tree.
    /// </summary>
    public void RefreshStyles()
    {
        bool didSuspend = false;
        if (!IsAllLayoutSuspended)
        {
            IsAllLayoutSuspended = true;
            didSuspend = true;
        }

        // Three-pass approach:
        // 1. Save runtime properties (text, caret, scroll position, etc.)
        SaveFormsRuntimePropertiesRecursive();
        // 2. Re-apply all states (default + categorical)
        RefreshStylesRecursive();
        // 3. Restore runtime properties on top
        RestoreFormsRuntimePropertiesRecursive();

        if (didSuspend)
        {
            IsAllLayoutSuspended = false;
            this.UpdateLayout();
        }
    }

    private void SaveFormsRuntimePropertiesRecursive()
    {
        if (this is InteractiveGue interactive && interactive.FormsControlAsObject != null)
        {
            SaveFormsRuntimePropertiesAction?.Invoke(interactive.FormsControlAsObject);
        }
        for (int i = 0; i < Children.Count; i++)
        {
            Children[i].SaveFormsRuntimePropertiesRecursive();
        }
    }

    private void RestoreFormsRuntimePropertiesRecursive()
    {
        if (this is InteractiveGue interactive && interactive.FormsControlAsObject != null)
        {
            UpdateFormsStateAction?.Invoke(interactive.FormsControlAsObject);
        }
        for (int i = 0; i < Children.Count; i++)
        {
            Children[i].RestoreFormsRuntimePropertiesRecursive();
        }
    }

    private void RefreshStylesRecursive()
    {
        // Children first — each child re-applies its own component defaults
        for (int i = 0; i < Children.Count; i++)
        {
            Children[i].RefreshStylesRecursive();
        }

        // Re-apply this element's default state (includes instance-qualified
        // variables that override child properties)
        var elementSave = this.Tag as ElementSave ?? this.ElementSave;
        if (elementSave != null)
        {
            this.SetVariablesRecursively(elementSave, elementSave.DefaultState!);
        }
    }

    /// <summary>
    /// Optional delegate that re-translates the <c>Text</c> property on a single
    /// element using the most recently assigned localization key, if any. Wired
    /// by MonoGameGum since GumRuntime cannot reference
    /// <c>CustomSetPropertyOnRenderable</c> directly. Used by
    /// <c>GumService.RefreshLocalization</c>.
    /// </summary>
    public static Action<GraphicalUiElement>? RefreshLocalizationOnElementAction;

    /// <summary>
    /// Optional lookup for the original (untranslated) string ID last assigned to this element's
    /// "Text" property via the localized path, if any. Wired by the tool/runtime since GumRuntime
    /// cannot reference <c>CustomSetPropertyOnRenderable</c> directly. Used by callers (e.g. the
    /// Gum tool's <c>WireframeObjectManager.ApplyLocalization</c>) that need to re-translate an
    /// element without re-translating its already-translated live text.
    /// </summary>
    public static Func<GraphicalUiElement, string?>? TryGetLocalizationKey;

    /// <summary>
    /// Optional hook to enable/disable translation for every subsequent <c>SetProperty("Text",
    /// ...)</c> call (the tool's design-time "show localized text" preview toggle). Wired by the
    /// tool since GumRuntime cannot reference <c>CustomSetPropertyOnRenderable</c> directly - the
    /// wired implementation swaps its static <c>LocalizationService</c> between the real,
    /// database-populated instance and null. Passing false must be indistinguishable from no
    /// localization database ever having been loaded (raw string IDs display unchanged); passing
    /// true restores translation.
    /// </summary>
    public static Action<bool>? SetLocalizationEnabled;

    /// <summary>
    /// Re-applies the most recently assigned localization key on this element
    /// and all descendants via <see cref="RefreshLocalizationOnElementAction"/>.
    /// Each element that had its <c>Text</c> set via the localization path
    /// re-runs <c>SetProperty("Text", key)</c>, which routes through translation
    /// again with the current language.
    /// </summary>
    /// <remarks>
    /// Elements whose text was assigned via <c>SetTextNoTranslate</c> (e.g. user
    /// input in a <c>TextBox</c>) are skipped. Bound Text values may be overwritten
    /// — refresh while bindings are active is not supported.
    /// </remarks>
    public void RefreshLocalization()
    {
        RefreshLocalizationOnElementAction?.Invoke(this);
        for (int i = 0; i < Children.Count; i++)
        {
            Children[i].RefreshLocalization();
        }
    }

    string NameOrType => !string.IsNullOrEmpty(Name) ? Name : $"<{GetType().Name}>";

    string ParentQualifiedName => Parent as GraphicalUiElement == null ? NameOrType : (Parent as GraphicalUiElement).ParentQualifiedName + "." + NameOrType;

    public static bool AreUpdatesAppliedWhenInvisible { get; set; } = false;

    public virtual void PreRender()
    {
        if (mContainedObjectAsIpso != null)
        {
            mContainedObjectAsIpso.PreRender();
        }
    }
}
