using System;
using System.Collections.Generic;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using GumRuntime;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A binding that writes to the view model reaches a setter of any visibility, as a visual's SetBinding does
/// through PropertyInfo.SetValue. A view model property with no setter still feeds a TwoWay binding (the
/// default mode) in the view model to control direction, while a OneWayToSource binding, which only writes,
/// fails at attach.
/// </summary>
public class SourceWithoutSetterBindingTests : BaseTestClass
{
    private readonly Action<string>? _previousReporter;
    private readonly List<string> _reports;

    public SourceWithoutSetterBindingTests()
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
        private readonly string _fullName;

        public ComputedViewModel(string fullName)
        {
            _fullName = fullName;
        }

        public string FullName => _fullName;
    }

    public class PrivateSetterViewModel
    {
        public PrivateSetterViewModel(string nickname)
        {
            Nickname = nickname;
        }

        public string Nickname { get; private set; }
    }

    public class BasePrivateSetterViewModel
    {
        public BasePrivateSetterViewModel(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }

    public class DerivedPrivateSetterViewModel : BasePrivateSetterViewModel
    {
        public DerivedPrivateSetterViewModel(string name) : base(name)
        {
        }
    }

    public interface IPerson
    {
        string FullName { get; }
    }

    public class SettablePerson : IPerson
    {
        public string FullName { get; set; } = string.Empty;
    }

    public class HolderViewModel
    {
        public readonly string Id = "fixed";

        public ComputedViewModel? Person { get; set; }

        public IPerson? Contact { get; set; }

        public DerivedPrivateSetterViewModel? Derived { get; set; }

        public object? Untyped { get; set; }

        public IReadOnlyList<string> Names { get; set; } = new[] { "first" };
    }

    [Fact]
    public void OneWayToSource_BasePrivateSetter_WritesControlValue()
    {
        TextBox textBox = new() { Text = "typed" };
        DerivedPrivateSetterViewModel viewModel = new("start");
        textBox.SetBinding(nameof(TextBox.Text),
            new Binding(nameof(DerivedPrivateSetterViewModel.Name)) { Mode = BindingMode.OneWayToSource });

        textBox.BindingContext = viewModel;
        textBox.Text = "edited";

        viewModel.Name.ShouldBe("edited");
    }

    [Fact]
    public void OneWayToSource_InterfaceTypedIntermediateWithGetOnlyLeaf_ThrowsNamingTheDeclaredType()
    {
        TextBox textBox = new() { Text = "typed" };
        HolderViewModel viewModel = new() { Contact = new SettablePerson() };
        textBox.BindingContext = viewModel;

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            textBox.SetBinding(nameof(TextBox.Text),
                new Binding("Contact.FullName") { Mode = BindingMode.OneWayToSource }));

        exception.Message.ShouldBe(
            "IPerson.FullName has no setter (IPerson is the declared type of HolderViewModel.Contact), so it " +
            "cannot be the source of a OneWayToSource binding, which writes TextBox.Text to it. Give " +
            "IPerson.FullName a setter, or declare HolderViewModel.Contact as a type whose FullName has one.");
    }

    [Theory]
    [InlineData("Derived.Name")]
    [InlineData("Untyped.Name")]
    public void TwoWay_BasePrivateSetter_EditReachesViewModel(string path)
    {
        TextBox textBox = new();
        DerivedPrivateSetterViewModel inner = new("start");
        HolderViewModel viewModel = new() { Derived = inner, Untyped = inner };
        textBox.SetBinding(nameof(TextBox.Text), new Binding(path));

        textBox.BindingContext = viewModel;
        textBox.Text = "edited";

        inner.Name.ShouldBe("edited");
    }

    [Theory]
    [InlineData("Names[0]", "HolderViewModel.Names[0]")]
    [InlineData("Id", "HolderViewModel.Id")]
    public void OneWayToSource_LeafWithNoSetter_ThrowsNamingTheLeafOwner(string path, string expectedLeaf)
    {
        TextBox textBox = new() { Text = "typed" };
        HolderViewModel viewModel = new() { Person = new ComputedViewModel("Ada Lovelace") };
        textBox.BindingContext = viewModel;

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            textBox.SetBinding(nameof(TextBox.Text), new Binding(path) { Mode = BindingMode.OneWayToSource }));

        exception.Message.ShouldBe(
            $"{expectedLeaf} has no setter, so it cannot be the source of a OneWayToSource binding, " +
            "which writes TextBox.Text to it. Give the property a setter.");
    }

    [Fact]
    public void OneWayToSource_GetOnlySource_ThrowsAtAttachNamingViewModelAndProperty()
    {
        TextBox textBox = new() { Text = "typed" };
        ComputedViewModel viewModel = new("Ada Lovelace");
        textBox.SetBinding(nameof(TextBox.Text),
            new Binding(nameof(ComputedViewModel.FullName)) { Mode = BindingMode.OneWayToSource });

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            textBox.BindingContext = viewModel);

        exception.Message.ShouldBe(
            "ComputedViewModel.FullName has no setter, so it cannot be the source of a OneWayToSource binding, " +
            "which writes TextBox.Text to it. Give the property a setter.");
    }

    [Fact]
    public void OneWayToSource_PrivateSetterSource_WritesControlValue()
    {
        TextBox textBox = new() { Text = "typed" };
        PrivateSetterViewModel viewModel = new("start");
        textBox.SetBinding(nameof(TextBox.Text),
            new Binding(nameof(PrivateSetterViewModel.Nickname)) { Mode = BindingMode.OneWayToSource });

        textBox.BindingContext = viewModel;
        textBox.Text = "edited";

        viewModel.Nickname.ShouldBe("edited");
    }

    [Fact]
    public void TwoWay_GetOnlySource_ShowsValueAndIgnoresEditsWithoutReporting()
    {
        TextBox textBox = new();
        ComputedViewModel viewModel = new("Ada Lovelace");
        textBox.SetBinding(nameof(TextBox.Text), nameof(ComputedViewModel.FullName));

        textBox.BindingContext = viewModel;
        string? shown = textBox.Text;
        textBox.Text = "edited";

        shown.ShouldBe("Ada Lovelace");
        viewModel.FullName.ShouldBe("Ada Lovelace");
        _reports.ShouldBeEmpty();
    }

    [Fact]
    public void TwoWay_GetOnlySource_LabelShowsValue()
    {
        Label label = new();
        ComputedViewModel viewModel = new("Ada Lovelace");
        label.SetBinding(nameof(Label.Text), nameof(ComputedViewModel.FullName));

        label.BindingContext = viewModel;

        label.Text.ShouldBe("Ada Lovelace");
        _reports.ShouldBeEmpty();
    }

    [Fact]
    public void TwoWay_PrivateSetterBehindObjectTypedIntermediate_EditReachesViewModel()
    {
        TextBox textBox = new();
        PrivateSetterViewModel inner = new("start");
        HolderViewModel viewModel = new() { Untyped = inner };
        textBox.SetBinding(nameof(TextBox.Text), new Binding("Untyped.Nickname"));

        textBox.BindingContext = viewModel;
        textBox.Text = "edited";

        inner.Nickname.ShouldBe("edited");
    }

    [Fact]
    public void TwoWay_PrivateSetterSource_EditReachesViewModel()
    {
        TextBox textBox = new();
        PrivateSetterViewModel viewModel = new("start");
        textBox.SetBinding(nameof(TextBox.Text), nameof(PrivateSetterViewModel.Nickname));

        textBox.BindingContext = viewModel;
        string? shown = textBox.Text;
        textBox.Text = "edited";

        shown.ShouldBe("start");
        viewModel.Nickname.ShouldBe("edited");
    }
}
