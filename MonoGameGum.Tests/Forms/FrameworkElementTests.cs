using MonoGameGum.Forms.Controls;
using NVorbis.Ogg;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class FrameworkElementTests : BaseTestClass
{
    [Fact]
    public void Loaded_ShouldBeCalled_WhenAddedToRoot()
    {
        Button button = new ();
        bool loadedCalled = false;
        button.Loaded += (_,_) => loadedCalled = true;
        button.AddToRoot();
        loadedCalled.ShouldBeTrue();
    }

    [Fact]
    public void Loaded_ShouldBeCalled_WhenParentIsAddedToRoot()
    {
        Button button = new();
        bool loadedCalled = false;
        button.Loaded += (_, _) => loadedCalled = true;
        Panel parent = new ();
        parent.AddChild(button);
        parent.AddToRoot();
        loadedCalled.ShouldBeTrue();
    }

    [Fact]
    public void Loaded_ShouldBeCalledMultipleTimes_IfAddedMultipleTimes()
    {
        Button button = new();
        int loadCallCount = 0;
        button.Loaded += (_, _) => loadCallCount++;
        Panel parent = new();
        parent.AddToRoot();
        parent.AddChild(button);

        button.Visual.Parent = null;
        parent.AddChild(button);

        loadCallCount.ShouldBe(2);

    }


    [Fact]
    public void EffectiveManagers_ShouldBeSet_IfAddedToRoot()
    {
        Button button = new();
        button.Visual.EffectiveManagers.ShouldBeNull();
        button.AddToRoot();
        button.Visual.EffectiveManagers.ShouldNotBeNull();
        button.Visual.Parent = null;
        button.Visual.EffectiveManagers.ShouldBeNull();
    }
}
