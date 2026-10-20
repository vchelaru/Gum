using Gum.ProjectServices.CodeGeneration;
using Newtonsoft.Json;
using Shouldly;
using System;
using System.Reflection;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Issue #6248: <see cref="OutputLibrary.GumForms"/> replaces the misnamed
/// <c>MonoGameForms</c>, which stays as an obsolete alias with the same persisted value.
/// </summary>
public class OutputLibraryTests
{
#pragma warning disable CS0618 // deliberately pins the obsolete alias
    [Fact]
    public void MonoGameForms_IsAliasOfGumForms()
    {
        ((int)OutputLibrary.GumForms).ShouldBe(5);
        OutputLibrary.MonoGameForms.ShouldBe(OutputLibrary.GumForms);
    }

    [Fact]
    public void Enum_ParseOfOldName_ReturnsGumForms()
    {
        Enum.Parse<OutputLibrary>("MonoGameForms").ShouldBe(OutputLibrary.GumForms);
    }

    [Fact]
    public void Deserialize_OldNameString_ReturnsGumForms()
    {
        CodeOutputProjectSettings? settings = JsonConvert.DeserializeObject<CodeOutputProjectSettings>(
            "{ \"OutputLibrary\": \"MonoGameForms\" }");

        settings!.OutputLibrary.ShouldBe(OutputLibrary.GumForms);
    }
#pragma warning restore CS0618

    [Fact]
    public void GetName_Value5_ReturnsGumForms()
    {
        // gumcli codegen-init prints this; Enum.ToString() may pick either alias.
        OutputLibrary.GumForms.GetName().ShouldBe("GumForms");
        ((OutputLibrary)5).GetName().ShouldBe("GumForms");
        OutputLibrary.Raylib.GetName().ShouldBe("Raylib");
    }

    [Fact]
    public void Deserialize_PersistedInt5_ReturnsGumForms()
    {
        CodeOutputProjectSettings? settings = JsonConvert.DeserializeObject<CodeOutputProjectSettings>(
            "{ \"OutputLibrary\": 5 }");

        settings!.OutputLibrary.ShouldBe(OutputLibrary.GumForms);
    }

    [Fact]
    public void Serialize_GumForms_PersistsInt5()
    {
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.GumForms };

        string json = JsonConvert.SerializeObject(settings);

        json.ShouldContain("\"OutputLibrary\":5");
    }

    [Fact]
    public void MonoGameForms_IsObsoleteWithDatedRetention()
    {
        FieldInfo field = typeof(OutputLibrary).GetField("MonoGameForms")!;

        ObsoleteAttribute obsolete = field.GetCustomAttribute<ObsoleteAttribute>()!;

        obsolete.ShouldNotBeNull();
        obsolete.IsError.ShouldBeFalse();
        obsolete.Message.ShouldContain("GumForms");
        obsolete.Message.ShouldContain("May 2027");
        obsolete.Message.ShouldContain("November 2027");
    }
}
