using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms.DocsSamples;

/// <summary>
/// Runs the "Bindings and trimming" sample on docs/code/performance-and-optimization/native-aot-and-trimming.md.
/// </summary>
public class NativeAotAndTrimmingDocsSampleTests : BaseTestClass
{
    public class MyViewModel : ViewModel
    {
        public string PlayerName
        {
            get => Get<string>();
            set => Set(value);
        }
    }

    [Fact]
    public void LambdaBinding_ShowsValue_AndOnlyStringOverloadsWarnForTrimming()
    {
        TextBox textBox = new() { BindingContext = new MyViewModel { PlayerName = "Ada" } };

        textBox.SetBinding<MyViewModel>(
            nameof(TextBox.Text),
            vm => vm.PlayerName);

        textBox.Text.ShouldBe("Ada");

        // The page says each string overload produces IL2026 on publish and the lambda overloads do not.
        typeof(FrameworkElement).GetMethod(nameof(FrameworkElement.SetBinding), new[] { typeof(string), typeof(string) })!
            .GetCustomAttribute<RequiresUnreferencedCodeAttribute>().ShouldNotBeNull();
        typeof(FrameworkElement).GetMethod(nameof(FrameworkElement.SetBinding), new[] { typeof(string), typeof(Binding) })!
            .GetCustomAttribute<RequiresUnreferencedCodeAttribute>().ShouldNotBeNull();
        typeof(FrameworkElementExt).GetMethod(nameof(FrameworkElementExt.SetBinding), new[] { typeof(FrameworkElement), typeof(string), typeof(LambdaExpression) })!
            .GetCustomAttribute<RequiresUnreferencedCodeAttribute>().ShouldBeNull();
    }
}
