using Moq;
using Moq.Protected;
using RaylibGum.Input;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaylibGum.Tests.Inputs;

public class KeyboardTests : BaseTestClass
{
    [Fact]
    public void GetStringTyped_ShouldReturnSameValue_WhenCalledMultipleTimes()
    {
        var sut = new Mock<Keyboard>();
        var codepoints = new Queue<int>(new[] { 72, 101, 108, 108, 111, 0 });
        sut.Protected()
            .Setup<int>("GetCharPressed")
            .Returns(() => codepoints.Count > 0 ? codepoints.Dequeue() : 0);

        sut.Object.Activity(1);

        string firstCallResult = sut.Object.GetStringTyped();
        string secondCallResult = sut.Object.GetStringTyped();

        firstCallResult.ShouldBe("Hello");
        secondCallResult.ShouldBe("Hello");

        sut.Object.Activity(2);

        string thirdCallResult = sut.Object.GetStringTyped();
        thirdCallResult.ShouldBe(string.Empty);
    }
}
