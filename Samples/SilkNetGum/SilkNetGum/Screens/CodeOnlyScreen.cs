using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaGum.GueDeriving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkNetGum.Screens;
internal class CodeOnlyScreen : BindableGue
{
    private ContainerRuntime root;

    public CodeOnlyScreen() : base(new InvisibleRenderable())
    {
        root = new ContainerRuntime();
        this.Children.Add(root);
        root.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        var random = new Random(0);

        for (int i = 0; i < 5; i++)
        {
            var rectangle = new RoundedRectangleRuntime();
            rectangle.Red = 50 + random.Next(150);
            rectangle.Green = 50 + random.Next(150);
            rectangle.Blue = 50 + random.Next(150);

            rectangle.Width = 50;
            rectangle.Height = 50;

            root.Children.Add(rectangle);
        }
    }
}
