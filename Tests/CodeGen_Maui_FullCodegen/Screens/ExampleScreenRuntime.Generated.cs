//Code for ExampleScreen
using GumRuntime;
using System.Linq;
using SkiaGum.GueDeriving;
using CodeGen_Maui_FullCodegen.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_Maui_FullCodegen.Screens;
partial class ExampleScreenRuntime : Gum.Wireframe.BindableGue
{
    public ContainerRuntime ContainerInstance { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public TextRuntime TextInstance1 { get; protected set; }
    public TextRuntime TextInstance2 { get; protected set; }
    public TextRuntime TextInstance3 { get; protected set; }
    public TextRuntime TextInstance4 { get; protected set; }
    public EmptyComponentRuntime EmptyComponentInstance { get; protected set; }

    public ExampleScreenRuntime(bool fullInstantiation = true)
    {
        if(fullInstantiation)
        {
        }


        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        ContainerInstance = new global::SkiaGum.GueDeriving.ContainerRuntime();
        ContainerInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (ContainerInstance.ElementSave != null) ContainerInstance.AddStatesAndCategoriesRecursivelyToGue(ContainerInstance.ElementSave);
        if (ContainerInstance.ElementSave != null) ContainerInstance.SetInitialState();
        ContainerInstance.Name = "ContainerInstance";
        TextInstance = new global::SkiaGum.GueDeriving.TextRuntime();
        TextInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TextInstance.ElementSave != null) TextInstance.AddStatesAndCategoriesRecursivelyToGue(TextInstance.ElementSave);
        if (TextInstance.ElementSave != null) TextInstance.SetInitialState();
        TextInstance.Name = "TextInstance";
        TextInstance1 = new global::SkiaGum.GueDeriving.TextRuntime();
        TextInstance1.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TextInstance1.ElementSave != null) TextInstance1.AddStatesAndCategoriesRecursivelyToGue(TextInstance1.ElementSave);
        if (TextInstance1.ElementSave != null) TextInstance1.SetInitialState();
        TextInstance1.Name = "TextInstance1";
        TextInstance2 = new global::SkiaGum.GueDeriving.TextRuntime();
        TextInstance2.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TextInstance2.ElementSave != null) TextInstance2.AddStatesAndCategoriesRecursivelyToGue(TextInstance2.ElementSave);
        if (TextInstance2.ElementSave != null) TextInstance2.SetInitialState();
        TextInstance2.Name = "TextInstance2";
        TextInstance3 = new global::SkiaGum.GueDeriving.TextRuntime();
        TextInstance3.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TextInstance3.ElementSave != null) TextInstance3.AddStatesAndCategoriesRecursivelyToGue(TextInstance3.ElementSave);
        if (TextInstance3.ElementSave != null) TextInstance3.SetInitialState();
        TextInstance3.Name = "TextInstance3";
        TextInstance4 = new global::SkiaGum.GueDeriving.TextRuntime();
        TextInstance4.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TextInstance4.ElementSave != null) TextInstance4.AddStatesAndCategoriesRecursivelyToGue(TextInstance4.ElementSave);
        if (TextInstance4.ElementSave != null) TextInstance4.SetInitialState();
        TextInstance4.Name = "TextInstance4";
        EmptyComponentInstance = new CodeGen_Maui_FullCodegen.Components.EmptyComponentRuntime();
        EmptyComponentInstance.Name = "EmptyComponentInstance";
    }
    protected virtual void AssignParents()
    {
        this.WhatThisContains.Add(ContainerInstance);
        ContainerInstance.Children.Add(TextInstance);
        ContainerInstance.Children.Add(TextInstance1);
        ContainerInstance.Children.Add(TextInstance2);
        ContainerInstance.Children.Add(TextInstance3);
        ContainerInstance.Children.Add(TextInstance4);
        this.WhatThisContains.Add(EmptyComponentInstance);
    }
    private void ApplyDefaultVariables()
    {
        this.ContainerInstance.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
        this.ContainerInstance.Height = 358f;
        this.ContainerInstance.Width = 256f;
        this.ContainerInstance.X = 0f;
        this.ContainerInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.ContainerInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.ContainerInstance.Y = 0f;
        this.ContainerInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.ContainerInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;







    }
    partial void CustomInitialize();
}
