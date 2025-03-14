//Code for TestScreen
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
    public partial class TestScreenRuntime:Gum.Wireframe.BindableGue
    {
        public NineSliceRuntime Background { get; protected set; }

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
            Background = new NineSliceRuntime();
            Background.Name = "Background";
        }
        protected virtual void AssignParents()
        {
            if(this.Children != null) this.Children.Add(Background);
            else this.WhatThisContains.Add(Background);
        }
        private void ApplyDefaultVariables()
        {
Background.SetProperty("StyleCategoryState", "Bordered");
Background.SetProperty("ColorCategoryState", "DarkGray");
            this.Background.YUnits = GeneralUnitType.PixelsFromMiddle;
            this.Background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Background.Y = 0f;
            this.Background.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.Background.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.Background.X = 0f;
            this.Background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Background.Width = 0f;
            this.Background.Red = 70;
            this.Background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Background.Height = 0f;
            this.Background.Green = 70;
            this.Background.Blue = 70;

        }
        partial void CustomInitialize();
    }
}
