using Gum.Forms.Controls;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A path through a struct-typed view model property (Position.X) reads the struct member.
/// </summary>
public class StructMemberPathReadTests : BaseTestClass
{
    public struct Position
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    private class PlayerViewModel : ViewModel
    {
        public Position Position
        {
            get => Get<Position>();
            set => Set(value);
        }
    }

    [Fact]
    public void StructMemberPath_Displays()
    {
        PlayerViewModel vm = new() { Position = new Position { X = 3 } };
        TextBox textBox = new() { BindingContext = vm };

        textBox.SetBinding(nameof(TextBox.Text), "Position.X");

        textBox.Text.ShouldBe("3");
    }
}
