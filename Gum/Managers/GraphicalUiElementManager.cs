using Gum.Wireframe;
using System.Collections.Generic;

namespace Gum.Managers
{
    public class GraphicalUiElementManager
    {
        List<GraphicalUiElement> GraphicalUiElements;

        public GraphicalUiElementManager()
        {
            GraphicalUiElements = new List<GraphicalUiElement>();
        }

        public void Add(GraphicalUiElement graphicalUiElement)
        {
            GraphicalUiElements.Add(graphicalUiElement);
        }

        public void Remove(GraphicalUiElement graphicalUiElement)
        {
            GraphicalUiElements.Remove(graphicalUiElement);
        }

        public void Activity()
        {
            var count = GraphicalUiElements.Count;

            for(int i = 0; i < count; i++)
            {
                GraphicalUiElements[i].AnimateSelf();
            }
        }
    }
}
