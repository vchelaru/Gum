using Gum.GueDeriving;
using Shouldly;
using System.IO;
using System.Reflection;
using Xunit;

namespace RaylibGum.Tests;

public class TrimSafetyTests
{
    [Fact]
    public void IlLinkDescriptor_PreservesRuntimesAndRenderablesForTrimmedPublish()
    {
        // Pins part of the #4116 audit: GraphicalUiElement.Binding.cs's SetBinding-family
        // reflection (ApplyVmValueToUi/BindEvent) and SetPropertyThroughReflection target
        // concrete runtime wrapper types (Gum.GueDeriving.*) and the renderables they wrap
        // (RenderingLibrary.Graphics.*/.Math.Geometry.*), which the .NET trimmer can't see
        // through via Type.GetProperty(string)/Delegate.CreateDelegate(string). These are
        // compiled directly into RaylibGum (not GumCommon), so they need their own descriptor.
        Assembly assembly = typeof(TextRuntime).Assembly;

        assembly.GetManifestResourceNames().ShouldContain("ILLink.Descriptors.xml");

        using Stream stream = assembly.GetManifestResourceStream("ILLink.Descriptors.xml")!;
        using StreamReader reader = new(stream);
        string descriptorXml = reader.ReadToEnd();

        descriptorXml.ShouldContain("Gum.GueDeriving.*");
        descriptorXml.ShouldContain("RenderingLibrary.Graphics.*");
        descriptorXml.ShouldContain("RenderingLibrary.Math.Geometry.*");
    }
}
