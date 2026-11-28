using System;
using System.Collections.Generic;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Mvvm;
using GumRuntime;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A TwoWay edit through a struct member on the path (Position.X) reaches the view model or is reported once.
/// Writing the member of a copy and dropping it is never silent.
/// </summary>
public class StructMemberPathWriteBackTests : BaseTestClass
{
    private readonly Action<string>? _previousReporter;
    private readonly List<string> _reports = new();

    public StructMemberPathWriteBackTests()
    {
        _previousReporter = ElementSaveExtensions.ReportPropertyAssignmentError;
        ElementSaveExtensions.ReportPropertyAssignmentError = _reports.Add;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.ReportPropertyAssignmentError = _previousReporter;
        base.Dispose();
    }

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
    public void TwoWayEdit_ReachesViewModelOrIsReported()
    {
        PlayerViewModel vm = new() { Position = new Position { X = 3 } };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), "Position.X");

        textBox.Text = "7";

        bool reachedViewModel = vm.Position.X == 7;
        (reachedViewModel || _reports.Count > 0).ShouldBeTrue(
            "the edit was dropped without reaching the view model or being reported");
    }

    public struct Inner
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    public struct FieldPoint
    {
        public float X;
        public float Y;
    }

    public struct Outer
    {
        public Inner Inner { get; set; }

        public FieldPoint Corner;

        public Inner FixedInner => default;
    }

    private class WorldViewModel : ViewModel
    {
        public Outer Outer
        {
            get => Get<Outer>();
            set => Set(value);
        }

        public List<Position> Positions { get; } = new();

        public Position Fixed => default;

        public Position PositionField;

        public Microsoft.Xna.Framework.Vector2 XnaPosition
        {
            get => Get<Microsoft.Xna.Framework.Vector2>();
            set => Set(value);
        }

        public System.Numerics.Vector2 NumericsPosition
        {
            get => Get<System.Numerics.Vector2>();
            set => Set(value);
        }

        public Microsoft.Xna.Framework.Vector2? NullablePosition
        {
            get => Get<Microsoft.Xna.Framework.Vector2?>();
            set => Set(value);
        }

        // Declared as object, so the rest of a path through it is resolved on the held object's runtime type.
        public object? Holder
        {
            get => Get<object?>();
            set => Set(value);
        }
    }

    private class Player
    {
        public Position Position { get; set; }
        public Microsoft.Xna.Framework.Vector2 Velocity { get; set; }
        public Position Fixed => default;
    }

    [Fact]
    public void TwoWayEdit_WritesNullableStructBack_ThroughPropertySetter()
    {
        WorldViewModel vm = new() { NullablePosition = new Microsoft.Xna.Framework.Vector2(3, 4) };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), "NullablePosition.X");
        textBox.Text.ShouldBe("3");

        textBox.Text = "7";

        vm.NullablePosition.ShouldBe(new Microsoft.Xna.Framework.Vector2(7, 4));
        _reports.ShouldBeEmpty();
    }

    [Fact]
    public void TwoWayEdit_PastRuntimeResolvedHolder_WritesStructBack()
    {
        Player player = new()
        {
            Position = new Position { X = 3, Y = 4 },
            Velocity = new Microsoft.Xna.Framework.Vector2(5, 6)
        };
        WorldViewModel vm = new() { Holder = player };
        TextBox positionBox = new() { BindingContext = vm };
        positionBox.SetBinding(nameof(TextBox.Text), "Holder.Position.X");
        TextBox velocityBox = new() { BindingContext = vm };
        velocityBox.SetBinding(nameof(TextBox.Text), "Holder.Velocity.X");

        positionBox.Text = "7";
        velocityBox.Text = "8";

        player.Position.X.ShouldBe(7);
        player.Position.Y.ShouldBe(4);
        player.Velocity.ShouldBe(new Microsoft.Xna.Framework.Vector2(8, 6));
        _reports.ShouldBeEmpty();
    }

    [Fact]
    public void TwoWayEdit_PastRuntimeResolvedHolder_ToStructBehindGetOnlyProperty_IsReportedOnce()
    {
        WorldViewModel vm = new() { Holder = new Player() };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), "Holder.Fixed.X");

        textBox.Text = "7";
        textBox.Text = "8";

        _reports.Count.ShouldBe(1);
        _reports[0].ShouldContain("Player.Fixed");
    }

    [Fact]
    public void Read_ThroughViewModelFieldHoldingStruct_ShowsMember()
    {
        WorldViewModel vm = new() { PositionField = new Position { X = 3 } };
        TextBox textBox = new() { BindingContext = vm };

        textBox.SetBinding(nameof(TextBox.Text), "PositionField.X");

        textBox.Text.ShouldBe("3");
    }

    [Fact]
    public void TwoWayEdit_WritesXnaVector2FieldBack_ThroughPropertySetter()
    {
        WorldViewModel vm = new() { XnaPosition = new Microsoft.Xna.Framework.Vector2(3, 4) };
        List<string?> changed = new();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), "XnaPosition.X");

        textBox.Text = "7";

        vm.XnaPosition.ShouldBe(new Microsoft.Xna.Framework.Vector2(7, 4));
        changed.ShouldContain(nameof(WorldViewModel.XnaPosition));
        _reports.ShouldBeEmpty();
    }

    [Fact]
    public void TwoWayEdit_WritesNumericsVector2FieldBack_ThroughPropertySetter()
    {
        WorldViewModel vm = new() { NumericsPosition = new System.Numerics.Vector2(3, 4) };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), "NumericsPosition.Y");

        textBox.Text = "7";

        vm.NumericsPosition.ShouldBe(new System.Numerics.Vector2(3, 7));
        _reports.ShouldBeEmpty();
    }

    [Fact]
    public void LostFocusEdit_WritesStructFieldBack_OnLostFocus()
    {
        WorldViewModel vm = new() { XnaPosition = new Microsoft.Xna.Framework.Vector2(3, 4) };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text),
            new Binding("XnaPosition.X") { UpdateSourceTrigger = UpdateSourceTrigger.LostFocus });

        textBox.IsFocused = true;
        textBox.Text = "7";
        vm.XnaPosition.X.ShouldBe(3);

        textBox.IsFocused = false;

        vm.XnaPosition.ShouldBe(new Microsoft.Xna.Framework.Vector2(7, 4));
    }

    [Fact]
    public void TwoWayEdit_WritesBackThroughNestedStructFields()
    {
        WorldViewModel vm = new() { Outer = new Outer { Corner = new FieldPoint { X = 3, Y = 4 } } };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), "Outer.Corner.X");

        textBox.Text = "7";

        vm.Outer.Corner.X.ShouldBe(7);
        vm.Outer.Corner.Y.ShouldBe(4);
        _reports.ShouldBeEmpty();
    }

    [Fact]
    public void Read_ShowsStructMember_AndFollowsReplacedStruct()
    {
        PlayerViewModel vm = new() { Position = new Position { X = 3 } };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), "Position.X");

        textBox.Text.ShouldBe("3");

        vm.Position = new Position { X = 5 };

        textBox.Text.ShouldBe("5");
    }

    [Fact]
    public void TwoWayEdit_WritesBackThroughNestedStructs()
    {
        WorldViewModel vm = new() { Outer = new Outer { Inner = new Inner { X = 3, Y = 4 } } };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), "Outer.Inner.X");

        textBox.Text = "7";

        vm.Outer.Inner.X.ShouldBe(7);
        vm.Outer.Inner.Y.ShouldBe(4);
        _reports.ShouldBeEmpty();
    }

    [Fact]
    public void TwoWayEdit_WritesBackToStructHeldInList()
    {
        WorldViewModel vm = new();
        vm.Positions.Add(new Position { X = 3, Y = 4 });
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), "Positions[0].X");

        textBox.Text = "7";

        vm.Positions[0].X.ShouldBe(7);
        vm.Positions[0].Y.ShouldBe(4);
        _reports.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Fixed.X", "Fixed")]
    [InlineData("Outer.FixedInner.X", "FixedInner")]
    [InlineData("PositionField.X", "PositionField")]
    public void TwoWayEdit_ToStructBehindGetOnlyMember_IsReportedOnce(string path, string holderName)
    {
        WorldViewModel vm = new();
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), path);
        _reports.ShouldBeEmpty();

        textBox.Text = "7";
        textBox.Text = "8";

        vm.Fixed.X.ShouldBe(0);
        vm.Outer.FixedInner.X.ShouldBe(0);
        vm.PositionField.X.ShouldBe(0);
        _reports.Count.ShouldBe(1);
        _reports[0].ShouldContain(holderName);
    }

    [Fact]
    public void OneWayToSource_WritesBackThroughStruct()
    {
        PlayerViewModel vm = new() { Position = new Position { X = 3, Y = 4 } };
        TextBox textBox = new() { Text = "7", BindingContext = vm };

        textBox.SetBinding(nameof(TextBox.Text), new Binding("Position.X") { Mode = BindingMode.OneWayToSource });
        vm.Position.X.ShouldBe(7);

        textBox.Text = "9";

        vm.Position.X.ShouldBe(9);
        vm.Position.Y.ShouldBe(4);
    }

    [Fact]
    public void OneWayToSource_ToStructBehindGetOnlyProperty_Throws()
    {
        WorldViewModel vm = new();
        TextBox textBox = new() { BindingContext = vm };

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            textBox.SetBinding(nameof(TextBox.Text), new Binding("Fixed.X") { Mode = BindingMode.OneWayToSource }));

        exception.Message.ShouldContain("Fixed");
    }
}
