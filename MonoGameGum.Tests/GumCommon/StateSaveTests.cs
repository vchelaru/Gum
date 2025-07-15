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
        StateSave sut = new();

        sut.SetValue("TestVariable", 1f);

        sut.Variables.Count.ShouldBe(1);
        sut.Variables[0].Value.ShouldBe(1f);
        sut.Variables[0].Name.ShouldBe("TestVariable");

        sut.SetValue("TestVariable", 2f);
        sut.Variables.Count.ShouldBe(1);
        sut.Variables[0].Value.ShouldBe(2f);
        sut.Variables[0].Name.ShouldBe("TestVariable");
    }

    [Fact]
    public void RemoveValue_RemovesVariables()
    {
        StateSave sut = new();

        sut.SetValue("TestVariable", 1f);
        sut.RemoveValue("TestVariable");
        sut.Variables.Count.ShouldBe(0);
    }
}
