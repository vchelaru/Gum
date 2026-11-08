using Gum;
using Gum.Forms.Controls;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Engine;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    // A GraphicsCompositor must already exist when Initialize runs: Stride drives rendering
    // through that pipeline, so Initialize registers Gum's own scene renderer into it. From
    // here on Stride ticks Gum's Update/Draw every frame -- unlike every other Gum runtime,
    // there is nothing for this sample to call per frame.
    game.AddGraphicsCompositor().AddCleanUIStage();
    GumService.Default.Initialize(game);

    BuildDemoUi();

    game.Add3DCamera().Add3DCameraController();
    game.AddDirectionalLight();
    game.Add3DGround();
}

void BuildDemoUi()
{
    var stackPanel = new StackPanel();
    stackPanel.AddToRoot();
    stackPanel.X = 100;
    stackPanel.Y = 100;
    stackPanel.Spacing = 5;

    var label = new Label
    {
        Text = "Hello from Gum on Stride!",
    };
    stackPanel.AddChild(label);

    var button = new Button
    {
        Text = "Click me",
    };
    button.Click += (_, _) => label.Text = "Clicked!";
    stackPanel.AddChild(button);

    var textBox = new TextBox
    {
        Placeholder = "Type here",
    };
    stackPanel.AddChild(textBox);

    var checkBox = new CheckBox
    {
        Text = "Check me"
    };
    stackPanel.AddChild(checkBox);

    var listBox = new ListBox();
    for (int i = 0; i < 10; i++)
    {
        listBox.Items!.Add("Item " + i);
    }
    stackPanel.AddChild(listBox);
}
