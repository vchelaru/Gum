//Code for Spaced Component (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGame_ByReference.Components;
partial class Spaced_ComponentRuntime : ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Spaced Component", typeof(Spaced_ComponentRuntime));
    }
    public enum Spaced_State_Category
    {
        Spaced_State,
    }

    Spaced_State_Category? _spaced_State_CategoryState;
    public Spaced_State_Category? Spaced_State_CategoryState
    {
        get => _spaced_State_CategoryState;
        set
        {
            _spaced_State_CategoryState = value;
            if(value != null)
            {
                if(Categories.ContainsKey("Spaced State Category"))
                {
                    var category = Categories["Spaced State Category"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "Spaced State Category");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
    }

    public float Spaced_State_Variable
    {
        get;
        set;
    }
    public float Spaced_Variable
    {
        get;
        set;
    }
    public Spaced_ComponentRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Spaced Component");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
