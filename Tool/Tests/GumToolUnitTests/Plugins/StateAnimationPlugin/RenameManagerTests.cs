using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Wireframe;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Xunit;

namespace GumToolUnitTests.Plugins.StateAnimationPlugin;

/// <summary>
/// Characterization tests pinning the keyframe-mutating <see cref="RenameManager"/> overloads.
/// These were deferred when ACVMM/RenameManager were drained (#3281) because the animation view
/// models could not be constructed in isolation. They can now: <see cref="BitmapLoader"/> is an
/// injectable <see cref="IBitmapLoader"/> (stubbed here), the remaining ViewModel constructor
/// dependencies are satisfied via the service locator, and <see cref="ElementAnimationsViewModel"/>'s
/// WPF right-click <c>MenuItem</c> creation is handled by building it on an STA thread.
/// </summary>
public class RenameManagerTests : BaseTestClass
{
    private readonly IServiceProvider _testServiceProvider;
    private readonly IBitmapLoader _bitmapLoader = Mock.Of<IBitmapLoader>();

    public RenameManagerTests()
    {
        // AnimationViewModel/ElementAnimationsViewModel still resolve ISelectedState and
        // IWireframeObjectManager from the locator in their constructors.
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(Mock.Of<ISelectedState>());
        services.AddSingleton(Mock.Of<IWireframeObjectManager>());
        _testServiceProvider = services.BuildServiceProvider();
        Locator.Register(_testServiceProvider);
    }

    [Fact]
    public void HandleRename_Animation_RenamesMatchingKeyframeAnimationNames()
    {
        RunOnSta(() =>
        {
            // GetElementsReferencing walks the project; an empty project keeps the test on the
            // pure view-model mutation path (no file IO).
            ObjectFinder.Self.GumProjectSave = new GumProjectSave();

            AnimationViewModel animation = CreateAnimationWithKeyframes(
                Keyframe(animationName: "OldAnim"));
            AnimationViewModel renamed = new AnimationViewModel(_bitmapLoader) { Name = "NewAnim" };

            RenameManager renameManager = CreateRenameManager();

            renameManager.HandleRename(
                renamed, "OldAnim", new[] { animation }, new ComponentSave { Name = "Foo" });

            animation.Keyframes[0].AnimationName.ShouldBe("NewAnim");
        });
    }

    [Fact]
    public void HandleRename_Instance_RenamesQualifiedAnimationNamePrefix()
    {
        RunOnSta(() =>
        {
            ElementAnimationsViewModel viewModel = CreateViewModelWith(
                CreateAnimationWithKeyframes(Keyframe(animationName: "OldInstance.Walk")));

            RenameManager renameManager = CreateRenameManager();

            renameManager.HandleRename(
                new InstanceSave { Name = "NewInstance" }, "OldInstance", viewModel);

            viewModel.Animations[0].Keyframes[0].AnimationName.ShouldBe("NewInstance.Walk");
        });
    }

    [Fact]
    public void HandleRename_State_RenamesMatchingKeyframeStateName()
    {
        RunOnSta(() =>
        {
            ElementAnimationsViewModel viewModel = CreateViewModelWith(
                CreateAnimationWithKeyframes(Keyframe(stateName: "OldState")));

            // No selected element -> uncategorized rename (no category prefix).
            RenameManager renameManager = CreateRenameManager();

            renameManager.HandleRename(
                new StateSave { Name = "NewState" }, "OldState", viewModel);

            viewModel.Animations[0].Keyframes[0].StateName.ShouldBe("NewState");
        });
    }

    [Fact]
    public void HandleRename_StateCategory_RenamesCategoryPrefixOnKeyframeStateNames()
    {
        RunOnSta(() =>
        {
            ElementAnimationsViewModel viewModel = CreateViewModelWith(
                CreateAnimationWithKeyframes(Keyframe(stateName: "OldCategory/Idle")));

            RenameManager renameManager = CreateRenameManager();

            renameManager.HandleRename(
                new StateSaveCategory { Name = "NewCategory" }, "OldCategory", viewModel);

            viewModel.Animations[0].Keyframes[0].StateName.ShouldBe("NewCategory/Idle");
        });
    }

    private AnimatedKeyframeViewModel Keyframe(string? stateName = null, string? animationName = null)
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel(_bitmapLoader);
        if (stateName != null)
        {
            keyframe.StateName = stateName;
        }
        if (animationName != null)
        {
            keyframe.AnimationName = animationName;
        }
        return keyframe;
    }

    private AnimationViewModel CreateAnimationWithKeyframes(params AnimatedKeyframeViewModel[] keyframes)
    {
        AnimationViewModel animation = new AnimationViewModel(_bitmapLoader);
        foreach (AnimatedKeyframeViewModel keyframe in keyframes)
        {
            animation.Keyframes.Add(keyframe);
        }
        return animation;
    }

    private ElementAnimationsViewModel CreateViewModelWith(params AnimationViewModel[] animations)
    {
        ElementAnimationsViewModel viewModel = new ElementAnimationsViewModel(
            Mock.Of<INameVerifier>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IAnimationCollectionViewModelManager>(),
            Mock.Of<IRenameManager>(),
            _bitmapLoader);
        foreach (AnimationViewModel animation in animations)
        {
            viewModel.Animations.Add(animation);
        }
        return viewModel;
    }

    private static RenameManager CreateRenameManager()
    {
        return new RenameManager(
            Mock.Of<ISelectedState>(),
            Mock.Of<IOutputManager>(),
            Mock.Of<IAnimationFilePathService>(),
            Mock.Of<IAnimationCollectionViewModelManager>());
    }

    /// <summary>
    /// WPF controls (the right-click MenuItems built in ElementAnimationsViewModel's constructor)
    /// require an STA thread; xUnit's default runner is MTA.
    /// </summary>
    private static void RunOnSta(Action action)
    {
        Exception? caught = null;
        Thread thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { caught = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (caught != null)
        {
            throw new Exception("Test body threw on the STA thread.", caught);
        }
    }

    public override void Dispose()
    {
        PropertyInfo prop = typeof(Locator).GetProperty(
            "ServiceProviders", BindingFlags.NonPublic | BindingFlags.Static)!;
        List<IServiceProvider> providers = (List<IServiceProvider>)prop.GetValue(null)!;
        providers.Remove(_testServiceProvider);

        base.Dispose();
    }
}
