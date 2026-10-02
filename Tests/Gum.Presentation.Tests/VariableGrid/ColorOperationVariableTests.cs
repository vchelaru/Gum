using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Reflection;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Gum.Presentation.Tests.VariableGrid;

/// <summary>
/// The Sprite/NineSlice <c>ColorOperation</c> variable (#4880): shown in the Variables tab as an
/// enum row, hidden on projects that predate it, and round-tripped through save/load as an int
/// without changing the project version.
/// </summary>
public class ColorOperationVariableTests : BaseTestClass
{
    private readonly AutoMocker _mocker = new();
    private readonly ElementSaveDisplayer _displayer;
    private readonly GumProjectSave _project;
    private readonly ScreenSave _screen;
    private readonly StateSave _screenDefaultState;

    public ColorOperationVariableTests()
    {
        _project = new GumProjectSave { Version = GumProjectSave.NativeVersion };
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(_project);

        _screen = new ScreenSave { Name = "TestScreen" };
        _screenDefaultState = new StateSave { Name = "Default", ParentContainer = _screen };
        _screen.States.Add(_screenDefaultState);
        _project.Screens.Add(_screen);

        ObjectFinder.Self.GumProjectSave = _project;

        _mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedStateSave)
            .Returns(_screenDefaultState);
        _mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedElement)
            .Returns(_screen);

        _mocker.Use(StandardElementsManager.Self);

        TypeManager typeManager = new TypeManager();
        typeManager.Initialize();
        _mocker.Use(typeManager);

        _mocker.GetMock<IPluginManager>()
            .Setup(x => x.GetAttributesFor(It.IsAny<VariableSave>()))
            .Returns(new List<Attribute>());

        _mocker.Use<IVariableSaveLogic>(_mocker.CreateInstance<VariableSaveLogic>());

        _mocker.GetMock<IProjectState>()
            .Setup(x => x.GumProjectSave)
            .Returns(_project);

        _displayer = _mocker.CreateInstance<ElementSaveDisplayer>();
    }

    public override void Dispose()
    {
        VariableSaveExtensionMethods.CustomFixEnumerations = null;
        base.Dispose();
    }

    private List<VariableGridEntry> GetColorOperationMembers(string baseType)
    {
        InstanceSave instance = new InstanceSave
        {
            Name = "Instance",
            BaseType = baseType,
            ParentContainer = _screen
        };
        _screen.Instances.Add(instance);

        return _displayer.GetCategories(_screen, instance, _screenDefaultState, null)
            .SelectMany(category => category.Members)
            .Where(member => member.RootVariableName == "ColorOperation")
            .ToList();
    }

    [Theory]
    [InlineData("Sprite")]
    [InlineData("NineSlice")]
    public void GetCategories_ShouldShowColorOperationRow_OnCurrentVersionProject(string baseType)
    {
        List<VariableGridEntry> members = GetColorOperationMembers(baseType);

        members.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("Sprite")]
    [InlineData("NineSlice")]
    public void GetCategories_ShouldHideColorOperation_OnPreV5Project(string baseType)
    {
        _project.Version = (int)GumProjectSave.GumxVersions.LocalizeTextExpansion;

        GetColorOperationMembers(baseType).ShouldBeEmpty();
    }

    [Fact]
    public void TypeManager_ShouldResolveColorOperationByName()
    {
        TypeManager typeManager = new TypeManager();
        typeManager.Initialize();

        typeManager.GetTypeFromString("ColorOperation").ShouldBe(typeof(ColorOperation));
    }

    [Fact]
    public void SaveAndLoad_ShouldRoundTripColorOperationAsEnum_AndKeepProjectVersion()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "GumColorOpRoundTrip_" + Guid.NewGuid().ToString("N"));
        try
        {
            TypeManager typeManager = new TypeManager();
            typeManager.Initialize();
            ServiceCollection services = new ServiceCollection();
            services.AddSingleton<ITypeManager>(typeManager);
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            Locator.Register(serviceProvider);
            try
            {
                VariableSaveExtensionMethods.CustomFixEnumerations = VariableSaveExtensionMethodsGumTool.FixEnumerationsWithReflection;

                _screen.Instances.Add(new InstanceSave { Name = "SpriteInstance", BaseType = "Sprite", ParentContainer = _screen });
                _screenDefaultState.Variables.Add(new VariableSave
                {
                    Name = "SpriteInstance.ColorOperation",
                    Type = "ColorOperation",
                    Value = ColorOperation.Add,
                    SetsValue = true,
                });
                _project.ScreenReferences.Add(new ElementReference { Name = "TestScreen", ElementType = ElementType.Screen });

                Directory.CreateDirectory(Path.Combine(tempDirectory, ElementReference.ScreenSubfolder));
                Directory.CreateDirectory(Path.Combine(tempDirectory, ElementReference.StandardSubfolder));
                string gumxPath = Path.Combine(tempDirectory, "Test." + GumProjectSave.ProjectExtension);
                int versionBeforeSave = _project.Version;
                _project.Save(gumxPath, saveElements: true);

                GumProjectSave? loaded = GumProjectSave.Load(gumxPath, out GumLoadResult loadResult);
                loaded.ShouldNotBeNull();
                loadResult.ErrorMessage.ShouldBeNullOrEmpty();
                loaded.Initialize();

                loaded.Version.ShouldBe(versionBeforeSave);
                VariableSave variable = loaded.Screens.First().DefaultState.Variables
                    .First(item => item.Name == "SpriteInstance.ColorOperation");
                variable.Value.ShouldBe(ColorOperation.Add);
            }
            finally
            {
                // Locator has no Unregister.
                PropertyInfo providersProperty = typeof(Locator).GetProperty(
                    "ServiceProviders", BindingFlags.NonPublic | BindingFlags.Static)!;
                ((List<IServiceProvider>)providersProperty.GetValue(null)!).Remove(serviceProvider);
            }
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }
}
