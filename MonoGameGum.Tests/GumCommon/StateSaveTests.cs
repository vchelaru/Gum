using Gum.DataTypes.Variables;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.GumCommon;
public class StateSaveTests
{
    [Fact]
    public void SetValue_CreatesVariable()
    {
        StateSave stateSave = new();

        stateSave.SetValue("TestVariable", 1f);

        stateSave.Variables.Count.ShouldBe(1);
        stateSave.Variables[0].Value.ShouldBe(1f);
        stateSave.Variables[0].Name.ShouldBe("TestVariable");

        stateSave.SetValue("TestVariable", 2f);
        stateSave.Variables.Count.ShouldBe(1);
        stateSave.Variables[0].Value.ShouldBe(2f);
        stateSave.Variables[0].Name.ShouldBe("TestVariable");
    }

    [Fact]
    public void RemoveValue_RemovesVariables()
    {
        StateSave stateSave = new();

        stateSave.SetValue("TestVariable", 1f);
        stateSave.RemoveValue("TestVariable");
        stateSave.Variables.Count.ShouldBe(0);
    }
}
