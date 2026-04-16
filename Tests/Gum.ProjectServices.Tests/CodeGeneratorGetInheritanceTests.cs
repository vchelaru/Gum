using Gum.DataTypes;
using Gum.ProjectServices.CodeGeneration;
using Newtonsoft.Json;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class CodeGeneratorGetInheritanceTests
{
    [Fact]
    public void Version0_WithStaleDefaultScreenBase_MigratesAndGeneratesFrameworkElement()
    {
        // Simulate a version 0 .codsj with the old stale default
        string json = JsonConvert.SerializeObject(new
        {
            DefaultScreenBase = "Gum.Wireframe.BindableGue",
            OutputLibrary = (int)OutputLibrary.MonoGameForms
        });
        CodeOutputProjectSettings settings = JsonConvert.DeserializeObject<CodeOutputProjectSettings>(json)!;

        // Version should be 0 (absent from JSON = default)
        settings.Version.ShouldBe(0);
        settings.DefaultScreenBase.ShouldBe("Gum.Wireframe.BindableGue");

        // After migration, DefaultScreenBase should be cleared
        CodeOutputProjectSettingsManager.MigrateIfNeeded(settings);
        settings.Version.ShouldBe(1);
        settings.DefaultScreenBase.ShouldBe("");

        // Codegen should use the MonoGameForms fallback
        ScreenSave screen = new ScreenSave();
        string? result = CodeGenerator.GetInheritance(screen, settings);
        result.ShouldBe("global::Gum.Forms.Controls.FrameworkElement");
    }

    [Fact]
    public void Version1_WithCustomDefaultScreenBase_PreservesUserValue()
    {
        string json = JsonConvert.SerializeObject(new
        {
            Version = 1,
            DefaultScreenBase = "MyProject.Screens.MyBaseScreen",
            OutputLibrary = (int)OutputLibrary.MonoGameForms
        });
        CodeOutputProjectSettings settings = JsonConvert.DeserializeObject<CodeOutputProjectSettings>(json)!;

        // Migration should not touch version 1 settings
        CodeOutputProjectSettingsManager.MigrateIfNeeded(settings);
        settings.DefaultScreenBase.ShouldBe("MyProject.Screens.MyBaseScreen");

        ScreenSave screen = new ScreenSave();
        string? result = CodeGenerator.GetInheritance(screen, settings);
        result.ShouldBe("MyProject.Screens.MyBaseScreen");
    }


    [Fact]
    public void GetInheritance_MonoGame_Screen_NoBaseType_NoDefaultScreenBase_ReturnsGraphicalUiElement()
    {
        ScreenSave screen = new ScreenSave();
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings();
        settings.OutputLibrary = OutputLibrary.MonoGame;
        settings.DefaultScreenBase = "";

        string? result = CodeGenerator.GetInheritance(screen, settings);

        result.ShouldBe("Gum.Wireframe.GraphicalUiElement");
    }

    [Fact]
    public void GetInheritance_MonoGame_Screen_NoBaseType_UsesDefaultScreenBase()
    {
        ScreenSave screen = new ScreenSave();
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings();
        settings.OutputLibrary = OutputLibrary.MonoGame;
        settings.DefaultScreenBase = "MyProject.Screens.MyBaseScreen";

        string? result = CodeGenerator.GetInheritance(screen, settings);

        result.ShouldBe("MyProject.Screens.MyBaseScreen");
    }

    [Fact]
    public void GetInheritance_MonoGame_Screen_WithBaseType_IgnoresDefaultScreenBase()
    {
        ScreenSave screen = new ScreenSave();
        screen.BaseType = "SomeOtherScreen";
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings();
        settings.OutputLibrary = OutputLibrary.MonoGame;
        settings.DefaultScreenBase = "MyProject.Screens.MyBaseScreen";

        string? result = CodeGenerator.GetInheritance(screen, settings);

        result.ShouldBe("SomeOtherScreen");
    }

    [Fact]
    public void GetInheritance_MonoGameForms_Screen_NoBaseType_NoDefaultScreenBase_ReturnsFrameworkElement()
    {
        ScreenSave screen = new ScreenSave();
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings();
        settings.OutputLibrary = OutputLibrary.MonoGameForms;
        settings.DefaultScreenBase = "";

        string? result = CodeGenerator.GetInheritance(screen, settings);

        result.ShouldBe("global::Gum.Forms.Controls.FrameworkElement");
    }

    [Fact]
    public void GetInheritance_MonoGameForms_Screen_NoBaseType_UsesDefaultScreenBase()
    {
        ScreenSave screen = new ScreenSave();
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings();
        settings.OutputLibrary = OutputLibrary.MonoGameForms;
        settings.DefaultScreenBase = "MyProject.Screens.MyBaseScreen";

        string? result = CodeGenerator.GetInheritance(screen, settings);

        result.ShouldBe("MyProject.Screens.MyBaseScreen");
    }

    [Fact]
    public void GetInheritance_MonoGameForms_Screen_WithBaseType_IgnoresDefaultScreenBase()
    {
        ScreenSave screen = new ScreenSave();
        screen.BaseType = "SomeOtherScreen";
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings();
        settings.OutputLibrary = OutputLibrary.MonoGameForms;
        settings.DefaultScreenBase = "MyProject.Screens.MyBaseScreen";

        string? result = CodeGenerator.GetInheritance(screen, settings);

        result.ShouldBe("SomeOtherScreen");
    }
}
