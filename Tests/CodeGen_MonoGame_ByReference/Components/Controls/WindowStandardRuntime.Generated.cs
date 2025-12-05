//Code for Controls/WindowStandard (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGen_MonoGame_ByReference.Components.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGame_ByReference.Components.Controls;
partial class WindowStandardRuntime : ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/WindowStandard", typeof(WindowStandardRuntime));
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsComponents[typeof(global::Gum.Forms.Window)] = typeof(WindowStandardRuntime);
    }
    public global::Gum.Forms.Window FormsControl => FormsControlAsObject as global::Gum.Forms.Window;
    public NineSliceRuntime Background { get; protected set; }
    public PanelRuntime InnerPanelInstance { get; protected set; }
    public PanelRuntime TitleBarInstance { get; protected set; }
    public PanelRuntime BorderTopLeftInstance { get; protected set; }
    public PanelRuntime BorderTopRightInstance { get; protected set; }
    public PanelRuntime BorderBottomLeftInstance { get; protected set; }
    public PanelRuntime BorderBottomRightInstance { get; protected set; }
    public PanelRuntime BorderTopInstance { get; protected set; }
    public PanelRuntime BorderBottomInstance { get; protected set; }
    public PanelRuntime BorderLeftInstance { get; protected set; }
    public PanelRuntime BorderRightInstance { get; protected set; }

    public WindowStandardRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Controls/WindowStandard");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        if (FormsControl == null)
        {
            FormsControlAsObject = new global::Gum.Forms.Window(this);
        }
        Background = this.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        InnerPanelInstance = this.GetGraphicalUiElementByName("InnerPanelInstance") as CodeGen_MonoGame_ByReference.Components.Controls.PanelRuntime;
        TitleBarInstance = this.GetGraphicalUiElementByName("TitleBarInstance") as CodeGen_MonoGame_ByReference.Components.Controls.PanelRuntime;
        BorderTopLeftInstance = this.GetGraphicalUiElementByName("BorderTopLeftInstance") as CodeGen_MonoGame_ByReference.Components.Controls.PanelRuntime;
        BorderTopRightInstance = this.GetGraphicalUiElementByName("BorderTopRightInstance") as CodeGen_MonoGame_ByReference.Components.Controls.PanelRuntime;
        BorderBottomLeftInstance = this.GetGraphicalUiElementByName("BorderBottomLeftInstance") as CodeGen_MonoGame_ByReference.Components.Controls.PanelRuntime;
        BorderBottomRightInstance = this.GetGraphicalUiElementByName("BorderBottomRightInstance") as CodeGen_MonoGame_ByReference.Components.Controls.PanelRuntime;
        BorderTopInstance = this.GetGraphicalUiElementByName("BorderTopInstance") as CodeGen_MonoGame_ByReference.Components.Controls.PanelRuntime;
        BorderBottomInstance = this.GetGraphicalUiElementByName("BorderBottomInstance") as CodeGen_MonoGame_ByReference.Components.Controls.PanelRuntime;
        BorderLeftInstance = this.GetGraphicalUiElementByName("BorderLeftInstance") as CodeGen_MonoGame_ByReference.Components.Controls.PanelRuntime;
        BorderRightInstance = this.GetGraphicalUiElementByName("BorderRightInstance") as CodeGen_MonoGame_ByReference.Components.Controls.PanelRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
