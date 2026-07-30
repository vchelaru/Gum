using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System.IO;
using System.Reflection;
using Xunit;

namespace MonoGameGum.Tests.BindingTrimSafety;

public class GraphicalUiElementBindingTrimSafetyTests
{
    private static string ReadDescriptorXml(Assembly assembly)
    {
        assembly.GetManifestResourceNames().ShouldContain("ILLink.Descriptors.xml");

        using Stream stream = assembly.GetManifestResourceStream("ILLink.Descriptors.xml")!;
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    [Fact]
    public void GumCommon_IlLinkDescriptor_PreservesGraphicalUiElementForTrimmedPublish()
    {
        // Pins part of the #4116 audit: ApplyVmValueToUi/BindEvent (GraphicalUiElement.Binding.cs)
        // resolve SetBinding's UI-side target via thisType.GetProperty(name)/GetType().GetMethod
        // for Delegate.CreateDelegate, invisible to the trimmer. Properties/methods declared
        // directly on the GraphicalUiElement base (X, Y, Width, etc., compiled into GumCommon)
        // need preserving separately from concrete runtime subclasses (MonoGameGum etc.).
        string descriptorXml = ReadDescriptorXml(typeof(GraphicalUiElement).Assembly);

        descriptorXml.ShouldContain("Gum.Wireframe.GraphicalUiElement");
    }

    [Fact]
    public void MonoGameGum_IlLinkDescriptor_PreservesRuntimesAndRenderablesForTrimmedPublish()
    {
        // Pins part of the #4116 audit: the same SetBinding/SetPropertyThroughReflection
        // reflection also targets concrete runtime wrapper types (TextRuntime, ButtonRuntime,
        // etc. -- Gum.GueDeriving.*) and the renderables they wrap (RenderingLibrary.Graphics.*,
        // RenderingLibrary.Math.Geometry.*), all compiled directly into MonoGameGum rather than
        // GumCommon, so they need their own descriptor.
        string descriptorXml = ReadDescriptorXml(typeof(TextRuntime).Assembly);

        descriptorXml.ShouldContain("Gum.GueDeriving.*");
        descriptorXml.ShouldContain("RenderingLibrary.Graphics.*");
        descriptorXml.ShouldContain("RenderingLibrary.Math.Geometry.*");
    }
}
