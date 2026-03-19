using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsSample.Screens
{
    internal class GumFormsSampleScreenFactory
    {
        public GraphicalUiElement DefaultScreen => CreateScreen(1);

        private readonly Dictionary<int, Func<GraphicalUiElement>> _screenCreators = new()
        {
            { 1, () => new DemoScreenGumRuntime() },
            { 2, () => new FrameworkElementExampleScreen() },
            { 3, () => new FormsCustomizationScreen() },
            { 4, () => new ComplexListBoxItemScreen() },
            { 5, () => new ListBoxBindingScreen() },
            { 6, () => new TestScreenRuntime() }
        };

        public GraphicalUiElement CreateScreen(int screenNumber)
        {
            if (_screenCreators.TryGetValue(screenNumber, out var creator))
                return creator();
            throw new ArgumentException($"Invalid screen number: {screenNumber}");
        }

    }
}
