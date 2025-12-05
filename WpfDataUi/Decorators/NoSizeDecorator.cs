using System.Windows;
using System.Windows.Controls;

namespace WpfDataUi.Decorators
{
// from
// https://stackoverflow.com/questions/4854079/make-wpf-sl-grid-ignore-a-child-element-when-determining-size#:~:text=Create%20a%20decorator%20that%20will%20ask%20for%200,%280%2C0%29%29%3B%20return%20new%20Size%20%280%2C%200%29%3B%20%7D%20%7D
    public class NoSizeDecorator : Decorator
    {
        protected override Size MeasureOverride(Size constraint)
        {
            // Ask for no space
            Child.Measure(new Size(0, 0));
            return new Size(0, 0);
        }
    }
}
