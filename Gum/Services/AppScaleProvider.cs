using System;
using Gum.Themes;

namespace Gum.Services;

/// <inheritdoc cref="IAppScaleProvider"/>
public class AppScaleProvider : IAppScaleProvider
{
    private readonly Lazy<AppScale> _scale =
        new(() => (AppScale)System.Windows.Application.Current.Resources["Scale"]);

    public double BaseFontSize
    {
        get => _scale.Value.BaseFontSize;
        set => _scale.Value.BaseFontSize = value;
    }
}
