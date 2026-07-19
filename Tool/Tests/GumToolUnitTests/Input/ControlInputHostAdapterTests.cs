using InputLibrary;
using Shouldly;
using System.Drawing;
using System.Windows.Forms;
using WinCursor = System.Windows.Forms.Cursor;

namespace GumToolUnitTests.Input;

public class ControlInputHostAdapterTests : BaseTestClass
{
    [StaFact]
    public void Cursor_Get_ForwardsToWrappedControl()
    {
        using Control control = new Control { Cursor = Cursors.Hand };
        ControlInputHostAdapter adapter = new ControlInputHostAdapter(control);

        adapter.Cursor.ShouldBe(Cursors.Hand);
    }

    [StaFact]
    public void Cursor_Set_ForwardsToWrappedControl()
    {
        using Control control = new Control();
        ControlInputHostAdapter adapter = new ControlInputHostAdapter(control);

        adapter.Cursor = Cursors.Cross;

        control.Cursor.ShouldBe(Cursors.Cross);
    }

    [StaFact]
    public void Focused_ForwardsToWrappedControl()
    {
        using Control control = new Control();
        ControlInputHostAdapter adapter = new ControlInputHostAdapter(control);

        adapter.Focused.ShouldBe(control.Focused);
    }

    [StaFact]
    public void PointToClient_ForwardsToWrappedControl()
    {
        using Control control = new Control { Width = 200, Height = 100 };
        ControlInputHostAdapter adapter = new ControlInputHostAdapter(control);
        Point screenPoint = new Point(50, 60);

        Point actual = adapter.PointToClient(screenPoint);

        actual.ShouldBe(control.PointToClient(screenPoint));
    }

    [StaFact]
    public void WidthAndHeight_ForwardToWrappedControl()
    {
        using Control control = new Control { Width = 320, Height = 240 };
        ControlInputHostAdapter adapter = new ControlInputHostAdapter(control);

        adapter.Width.ShouldBe(320);
        adapter.Height.ShouldBe(240);
    }
}
