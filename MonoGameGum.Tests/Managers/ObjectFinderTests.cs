using Gum.DataTypes;
using Gum.Managers;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Managers;
public class ObjectFinderTests
{
    [Fact]
    public void GetScreen_ShouldReturnScreen()
    {
        // ObjectFinder is used as a singleton in a variety of places in both FRB
        // and Gum. This isn't great, but this is a delecate refactor requiring testing all Gum 
        // runtimes including the one-off FlatRedBall implementation. Until that can be carefully
        // refactored, this must stay as a proper singleton. This test enforces that. Do not change
        // this from self access, and do not explicitly instantiate a ObjectFinder! Doing
        // so will no longer reflect how runtimes interact with ObjectFinder.
        var objectFinder = ObjectFinder.Self;

        var project = new GumProjectSave();
        project.Screens.Add(new ScreenSave
        {
            Name = "Screen1"
        });
        project.Screens.Add(new ScreenSave
        {
            Name = "Screen2"
        });


        objectFinder.GumProjectSave = project;



        objectFinder.GetScreen("Screen1").ShouldNotBeNull();
        objectFinder.GetScreen("Screen2").ShouldNotBeNull();
        objectFinder.GetScreen("Screen3").ShouldBeNull();


    }
}
