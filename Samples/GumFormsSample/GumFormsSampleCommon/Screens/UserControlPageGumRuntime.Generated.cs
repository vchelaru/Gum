//Code for UserControlPageGum
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GumFormsSample.Screens
{
    public partial class UserControlPageGumRuntime:Gum.Wireframe.BindableGue
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("UserControlPageGum", typeof(UserControlPageGumRuntime));
        }

        public UserControlPageGumRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

             

            InitializeInstances();

            ApplyDefaultVariables();
            AssignParents();
            if(tryCreateFormsObject)
            {
            }
            CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
        }
        protected virtual void AssignParents()
        {
        }
        private void ApplyDefaultVariables()
        {
        }
        partial void CustomInitialize();
    }
}
