//Code for TestScreen
using GumRuntime;
using GumFormsSample.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GumFormsSample.Screens
{
    public partial class TestScreenRuntime:Gum.Wireframe.BindableGue
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("TestScreen", typeof(TestScreenRuntime));
        }
        public CheckBoxRuntime CheckBoxInstance { get; protected set; }

        public TestScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
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
            CheckBoxInstance = new CheckBoxRuntime();
            CheckBoxInstance.Name = "CheckBoxInstance";
        }
        protected virtual void AssignParents()
        {
            if(this.Children != null) this.Children.Add(CheckBoxInstance);
            else this.WhatThisContains.Add(CheckBoxInstance);
        }
        private void ApplyDefaultVariables()
        {
            this.CheckBoxInstance.CheckboxDisplayText = @"This is my label";

        }
        partial void CustomInitialize();
    }
}
