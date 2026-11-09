using Gum.Forms.Controls;
using Gum.GueDeriving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if RAYLIB
namespace Examples.Shapes;
#elif SKIA
namespace SilkNetGum.Screens;
#else
namespace MonoGameGumInCode.Screens;
#endif

internal class StandardsScreen : FrameworkElement
{
#if RAYLIB
    // raylib loads loose files from the output folder.
    private const string ContentPrefix = "resources\\";
#else
    private const string ContentPrefix = "";
#endif

    public StandardsScreen() : base (new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        var innerContainer = new ContainerRuntime();
        innerContainer.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        innerContainer.StackSpacing = 2;
        this.AddChild(innerContainer);

        var entireFileNineSlice = new NineSliceRuntime();
        entireFileNineSlice.Width = 100;
        entireFileNineSlice.Height = 100;
        entireFileNineSlice.SourceFileName = ContentPrefix + "SquareFrame.png";
        innerContainer.AddChild(entireFileNineSlice);

        var partialFrame = new NineSliceRuntime();
        partialFrame.Height = 150;
        partialFrame.Width = 150;
        partialFrame.SourceFileName = ContentPrefix + "FrameSheet.png";
        partialFrame.TextureAddress = Gum.Managers.TextureAddress.Custom;
        partialFrame.TextureLeft = 438;
        partialFrame.TextureTop = 231;
        partialFrame.TextureWidth = 42;
        partialFrame.TextureHeight = 42;
        innerContainer.AddChild(partialFrame);


        var textRuntime = new TextRuntime();
        textRuntime.Text = "Hello world";
        innerContainer.AddChild(textRuntime);




    }
}
