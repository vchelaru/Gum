//Code for Popup (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using MonoGameGum.GueDeriving;

namespace MonoGameGumCodeGeneration.Components
{
    public partial class PopupRuntime
    {
        public NineSliceRuntime NineSliceInstance { get; protected set; }
        public TextRuntime TextInstance { get; protected set; }

        public PopupRuntime()
        {

            //InitializeInstances();

            //AssignParents();
            //CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
            NineSliceInstance = new NineSliceRuntime();
            NineSliceInstance.Name = "NineSliceInstance";
            TextInstance = new TextRuntime();
            TextInstance.Name = "TextInstance";
        }
        public override void AfterFullCreation()
        {
            NineSliceInstance = this.GetGraphicalUiElementByName("NineSliceInstance") as NineSliceRuntime;
            TextInstance = this.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
            base.AfterFullCreation();
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(NineSliceInstance);
            NineSliceInstance.Children.Add(TextInstance);
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
