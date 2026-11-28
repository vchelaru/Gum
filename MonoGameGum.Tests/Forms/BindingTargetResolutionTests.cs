using System;
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

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// SetBinding resolves the target name to a property of the control when it is called: a name that is not a
/// bindable property fails there with a message naming it, and a name that is one always resolves.
/// </summary>
public class BindingTargetResolutionTests : BaseTestClass
{
    public class ValueViewModel : ViewModel
    {
        public string? Value
        {
            get => Get<string?>();
            set => Set(value);
        }
    }

    public class FieldTargetButton : Button
    {
        public string? FieldTarget;
    }

    public class HidingButton : Button
    {
        public new string? Text
        {
            get => base.Text;
            set => base.Text = value;
        }
    }

    public class HidingTextRuntime : TextRuntime
    {
        public new string? Text
        {
            get => base.Text;
            set => base.Text = value;
        }
    }

    [Theory]
    [InlineData("DoesNotExist")]
    [InlineData("Txt")]
    [InlineData("text")]
    [InlineData("Visual.X")]
    [InlineData("Item[0]")]
    public void FormsSetBinding_NameThatIsNotAProperty_ThrowsNamingIt(string targetName)
    {
        Button button = new();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => button.SetBinding(targetName, nameof(ValueViewModel.Value)));

        exception.Message.ShouldContain(targetName);
    }

    [Fact]
    public void FormsSetBinding_PublicFieldTarget_ThrowsNamingIt()
    {
        FieldTargetButton button = new();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => button.SetBinding(nameof(FieldTargetButton.FieldTarget), nameof(ValueViewModel.Value)));

        exception.Message.ShouldContain(nameof(FieldTargetButton.FieldTarget));
    }

    [Fact]
    public void FormsSetBinding_PropertyHiddenWithNew_BindsDerivedProperty()
    {
        HidingButton button = new();

        button.SetBinding(nameof(HidingButton.Text), nameof(ValueViewModel.Value));
        button.BindingContext = new ValueViewModel { Value = "bound" };

        button.Text.ShouldBe("bound");
    }

    [Fact]
    public void RuntimeSetBinding_PropertyHiddenWithNew_BindsDerivedProperty()
    {
        HidingTextRuntime text = new();

        text.SetBinding(nameof(HidingTextRuntime.Text), nameof(ValueViewModel.Value));
        text.BindingContext = new ValueViewModel { Value = "bound" };

        text.Text.ShouldBe("bound");
    }

    [Fact]
    public void FormsSetBinding_EverySettableFormsProperty_ResolvesAtSetBinding()
    {
        List<string> failures = new();
        int checkedCount = 0;

        IEnumerable<Type> controlTypes = typeof(FrameworkElement).Assembly.GetTypes()
            .Where(type => type.IsPublic && !type.IsAbstract && !type.IsGenericTypeDefinition
                && type.Namespace == "Gum.Forms.Controls"
                && typeof(FrameworkElement).IsAssignableFrom(type)
                && type.GetConstructor(Type.EmptyTypes) != null
                && type.GetCustomAttribute<ObsoleteAttribute>() == null)
            .OrderBy(type => type.Name);

        foreach (Type controlType in controlTypes)
        {
            IEnumerable<string> names = controlType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.GetSetMethod() != null && property.GetIndexParameters().Length == 0
                    && property.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Select(property => property.Name)
                .Distinct()
                .OrderBy(name => name);

            foreach (string name in names)
            {
                try
                {
                    FrameworkElement control = (FrameworkElement)Activator.CreateInstance(controlType)!;
                    if (control.Visual == null)
                    {
                        continue;
                    }
                    // A few members are unimplemented stubs whose getter throws (NextInTabSequence on some
                    // controls); they are not binding targets.
                    PropertyInfo property = controlType.GetProperty(name)!;
                    try
                    {
                        if (property.CanRead)
                        {
                            property.GetValue(control);
                        }
                    }
                    catch (TargetInvocationException)
                    {
                        continue;
                    }
                    checkedCount++;
                    control.SetBinding(name, new Binding(nameof(ValueViewModel.Value)) { Mode = BindingMode.OneWay });
                }
                catch (Exception exception)
                {
                    failures.Add($"{controlType.Name}.{name}: threw {exception.GetType().Name} {exception.Message}");
                }
            }
        }

        checkedCount.ShouldBeGreaterThan(100);
        failures.ShouldBeEmpty(string.Join(Environment.NewLine, failures));
    }
}
