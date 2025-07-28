using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

#if FRB
using FlatRedBall.Forms.Controls;
namespace FlatRedBall.Forms.Data;
#elif RAYLIB
using Gum.Forms.Controls;
using Gum.Forms.Data;
#else
using MonoGameGum.Forms.Controls;
namespace MonoGameGum.Forms.Data;
#endif


internal static class BinderHelpers
{
    /// <summary>
    /// Null-safe getter: returns DependencyProperty.UnsetValue if any intermediate is null.
    /// </summary>
    public static Func<object, object?> BuildGetter(Type targetType, string path)
    {
        //parameter: the incoming object
        ParameterExpression instanceParam = Expression.Parameter(typeof(object), "instance");
        // start by casting to the actual root type
        Expression current = Expression.Convert(instanceParam, targetType);

        // walk down each segment in the dotted path
        string[] segments = path.Split('.');
        foreach (string segment in segments)
        {
            // raw access to the next property/field
            Expression rawAccess = Expression.PropertyOrField(current, segment);
            Type segmentType = rawAccess.Type;

            // If this is a non-nullable value type, just take it
            if (segmentType.IsValueType && Nullable.GetUnderlyingType(segmentType) == null)
            {
                current = rawAccess;
            }
            else
            {
                // reference type or Nullable<T> ⇒ inject null‐propagation:
                //    current == null ? default(T) : current.Segment
                Expression testNull = Expression.Equal(current, Expression.Constant(null, current.Type));
                Expression ifNull = Expression.Default(segmentType);
                Expression ifNotNull = rawAccess;
                current = Expression.Condition(testNull, ifNull, ifNotNull);
            }
        }

        // now 'current' is the final value of type T (value or ref).  Box it to object,
        // but if it's null (or Nullable<T>.HasValue==false), return UnsetValue instead.
        Type finalType = current.Type;
        Expression body;
        if (finalType.IsValueType && Nullable.GetUnderlyingType(finalType) == null)
        {
            // pure value‐type ⇒ just box
            body = Expression.Convert(current, typeof(object));
        }
        else
        {
            // ref‐type or Nullable<T> ⇒ test for null/default
            Expression isNull = Expression.Equal(current, Expression.Constant(null, finalType));
            Expression ifUnset = Expression.Constant(GumProperty.UnsetValue, typeof(object));
            Expression ifValue = Expression.Convert(current, typeof(object));
            body = Expression.Condition(isNull, ifUnset, ifValue);
        }

        // compile into a Func<object,object?>
        LambdaExpression lambda = Expression.Lambda<Func<object, object?>>(body, instanceParam);
        return (Func<object, object?>)lambda.Compile();
    }

    /// <summary>
    /// Null-safe setter: no-op if any intermediate is null.
    /// </summary>
    public static Action<object, object?> BuildSetter(Type targetType, string path)
    {
        // parameters: (object instance, object? value)
        ParameterExpression instanceParam = Expression.Parameter(typeof(object), "instance");
        ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");

        // cast instance → T
        Expression current = Expression.Convert(instanceParam, targetType);

        // walk all but last segment, propagating nulls
        string[] segments = path.Split('.');
        for (int i = 0; i < segments.Length - 1; i++)
        {
            Expression rawAccess = Expression.PropertyOrField(current, segments[i]);
            if (rawAccess.Type.IsValueType)
            {
                // value types can’t be null ⇒ just take it
                current = rawAccess;
            }
            else
            {
                // reference types ⇒ if current==null, propagate null
                current = Expression.Condition(
                    test: Expression.Equal(current, Expression.Constant(null, current.Type)),
                    ifTrue: Expression.Constant(null, rawAccess.Type),
                    ifFalse: rawAccess
                );
            }
        }

        // now 'current' owns the final member
        string lastName = segments[segments.Length - 1];
        MemberExpression memberExpr = Expression.PropertyOrField(current, lastName);

        // figure out the member’s declared type
        Type memberType = memberExpr.Member switch
        {
            PropertyInfo pi => pi.PropertyType,
            FieldInfo fi => fi.FieldType,
            _ => throw new InvalidOperationException()
        };

        
        Expression valueExpr;

        if (memberType.IsValueType && Nullable.GetUnderlyingType(memberType) == null)
        {
            // T is a non-nullable value type
            Expression isNullValue = Expression.Equal(
                valueParam,
                Expression.Constant(null, typeof(object))
            );
            Expression defaultValue = Expression.Default(memberType);
            Expression converted = Expression.Convert(valueParam, memberType);
            valueExpr = Expression.Condition(isNullValue, defaultValue, converted);
        }
        else
        {
            // nullable<T> or reference: just convert
            valueExpr = Expression.Convert(valueParam, memberType);
        }

        // assignment
        Expression assign = Expression.Assign(memberExpr, valueExpr);

        // guard: owner != null (for reference‐type owners); for value-type owner this is always true
        Expression guard = Expression.NotEqual(
            current,
            Expression.Constant(null, current.Type)
        );
        Expression ifAssign = Expression.IfThen(guard, assign);

        // compile to Action<object, object?>
        return Expression
            .Lambda<Action<object, object?>>(ifAssign, instanceParam, valueParam)
            .Compile();
    }
    
    public static string ExtractPath(LambdaExpression expression)
    {
        Expression? body = expression.Body;
        
        if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary)
            body = unary.Operand;

        Stack<string> segments = new();

        while (body is MemberExpression member)
        {
            segments.Push(member.Member.Name);
            body = member.Expression;
        }

        return body switch
        {
            ParameterExpression => string.Join(".", segments),
            ConstantExpression when segments.Count > 1 => string.Join(".", segments.Skip(1)), // skip closure root
            _ => throw new InvalidOperationException("Unsupported expression. Only property/field access is supported.")
        };
    }

    public static string ExtractPath<T>(Expression<Func<T, object?>> expression) =>
        ExtractPath((LambdaExpression)expression);
}
