using Gum.DataTypes;
using Gum.Managers;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using Moq.AutoMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumToolUnitTests.PropertyGridHelpers;
public class SetVariableLogicTests : BaseTestClass
{
    private readonly AutoMocker mocker;

    private readonly SetVariableLogic _setVariableLogic;
    public SetVariableLogicTests()
    {
        mocker = new();

        _setVariableLogic = mocker.CreateInstance<SetVariableLogic>();
        StandardElementsManager.Self.Initialize();
    }

    [Fact(Skip ="ProjectManager.Self needs to be removed")]
    public void ReactToPropertyValueChanged_ShouldAddTextureAddressValues_WhenSettingTextureAddresOnNineSlice()
    {
        var container = new ComponentSave();
        container.States.Add(new Gum.DataTypes.Variables.StateSave());
        container.DefaultState.ParentContainer = container;
        container.DefaultState.SetValue(
            "NineSliceInstance.TextureAddress",
            TextureAddress.Custom);
        container.DefaultState.SetValue(
            "NineSliceInstance.SourceFile",
            "c:/MyFile.png");
        

        var instance = new InstanceSave();
        instance.Name = "NineSliceInstance";

        var selectedState = mocker.GetMock<ISelectedState>();
        selectedState
            .Setup(x => x.SelectedInstance)
            .Returns(instance);

        _setVariableLogic.ReactToPropertyValueChanged(
            "TextureAddress",
            TextureAddress.EntireTexture, 
            container,
            instance,
            container.DefaultState,
            refresh:true);
    }
}
