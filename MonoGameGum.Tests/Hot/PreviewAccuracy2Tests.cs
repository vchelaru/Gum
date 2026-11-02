#pragma warning disable CS0618
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using static MonoGameGum.Tests.Hot.PreviewAccuracy2World;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// Preview accuracy sweep, round 2: fonts, textures, animation files, shaders, localization, variable
/// references and the tool's multi-file saves. One edit applied to a baseline project, then the hot reloaded
/// tree (with layout results, resolved fonts and texture sizes) compared with a restart of the saved folder
/// and the reload's <see cref="HotReloadResult"/> checked against it. A false negative (the tree differs and
/// the result says it matches) leaves Preview stale without anyone knowing; a false positive (the tree is
/// right and the result asks for a restart) rebuilds Preview for nothing.
/// </summary>
public partial class PreviewAccuracy2Tests : BaseTestClass
{
    private readonly List<PreviewAccuracy2World> _worlds = new List<PreviewAccuracy2World>();
    private readonly ITestOutputHelper _output;

    public PreviewAccuracy2Tests(ITestOutputHelper output)
    {
        _output = output;
    }

    public override void Dispose()
    {
        foreach (PreviewAccuracy2World world in _worlds)
        {
            world.Manager?.Stop();
            world.Dispose();
        }
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private PreviewAccuracy2World Boot(string shown = "MainScreen", bool separateBin = false)
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World();
        _worlds.Add(world);
        world.Boot(shown, separateBin);
        return world;
    }

    private PreviewVerdict2 Run(Action<GumProjectSave> edit, Action<string>? assets = null, string shown = "MainScreen", bool separateBin = false)
    {
        return Boot(shown, separateBin).Save(edit, assets);
    }

    /// <summary>The tree equals a restart and the result agrees: nothing stale, no needless rebuild.</summary>
    private static void ShouldBeCorrect(PreviewVerdict2 verdict) =>
        (verdict.Differs || !verdict.MatchesRestart).ShouldBeFalse(verdict.Explain());

    /// <summary>Whatever the tree is, the result must not claim a match while it differs.</summary>
    private static void ShouldNotBeStaleSilently(PreviewVerdict2 verdict) => verdict.IsFalseNegative.ShouldBeFalse(verdict.Explain());

    [Fact]
    public void Control_NothingEdited_IsCorrect()
    {
        PreviewVerdict2 verdict = Run(_ => { });
        
        ShouldBeCorrect(verdict);
    }
}
