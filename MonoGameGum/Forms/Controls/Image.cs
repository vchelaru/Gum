using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#else
#endif

#if !FRB

namespace Gum.Forms.Controls;
#endif
public class Image : Gum.Forms.Controls.FrameworkElement
{
    public string Source
    {
        set
        {
            Visual.SetProperty("SourceFile", value);
        }
    }

    public Image() :
        // SpriteRuntime is not an InteractiveGue, so don't use that:
        //base(new SpriteRuntime(fullInstantiation:true, tryCreateFormsObject:false))
        base(new Gum.Wireframe.InteractiveGue(CreateSpriteRenderable()))
    {
        Visual.Width = 100;
        Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        Visual.Height = 100;
        Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;

        IsVisible = true;
    }

    // Resolved through IGumService so Image stays platform-agnostic — each runtime's
    // GumService returns its own Sprite type. GumService.Initialize must have run before
    // an Image is constructed; the InvalidOperationException replaces an obscure null-ref
    // that used to surface from the InteractiveGue ctor.
    private static IRenderable CreateSpriteRenderable() =>
        global::RenderingLibrary.IGumService.Default?.CreateSpriteRenderable()
        ?? throw new InvalidOperationException(
            "GumService must be initialized before constructing an Image. " +
            "Call GumService.Default.Initialize(...) first.");
}
