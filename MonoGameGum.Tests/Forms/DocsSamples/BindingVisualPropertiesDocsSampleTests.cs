// Sample code is copied from the docs as written, and the docs samples do not use nullable annotations.
#nullable disable warnings
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Mvvm;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms.DocsSamples;

/// <summary>
/// Runs the samples on docs/code/binding-viewmodels/binding-deep-dive.md (Binding Visual Properties).
/// The code-generation samples use hand-written stand-ins for the generated classes.
/// </summary>
public class BindingVisualPropertiesDocsSampleTests : BaseTestClass
{
    class MyViewModel : ViewModel
    {
        public float ButtonWidth
        {
            get => Get<float>();
            set => Set(value);
        }

        public float ButtonFontScale
        {
            get => Get<float>();
            set => Set(value);
        }

        public bool IsLarge
        {
            get => Get<bool>();
            set => Set(value);
        }

        public SizeCategory SizeCategory
        {
            get => Get<SizeCategory>();
            set => Set(value);
        }
    }

    class GameScreenViewModel : ViewModel
    {
        public int Health
        {
            get => Get<int>();
            set => Set(value);
        }

        [DependsOn(nameof(Health))]
        public bool IsLowHealth => Health < 10;
    }

    // Stand-in for a generated Forms screen with a HealthLabel.
    class GameScreenHud : FrameworkElement
    {
        public Label HealthLabel { get; } = new();

        public GameScreenHud() : base(new ContainerRuntime())
        {
            this.AddChild(HealthLabel);
            CustomInitialize();
        }

        public int CurrentHealth
        {
            set => this.HealthLabel.Text =
                    $"Current Health: {value}";
        }

        public bool IsLowHealth
        {
            set
            {
                var textInstance = HealthLabel.GetVisual<TextRuntime>();
                textInstance.Color = value
                    ? Microsoft.Xna.Framework.Color.Red
                    : Microsoft.Xna.Framework.Color.White;
            }
        }

        void CustomInitialize()
        {
            this.SetBinding(
                nameof(this.CurrentHealth),
                nameof(GameScreenViewModel.Health));

            this.SetBinding(
                nameof(this.IsLowHealth),
                nameof(GameScreenViewModel.IsLowHealth));
        }
    }

    public enum SizeCategory
    {
        Large,
        Small,
    }

    // Stand-in for a generated component with a SizeCategory state category.
    class ComponentWithState : FrameworkElement
    {
        public ComponentWithState() : base(new ContainerRuntime()) { }

        public SizeCategory? SizeCategoryState { get; set; }

        public bool IsLarge
        {
            set => SizeCategoryState = value ? SizeCategory.Large : SizeCategory.Small;
        }
    }

    [Fact]
    public void VisualWidth_LambdaAndString_FollowViewModel()
    {
        Button lambdaButton = new();
        Button stringButton = new();
        MyViewModel viewModel = new() { ButtonWidth = 123 };
        lambdaButton.BindingContext = viewModel;
        stringButton.BindingContext = viewModel;

        var buttonVisual = lambdaButton.Visual;
        buttonVisual.SetBinding<MyViewModel>(
            nameof(buttonVisual.Width),
            vm => vm.ButtonWidth);
        var stringVisual = stringButton.Visual;
        stringVisual.SetBinding(
            nameof(stringVisual.Width),
            nameof(MyViewModel.ButtonWidth));

        buttonVisual.Width.ShouldBe(123);
        stringVisual.Width.ShouldBe(123);
        viewModel.ButtonWidth = 200;
        buttonVisual.Width.ShouldBe(200);
        stringVisual.Width.ShouldBe(200);
    }

    [Fact]
    public void TextInstanceFontScale_Lambda_FollowsViewModel()
    {
        Button MyButton = new();
        MyButton.BindingContext = new MyViewModel { ButtonFontScale = 2 };

        var buttonVisual = (ButtonVisual)MyButton.Visual;
        var text = buttonVisual.TextInstance;
        text.SetBinding<MyViewModel>(
            nameof(text.FontScale),
            vm => vm.ButtonFontScale);

        text.FontScale.ShouldBe(2);
    }

    [Fact]
    public void ConverterProperties_SetterOnly_ApplyHealth()
    {
        GameScreenHud screen = new();
        GameScreenViewModel viewModel = new() { Health = 50 };

        screen.BindingContext = viewModel;
        screen.HealthLabel.Text.ShouldBe("Current Health: 50");

        viewModel.Health = 5;
        screen.HealthLabel.Text.ShouldBe("Current Health: 5");
        screen.HealthLabel.GetVisual<TextRuntime>().Color.ShouldBe(Microsoft.Xna.Framework.Color.Red);
    }

    [Fact]
    public void StateBinding_ConverterPropertyAndEnum_SetState()
    {
        ComponentWithState converter = new();
        ComponentWithState direct = new();
        MyViewModel viewModel = new() { IsLarge = true, SizeCategory = SizeCategory.Small };
        converter.BindingContext = viewModel;
        direct.BindingContext = viewModel;

        converter.SetBinding(
            nameof(converter.IsLarge),
            nameof(MyViewModel.IsLarge));
        direct.SetBinding(
            nameof(direct.SizeCategoryState),
            nameof(MyViewModel.SizeCategory));

        converter.SizeCategoryState.ShouldBe(SizeCategory.Large);
        direct.SizeCategoryState.ShouldBe(SizeCategory.Small);
    }
}
