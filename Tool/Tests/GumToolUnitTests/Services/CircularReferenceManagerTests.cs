using AutoFixture;
using AutoFixture.AutoMoq;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Services;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumToolUnitTests.Services;
public class CircularReferenceManagerTests : BaseTestClass
{
    private readonly IFixture _fixture;

    CircularReferenceManager _sut;

    // Since ObjectFinder only provides 
    // methods to interact with a GumProjectSave,
    // we can use a concrete class and pass it a GumProjectSave
    // to avoid having to Setup a ton of methods for tests
    ObjectFinder _objectFinder;

    public CircularReferenceManagerTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        _objectFinder = new ObjectFinder();
        _objectFinder.GumProjectSave = new GumProjectSave();

        _sut = new CircularReferenceManager(
            _objectFinder);


    }

    [Fact]
    public void CanTypeBeAddedToElement_ShouldReturnTrue_ForUnrelatedTypes()
    {
        ComponentSave element1 = new()
        {
            Name = "Element1"
        };

        _objectFinder.GumProjectSave!.Components.Add(element1);

        _sut.CanTypeBeAddedToElement(element1, "SomeType").ShouldBeTrue();
    }

    [Fact]
    public void CanTypeBeAddedToElement_ShouldReturnFalse_ForSameType()
    {
        ComponentSave element1 = new()
        {
            Name = "Element1"
        };

        _objectFinder.GumProjectSave!.Components.Add(element1);


        _sut.CanTypeBeAddedToElement(element1, "Element1").ShouldBeFalse();
    }

    [Fact]
    public void CanTypeBeAddedToElement_ShouldReturnTrue_ForBaseInDerivedType()
    {
        ComponentSave derivedType = new()
        {
            Name = "DerviedType",
            BaseType = "BaseType"
        };
        ComponentSave baseElement = new()
        {
            Name = "BaseType"
        };
        _objectFinder.GumProjectSave!.Components.Add(derivedType);
        _objectFinder.GumProjectSave.Components.Add(baseElement);
        _sut.CanTypeBeAddedToElement(derivedType, "BaseType").ShouldBeTrue();
    }

    [Fact]
    public void CanTypeBeAddedToElement_ShouldReturnFalse_ForDerivedInBaseType()
    {
        ComponentSave derivedType = new()
        {
            Name = "DerviedType",
            BaseType = "BaseType"
        };
        ComponentSave baseElement = new()
        {
            Name = "BaseType"
        };

        _objectFinder.GumProjectSave!.Components.Add(derivedType);
        _objectFinder.GumProjectSave.Components.Add(baseElement);
        _sut.CanTypeBeAddedToElement(baseElement, "DerviedType").ShouldBeFalse();
    }

    [Fact]
    public void CanBeAddedToElement_ShouldReturnTrue_ForAddingSameInstances()
    {
        ComponentSave first = new()
        {
            Name = "First"
        };
        ComponentSave second = new()
        {
            Name = "Second"
        };

        first.Instances.Add(new InstanceSave
        {
            BaseType = "Second"
        });

        _objectFinder.GumProjectSave!.Components.Add(first);
        _objectFinder.GumProjectSave.Components.Add(second);

        _sut.CanTypeBeAddedToElement(first, "Second").ShouldBeTrue();

    }

    [Fact]
    public void CanBeAddedToElement_ShouldReturnFalse_ForCircularInstanceReferences()
    {
        ComponentSave first = new()
        {
            Name = "First"
        };
        ComponentSave second = new()
        {
            Name = "Second"
        };

        first.Instances.Add(new InstanceSave
        {
            BaseType = "Second"
        });

        _objectFinder.GumProjectSave!.Components.Add(first);
        _objectFinder.GumProjectSave.Components.Add(second);

        _sut.CanTypeBeAddedToElement(second, "First").ShouldBeFalse("because the second is already in the first");
    }
}