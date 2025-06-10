using Gum.DataTypes;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;
public class ElementSaveExtensionsTests : IDisposable
{

    public ElementSaveExtensionsTests()
    {
        int m = 3;
    }



    [Fact]
    public void CreateGueForElement_ShouldCreateGraphicalUiElement_IfNoTypesAreRegistered()
    {
        var element = new ComponentSave();
        element.Name = "Test1";
        var gue = ElementSaveExtensions.CreateGueForElement(element);
        gue.GetType().ShouldBe(typeof(GraphicalUiElement));
    }

    public void Dispose()
    {
        ElementSaveExtensions.Reset();
    }

    public void RegisterGueInstantiation_ShouldCreateGue_ThroughDelegate()
    {
        ElementSaveExtensions.RegisterGueInstantiation("GueInstantiation", () =>
        {
            return new GraphicalUiElement(new InvisibleRenderable()) { Name = "Registered Gue Type" };
        });

        var element = new ComponentSave { Name = "GueInstantiation" };

        var runtime = ElementSaveExtensions.CreateGueForElement(element);
        runtime.Name.ShouldBe("Registered Gue Type");

    }
}
