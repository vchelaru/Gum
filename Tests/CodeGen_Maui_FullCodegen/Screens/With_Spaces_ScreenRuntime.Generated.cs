//Code for With Spaces Screen
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
partial class With_Spaces_ScreenRuntime : Gum.Wireframe.BindableGue
{
    public ContainerRuntime Container_With_Spaces { get; protected set; }
    public ContainerRuntime Parent_With_Spaces { get; protected set; }
    public ContainerRuntime Child_With_Spaces { get; protected set; }
    public Component_With_SpacesRuntime Component_With_SpacesInstance { get; protected set; }
    public Component_With_SpacesRuntime Component_With_SpacesInstance1 { get; protected set; }

    public With_Spaces_ScreenRuntime(bool fullInstantiation = true)
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
        Container_With_Spaces = new global::SkiaGum.GueDeriving.ContainerRuntime();
        Container_With_Spaces.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (Container_With_Spaces.ElementSave != null) Container_With_Spaces.AddStatesAndCategoriesRecursivelyToGue(Container_With_Spaces.ElementSave);
        if (Container_With_Spaces.ElementSave != null) Container_With_Spaces.SetInitialState();
        Container_With_Spaces.Name = "Container With Spaces";
        Parent_With_Spaces = new global::SkiaGum.GueDeriving.ContainerRuntime();
        Parent_With_Spaces.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (Parent_With_Spaces.ElementSave != null) Parent_With_Spaces.AddStatesAndCategoriesRecursivelyToGue(Parent_With_Spaces.ElementSave);
        if (Parent_With_Spaces.ElementSave != null) Parent_With_Spaces.SetInitialState();
        Parent_With_Spaces.Name = "Parent With Spaces";
        Child_With_Spaces = new global::SkiaGum.GueDeriving.ContainerRuntime();
        Child_With_Spaces.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (Child_With_Spaces.ElementSave != null) Child_With_Spaces.AddStatesAndCategoriesRecursivelyToGue(Child_With_Spaces.ElementSave);
        if (Child_With_Spaces.ElementSave != null) Child_With_Spaces.SetInitialState();
        Child_With_Spaces.Name = "Child With Spaces";
        Component_With_SpacesInstance = new CodeGen_Maui_FullCodegen.Components.Component_With_SpacesRuntime();
        Component_With_SpacesInstance.Name = "Component With SpacesInstance";
        Component_With_SpacesInstance1 = new CodeGen_Maui_FullCodegen.Components.Component_With_SpacesRuntime();
        Component_With_SpacesInstance1.Name = "Component With SpacesInstance1";
    }
    protected virtual void AssignParents()
    {
        this.WhatThisContains.Add(Container_With_Spaces);
        this.WhatThisContains.Add(Parent_With_Spaces);
        Parent_With_Spaces.Children.Add(Child_With_Spaces);
        this.WhatThisContains.Add(Component_With_SpacesInstance);
        this.WhatThisContains.Add(Component_With_SpacesInstance1);
    }
    private void ApplyDefaultVariables()
    {





    }
    partial void CustomInitialize();
}
