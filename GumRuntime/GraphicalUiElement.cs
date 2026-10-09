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

#region Enums

public enum MissingFileBehavior
{
    ConsumeSilently,
    ThrowException
}

public enum Anchor
{
    TopLeft,
    Top,
    TopRight,
    Left,
    Center,
    Right,
    BottomLeft,
    Bottom,
    BottomRight,
    CenterHorizontally,
    CenterVertically
}

public enum Dock
{
    Top,
    Left,
    Fill,
    Right,
    Bottom,
    FillHorizontally,
    FillVertically,
    SizeToChildren
}

#endregion

/// <summary>
/// The base object for all Gum runtime objects. It contains functionality for
/// setting variables, states, and performing layout. The GraphicalUiElement can
/// wrap an underlying rendering object.
/// GraphicalUiElements are also considered "Visuals" for Forms objects such as Button and TextBox.
/// </summary>
/// <remarks>
/// The type annotation makes the trimmer keep the public properties of every derived runtime type,
/// including the ones the Gum tool generates. Binding and state application resolve those properties
/// by name through reflection, so they would silently stop working if they were trimmed.
/// </remarks>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public partial class GraphicalUiElement : IRenderableIpso, IVisible, INotifyPropertyChanged, IHasRenderableComponent
{
    #region Enums/Internal Classes

    enum ChildType
    {
        Absolute = 1,
        Relative = 1 << 1,
        BothAbsoluteAndRelative = Absolute | Relative,
        StackedWrapped = 1 << 2,
        All = Absolute | Relative | StackedWrapped
    }

    class DirtyState
    {
        // Indicates situations where a parent should be updated. If None is specified, then only this object
        // is updated and its parent is not.
        public ParentUpdateType ParentUpdateType;
        public int ChildrenUpdateDepth;
        public XOrY? XOrY;
    }

    public enum ParentUpdateType
    {
        None = 0,
        IfParentStacks = 1,
        IfParentWidthHeightDependOnChildren = 2,
        IfParentIsAutoGrid = 4,
        IfParentHasRatioSizedChildren = 8,
        All = 16

    }

    #endregion

    #region Fields

    public static float GlobalFontScale = 1;

    private DirtyState? currentDirtyState;
    private ParentUpdateType EffectiveDirtyStateParentUpdateType
    {
        get
        {
            var toReturn = currentDirtyState?.ParentUpdateType ?? ParentUpdateType.None;

            if(GetIfParentHasRatioChildren())
            {
                toReturn = toReturn | ParentUpdateType.IfParentHasRatioSizedChildren;
            }

            if(GetIfParentStacks())
            {
                toReturn = toReturn | ParentUpdateType.IfParentStacks;
            }
            if(GetIfParentWidthHeightDependOnChildren())
            {
                toReturn = toReturn | ParentUpdateType.IfParentWidthHeightDependOnChildren;
            }
            return toReturn;
        }
    }
    // Deferred font loading flag. Set to true when a font-related property is changed while
    // layout is suspended (see UpdateToFontValues). Cleared once the actual font load runs.
    // Two code paths consume this flag and perform the real load:
    //   1. WireframeObjectManager calls RootGue.UpdateFontRecursive() after IsAllLayoutSuspended = false.
    //   2. ResumeLayoutUpdateIfDirtyRecursive() calls UpdateFontRecursive() when instance-level
    //      suspension is lifted via ResumeLayout(recursive: true).
    // NOTE: the set-by-string path (SetProperty -> CustomSetPropertyOnRenderable.UpdateToFontValues)
    // only defers for IsAllLayoutSuspended, not for IsLayoutSuspended. See the comments in
    // CustomSetPropertyOnRenderable.UpdateToFontValues for why.
    bool isFontDirty = false;
    public bool IsFontDirty
    {
        get => isFontDirty;
        internal set => isFontDirty = value;
    }

    // Set true while UpdateLayout realizes a deferred font (see the isFontDirty flush in
    // UpdateLayout). While set, the font assignment in CustomSetPropertyOnRenderable skips the
    // UpdateLayout call it would otherwise make for RelativeToChildren text — the in-progress
    // layout pass already sizes the element, so that extra call would be redundant (and, because
    // a no-arg UpdateLayout requests a parent update, would re-enter the current pass). See #2999.
    internal static bool SuppressLayoutFromFontChange = false;

    /// <summary>
    /// While true, <c>CustomSetPropertyOnRenderable</c>'s font-value update (bitmap font
    /// resolution/regeneration, including KernSmith dropshadow baking) is skipped entirely on every
    /// font-property assignment (both the direct-setter and string <c>SetProperty</c> paths). Set by
    /// the tool's Variable Grid while a numeric drag-scrub is mid-gesture (<c>VariablePropertyCommitType
    /// .Intermediate</c>) so a continuous drag doesn't regenerate a font file per tick; cleared before
    /// the final committed <c>SetProperty</c> call so that one performs the real (and only) generation.
    /// Public (not internal, unlike the sibling flags above) because the tool code that sets it lives
    /// in the separate Gum.Presentation assembly.
    /// </summary>
    public static bool SuppressFontRegeneration = false;

    /// <summary>
    /// The total number of layout calls that have been performed since the application has started running.
    /// This value can be used as a rough indication of the layout cost and to measure whether efforts to reduce
    /// layout calls have been effective.
    /// </summary>
    public static int UpdateLayoutCallCount;
    public static int ChildrenUpdatingParentLayoutCalls;

    // This used to be true until Jan 26, 2024, but it's
    // confusing for new users. Let's keep this off and document
    // how to use it (eventually).
    public static bool ShowLineRectangles = false;

    // to save on casting:
    protected IRenderableIpso? mContainedObjectAsIpso;
    protected IVisible? mContainedObjectAsIVisible;

    // Rendering and layout sizing of this element's own renderable only happen when it has one.
    // A GraphicalUiElement with no contained renderable (such as a Screen) never reaches them.
    IRenderableIpso RequiredContainedObject => mContainedObjectAsIpso ??
        throw new InvalidOperationException("This GraphicalUiElement has not had its visual set, so it has no renderable. " +
            "This can happen if a GraphicalUiElement was added as a child without its contained renderable having been set.");

    GraphicalUiElement? mWhatContainsThis;

    /// <summary>
    /// A flat list of all GraphicalUiElements contained by this element. For example, if this GraphicalUiElement
    /// is a Screen, this list is all GraphicalUielements for every instance contained regardless of hierarchy.
    /// </summary>
    List<GraphicalUiElement> mWhatThisContains = new List<GraphicalUiElement>();

    protected List<GraphicalUiElement> WhatThisContains => mWhatThisContains;

    Dictionary<string, string> mExposedVariables = new Dictionary<string, string>();

    GeneralUnitType mXUnits;
    GeneralUnitType mYUnits;
    HorizontalAlignment mXOrigin;
    VerticalAlignment mYOrigin;
    DimensionUnitType mWidthUnit;
    DimensionUnitType mHeightUnit;

    protected ISystemManagers? mManagers;

    // hack for FRB:
    [Obsolete("Don't use this, it exists only for FRB")]
    public void ClearManagers() => mManagers = null;

    int mTextureTop;
    int mTextureLeft;
    int mTextureWidth;
    int mTextureHeight;
    bool mWrap;

    bool mWrapsChildren = false;
    // The longest stacked line the last children-based measure found, before padding.
    float _measuredLineWidth;
    float _measuredLineHeight;

    float mTextureWidthScale = 1;
    float mTextureHeightScale = 1;

    TextureAddress mTextureAddress;

    float mX;
    float mY;
    // Since these are protected, we can't change them to _width and _height 
    // FRB already uses mWidth and mHeight in its codegen
    protected float mWidth;
    protected float mHeight;
    float mRotation;

    GraphicalUiElement? _parent;
    bool _isSettingParent;

    protected bool mIsLayoutSuspended = false;
    public bool IsLayoutSuspended => mIsLayoutSuspended;

    // We need ThreadStatic in case screens are being loaded
    // in the background - we don't want to interrupt the foreground
    // layout behavior.
    [ThreadStatic]
    public static bool IsAllLayoutSuspended = false;

    Dictionary<string, Gum.DataTypes.Variables.StateSave> mStates =
        new Dictionary<string, DataTypes.Variables.StateSave>();

    public Dictionary<string, Gum.DataTypes.Variables.StateSave> States => mStates;

    Dictionary<string, Gum.DataTypes.Variables.StateSaveCategory> mCategories =
        new Dictionary<string, Gum.DataTypes.Variables.StateSaveCategory>();

    // This needs to be made public so that individual Forms objects can be customized:
    public Dictionary<string, Gum.DataTypes.Variables.StateSaveCategory> Categories => mCategories;

    // the row or column index when anobject is sorted.
    // This is used by the stacking logic to properly sort objects
    public int StackedRowOrColumnIndex { get; set; } = -1;

    // Cached index of this element within its parent's Children list.
    // Set by UpdateChildren before each child's UpdateLayout call to avoid
    // an O(n) IndexOf lookup in GetWhatToStackAfter. A value of -1 means
    // unset; GetWhatToStackAfter will fall back to IndexOf in that case.
    private int _cachedSiblingIndex = -1;

    // The size of the wrapped line this was last positioned in, when its cross-axis position is
    // measured from that line (#5802); NaN otherwise. A line's size is final only after its last
    // child is measured, so the parent repositions this child when the line ends up a different size.
    private float _wrappedLineSizeUsedForPosition = float.NaN;

    // The cross-axis start of the wrapped line this was last positioned in, unflipped.
    private float _wrappedLineStart;

    // null by default, non-null if an object uses
    // stacked layout for its children.
    public List<float>? StackedRowOrColumnDimensions { get; private set; }

    // Custom variables that arrived before this had a Forms control to receive them.
    // See TrySetCustomVariableOnFormsControl.
    private Dictionary<string, object?>? _pendingCustomVariables;
    #endregion


    #region Events

    // It's possible that a size change could result in a layout which 
    // results in a further size change. This recursive call of size changes
    // could happen indefinitely so we only want to do this one time.
    // This prevents the size change from happening over and over:
    bool isInSizeChange;
    /// <summary>
    /// Event raised whenever this instance's absolute size changes. This size change can occur by a direct value being
    /// set (such as Width or WidthUnits), or by an indirect value changing, such as if a Parent is resized and if
    /// this uses a WidthUnits depending on the parent.
    /// </summary>
    public event EventHandler? SizeChanged;
    public event EventHandler? PositionChanged;
    public event EventHandler? VisibleChanged;
    public event EventHandler<ParentChangedEventArgs>? ParentChanged;

    public class ParentChangedEventArgs
    {
        public IRenderableIpso? OldValue { get; set; }
        public IRenderableIpso? NewValue { get; set; }
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null!)
    {
        if (PropertyChanged != null)
        {
            var args = new PropertyChangedEventArgs(propertyName);
            PropertyChanged(this, args);
        }
    }

    public static Action<IText, GraphicalUiElement>? UpdateFontFromProperties;
    public static Action<GraphicalUiElement>? ThrowExceptionsForMissingFiles;
    public static Action<IRenderableIpso, ISystemManagers>? RemoveRenderableFromManagers;
    public static Action<IRenderableIpso, ISystemManagers, Layer?>? AddRenderableToManagers;
    public static Action<string, GraphicalUiElement>? ApplyMarkup;

    /// <summary>
    /// Applies constant RGBA pixel data (width*height*4 bytes) to the sprite renderable of the target
    /// as a texture shared/cached under the given key. For backgrounds identical across every instance
    /// (e.g. a color picker hue bar). Assigned per rendering backend; null on backends that don't support it.
    /// </summary>
    public static Action<GraphicalUiElement, string, byte[], int, int>? ApplyCachedTextureFromPixelData;

    /// <summary>
    /// Applies changing RGBA pixel data (width*height*4 bytes) to the sprite renderable of the target
    /// using a texture pooled per owner (second argument). Pooled textures whose owner has detached from
    /// the visual tree are reclaimed, so repeated create/destroy cycles reuse a bounded set. For per-instance
    /// procedural backgrounds (e.g. a color picker saturation/value square). Assigned per rendering backend.
    /// </summary>
    public static Action<GraphicalUiElement, GraphicalUiElement, byte[], int, int>? ApplyPooledTextureFromPixelData;

    // bool return: whether the assignment was actually handled, so SetProperty can fall back to
    // TrySetCustomVariableOnThis when nothing along this dispatch claims the name (issue #4891).
    public static Func<IRenderableIpso, GraphicalUiElement, string, object?, bool> SetPropertyOnRenderable =
        // This is the default fallback to make Gum work. Specific rendering libraries can change this to provide
        // better performance.
        SetPropertyThroughReflection;

    public static Func<IRenderable, IRenderable>? CloneRenderableFunction;

    /// <summary>
    /// Resolves the popup and modal root containers used when showing a popup (for example a
    /// ComboBox or MenuItem dropdown), keyed off the element being shown. When null, callers
    /// fall back to their own default global popup/modal roots. Override this to support
    /// multiple popup/modal root pairs, for example one pair per camera/viewport in a host
    /// that embeds several independent Gum viewports.
    /// A resolved root only receives content — it is the caller's responsibility to also add
    /// it to whatever list drives input dispatch (the same mechanism used for any other root).
    /// To also opt into Gum's automatic z-order raising, canvas sizing, and modal-exclusivity
    /// handling for a resolved pair (the behavior Gum's input loop otherwise only applies to the
    /// global default popup/modal roots), register the pair in
    /// FrameworkElement.AdditionalPopupRootPairs — see that property's docs for the canvas-sizing
    /// tradeoff a per-camera root should consider before opting in.
    /// </summary>
    public static Func<GraphicalUiElement, (InteractiveGue popup, InteractiveGue modal)>? ResolvePopupRoots;


    #endregion

    #region Constructor / Clone

    public GraphicalUiElement()
        : this(null, null)
    {
        mIsLayoutSuspended = true;
        Width = 32;
        Height = 32;
        mIsLayoutSuspended = false;
    }

    public GraphicalUiElement(IRenderable? containedObject, GraphicalUiElement? whatContainsThis = null)
    {
        mIsLayoutSuspended = true;
        Width = 32;
        Height = 32;
#if FULL_DIAGNOSTICS
        if (containedObject is GraphicalUiElement)
        {
            throw new InvalidOperationException("GraphicalUiElements cannot contain other GraphicalUiElements as their renderable. " +
                $"The contained object should be a renderable, such as a (platform specific) Sprite or Text. " +
                $"It cannot be {containedObject.GetType()}");
        }
#endif
        SetContainedObject(containedObject);

        mWhatContainsThis = whatContainsThis;
        if (mWhatContainsThis != null)
        {
            mWhatContainsThis.mWhatThisContains.Add(this);

            // I don't think we want to do this. 
            if (mWhatContainsThis.mContainedObjectAsIpso != null)
            {
                this.Parent = mWhatContainsThis;
            }
        }

        mIsLayoutSuspended = false;
        // This is a bit of a hack to support GraphicalUiElement.IWindow.
        // This isn't needed in MonoGame:
        OnConstructor();
    }

    partial void OnConstructor();

    // Instances held through ElementGueContainingThis while this element had no renderable are not
    // reparented when a renderable is assigned here: they keep a null Parent, stay out of Children,
    // and keep laying out against the canvas. This is intended (#5772). Only code-only construction
    // reaches this; project load, generated code and FRB all assign the renderable before instances.
    public void SetContainedObject(IRenderable? containedObject)
    {
        if (containedObject == this)
        {
            throw new ArgumentException("The argument containedObject cannot be 'this'");
        }

        mContainedObjectAsIpso = containedObject as IRenderableIpso;

        if (mContainedObjectAsIpso == null)
        {
            _childrenWrapper = GraphicalUiElementCollection.Empty;
        }
        else
        {
            _childrenWrapper = new GraphicalUiElementCollection(mContainedObjectAsIpso.Children);
            _childrenWrapper.CollectionChanged += HandleCollectionChanged;
        }

        mContainedObjectAsIVisible = containedObject as IVisible;

        if (mContainedObjectAsIpso != null)
        {
            mContainedObjectAsIpso.Name ??= name;
            name = mContainedObjectAsIpso.Name;
        }

        // in case this had been changed before the Text was assigned, or in case the text
        // default differs.
        if (containedObject is IText asText)
        {
            asText.TextOverflowVerticalMode = this.TextOverflowVerticalMode;
        }

        if (containedObject != null)
        {
            UpdateLayout();
        }
    }

    public virtual void CreateChildrenRecursively(ElementSave elementSave, ISystemManagers systemManagers)
    {
        bool isScreen = elementSave is ScreenSave;

        foreach (var instance in elementSave.Instances)
        {
            var childGue = instance.ToGraphicalUiElement(systemManagers);

            if (childGue != null)
            {
                // As of November 22, 2024 we now add children
                // to Screen GraphicalUiElements to make Entities
                // and Screens consistent.
                if (!isScreen || this.Children != null)
                {
                    childGue.Parent = this;
                }
                childGue.ElementGueContainingThis = this;
            }
        }
    }

    /// <summary>
    /// Creates a copy of this element. The clone starts detached, with no children or parent. It gets its
    /// own copies of this element's property bindings, built from each binding's path and format, but not
    /// this element's <see cref="BindingContext"/>, so whoever places the clone supplies its context.
    /// </summary>
    public virtual GraphicalUiElement Clone()
    {

        IRenderable? clonedRenderable = (this.mContainedObjectAsIpso as ICloneable)?.Clone() as IRenderable;

        if (clonedRenderable == null)
        {
            if (CloneRenderableFunction == null)
            {
                throw new InvalidOperationException($"{this.mContainedObjectAsIpso?.GetType()} needs to implement ICloneable or " +
                    $"GraphicalUiElement.CloneRenderableFunction must be set before calling clone");
            }
            clonedRenderable = this.mContainedObjectAsIpso == null
                ? null
                : GraphicalUiElement.CloneRenderableFunction(this.mContainedObjectAsIpso);
        }

        GraphicalUiElement newClone = (GraphicalUiElement)this.MemberwiseClone();

        // MemberwiseClone copies the source's place in the hierarchy. The clone starts detached
        // with no children, matching the renderable clones: otherwise layout parents the clone's
        // renderable to the source's parent, and assigning that parent is a no-op.
        newClone._parent = null;
        newClone.mWhatContainsThis = null;
        newClone.mWhatThisContains = new List<GraphicalUiElement>();
        // The source's managers and layer describe where the source was added. Keeping them makes
        // AddToManagers treat the clone as already added, so it never reaches a layer.
        newClone.mManagers = null;
        newClone.mLayer = null;
        newClone.SetContainedObject(clonedRenderable);
        // Per-instance state gets its own copy, so changes to one element can't reach the other.
        // The clone keeps the source's pending layout, definitions and pending Forms values; the
        // scratch sets and row sizes are only meaningful mid-layout or for its own children.
        newClone.currentDirtyState = currentDirtyState == null ? null : new DirtyState
        {
            ParentUpdateType = currentDirtyState.ParentUpdateType,
            ChildrenUpdateDepth = currentDirtyState.ChildrenUpdateDepth,
            XOrY = currentDirtyState.XOrY
        };
        newClone.fullyUpdatedChildren = new HashSet<GraphicalUiElement>();
        newClone.statesInStack = new HashSet<StateSave>();
        newClone.StackedRowOrColumnDimensions = null;
        newClone._pendingCustomVariables = _pendingCustomVariables == null
            ? null
            : new Dictionary<string, object?>(_pendingCustomVariables);
        newClone.mExposedVariables = new Dictionary<string, string>(mExposedVariables);
        newClone.mStates = new Dictionary<string, StateSave>(mStates);
        newClone.mCategories = new Dictionary<string, StateSaveCategory>(mCategories);
#if !FRB
        newClone.Animations = Animations == null ? null : new List<AnimationRuntime>(Animations);
        newClone.AnimationController = new AnimationController();
#endif
        // The copied handlers belong to whoever subscribed to the source (its binding handler, its
        // Forms control, user code), so raising them on the clone would act on the source. Re-run
        // the constructor's per-instance wiring so the clone's own binding handler is the only subscriber.
        newClone.SizeChanged = null;
        newClone.PositionChanged = null;
        newClone.VisibleChanged = null;
        newClone.ParentChanged = null;
        newClone.PropertyChanged = null;
        newClone.ResetBindingStateForClone();
        newClone.OnConstructor();
        return newClone;
    }

    partial void ResetBindingStateForClone();

    #endregion

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Name))
        {
            return GetType().Name;
        }
        else
        {
            return Name;
        }
    }

}

#region GraphicalUiElementExtensions
public static class GraphicalUiElementExtensions
{
    #region State Lookup Extensions

    /// <summary>
    /// Attempts to resolve a state by its bare name, mirroring the lookup
    /// <see cref="GraphicalUiElement.ApplyState(string)"/> uses: first the element's uncategorized
    /// <see cref="GraphicalUiElement.States"/>, then every <see cref="GraphicalUiElement.Categories"/>
    /// category's own states. Unlike <c>ApplyState(string)</c> (which applies every same-named match
    /// it finds across categories), this returns only the first match - the right semantics for a
    /// single lookup rather than an apply-everything call.
    /// </summary>
    public static bool TryGetStateByName(this GraphicalUiElement graphicalUiElement, string name,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Gum.DataTypes.Variables.StateSave? state)
    {
        if (graphicalUiElement.States.TryGetValue(name, out state))
        {
            return true;
        }

        foreach (var category in graphicalUiElement.Categories.Values)
        {
            state = category.States.FirstOrDefault(item => item.Name == name);
            if (state != null)
            {
                return true;
            }
        }

        state = null;
        return false;
    }

    #endregion

    #region Animation Extensions
#if !FRB

    /// <summary>
    /// Sets variables on the argument GraphicalUiElement from the animation at the specified index based on the given time.
    /// </summary>
    /// <param name="graphicalUiElement">The GraphicalUiElement on which to apply the animation.</param>
    /// <param name="index">The index of the animation to apply.</param>
    /// <param name="timeInSeconds">The elapsed time since the animation started, in seconds.</param>
    public static void ApplyAnimation(this GraphicalUiElement graphicalUiElement, int index, double timeInSeconds)
    {
        var animation = graphicalUiElement.GetAnimation(index);
        if (animation == null)
        {
            throw new ArgumentException(BuildMissingAnimationMessage(graphicalUiElement, index), nameof(index));
        }
        graphicalUiElement.ApplyAnimation(animation, timeInSeconds);
    }

    /// <summary>
    /// Sets variables on the argument GraphicalUiElement from the animation with the specified name based on the given time.
    /// </summary>
    /// <param name="graphicalUiElement">The GraphicalUiElement on which to apply the animation.</param>
    /// <param name="name">The name of the animation to apply.</param>
    /// <param name="timeInSeconds">The elapsed time since the animation started, in seconds.</param>
    public static void ApplyAnimation(this GraphicalUiElement graphicalUiElement, string name, double timeInSeconds)
    {
        var animation = graphicalUiElement.GetAnimation(name);
        if (animation == null)
        {
            throw new ArgumentException(BuildMissingAnimationMessage(graphicalUiElement, name), nameof(name));
        }
        graphicalUiElement.ApplyAnimation(animation, timeInSeconds);
    }

    /// <summary>
    /// Sets variables on the argument GraphicalUiElement from the specified AnimationRuntime based on the given time.
    /// </summary>
    /// <param name="graphicalUiElement">The GraphicalUiElement on which to apply the animation</param>
    /// <param name="animation">The AnimationRuntime object to apply</param>
    /// <param name="timeInSeconds">The elapesd time since the animation started, in seconds.</param>
    /// <exception cref="ArgumentNullException">Thrown when animation is null.</exception>
    public static void ApplyAnimation(this GraphicalUiElement graphicalUiElement, AnimationRuntime animation, double timeInSeconds)
    {
        if (animation != null)
        {
            animation.ApplyAtTimeTo(timeInSeconds, graphicalUiElement);
        }
        else
        {
            throw new ArgumentNullException(nameof(animation), "the AnimationRuntime cannot be null");
        }
    }

    /// <summary>
    /// Starts playing the animation at the specified index.
    /// </summary>
    /// <param name="graphicalUiElement">The GraphicalUiElement on which to play the animation.</param>
    /// <param name="index">The index of the animation to play.</param>
    public static void PlayAnimation(this GraphicalUiElement graphicalUiElement, int index)
    {
        var animation = graphicalUiElement.GetAnimation(index);
        if (animation == null)
        {
            throw new ArgumentException(BuildMissingAnimationMessage(graphicalUiElement, index), nameof(index));
        }
        graphicalUiElement.PlayAnimation(animation);
    }

    /// <summary>
    /// Starts playing the animation with the specified name.
    /// </summary>
    /// <param name="graphicalUiElement">The GraphicalUiElement on which to play the animation.</param>
    /// <param name="name">The name of the animation to play.</param>
    public static void PlayAnimation(this GraphicalUiElement graphicalUiElement, string name)
    {
        var animation = graphicalUiElement.GetAnimation(name);
        if (animation == null)
        {
            throw new ArgumentException(BuildMissingAnimationMessage(graphicalUiElement, name), nameof(name));
        }
        graphicalUiElement.PlayAnimation(animation);
    }

    private static string BuildMissingAnimationMessage(GraphicalUiElement graphicalUiElement, object searchKey)
    {
        var elementName = (graphicalUiElement.Tag as ElementSave)?.Name;
        var elementSuffix = elementName != null ? $" for element '{elementName}'" : "";
        var searchedFor = searchKey is string s ? $"name '{s}'" : $"index {searchKey}";

        if (graphicalUiElement.Animations == null)
        {
            return $"No animations have been loaded{elementSuffix}. Did you call GumService.LoadAnimations(), " +
                   $"and is the animation file (e.g. '{elementName ?? "<elementName>"}Animations.ganx') present? " +
                   $"Searched for {searchedFor}.";
        }

        if (graphicalUiElement.Animations.Count == 0)
        {
            return $"The animation list{elementSuffix} is empty. Searched for {searchedFor}.";
        }

        var available = string.Join(", ", graphicalUiElement.Animations.Select(a => $"'{a.Name}'"));
        return $"Could not find an animation with {searchedFor}{elementSuffix}. Available animations: {available}.";
    }


    /// <summary>
    /// Gets the animation at the specified index.
    /// </summary>
    /// <param name="graphicalUiElement">the GraphicalUiElement to get the animation from</param>
    /// <param name="index">the index of the animation to get</param>
    /// <returns>The animation if found, otherwise returns null.</returns>
    public static AnimationRuntime? GetAnimation(this GraphicalUiElement graphicalUiElement, int index)
    {
        if (graphicalUiElement.Animations != null && index >= 0 && index < graphicalUiElement.Animations.Count)
        {
            return graphicalUiElement.Animations[index];
        }

        return null;
    }

    /// <summary>
    /// Get the animation at the specified name.
    /// </summary>
    /// <param name="graphicalUiElement">The GraphicalUiElement to get the animation from</param>
    /// <param name="animationName">The name of the animation to get</param>
    /// <returns>The animation if found, otherwise returns null.</returns>
    public static AnimationRuntime? GetAnimation(this GraphicalUiElement graphicalUiElement, string animationName)
    {
        return graphicalUiElement.Animations?.FirstOrDefault(item => item.Name == animationName);
    }


#endif
#endregion

}
#endregion

#region Interfaces

// additional interfaces, added here to make it easier to manage multiple projects.
public interface IManagedObject
{
    void AddToManagers();
    void RemoveFromManagers();
}

#endregion