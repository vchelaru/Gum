using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Graphics.Animation;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// CurrentChainName is the one typed variable the shared TypedSetterSweep cannot drive: the property keeps
/// only the name of a chain the element actually has, so the sweep's invented value reads back as null. This
/// gives both elements the same chains and checks that a same-component reference follows an assignment (#6410).
/// </summary>
public class CurrentChainNameReferenceTests : BaseTestClass
{
    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static AnimationChainList CreateChains()
    {
        Texture2D texture = (Texture2D)RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        AnimationChain walk = new AnimationChain { Name = "Walk" };
        walk.Add(new AnimationFrame { FrameLength = 1, Texture = texture, RightCoordinate = 1, BottomCoordinate = 1 });
        AnimationChain run = new AnimationChain { Name = "Run" };
        run.Add(new AnimationFrame { FrameLength = 1, Texture = texture, RightCoordinate = 1, BottomCoordinate = 1 });
        return new AnimationChainList { walk, run };
    }

    [Theory]
    [InlineData("Sprite")]
    [InlineData("NineSlice")]
    public void CurrentChainName_SetInCode_SameComponentReferenceFollows(string standard)
    {
        StandardElementsManager.Self.Initialize();
        GumProjectSave project = new GumProjectSave();
        foreach (string name in new[] { "Sprite", "NineSlice" })
        {
            StandardElementSave element = new StandardElementSave { Name = name };
            element.States.Add(StandardElementsManager.Self.GetDefaultStateFor(name)!);
            element.DefaultState!.ParentContainer = element;
            project.StandardElements.Add(element);
        }
        ObjectFinder.Self.GumProjectSave = project;
        ElementSaveExtensions.RegisterGueInstantiation("Sprite", () => new SpriteRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("NineSlice", () => new NineSliceRuntime());

        ComponentSave component = new ComponentSave { Name = "Label", BaseType = standard };
        StateSave state = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(state);
        component.Instances.Add(new InstanceSave { Name = "Shadow", BaseType = standard, ParentContainer = component });
        VariableListSave<string> rows = new VariableListSave<string> { Type = "string", Name = "Shadow.VariableReferences" };
        rows.Value.Add("CurrentChainName = CurrentChainName");
        state.VariableLists.Add(rows);
        project.Components.Add(component);
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Label1", BaseType = "Label", ParentContainer = screen });
        project.Screens.Add(screen);

        GraphicalUiElement root = screen.ToGraphicalUiElement();
        GraphicalUiElement label = root.GetGraphicalUiElementByName("Label1")!;
        GraphicalUiElement shadow = label.GetGraphicalUiElementByName("Shadow")!;
        if (standard == "Sprite")
        {
            ((SpriteRuntime)label).AnimationChains = CreateChains();
            ((SpriteRuntime)shadow).AnimationChains = CreateChains();
            ((SpriteRuntime)label).CurrentChainName = "Run";
            ((SpriteRuntime)shadow).CurrentChainName.ShouldBe("Run");
        }
        else
        {
            ((NineSliceRuntime)label).AnimationChains = CreateChains();
            ((NineSliceRuntime)shadow).AnimationChains = CreateChains();
            ((NineSliceRuntime)label).CurrentChainName = "Run";
            ((NineSliceRuntime)shadow).CurrentChainName.ShouldBe("Run");
        }
    }
}
