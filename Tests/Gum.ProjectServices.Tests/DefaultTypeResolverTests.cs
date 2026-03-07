using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class DefaultTypeResolverTests
{
    private readonly DefaultTypeResolver _sut;

    public DefaultTypeResolverTests()
    {
        _sut = new DefaultTypeResolver();
    }

    [Theory]
    [InlineData("Boolean", typeof(bool))]
    [InlineData("Int32", typeof(int))]
    [InlineData("Single", typeof(float))]
    [InlineData("String", typeof(string))]
    [InlineData("Double", typeof(double))]
    public void GetTypeFromString_ShouldResolveRegisteredTypes(string typeName, Type expectedType)
    {
        Type? result = _sut.GetTypeFromString(typeName);

        result.ShouldBe(expectedType);
    }

    [Fact]
    public void GetTypeFromString_ShouldReturnNull_ForUnknownType()
    {
        Type? result = _sut.GetTypeFromString("CompletelyFakeType");

        result.ShouldBeNull();
    }

    [Fact]
    public void RegisterType_ShouldMakeTypeResolvable()
    {
        _sut.RegisterType(typeof(Uri));

        Type? result = _sut.GetTypeFromString("Uri");

        result.ShouldBe(typeof(Uri));
    }
}
