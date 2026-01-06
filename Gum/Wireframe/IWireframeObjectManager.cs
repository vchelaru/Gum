using Gum.DataTypes;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe;

public interface IWireframeObjectManager
{
    GraphicalUiElement? RootGue { get; }

    void RefreshAll(bool forceLayout, bool forceReloadTextures = false);

    GraphicalUiElement GetRepresentation(ElementSave elementSave);

    GraphicalUiElement GetRepresentation(InstanceSave instanceSave, List<ElementWithState> elementStack = null);

    GraphicalUiElement? GetSelectedRepresentation();

    InstanceSave GetInstance(IRenderableIpso representation, InstanceFetchType fetchType,
        List<ElementWithState> elementStack);

    InstanceSave GetInstance(IRenderableIpso representation, ElementSave instanceContainer,
        string prefix, InstanceFetchType fetchType, List<ElementWithState> elementStack);

}
