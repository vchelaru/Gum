using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;

namespace Gum.Themes;

[MarkupExtensionReturnType(typeof(ResourceDictionary))]
public sealed class DesignOnlyDictionary : MarkupExtension
{
    public Uri? Source { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        bool isDesign =
            (bool)DesignerProperties.IsInDesignModeProperty
                .GetMetadata(typeof(DependencyObject)).DefaultValue ||
            LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        return isDesign && Source != null
            ? new ResourceDictionary { Source = Source }
            : new ResourceDictionary(); // empty at runtime
    }
}