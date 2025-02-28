//Code for TestScreen
using GumRuntime;
using GameUiSamples.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Screens
{
    public partial class TestScreenRuntime:Gum.Wireframe.BindableGue
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("TestScreen", typeof(TestScreenRuntime));
        }
        public enum TestCategory
        {
            State1,
        }

        public TestCategory TestCategoryState
        {
            set
            {
                if(Categories.ContainsKey("TestCategory"))
                {
                    var category = Categories["TestCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "TestCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
        public TextBoxRuntime TextBoxInstance_with_space { get; protected set; }

        public float TextBoxInstancewithspaceX
        {
            get => TextBoxInstance_with_space.X;
            set => TextBoxInstance_with_space.X = value;
        }

        public TestScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
                var element = ObjectFinder.Self.GetElementSave("TestScreen");
                element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
            }



        }
        public override void AfterFullCreation()
        {
            TextBoxInstance_with_space = this.GetGraphicalUiElementByName("TextBoxInstance with space") as TextBoxRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
