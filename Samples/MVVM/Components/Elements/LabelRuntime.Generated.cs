//Code for Elements/Label (Container)
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
public partial class LabelRuntime:ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Elements/Label", typeof(LabelRuntime));
    }
    public MonoGameGum.Forms.Controls.Label FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.Label;
    public TextRuntime TextInstance { get; protected set; }

    public string LabelColor
    {
        set => TextInstance.SetProperty("ColorCategoryState", value?.ToString());
    }

    public HorizontalAlignment HorizontalAlignment
    {
        get => TextInstance.HorizontalAlignment;
        set => TextInstance.HorizontalAlignment = value;
    }

    public string Style
    {
        set => TextInstance.SetProperty("StyleCategoryState", value?.ToString());
    }

    public string LabelText
    {
        get => TextInstance.Text;
        set => TextInstance.Text = value;
    }

    public VerticalAlignment VerticalAlignment
    {
        get => TextInstance.VerticalAlignment;
        set => TextInstance.VerticalAlignment = value;
    }

    public LabelRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Elements/Label");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        if (FormsControl == null)
        {
            FormsControlAsObject = new MonoGameGum.Forms.Controls.Label(this);
        }
        TextInstance = this.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
