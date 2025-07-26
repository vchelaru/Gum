using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Gum.Wireframe;

#if FRB
namespace FlatRedBall.Forms.Data;
#elif RAYLIB
namespace Gum.Forms.Data;
#else
namespace MonoGameGum.Forms.Data;
#endif

public enum BindingMode
{
    OneWay,
    TwoWay,
    OneWayToSource
}

public enum UpdateSourceTrigger
{
    Default,
    PropertyChanged,
    LostFocus,
}

public interface IValueConverter
{
    object? Convert(object? value, Type targetType, object? parameter);
    object? ConvertBack(object? value, Type sourceType, object? parameter);
}

public class Binding(string path)
{
    public static object DoNothing { get; } = new();
    public Binding(LambdaExpression propertyExpression) : this(BinderHelpers.ExtractPath(propertyExpression)) { }
    public string Path { get; } = path;
    public BindingMode Mode { get; init; } = BindingMode.TwoWay;
    public UpdateSourceTrigger UpdateSourceTrigger { get; init; } = UpdateSourceTrigger.Default;
    public object? FallbackValue { get; init; }
    public object? TargetNullValue { get; init; }
    public IValueConverter? Converter { get; init; }
    public object? ConverterParameter { get; init; }
    public string? StringFormat { get; init; }
}

public class Binding<T>(Expression<Func<T, object?>> propertyExpression)
    : Binding(BinderHelpers.ExtractPath(propertyExpression));
