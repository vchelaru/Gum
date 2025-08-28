namespace Gum.Gui.Windows;

public abstract class Window : System.Windows.Window
{
    public new virtual bool? ShowDialog() =>
        base.ShowDialog();
    
    // Implement here other methods you want to "virtualize" from base in order to be able to mock them
}