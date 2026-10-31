//Code for Controls/UserControl (Container)
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
[global::Gum.DataTypes.GumElement("Controls/UserControl", FormsType = typeof(global::Gum.Forms.Controls.UserControl))]
partial class UserControlRuntime : ContainerRuntime
{
    #if UNITY_5_3_OR_NEWER && !GUM_NO_UNITY_ENGINE
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    #else
    [System.Runtime.CompilerServices.ModuleInitializer]
    #endif
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/UserControl", typeof(UserControlRuntime));
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::Gum.Forms.Controls.UserControl)] = new global::Gum.Forms.VisualTemplate((vm, createForms) => new UserControlRuntime(fullInstantiation: true, tryCreateFormsObject: createForms));
    }
    public global::Gum.Forms.Controls.UserControl FormsControl => FormsControlAsObject as global::Gum.Forms.Controls.UserControl;
    private bool _tryCreateFormsObject = true;
    public NineSliceRuntime Background { get; protected set; }

    public UserControlRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        _tryCreateFormsObject = tryCreateFormsObject;
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetRequiredElementSave("Controls/UserControl");
            element.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        if (FormsControl == null && _tryCreateFormsObject)
        {
            FormsControlAsObject = new global::Gum.Forms.Controls.UserControl(this);
        }
        Background = this.GetGraphicalUiElementByName("Background") as global::Gum.GueDeriving.NineSliceRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
