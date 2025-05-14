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
        private readonly Dictionary<int, Func<BindableGue>> _screenCreators = new()
        {
            { 0, () => new DemoScreenGumRuntime() },
            { 1, () => new FrameworkElementExampleScreen() },
            { 2, () => new FormsCustomizationScreen() },
            { 3, () => new ComplexListBoxItemScreen() },
            { 4, () => new ListBoxBindingScreen() },
            { 5, () => new TestScreenRuntime() }
        };

        public BindableGue CreateScreen(int screenNumber)
        {
            if (_screenCreators.TryGetValue(screenNumber, out var creator))
                return creator();
            throw new ArgumentException($"Invalid screen number: {screenNumber}");
        }
    }
}
