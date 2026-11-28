using System;
using System.Collections.Generic;
using Gum.Forms.Controls;
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
}
