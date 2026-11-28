using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gum.Forms.Controls;
using Gum.Forms.Controls.Games;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Reading a public property of a Forms control must not throw, so a debugger watch, a property grid or a
/// reflection-based sweep can read every property of every control (#8523).
/// </summary>
public class FormsControlPropertyReadTests : BaseTestClass
{
    [Fact]
    public void FormsControls_EveryPublicProperty_ReadsWithoutThrowing()
    {
        Type[] controlTypes = typeof(FrameworkElement).Assembly.GetTypes()
            .Where(type => type.IsPublic && !type.IsAbstract && !type.IsGenericTypeDefinition
                && type.Namespace != null && type.Namespace.StartsWith("Gum.Forms.Controls")
                && typeof(FrameworkElement).IsAssignableFrom(type)
                && type.GetConstructor(Type.EmptyTypes) != null
                && type.GetCustomAttribute<ObsoleteAttribute>() == null)
            .OrderBy(type => type.FullName)
            .ToArray();
        List<string> failures = new();
        int readCount = 0;

        foreach (Type controlType in controlTypes)
        {
            FrameworkElement control = (FrameworkElement)Activator.CreateInstance(controlType)!;
            // A control with no default visual registered forwards its layout properties to a null Visual.
            if (control.Visual == null)
            {
                continue;
            }
            foreach (PropertyInfo property in controlType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetGetMethod() == null || property.GetIndexParameters().Length > 0
                    || property.GetCustomAttribute<ObsoleteAttribute>() != null)
                {
                    continue;
                }
                readCount++;
                try
                {
                    property.GetValue(control);
                }
                catch (TargetInvocationException exception)
                {
                    failures.Add($"{controlType.Name}.{property.Name}: {exception.InnerException?.GetType().Name}");
                }
            }
        }

        controlTypes.ShouldContain(typeof(DialogBox));
        readCount.ShouldBeGreaterThan(500);
        failures.ShouldBeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Theory]
    [InlineData(typeof(Button))]
    [InlineData(typeof(CheckBox))]
    [InlineData(typeof(ComboBox))]
    [InlineData(typeof(DialogBox))]
    [InlineData(typeof(ListBox))]
    [InlineData(typeof(Slider))]
    [InlineData(typeof(TextBox))]
    public void InputReceiverMembers_HaveDefaultsAndRoundTripTabSequence(Type controlType)
    {
        FrameworkElement control = (FrameworkElement)Activator.CreateInstance(controlType)!;
        IInputReceiver next = new TextBox();
        dynamic receiver = control;

        ((List<Gum.Forms.Input.Keys>)receiver.IgnoredKeys).ShouldBeEmpty();
        ((bool)receiver.TakingInput).ShouldBeTrue();
        ((object?)receiver.NextInTabSequence).ShouldBeNull();
        receiver.NextInTabSequence = next;
        ((object?)receiver.NextInTabSequence).ShouldBeSameAs(next);
    }
}
