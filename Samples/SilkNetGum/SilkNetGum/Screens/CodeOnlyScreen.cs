using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Gum.GueDeriving;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkNetGum.Screens;
internal class CodeOnlyScreen : GraphicalUiElement
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
            var rectangle = new RectangleRuntime();
            rectangle.FillColor = new SKColor(
                (byte)(50 + random.Next(150)),
                (byte)(50 + random.Next(150)),
                (byte)(50 + random.Next(150)));
            rectangle.IsFilled = true;

            rectangle.Width = 50;
            rectangle.Height = 50;

            root.Children.Add(rectangle);
        }
    }
}
