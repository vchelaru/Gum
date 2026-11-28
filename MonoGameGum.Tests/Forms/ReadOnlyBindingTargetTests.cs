using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.GueDeriving;
using Gum.Mvvm;
using Gum.Wireframe;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A binding whose mode writes the source value to the target (OneWay, TwoWay) needs a target property with a
/// public setter. SetBinding refuses one without, at the SetBinding call, instead of failing later when a
/// BindingContext arrives. OneWayToSource only reads the target, so it stays allowed.
/// </summary>
public class ReadOnlyBindingTargetTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;

    public ReadOnlyBindingTargetTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public class ShopViewModel : ViewModel
    {
        public IList? SelectedItems
        {
            get => Get<IList?>();
            set => Set(value);
        }

        public object? Value
        {
            get => Get<object?>();
            set => Set(value);
        }
    }

    private static IEnumerable<Type> ConstructibleTypes(Type baseType, Assembly assembly, string ns) =>
        assembly.GetTypes()
            .Where(type => type.IsPublic && !type.IsAbstract && !type.IsGenericTypeDefinition
                && type.Namespace == ns
                && baseType.IsAssignableFrom(type)
                && type.GetConstructor(Type.EmptyTypes) != null
                && type.GetCustomAttribute<ObsoleteAttribute>() == null)
            .OrderBy(type => type.Name);

    private static IEnumerable<PropertyInfo> ReadOnlyProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetSetMethod() == null
                && property.GetIndexParameters().Length == 0
                && property.GetCustomAttribute<ObsoleteAttribute>() == null)
            .GroupBy(property => property.Name)
            .Select(group => group.First())
            .OrderBy(property => property.Name);

    [Fact]
    public void SetBinding_ListBoxSelectedItems_DefaultMode_ThrowsAtSetBinding()
    {
        ListBox listBox = new();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => listBox.SetBinding(nameof(ListBox.SelectedItems), nameof(ShopViewModel.SelectedItems)));

        exception.Message.ShouldBe(
            "ListBox.SelectedItems has no public setter, so it cannot be the target of a TwoWay binding, which " +
            "writes the view model's value to it. Bind a property that has a public setter.");
    }

    [Fact]
    public void SetBinding_ListBoxSelectedObject_FollowsViewModelSelection()
    {
        ListBox listBox = new();
        listBox.Items.Add("a");
        listBox.Items.Add("b");
        ShopViewModel viewModel = new() { Value = "b" };

        listBox.SetBinding(nameof(ListBox.SelectedObject), nameof(ShopViewModel.Value));
        listBox.BindingContext = viewModel;
        listBox.SelectedIndex = 0;

        viewModel.Value.ShouldBe("a");
    }

    [Fact]
    public void SetBinding_ListBoxSelectedItems_LambdaOverloads_ThrowAtSetBinding()
    {
        ListBox listBox = new();
        ShopViewModel viewModel = new();

        Should.Throw<InvalidOperationException>(
            () => listBox.SetBinding<ShopViewModel>(nameof(ListBox.SelectedItems), vm => vm.SelectedItems));
        Should.Throw<InvalidOperationException>(
            () => listBox.SetBinding(nameof(ListBox.SelectedItems), () => viewModel.SelectedItems));
    }

    [Fact]
    public void SetBinding_ListBoxSelectedItems_OneWayToSource_AttachesWithoutThrowing()
    {
        ListBox listBox = new();
        listBox.SetBinding(nameof(ListBox.SelectedItems),
            new Binding(nameof(ShopViewModel.SelectedItems)) { Mode = BindingMode.OneWayToSource });

        Should.NotThrow(() => listBox.BindingContext = new ShopViewModel());
    }

    [Fact]
    public void SetBinding_ReadOnlyTarget_RejectedBinding_KeepsEarlierBindingOnOtherProperty()
    {
        ListBox listBox = new();
        ShopViewModel viewModel = new() { Value = "a" };
        listBox.Items.Add("a");
        listBox.Items.Add("b");
        listBox.SetBinding(nameof(ListBox.SelectedObject), nameof(ShopViewModel.Value));
        listBox.BindingContext = viewModel;

        Should.Throw<InvalidOperationException>(
            () => listBox.SetBinding(nameof(ListBox.SelectedItems), nameof(ShopViewModel.SelectedItems)));
        viewModel.Value = "b";

        listBox.SelectedObject.ShouldBe("b");
    }

    [Fact]
    public void SetBinding_EveryFormsReadOnlyProperty_WritingModes_ThrowAtSetBinding_OneWayToSourceAllowed()
    {
        List<string> failures = new();
        int checkedCount = 0;

        foreach (Type controlType in ConstructibleTypes(typeof(FrameworkElement), typeof(FrameworkElement).Assembly, "Gum.Forms.Controls"))
        {
            FrameworkElement probe;
            try
            {
                probe = (FrameworkElement)Activator.CreateInstance(controlType)!;
            }
            catch
            {
                continue;
            }
            if (probe.Visual == null)
            {
                continue;
            }

            foreach (PropertyInfo property in ReadOnlyProperties(controlType))
            {
                checkedCount++;
                string caseName = $"{controlType.Name}.{property.Name}";
                foreach (BindingMode mode in new[] { BindingMode.OneWay, BindingMode.TwoWay })
                {
                    FrameworkElement control = (FrameworkElement)Activator.CreateInstance(controlType)!;
                    try
                    {
                        control.SetBinding(property.Name, new Binding(nameof(ShopViewModel.Value)) { Mode = mode });
                        failures.Add($"{caseName} {mode}: SetBinding did not throw");
                    }
                    catch (InvalidOperationException exception)
                    {
                        if (!exception.Message.Contains(caseName))
                        {
                            failures.Add($"{caseName} {mode}: message does not name {caseName}: {exception.Message}");
                        }
                    }
                    catch (Exception exception)
                    {
                        failures.Add($"{caseName} {mode}: threw {exception.GetType().Name} {exception.Message}");
                    }
                }

                // A few input-receiver members are unimplemented stubs whose getter throws; nothing can read them.
                try
                {
                    property.GetValue(probe);
                }
                catch (TargetInvocationException)
                {
                    continue;
                }

                try
                {
                    FrameworkElement control = (FrameworkElement)Activator.CreateInstance(controlType)!;
                    control.SetBinding(property.Name,
                        new Binding(nameof(ShopViewModel.Value)) { Mode = BindingMode.OneWayToSource });
                    control.BindingContext = new ShopViewModel();
                }
                catch (Exception exception)
                {
                    failures.Add($"{caseName} OneWayToSource: threw {exception.GetType().Name} {exception.Message}");
                }
            }
        }

        _output.WriteLine($"Read-only Forms properties checked: {checkedCount}");
        checkedCount.ShouldBeGreaterThan(20);
        failures.ShouldBeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void SetBinding_EveryRuntimeReadOnlyProperty_ThrowsAtSetBinding()
    {
        List<string> failures = new();
        int checkedCount = 0;

        foreach (Type runtimeType in ConstructibleTypes(typeof(GraphicalUiElement), typeof(TextRuntime).Assembly, "Gum.GueDeriving"))
        {
            foreach (PropertyInfo property in ReadOnlyProperties(runtimeType))
            {
                checkedCount++;
                string caseName = $"{runtimeType.Name}.{property.Name}";
                try
                {
                    GraphicalUiElement runtime = (GraphicalUiElement)Activator.CreateInstance(runtimeType)!;
                    runtime.SetBinding(property.Name, nameof(ShopViewModel.Value));
                    failures.Add($"{caseName}: SetBinding did not throw");
                }
                catch (InvalidOperationException exception)
                {
                    if (!exception.Message.Contains(caseName))
                    {
                        failures.Add($"{caseName}: message does not name {caseName}: {exception.Message}");
                    }
                }
                catch (Exception exception)
                {
                    failures.Add($"{caseName}: threw {exception.GetType().Name} {exception.Message}");
                }
            }
        }

        _output.WriteLine($"Read-only runtime properties checked: {checkedCount}");
        checkedCount.ShouldBeGreaterThan(10);
        failures.ShouldBeEmpty(string.Join(Environment.NewLine, failures));
    }
}
