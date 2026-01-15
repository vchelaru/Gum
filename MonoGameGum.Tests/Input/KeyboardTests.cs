using MonoGameGum.Input;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Input;

public class KeyboardTests
{
    [Fact]
    public void Activity_ShouldNotCauseCrash_OnEnterKey_IfNoProcessedStringFromWindows()
    {
        Keyboard sut = new();

        Mock<KeyboardStateProcessor> keyboardStateProcessor = new Mock<KeyboardStateProcessor>();

        Microsoft.Xna.Framework.Input.Keys enterKey = (Microsoft.Xna.Framework.Input.Keys)10;
        keyboardStateProcessor.Setup(
            x => x.KeyPushed(enterKey))
            .Returns(true);
        sut.KeyboardStateProcessor = keyboardStateProcessor.Object;

        sut.Activity(0);
    }
}
