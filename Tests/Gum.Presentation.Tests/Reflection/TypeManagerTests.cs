using Gum.DataTypes;
using Gum.Reflection;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests.Reflection;

public class TypeManagerTests
{
    private enum AddedLaterEnum { First, Second }

    [Fact]
    public void GetTypeFromString_RepeatedLookups_ReturnTheSameTypes()
    {
        TypeManager typeManager = new TypeManager();
        typeManager.Initialize();

        for (int i = 0; i < 2; i++)
        {
            typeManager.GetTypeFromString("DimensionUnitType").ShouldBe(typeof(DimensionUnitType));
            typeManager.GetTypeFromString("Gum.DataTypes.DimensionUnitType").ShouldBe(typeof(DimensionUnitType));
            typeManager.GetTypeFromString("DimensionUnitType?").ShouldBe(typeof(DimensionUnitType?));
            typeManager.GetTypeFromString("float").ShouldBe(typeof(float));
            typeManager.GetTypeFromString("NoSuchTypeAnywhere").ShouldBeNull();
        }
    }

    [Fact]
    public void GetTypeFromString_TypeAddedAfterAMiss_IsFound()
    {
        TypeManager typeManager = new TypeManager();
        typeManager.Initialize();
        typeManager.GetTypeFromString(nameof(AddedLaterEnum)).ShouldBeNull();

        typeManager.AddType(typeof(AddedLaterEnum));

        typeManager.GetTypeFromString(nameof(AddedLaterEnum)).ShouldBe(typeof(AddedLaterEnum));
        typeManager.GetTypeFromString(nameof(AddedLaterEnum) + "?").ShouldBe(typeof(AddedLaterEnum?));
    }
}
