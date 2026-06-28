using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Raylib_cs;

namespace Examples.Shapes;

// Raylib mirror of Samples/MonoGameGumInCode/MonoGameGumInCode/Screens/TextScreen.cs (#3414).
// Section order matches the MG screen so baked-shadow regressions are easy to spot side by side.
// Requires KernSmithRaylibFontCreator wired in Program.Main.
internal class TextScreen : FrameworkElement
{
    public TextScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        var container = new ContainerRuntime();
        container.WidthUnits = DimensionUnitType.RelativeToParent;
        container.HeightUnits = DimensionUnitType.RelativeToParent;
        container.X = 2;
        container.Y = 2;
        container.Width = -4;
        container.Height = -4;
        container.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        container.StackSpacing = 4;
        this.AddChild(container);

        var textRuntime = new TextRuntime();
        textRuntime.Text = "Hi, I'm default text";
        container.Children.Add(textRuntime);

        AddSectionLabel(container, "Baked drop shadow (HasDropshadow = true, first-enable defaults):");
        var shadowDefault = new TextRuntime();
        shadowDefault.Text = "Soft baked shadow";
        shadowDefault.FontSize = 24;
        shadowDefault.HasDropshadow = true;
        container.Children.Add(shadowDefault);

        AddSectionLabel(container, "Baked drop shadow (colored, offset, blur):");
        var shadowColored = new TextRuntime();
        shadowColored.Text = "Pink shadow";
        shadowColored.FontSize = 24;
        shadowColored.HasDropshadow = true;
        shadowColored.DropshadowColor = new Color(220, 40, 160, 220);
        shadowColored.DropshadowOffsetX = 2;
        shadowColored.DropshadowOffsetY = 4;
        shadowColored.DropshadowBlur = 4;
        container.Children.Add(shadowColored);

        AddSectionLabel(container, "Baked drop shadow + outline:");
        var shadowOutline = new TextRuntime();
        shadowOutline.Text = "Shadow and outline";
        shadowOutline.FontSize = 24;
        shadowOutline.OutlineThickness = 2;
        shadowOutline.HasDropshadow = true;
        container.Children.Add(shadowOutline);

        var withOutline = new TextRuntime();
        withOutline.Text = "I am text with OutlineThickness = 2";
        withOutline.FontSize = 24;
        withOutline.OutlineThickness = 2;
        container.Children.Add(withOutline);
    }

    private static void AddSectionLabel(ContainerRuntime container, string text)
    {
        var label = new TextRuntime();
        label.Text = text;
        label.FontSize = 14;
        container.Children.Add(label);
    }
}
