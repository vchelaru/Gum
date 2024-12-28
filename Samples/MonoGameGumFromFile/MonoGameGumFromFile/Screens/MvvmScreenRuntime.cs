using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using MonoGameGumFromFile.ViewModels;
using RenderingLibrary.Graphics;
using System;


namespace MonoGameGumFromFile.Screens;

partial class MvvmScreenRuntime : Gum.Wireframe.InteractiveGue
{
    MvvmScreenViewModel ViewModel => (MvvmScreenViewModel)BindingContext;

    partial void CustomInitialize()
    {
        BindingContext = new MvvmScreenViewModel();

        this.ColoredRectangleInstance.SetBinding(
            nameof(ColoredRectangleInstance.Width),
            nameof(ViewModel.RectangleWidth));

        this.ColoredRectangleInstance.SetBinding(
            nameof(ColoredRectangleInstance.Height),
            nameof(ViewModel.RectangleHeight));
    }

    internal void CustomActivity()
    {
        ViewModel.RectangleWidth += 0.5f;
        ViewModel.RectangleHeight += 0.25f;
    }
}
