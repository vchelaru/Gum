using System;
using Gum.Reflection;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for <see cref="LateBinder"/>/<see cref="LateBinder{T}"/>,
/// written after relocating the class from Gum.csproj into Gum.Presentation (#3887). The class
/// is pure reflection (no WPF/WinForms types) and had zero live production call sites at the
/// time of the move, so these tests pin its existing get/set-via-reflection behavior rather
/// than asserting on any consumer.
/// </summary>
public class LateBinderTests
{
    public class SampleTarget
    {
        public string PublicProperty { get; set; } = "initial";
        public int PublicField = 10;
#pragma warning disable CS0414 // read only via reflection in these tests
        private string _privateField = "secret";
#pragma warning restore CS0414
    }

    [Fact]
    public void GetInstance_ShouldReturnSameInstance_ForSameType()
    {
        LateBinder first = LateBinder.GetInstance(typeof(SampleTarget));
        LateBinder second = LateBinder.GetInstance(typeof(SampleTarget));

        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void GetInstance_ShouldReturnDifferentInstances_ForDifferentTypes()
    {
        LateBinder sampleBinder = LateBinder.GetInstance(typeof(SampleTarget));
        LateBinder stringBinder = LateBinder.GetInstance(typeof(string));

        sampleBinder.ShouldNotBeSameAs(stringBinder);
    }

    [Fact]
    public void GetValue_ShouldReturnPublicPropertyValue()
    {
        LateBinder binder = LateBinder.GetInstance(typeof(SampleTarget));
        var target = new SampleTarget { PublicProperty = "hello" };

        object result = binder.GetValue(target, nameof(SampleTarget.PublicProperty));

        result.ShouldBe("hello");
    }

    [Fact]
    public void SetValue_ShouldSetPublicPropertyValue()
    {
        LateBinder binder = LateBinder.GetInstance(typeof(SampleTarget));
        var target = new SampleTarget();

        binder.SetValue(target, nameof(SampleTarget.PublicProperty), "updated");

        target.PublicProperty.ShouldBe("updated");
    }

    [Fact]
    public void GetValue_ShouldReturnPublicFieldValue()
    {
        LateBinder binder = LateBinder.GetInstance(typeof(SampleTarget));
        var target = new SampleTarget { PublicField = 42 };

        object result = binder.GetValue(target, nameof(SampleTarget.PublicField));

        result.ShouldBe(42);
    }

    [Fact]
    public void SetValue_ShouldSetPublicFieldValue()
    {
        LateBinder binder = LateBinder.GetInstance(typeof(SampleTarget));
        var target = new SampleTarget();

        binder.SetValue(target, nameof(SampleTarget.PublicField), 99);

        target.PublicField.ShouldBe(99);
    }

    [Fact]
    public void GetValue_ShouldReturnPrivateFieldValue()
    {
        LateBinder binder = LateBinder.GetInstance(typeof(SampleTarget));
        var target = new SampleTarget();

        object result = binder.GetValue(target, "_privateField");

        result.ShouldBe("secret");
    }

    [Fact]
    public void Indexer_ShouldThrow_WhenTargetIsNotSet()
    {
        var binder = new LateBinder<SampleTarget>();

        Should.Throw<InvalidOperationException>(() => binder["PublicProperty"]);
    }

    [Fact]
    public void Indexer_ShouldUseBoundTarget_WhenTargetIsSet()
    {
        var target = new SampleTarget();
        var binder = new LateBinder<SampleTarget>(target);

        binder["PublicProperty"] = "bound";

        binder["PublicProperty"].ShouldBe("bound");
    }

    [Fact]
    public void GenericIndexer_ShouldGetAndSetPropertyOnGivenTarget()
    {
        LateBinder<SampleTarget> binder = LateBinder<SampleTarget>.Instance;
        var target = new SampleTarget();

        binder[target, nameof(SampleTarget.PublicProperty)] = "via indexer";

        binder[target, nameof(SampleTarget.PublicProperty)].ShouldBe("via indexer");
    }

    [Fact]
    public void GetProperty_Generic_ShouldReturnTypedValue()
    {
        LateBinder<SampleTarget> binder = LateBinder<SampleTarget>.Instance;
        var target = new SampleTarget { PublicProperty = "typed" };

        string result = binder.GetProperty<string>(target, nameof(SampleTarget.PublicProperty));

        result.ShouldBe("typed");
    }

    [Fact]
    public void SetProperty_Generic_ShouldSetPropertyValue()
    {
        LateBinder<SampleTarget> binder = LateBinder<SampleTarget>.Instance;
        var target = new SampleTarget();

        binder.SetProperty(target, nameof(SampleTarget.PublicProperty), "set via generic");

        target.PublicProperty.ShouldBe("set via generic");
    }

    [Fact]
    public void SetProperty_Generic_ShouldFallBackToPrivateField_WhenNoMatchingProperty()
    {
        LateBinder<SampleTarget> binder = LateBinder<SampleTarget>.Instance;
        var target = new SampleTarget();

        binder.SetProperty(target, "_privateField", "changed via reflection");

        binder.GetField<string>(target, "_privateField").ShouldBe("changed via reflection");
    }

    [Fact]
    public void GetField_Generic_ShouldReturnTypedFieldValue()
    {
        LateBinder<SampleTarget> binder = LateBinder<SampleTarget>.Instance;
        var target = new SampleTarget { PublicField = 7 };

        int result = binder.GetField<int>(target, nameof(SampleTarget.PublicField));

        result.ShouldBe(7);
    }
}
