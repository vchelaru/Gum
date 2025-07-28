using System.ComponentModel;
using Gum.Forms.Data;
using Xunit;

namespace MonoGameGum.Tests.Forms.Data;

public class PropertyPathObserverTests
{
    [Fact]
    [Description("Bug: when attaching to object not implementing Inpc did not resolve leaf type")]
    public void Attach_WhenNotInpc_ResolvesLeafType()
    {
        // Arrange
        NonReactiveObject context = new()
        {
            Child = new() { Text = "foo" }
        };
        PropertyPathObserver sut = new("Child.Text");

        // Act
        sut.Attach(context);
        
        // Assert
        Assert.Equal(typeof(string), sut.LeafType);
    }

    private class NonReactiveObject
    {
        public NonReactiveObject? Child { get; set; }
        public string? Text { get; set; }
    }
}