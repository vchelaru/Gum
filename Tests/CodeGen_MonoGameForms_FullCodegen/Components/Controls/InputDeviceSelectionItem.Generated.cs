//Code for Controls/InputDeviceSelectionItem (Container)
using CodeGen_MonoGameForms_FullCodegen.Components.Controls;
using CodeGen_MonoGameForms_FullCodegen.Components.Elements;
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGameForms_FullCodegen.Components.Controls;
[global::Gum.DataTypes.GumElement("Controls/InputDeviceSelectionItem")]
partial class InputDeviceSelectionItem : global::Gum.Forms.Controls.FrameworkElement
{
    #if UNITY_5_3_OR_NEWER
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    #else
    [System.Runtime.CompilerServices.ModuleInitializer]
    #endif
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::Gum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetRequiredElementSave("Controls/InputDeviceSelectionItem");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new InputDeviceSelectionItem(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(InputDeviceSelectionItem)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/InputDeviceSelectionItem", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum JoinedCategory
    {
        NoInputDevice,
        HasInputDevice,
    }

    private JoinedCategory? _joinedCategoryState;
    public JoinedCategory? JoinedCategoryState
    {
        get => _joinedCategoryState;
        set
        {
            _joinedCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case JoinedCategory.NoInputDevice:
                        this.IconInstance.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.None;
                        this.RemoveDeviceButtonInstance.Visual.Visible = false;
                        this.TextInstance.Visible = false;
                        break;
                    case JoinedCategory.HasInputDevice:
                        this.IconInstance.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Gamepad;
                        this.RemoveDeviceButtonInstance.Visual.Visible = true;
                        this.TextInstance.Visible = true;
                        break;
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public Icon IconInstance { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public ButtonClose RemoveDeviceButtonInstance { get; protected set; }

    public InputDeviceSelectionItem(InteractiveGue visual) : base(visual)
    {
        InitializeInstancesFromVisual();
        ReactToGeneratedVisual();
        CustomInitialize();
    }
    public InputDeviceSelectionItem() : base(new ContainerRuntime())
    {

        this.Visual.Height = 113f;
        this.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
         

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        ReactToGeneratedVisual();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        Background = new global::Gum.GueDeriving.NineSliceRuntime();
        Background.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (Background.ElementSave != null) Background.AddStatesAndCategoriesRecursivelyToGue(Background.ElementSave);
        if (Background.ElementSave != null) Background.SetInitialState();
        Background.Name = "Background";
        IconInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon();
        IconInstance.Name = "IconInstance";
        TextInstance = new global::Gum.GueDeriving.TextRuntime();
        TextInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TextInstance.ElementSave != null) TextInstance.AddStatesAndCategoriesRecursivelyToGue(TextInstance.ElementSave);
        if (TextInstance.ElementSave != null) TextInstance.SetInitialState();
        TextInstance.Name = "TextInstance";
        RemoveDeviceButtonInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonClose();
        RemoveDeviceButtonInstance.Name = "RemoveDeviceButtonInstance";
    }
    protected virtual void InitializeInstancesFromVisual()
    {
        Background = this.Visual?.GetGraphicalUiElementByName("Background") as global::Gum.GueDeriving.NineSliceRuntime;
        IconInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<Icon>(this.Visual,"IconInstance");
        TextInstance = this.Visual?.GetGraphicalUiElementByName("TextInstance") as global::Gum.GueDeriving.TextRuntime;
        RemoveDeviceButtonInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ButtonClose>(this.Visual,"RemoveDeviceButtonInstance");
    }
    protected virtual void AssignParents()
    {
        this.Visual.AddChild(this.Background);
        this.Visual.AddChild(this.IconInstance.Visual);
        this.Visual.AddChild(this.TextInstance);
        this.Visual.AddChild(this.RemoveDeviceButtonInstance.Visual);
    }
    protected virtual void ApplyDefaultVariables()
    {
        this.Background.SetProperty("ColorCategoryState", "Primary");
        this.Background.SetProperty("StyleCategoryState", "Panel");

        this.IconInstance.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Gamepad;
        this.IconInstance.Visual.X = 0f;
        this.IconInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.IconInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.IconInstance.Visual.Y = 5f;
        this.IconInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.IconInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

        this.TextInstance.SetProperty("ColorCategoryState", "White");
        this.TextInstance.SetProperty("StyleCategoryState", "H2");
        this.TextInstance.Height = -43f;
        this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        ((TextRuntime)this.TextInstance).HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.TextInstance.Text = @"Input Device Name Here With 3 Lines";
        ((TextRuntime)this.TextInstance).TextOverflowHorizontalMode = global::RenderingLibrary.Graphics.TextOverflowHorizontalMode.EllipsisLetter;
        this.TextInstance.TextOverflowVerticalMode = global::RenderingLibrary.Graphics.TextOverflowVerticalMode.TruncateLine;
        ((TextRuntime)this.TextInstance).VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.TextInstance.Width = 0f;
        this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.TextInstance.X = 0f;
        this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.TextInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.TextInstance.Y = 39f;
        this.TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.TextInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

        this.RemoveDeviceButtonInstance.Visual.Height = 22f;
        this.RemoveDeviceButtonInstance.Visual.Width = 22f;
        this.RemoveDeviceButtonInstance.Visual.X = -4f;
        this.RemoveDeviceButtonInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.RemoveDeviceButtonInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;
        this.RemoveDeviceButtonInstance.Visual.Y = 4f;
        this.RemoveDeviceButtonInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.RemoveDeviceButtonInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

    }
    private bool _isGeneratedVisualBuilt;
    protected override void ReactToVisualChanged()
    {
        if (_isGeneratedVisualBuilt) base.ReactToVisualChanged();
    }
    private void ReactToGeneratedVisual()
    {
        _isGeneratedVisualBuilt = true;
        ReactToVisualChanged();
    }
    partial void CustomInitialize();
}
