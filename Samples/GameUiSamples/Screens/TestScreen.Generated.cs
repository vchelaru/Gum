//Code for TestScreen
using GumRuntime;
using MonoGameGum.GueDeriving;
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
    partial class TestScreen:MonoGameGum.Forms.Controls.FrameworkElement
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            var template = new MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
            {
                var visual = new MonoGameGum.GueDeriving.ContainerRuntime();
                var element = ObjectFinder.Self.GetElementSave("TestScreen");
                element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
                if(createForms) visual.FormsControlAsObject = new TestScreen(visual);
                visual.Width = 0;
                visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                visual.Height = 0;
                visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                return visual;
            });
            MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(TestScreen)] = template;
            ElementSaveExtensions.RegisterGueInstantiation("TestScreen", () => 
            {
                var gue = template.CreateContent(null, true) as InteractiveGue;
                return gue;
            });
        }
        public enum TestCategory
        {
            State1,
        }

        TestCategory? _testCategoryState;
        public TestCategory? TestCategoryState
        {
            get => _testCategoryState;
            set
            {
                _testCategoryState = value;
                if(value != null)
                {
                    if(Visual.Categories.ContainsKey("TestCategory"))
                    {
                        var category = Visual.Categories["TestCategory"];
                        var state = category.States.Find(item => item.Name == value.ToString());
                        this.Visual.ApplyState(state);
                    }
                    else
                    {
                        var category = ((Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "TestCategory");
                        var state = category.States.Find(item => item.Name == value.ToString());
                        this.Visual.ApplyState(state);
                    }
                }
            }
        }
        public TextBox TextBoxInstance_with_space { get; protected set; }
        public SpriteRuntime SpriteInstance { get; protected set; }

        public float TextBoxInstancewithspaceX
        {
            get => TextBoxInstance_with_space.X;
            set => TextBoxInstance_with_space.X = value;
        }

        public TestScreen(InteractiveGue visual) : base(visual) { }
        public TestScreen()
        {



        }
        protected override void ReactToVisualChanged()
        {
            base.ReactToVisualChanged();
            TextBoxInstance_with_space = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.GetFrameworkElementByName<TextBox>(this.Visual,"TextBoxInstance with space");
            SpriteInstance = this.Visual?.GetGraphicalUiElementByName("SpriteInstance") as SpriteRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
