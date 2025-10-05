//Code for Controls/InputDeviceSelector (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGen_MonoGameForms_FullCodegen.Components.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGameForms_FullCodegen.Components.Controls;
partial class InputDeviceSelector : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/InputDeviceSelector");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/InputDeviceSelector - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new InputDeviceSelector(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(InputDeviceSelector)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/InputDeviceSelector", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public NineSliceRuntime Background { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public TextRuntime TextInstance1 { get; protected set; }
    public ContainerRuntime ContainerInstance1 { get; protected set; }
    public ContainerRuntime InputDeviceContainerInstance { get; protected set; }
    public ContainerRuntime ContainerInstance2 { get; protected set; }
    public InputDeviceSelectionItem InputDeviceSelectionItemInstance { get; protected set; }
    public InputDeviceSelectionItem InputDeviceSelectionItemInstance1 { get; protected set; }
    public InputDeviceSelectionItem InputDeviceSelectionItemInstance2 { get; protected set; }
    public InputDeviceSelectionItem InputDeviceSelectionItemInstance3 { get; protected set; }

    public InputDeviceSelector(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public InputDeviceSelector() : base(new ContainerRuntime())
    {

        this.Visual.Height = 6f;
        this.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
         
        this.Visual.Width = 60f;
        this.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.Visual.X = 0f;
        this.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.Visual.Y = 0f;
        this.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        Background = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        Background.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (Background.ElementSave != null) Background.AddStatesAndCategoriesRecursivelyToGue(Background.ElementSave);
        if (Background.ElementSave != null) Background.SetInitialState();
        Background.Name = "Background";
        TextInstance = new global::MonoGameGum.GueDeriving.TextRuntime();
        TextInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TextInstance.ElementSave != null) TextInstance.AddStatesAndCategoriesRecursivelyToGue(TextInstance.ElementSave);
        if (TextInstance.ElementSave != null) TextInstance.SetInitialState();
        TextInstance.Name = "TextInstance";
        TextInstance1 = new global::MonoGameGum.GueDeriving.TextRuntime();
        TextInstance1.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TextInstance1.ElementSave != null) TextInstance1.AddStatesAndCategoriesRecursivelyToGue(TextInstance1.ElementSave);
        if (TextInstance1.ElementSave != null) TextInstance1.SetInitialState();
        TextInstance1.Name = "TextInstance1";
        ContainerInstance1 = new global::MonoGameGum.GueDeriving.ContainerRuntime();
        ContainerInstance1.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (ContainerInstance1.ElementSave != null) ContainerInstance1.AddStatesAndCategoriesRecursivelyToGue(ContainerInstance1.ElementSave);
        if (ContainerInstance1.ElementSave != null) ContainerInstance1.SetInitialState();
        ContainerInstance1.Name = "ContainerInstance1";
        InputDeviceContainerInstance = new global::MonoGameGum.GueDeriving.ContainerRuntime();
        InputDeviceContainerInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (InputDeviceContainerInstance.ElementSave != null) InputDeviceContainerInstance.AddStatesAndCategoriesRecursivelyToGue(InputDeviceContainerInstance.ElementSave);
        if (InputDeviceContainerInstance.ElementSave != null) InputDeviceContainerInstance.SetInitialState();
        InputDeviceContainerInstance.Name = "InputDeviceContainerInstance";
        ContainerInstance2 = new global::MonoGameGum.GueDeriving.ContainerRuntime();
        ContainerInstance2.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (ContainerInstance2.ElementSave != null) ContainerInstance2.AddStatesAndCategoriesRecursivelyToGue(ContainerInstance2.ElementSave);
        if (ContainerInstance2.ElementSave != null) ContainerInstance2.SetInitialState();
        ContainerInstance2.Name = "ContainerInstance2";
        InputDeviceSelectionItemInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.InputDeviceSelectionItem();
        InputDeviceSelectionItemInstance.Name = "InputDeviceSelectionItemInstance";
        InputDeviceSelectionItemInstance1 = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.InputDeviceSelectionItem();
        InputDeviceSelectionItemInstance1.Name = "InputDeviceSelectionItemInstance1";
        InputDeviceSelectionItemInstance2 = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.InputDeviceSelectionItem();
        InputDeviceSelectionItemInstance2.Name = "InputDeviceSelectionItemInstance2";
        InputDeviceSelectionItemInstance3 = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.InputDeviceSelectionItem();
        InputDeviceSelectionItemInstance3.Name = "InputDeviceSelectionItemInstance3";
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
        this.AddChild(Background);
        ContainerInstance1.AddChild(TextInstance);
        ContainerInstance2.AddChild(TextInstance1);
        this.AddChild(ContainerInstance1);
        this.AddChild(InputDeviceContainerInstance);
        this.AddChild(ContainerInstance2);
        InputDeviceContainerInstance.AddChild(InputDeviceSelectionItemInstance);
        InputDeviceContainerInstance.AddChild(InputDeviceSelectionItemInstance1);
        InputDeviceContainerInstance.AddChild(InputDeviceSelectionItemInstance2);
        InputDeviceContainerInstance.AddChild(InputDeviceSelectionItemInstance3);
    }
    private void ApplyDefaultVariables()
    {
        this.Background.SetProperty("ColorCategoryState", "Primary");
        this.Background.SetProperty("StyleCategoryState", "Panel");

        this.TextInstance.SetProperty("ColorCategoryState", "White");
        this.TextInstance.SetProperty("StyleCategoryState", "H1");
        ((TextRuntime)this.TextInstance).HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.TextInstance.Text = @"Press A / Space to Join";
        ((TextRuntime)this.TextInstance).VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.TextInstance.X = 0f;
        this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.TextInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.TextInstance.Y = 0f;
        this.TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.TextInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.TextInstance1.SetProperty("ColorCategoryState", "White");
        this.TextInstance1.SetProperty("StyleCategoryState", "H1");
        ((TextRuntime)this.TextInstance1).HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.TextInstance1.Text = @"Press Start / Enter to Continue";
        ((TextRuntime)this.TextInstance1).VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.TextInstance1.X = 0f;
        this.TextInstance1.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.TextInstance1.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.TextInstance1.Y = 0f;
        this.TextInstance1.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.TextInstance1.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.ContainerInstance1.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
        this.ContainerInstance1.Height = 31f;
        this.ContainerInstance1.Width = 0f;
        this.ContainerInstance1.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.ContainerInstance1.X = 0f;
        this.ContainerInstance1.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.ContainerInstance1.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.ContainerInstance1.Y = 27f;
        this.ContainerInstance1.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.ContainerInstance1.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

        this.InputDeviceContainerInstance.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
        this.InputDeviceContainerInstance.Height = 20f;
        this.InputDeviceContainerInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.InputDeviceContainerInstance.StackSpacing = 4f;
        this.InputDeviceContainerInstance.Width = 0f;
        this.InputDeviceContainerInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.InputDeviceContainerInstance.X = 0f;
        this.InputDeviceContainerInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.InputDeviceContainerInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.InputDeviceContainerInstance.Y = 89f;
        this.InputDeviceContainerInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.InputDeviceContainerInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

        this.ContainerInstance2.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
        this.ContainerInstance2.Height = 31f;
        this.ContainerInstance2.Width = 0f;
        this.ContainerInstance2.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.ContainerInstance2.X = 0f;
        this.ContainerInstance2.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.ContainerInstance2.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.ContainerInstance2.Y = 228f;
        this.ContainerInstance2.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.ContainerInstance2.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;





    }
    partial void CustomInitialize();
}
