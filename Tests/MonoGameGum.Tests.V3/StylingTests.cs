using Gum.Forms.DefaultVisuals.V3;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V3;

public class StylingTests
{
    [Fact]
    public void ButtonVisual_ActiveStyleWithoutDefaults_ConstructsWithoutThrowing()
    {
        Styling saved = Styling.ActiveStyle;
        Styling withoutDefaults = new Styling(spriteSheet: null, useDefaults: false);

        try
        {
            Styling.ActiveStyle = withoutDefaults;

            ButtonVisual visual = Should.NotThrow(() => new ButtonVisual());

            visual.FormsControl.ShouldNotBeNull();
        }
        finally
        {
            Styling.ActiveStyle = saved;
        }
    }
}
