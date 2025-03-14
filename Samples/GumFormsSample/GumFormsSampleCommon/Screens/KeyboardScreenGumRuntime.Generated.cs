//Code for KeyboardScreenGum
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
    public partial class KeyboardScreenGumRuntime:Gum.Wireframe.BindableGue
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("KeyboardScreenGum", typeof(KeyboardScreenGumRuntime));
        }
        public KeyboardRuntime KeyboardInstance { get; protected set; }
        public TextBoxRuntime TextBoxInstance { get; protected set; }

        public KeyboardScreenGumRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
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
            KeyboardInstance = new KeyboardRuntime();
            KeyboardInstance.Name = "KeyboardInstance";
            TextBoxInstance = new TextBoxRuntime();
            TextBoxInstance.Name = "TextBoxInstance";
        }
        protected virtual void AssignParents()
        {
            if(this.Children != null) this.Children.Add(KeyboardInstance);
            else this.WhatThisContains.Add(KeyboardInstance);
            if(this.Children != null) this.Children.Add(TextBoxInstance);
            else this.WhatThisContains.Add(TextBoxInstance);
        }
        private void ApplyDefaultVariables()
        {
            this.KeyboardInstance.Height = 221f;
            this.KeyboardInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.KeyboardInstance.Width = 700f;
            this.KeyboardInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.KeyboardInstance.X = 0f;
            this.KeyboardInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.KeyboardInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.KeyboardInstance.Y = -77f;
            this.KeyboardInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
            this.KeyboardInstance.YUnits = GeneralUnitType.PixelsFromLarge;

            this.TextBoxInstance.Width = 700f;
            this.TextBoxInstance.X = 0f;
            this.TextBoxInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TextBoxInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.TextBoxInstance.Y = -309f;
            this.TextBoxInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
            this.TextBoxInstance.YUnits = GeneralUnitType.PixelsFromLarge;

        }
        partial void CustomInitialize();
    }
}
