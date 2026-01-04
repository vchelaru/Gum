using System.Collections.ObjectModel;
using Gum.Mvvm;

namespace MonoGameGum.Tests.Forms;

public class TestViewModel : ViewModel
{
    public TestViewModel? Child
    {
        get => Get<TestViewModel?>();
        set => Set(value);
    }

    public AuxTestViewModel? AuxVm
    {
        get => Get<AuxTestViewModel?>();
        set => Set(value);
    }

    [DependsOn(nameof(Text))]
    public string? ReadonlyText => Text;

    public ObservableCollection<TestViewModel> Items
    {
        get => Get<ObservableCollection<TestViewModel>>();
        set => Set(value);
    }

    public string? Text
    {
        get => Get<string?>();
        set => Set(value);
    }

    public bool IsChecked
    {
        get => Get<bool>();
        set => Set(value);
    }

    public float FloatValue
    {
        get => Get<float>(); set => Set(value);
    }

    public float? NullableFloatValue
    {
        get => Get<float?>();
        set => Set(value);
    }

    public override string ToString() => Text ?? string.Empty;
}

public class AuxTestViewModel : ViewModel
{
    public string? Text
    {
        get=> Get<string?>();
        set => Set(value);
    }

    public TestViewModel? TestVm
    {
        get => Get<TestViewModel?>();
        set => Set(value);
    }
}