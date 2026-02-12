using Gum.DataTypes;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe;

public interface IWireframeObjectManager
{
    List<GraphicalUiElement> AllIpsos { get; }

    ElementSave? ElementShowing { get; }

    GraphicalUiElement? RootGue { get; }

    void RefreshAll(bool forceLayout, bool forceReloadTextures = false);
    GraphicalUiElement? GetSelectedRepresentation();

    GraphicalUiElement[] GetSelectedRepresentations();

    GraphicalUiElement? GetRepresentation(ElementSave elementSave);

    GraphicalUiElement? GetRepresentation(InstanceSave instanceSave, List<ElementWithState> elementStack = null);


    InstanceSave GetInstance(IRenderableIpso representation, InstanceFetchType fetchType,
        List<ElementWithState> elementStack);

    InstanceSave GetInstance(IRenderableIpso representation, ElementSave instanceContainer,
        string prefix, InstanceFetchType fetchType, List<ElementWithState> elementStack);

    bool IsRepresentation(IPositionedSizedObject ipso);

    /// <summary>
    /// Gets all visible GraphicalUiElements in the current screen/component.
    /// This is used to find elements within a selection rectangle.
    /// </summary>
    IEnumerable<GraphicalUiElement> GetAllVisibleElements();
}
