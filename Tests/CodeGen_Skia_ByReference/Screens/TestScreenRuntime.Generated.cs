//Code for TestScreen
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_Skia_ByReference.Screens;
partial class TestScreenRuntime : Gum.Wireframe.GraphicalUiElement
{
    public TextRuntime TextInstance { get; protected set; }

    public TestScreenRuntime(bool fullInstantiation = true)
    {
        if(fullInstantiation)
        {
        }



    }
    public override void AfterFullCreation()
    {
        TextInstance = this.GetGraphicalUiElementByName("TextInstance") as global::Gum.GueDeriving.TextRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
