//Code for Controls/PlayerJoinView (Container)
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
partial class PlayerJoinView : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/PlayerJoinView");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new PlayerJoinView(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(PlayerJoinView)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/PlayerJoinView", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public ContainerRuntime InnerPanelInstance { get; protected set; }
    public PlayerJoinViewItem PlayerJoinViewItem1 { get; protected set; }
    public PlayerJoinViewItem PlayerJoinViewItem2 { get; protected set; }
    public PlayerJoinViewItem PlayerJoinViewItem3 { get; protected set; }
    public PlayerJoinViewItem PlayerJoinViewItem4 { get; protected set; }

    public PlayerJoinView(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public PlayerJoinView() : base(new ContainerRuntime())
    {

        this.Visual.Height = 463f;
         
        this.Visual.Width = 144f;

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        base.ReactToVisualChanged();
        InnerPanelInstance = new global::MonoGameGum.GueDeriving.ContainerRuntime();
        InnerPanelInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (InnerPanelInstance.ElementSave != null) InnerPanelInstance.AddStatesAndCategoriesRecursivelyToGue(InnerPanelInstance.ElementSave);
        if (InnerPanelInstance.ElementSave != null) InnerPanelInstance.SetInitialState();
        InnerPanelInstance.Name = "InnerPanelInstance";
        PlayerJoinViewItem1 = new PlayerJoinViewItem();
        PlayerJoinViewItem1.Name = "PlayerJoinViewItem1";
        PlayerJoinViewItem2 = new PlayerJoinViewItem();
        PlayerJoinViewItem2.Name = "PlayerJoinViewItem2";
        PlayerJoinViewItem3 = new PlayerJoinViewItem();
        PlayerJoinViewItem3.Name = "PlayerJoinViewItem3";
        PlayerJoinViewItem4 = new PlayerJoinViewItem();
        PlayerJoinViewItem4.Name = "PlayerJoinViewItem4";
    }
    protected virtual void AssignParents()
    {
        this.AddChild(InnerPanelInstance);
        InnerPanelInstance.AddChild(PlayerJoinViewItem1);
        InnerPanelInstance.AddChild(PlayerJoinViewItem2);
        InnerPanelInstance.AddChild(PlayerJoinViewItem3);
        InnerPanelInstance.AddChild(PlayerJoinViewItem4);
    }
    private void ApplyDefaultVariables()
    {
        this.InnerPanelInstance.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
        this.InnerPanelInstance.Height = 0f;
        this.InnerPanelInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.InnerPanelInstance.StackSpacing = 32f;
        this.InnerPanelInstance.Width = 0f;
        this.InnerPanelInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.InnerPanelInstance.X = 0f;
        this.InnerPanelInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.InnerPanelInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.InnerPanelInstance.Y = 0f;
        this.InnerPanelInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.InnerPanelInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;





    }
    partial void CustomInitialize();
}
