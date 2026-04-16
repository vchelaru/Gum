using System.IO;
using System.Reflection;
using Gum.ProjectServices.CodeGeneration;
using Newtonsoft.Json;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Covers how the .codsj Version field is initialized for new projects vs. loaded legacy files.
/// New projects (whether from the embedded FormsTemplate or CLI auto-setup) must land at the
/// current schema version on disk, so migration only runs for legitimately old files.
/// </summary>
public class CodeOutputProjectSettingsVersionTests
{
    [Fact]
    public void CurrentVersion_MatchesLatestMigration()
    {
        // Guards against adding a new Version bump to MigrateIfNeeded without
        // updating CurrentVersion (or vice versa). They must stay in sync.
        CodeOutputProjectSettings.CurrentVersion.ShouldBe(1);
    }

    [Fact]
    public void NewCodeOutputProjectSettings_HasVersionZero_ToPreserveLegacyMigrationBehavior()
    {
        // New in-memory objects start at 0 so legacy JSON without a Version field
        // (which deserializes as 0) still triggers MigrateIfNeeded on load.
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings();
        settings.Version.ShouldBe(0);
    }

    [Fact]
    public void SetDefaults_BringsVersionToCurrent()
    {
        // Callers that construct a fresh settings object (CLI auto-setup, the
        // default-creation fallback inside CodeOutputProjectSettingsManager)
        // run SetDefaults, which must stamp the current schema version so the
        // first write to disk lands at the latest version.
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings();
        settings.SetDefaults();
        settings.Version.ShouldBe(CodeOutputProjectSettings.CurrentVersion);
    }

    [Fact]
    public void MigrateIfNeeded_IsIdempotent()
    {
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings();
        CodeOutputProjectSettingsManager.MigrateIfNeeded(settings);
        CodeOutputProjectSettingsManager.MigrateIfNeeded(settings);
        settings.Version.ShouldBe(CodeOutputProjectSettings.CurrentVersion);
    }

    [Fact]
    public void MigrateIfNeeded_Version0ToVersion1_ClearsStaleDefaultScreenBase()
    {
        // Pure migration test: version 0 → 1 must clear DefaultScreenBase,
        // independent of any codegen behavior downstream.
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings();
        settings.Version = 0;
        settings.DefaultScreenBase = "Gum.Wireframe.BindableGue";

        CodeOutputProjectSettingsManager.MigrateIfNeeded(settings);

        settings.Version.ShouldBe(1);
        settings.DefaultScreenBase.ShouldBe("");
    }

    [Fact]
    public void MigrateIfNeeded_AlreadyCurrent_LeavesDefaultScreenBaseAlone()
    {
        // A user who has explicitly set DefaultScreenBase on a current-version
        // project must not have it cleared by a spurious migration pass.
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings();
        settings.Version = CodeOutputProjectSettings.CurrentVersion;
        settings.DefaultScreenBase = "MyProject.Screens.MyBase";

        CodeOutputProjectSettingsManager.MigrateIfNeeded(settings);

        settings.DefaultScreenBase.ShouldBe("MyProject.Screens.MyBase");
    }

    [Fact]
    public void FormsTemplate_ProjectCodeSettings_ShipsAtCurrentVersion()
    {
        // The embedded FormsTemplate ProjectCodeSettings.codsj must ship at the
        // current schema version so users creating new Forms projects don't
        // trigger migration on first load.
        Assembly assembly = typeof(CodeOutputProjectSettings).Assembly;
        string resourceName = "Gum.ProjectServices.Templates.FormsTemplate.ProjectCodeSettings.codsj";
        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        stream.ShouldNotBeNull();
        using StreamReader reader = new StreamReader(stream);
        string json = reader.ReadToEnd();

        CodeOutputProjectSettings settings = JsonConvert.DeserializeObject<CodeOutputProjectSettings>(json)!;
        settings.Version.ShouldBe(CodeOutputProjectSettings.CurrentVersion);
    }
}
