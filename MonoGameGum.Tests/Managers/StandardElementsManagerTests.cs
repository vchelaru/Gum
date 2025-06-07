using Gum.Managers;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Managers;
public class StandardElementsManagerTests
{
    [Fact]
    public void Initialize_ShouldCreateStandardTypes()
    {
        // StandardElmementsManager is used as a singleton in a variety of places in both FRB
        // and Gum. This isn't great, but this is a delecate refactor requiring testing all Gum 
        // runtimes including the one-off FlatRedBall implementation. Until that can be carefully
        // refactored, this must stay as a proper singleton. This test enforces that. Do not change
        // this from self access, and do not explicitly instantiate a StandardElementsManager! Doing
        // so will no longer reflect how runtimes interact with StandardElementsManager.
        var self = StandardElementsManager.Self;

        self.RefreshDefaults();

        self.DefaultTypes.Count().ShouldBeGreaterThan(0);

        self.DefaultStates["Circle"].ShouldNotBeNull();
        self.DefaultStates["ColoredRectangle"].ShouldNotBeNull();
        self.DefaultStates["Component"].ShouldNotBeNull();
        self.DefaultStates["Container"].ShouldNotBeNull();
        self.DefaultStates["NineSlice"].ShouldNotBeNull();
        self.DefaultStates["Polygon"].ShouldNotBeNull();
        self.DefaultStates["Rectangle"].ShouldNotBeNull();
        self.DefaultStates["Screen"].ShouldNotBeNull();
        self.DefaultStates["Sprite"].ShouldNotBeNull();
        self.DefaultStates["Text"].ShouldNotBeNull();
    }
}
