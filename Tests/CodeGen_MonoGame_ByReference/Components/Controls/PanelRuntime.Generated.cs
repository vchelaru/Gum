//Code for Controls/Panel (Container)
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGame_ByReference.Components.Controls;
[global::Gum.DataTypes.GumElement("Controls/Panel")]
partial class PanelRuntime : ContainerRuntime
{
    #if UNITY_5_3_OR_NEWER && !GUM_NO_UNITY_ENGINE
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    #else
    [System.Runtime.CompilerServices.ModuleInitializer]
    #endif
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/Panel", typeof(PanelRuntime));
    }
    public global::Gum.Forms.Controls.Panel FormsControl => FormsControlAsObject as global::Gum.Forms.Controls.Panel;
    private bool _tryCreateFormsObject = true;

    public PanelRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        _tryCreateFormsObject = tryCreateFormsObject;
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetRequiredElementSave("Controls/Panel");
            element.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        if (FormsControl == null && _tryCreateFormsObject)
        {
            FormsControlAsObject = new global::Gum.Forms.Controls.Panel(this);
        }
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
