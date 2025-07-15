using Gum.DataTypes.Variables;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.GumCommon;
public class StateSaveCategoryTests
{
    public void Constructor_ShouldSetStates()
    {
        StateSaveCategory sut = new ();
        sut.States.ShouldNotBeNull();


    }
}
