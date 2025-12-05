//Code for Controls/DialogBox (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGen_MonoGameForms_FullCodegen.Components.Elements;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGameForms_FullCodegen.Components.Controls;
partial class DialogBox : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/DialogBox");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/DialogBox - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new DialogBox(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(DialogBox)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/DialogBox", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public NineSliceRuntime NineSliceInstance { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public Icon ContinueIndicatorInstance { get; protected set; }

    public DialogBox(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public DialogBox() : base(new ContainerRuntime())
    {

         
        this.Visual.Height = 128f;
         
        this.Visual.Width = 256f;

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        NineSliceInstance = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        NineSliceInstance.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (NineSliceInstance.ElementSave != null) NineSliceInstance.AddStatesAndCategoriesRecursivelyToGue(NineSliceInstance.ElementSave);
        if (NineSliceInstance.ElementSave != null) NineSliceInstance.SetInitialState();
        NineSliceInstance.Name = "NineSliceInstance";
        TextInstance = new global::MonoGameGum.GueDeriving.TextRuntime();
        TextInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TextInstance.ElementSave != null) TextInstance.AddStatesAndCategoriesRecursivelyToGue(TextInstance.ElementSave);
        if (TextInstance.ElementSave != null) TextInstance.SetInitialState();
        TextInstance.Name = "TextInstance";
        ContinueIndicatorInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon();
        ContinueIndicatorInstance.Name = "ContinueIndicatorInstance";
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
        this.AddChild(NineSliceInstance);
        this.AddChild(TextInstance);
        this.AddChild(ContinueIndicatorInstance);
    }
    private void ApplyDefaultVariables()
    {
        this.NineSliceInstance.SetProperty("ColorCategoryState", "Primary");
        this.NineSliceInstance.SetProperty("StyleCategoryState", "Panel");

        this.TextInstance.SetProperty("ColorCategoryState", "White");
        this.TextInstance.SetProperty("StyleCategoryState", "Normal");
        this.TextInstance.Height = -32f;
        this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.TextInstance.Text = @"This is a dialog box. This text will be displayed one character at a time. Typically a dialog box is added to a Screen such as the GameScreen, but it defaults to being invisible.";
        this.TextInstance.TextOverflowVerticalMode = global::RenderingLibrary.Graphics.TextOverflowVerticalMode.TruncateLine;
        this.TextInstance.Width = -16f;
        this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.TextInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.TextInstance.Y = 8f;

        this.ContinueIndicatorInstance.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Arrow2;
        this.ContinueIndicatorInstance.Visual.Height = 24f;
        this.ContinueIndicatorInstance.Visual.Rotation = -90f;
        this.ContinueIndicatorInstance.Visual.Width = 24f;
        this.ContinueIndicatorInstance.Visual.X = -8f;
        this.ContinueIndicatorInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.ContinueIndicatorInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;
        this.ContinueIndicatorInstance.Visual.Y = -8f;
        this.ContinueIndicatorInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.ContinueIndicatorInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

    }
    partial void CustomInitialize();
}
