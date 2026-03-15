using Gum.DataTypes.Variables;
using Gum.Managers;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;

namespace MonoGameGum.Tests.V2.Runtimes;

public class NineSliceRuntimeTests : BaseTestClass
{
    #region IsTilingMiddleSections

    [Fact]
    public void IsTilingMiddleSections_ShouldBeFalseByDefault()
    {
        NineSliceRuntime sut = new();

        sut.IsTilingMiddleSections.ShouldBeFalse();
    }

    [Fact]
    public void IsTilingMiddleSections_ShouldForwardToContainedNineSlice()
    {
        NineSliceRuntime sut = new();

        sut.IsTilingMiddleSections = true;

        NineSlice renderable = (NineSlice)sut.RenderableComponent;
        renderable.IsTilingMiddleSections.ShouldBeTrue();
    }

    [Fact]
    public void IsTilingMiddleSections_ShouldNotBeAffectedByITextureCoordinateWrap()
    {
        NineSliceRuntime sut = new();
        sut.IsTilingMiddleSections = true;

        // Setting Wrap via the interface should not reset IsTilingMiddleSections
        ITextureCoordinate textureCoordinate = (ITextureCoordinate)sut.RenderableComponent;
        textureCoordinate.Wrap = false;

        NineSlice renderable = (NineSlice)sut.RenderableComponent;
        renderable.IsTilingMiddleSections.ShouldBeTrue();
    }

    #endregion

    #region StandardElementsManager

    [Fact]
    public void StandardElementsManager_ShouldHaveIsTilingMiddleSectionsVariable()
    {
        StateSave nineSliceDefaults = StandardElementsManager.Self.GetDefaultStateFor("NineSlice");

        nineSliceDefaults.Variables
            .ShouldContain(v => v.Name == "IsTilingMiddleSections");
    }

    [Fact]
    public void StandardElementsManager_IsTilingMiddleSections_ShouldDefaultToFalse()
    {
        StateSave nineSliceDefaults = StandardElementsManager.Self.GetDefaultStateFor("NineSlice");
        VariableSave variable = nineSliceDefaults.Variables
            .First(v => v.Name == "IsTilingMiddleSections");

        variable.Value.ShouldBe(false);
        variable.Type.ShouldBe("bool");
    }

    #endregion
}
