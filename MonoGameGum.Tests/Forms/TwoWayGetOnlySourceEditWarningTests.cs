using System;
using System.Collections.Generic;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using GumRuntime;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A TwoWay binding (the default mode) to a view model property with no setter shows the source value and drops
/// what the user enters, so the first dropped user edit is reported once per binding. A value set from code is not
/// a user edit and stays silent (see SourceWithoutSetterBindingTests), as does display-only use.
/// </summary>
public class TwoWayGetOnlySourceEditWarningTests : BeginnerDriverTestBase
{
    private readonly Action<string>? _previousReporter;
    private readonly List<string> _reports;

    public TwoWayGetOnlySourceEditWarningTests()
    {
        _previousReporter = ElementSaveExtensions.ReportPropertyAssignmentError;
        _reports = new List<string>();
        ElementSaveExtensions.ReportPropertyAssignmentError = _reports.Add;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.ReportPropertyAssignmentError = _previousReporter;
        base.Dispose();
    }

    public class ComputedViewModel
    {
        public string FullName => "Ada Lovelace";

        public float Volume => 0;

        public bool IsMuted => false;

        public readonly string Id = "fixed";
    }

    public class OtherComputedViewModel
    {
        public string FullName => "Grace Hopper";
    }

    [Fact]
    public void TextBox_TypedIntoGetOnlySourcesOfTwoTypes_ReportsOncePerType()
    {
        TextBox textBox = AddToRoot(new TextBox { Width = 200 }, "Tb");
        textBox.SetBinding(nameof(TextBox.Text), nameof(ComputedViewModel.FullName));
        textBox.BindingContext = new ComputedViewModel();
        Driver.Click("Tb");

        Driver.Type("x");
        textBox.BindingContext = new OtherComputedViewModel();
        Driver.Type("y");
        textBox.BindingContext = new ComputedViewModel();
        Driver.Type("z");

        _reports.Count.ShouldBe(2);
        _reports[0].ShouldStartWith("TextBox.Text was changed by the user, but ComputedViewModel.FullName has no setter");
        _reports[1].ShouldStartWith("TextBox.Text was changed by the user, but OtherComputedViewModel.FullName has no setter");
    }

    public class PrivateSetterViewModel
    {
        public string Nickname { get; private set; } = "start";
    }

    [Fact]
    public void TextBox_TypedIntoGetOnlySource_ReportsOnceAndLeavesViewModel()
    {
        TextBox textBox = AddToRoot(new TextBox { Width = 200 }, "Tb");
        textBox.SetBinding(nameof(TextBox.Text), nameof(ComputedViewModel.FullName));
        ComputedViewModel viewModel = new();
        textBox.BindingContext = viewModel;
        Driver.Click("Tb");

        Driver.Type("x");
        Driver.Type("y");

        viewModel.FullName.ShouldBe("Ada Lovelace");
        _reports.ShouldBe(new[]
        {
            "TextBox.Text was changed by the user, but ComputedViewModel.FullName has no setter, so the view " +
            "model was not changed. Bind with Mode = OneWay for a display-only control, or give the property a setter."
        });
    }

    [Fact]
    public void TextBox_TypedIntoFieldSource_ReportsOnce()
    {
        TextBox textBox = AddToRoot(new TextBox { Width = 200 }, "Tb");
        textBox.SetBinding(nameof(TextBox.Text), nameof(ComputedViewModel.Id));
        textBox.BindingContext = new ComputedViewModel();
        Driver.Click("Tb");

        Driver.Type("x");

        _reports.ShouldBe(new[]
        {
            "TextBox.Text was changed by the user, but ComputedViewModel.Id is a field, which bindings never " +
            "write, so the view model was not changed. Bind with Mode = OneWay for a display-only control, or " +
            "make it a property with a setter."
        });
    }

    [Fact]
    public void TextBox_LostFocusTrigger_TypedIntoGetOnlySource_ReportsWhenFocusLeaves()
    {
        StackPanel panel = AddToRoot(new StackPanel(), "Panel");
        TextBox textBox = new() { Name = "Tb", Width = 200 };
        Button other = new() { Name = "Other" };
        panel.AddChild(textBox);
        panel.AddChild(other);
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(ComputedViewModel.FullName))
        {
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
        });
        textBox.BindingContext = new ComputedViewModel();
        Driver.Step();
        Driver.Click("Tb");
        Driver.Type("x");
        int reportsWhileTyping = _reports.Count;

        Driver.Click("Other");

        reportsWhileTyping.ShouldBe(0);
        _reports.Count.ShouldBe(1);
        _reports[0].ShouldStartWith("TextBox.Text was changed by the user, but ComputedViewModel.FullName has no setter");
    }

    [Fact]
    public void Slider_DraggedWithGetOnlySource_ReportsOnce()
    {
        StackPanel panel = AddToRoot(new StackPanel(), "Panel");
        Slider slider = new() { Name = "Sl", Width = 200, Minimum = 0, Maximum = 100 };
        panel.AddChild(slider);
        slider.SetBinding(nameof(Slider.Value), nameof(ComputedViewModel.Volume));
        panel.BindingContext = new ComputedViewModel();
        Driver.Step();
        (int x, int y) = Driver.CenterOf("Sl");

        Driver.Drag("ThumbInstance", x + 400, y);

        slider.Value.ShouldBe(100, 0.001);
        _reports.Count.ShouldBe(1);
        _reports[0].ShouldStartWith("Slider.Value was changed by the user, but ComputedViewModel.Volume has no setter");
    }

    [Fact]
    public void CheckBox_ClickedWithGetOnlySource_ReportsOnce()
    {
        CheckBox checkBox = AddToRoot(new CheckBox(), "Mute");
        checkBox.SetBinding(nameof(CheckBox.IsChecked), nameof(ComputedViewModel.IsMuted));
        checkBox.BindingContext = new ComputedViewModel();
        Driver.Step();

        Driver.Click("Mute");
        Driver.Click("Mute");

        _reports.Count.ShouldBe(1);
        _reports[0].ShouldStartWith("CheckBox.IsChecked was changed by the user, but ComputedViewModel.IsMuted has no setter");
    }

    [Fact]
    public void TextBox_TypedIntoPrivateSetterSource_WritesWithoutReporting()
    {
        TextBox textBox = AddToRoot(new TextBox { Width = 200 }, "Tb");
        textBox.SetBinding(nameof(TextBox.Text), nameof(PrivateSetterViewModel.Nickname));
        PrivateSetterViewModel viewModel = new();
        textBox.BindingContext = viewModel;
        Driver.Click("Tb");

        Driver.Type("x");

        viewModel.Nickname.ShouldNotBe("start");
        _reports.ShouldBeEmpty();
    }

    [Fact]
    public void Controls_SetFromCodeWithGetOnlySource_DoNotReport()
    {
        StackPanel panel = AddToRoot(new StackPanel(), "Panel");
        TextBox textBox = new() { Width = 200 };
        Slider slider = new() { Width = 200, Minimum = 0, Maximum = 100 };
        CheckBox checkBox = new();
        panel.AddChild(textBox);
        panel.AddChild(slider);
        panel.AddChild(checkBox);
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(ComputedViewModel.FullName))
        {
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
        });
        slider.SetBinding(nameof(Slider.Value), nameof(ComputedViewModel.Volume));
        checkBox.SetBinding(nameof(CheckBox.IsChecked), nameof(ComputedViewModel.IsMuted));
        panel.BindingContext = new ComputedViewModel();

        textBox.Text = "edited";
        textBox.IsFocused = true;
        textBox.IsFocused = false;
        slider.Value = 50;
        checkBox.IsChecked = true;
        Driver.Step();

        _reports.ShouldBeEmpty();
    }
}
