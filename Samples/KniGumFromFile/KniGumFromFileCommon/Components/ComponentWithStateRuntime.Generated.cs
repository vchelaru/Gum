//Code for ComponentWithState (Container)
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
public partial class ComponentWithStateRuntime:ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("ComponentWithState", typeof(ComponentWithStateRuntime));
    }
    public enum LeftSideCategory
    {
        Red,
        Green,
    }
    public enum RightSideCategory
    {
        Yellow,
        Blue,
    }

    public LeftSideCategory LeftSideCategoryState
    {
        set
        {
            if(Categories.ContainsKey("LeftSideCategory"))
            {
                var category = Categories["LeftSideCategory"];
                var state = category.States.Find(item => item.Name == value.ToString());
                this.ApplyState(state);
            }
            else
            {
                var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "LeftSideCategory");
                var state = category.States.Find(item => item.Name == value.ToString());
                this.ApplyState(state);
            }
        }
    }

    public RightSideCategory RightSideCategoryState
    {
        set
        {
            if(Categories.ContainsKey("RightSideCategory"))
            {
                var category = Categories["RightSideCategory"];
                var state = category.States.Find(item => item.Name == value.ToString());
                this.ApplyState(state);
            }
            else
            {
                var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "RightSideCategory");
                var state = category.States.Find(item => item.Name == value.ToString());
                this.ApplyState(state);
            }
        }
    }
    public ColoredRectangleRuntime LeftSideRectangle { get; protected set; }
    public ColoredRectangleRuntime RightSideRectangle { get; protected set; }

    public ComponentWithStateRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("ComponentWithState");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        LeftSideRectangle = this.GetGraphicalUiElementByName("LeftSideRectangle") as ColoredRectangleRuntime;
        RightSideRectangle = this.GetGraphicalUiElementByName("RightSideRectangle") as ColoredRectangleRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
