using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

#if !FRB
using Gum.Mvvm;
#endif

#if FRB
using FlatRedBall.Forms.Controls;
#endif

#if !FRB
using Gum.Forms.Controls;
#endif

[assembly: InternalsVisibleTo("MonoGameGum.Tests")]
[assembly: InternalsVisibleTo("MonoGameGum.Tests.V2")]

#if FRB
namespace FlatRedBall.Forms.Data;
#endif

#if !FRB
namespace Gum.Forms.Data;
#endif

internal static class BinderHelpers
{
    /// <summary>
    /// Parses a binding path string into an array of <see cref="PathSegment"/> values.
    /// Supports dotted property access and integer indexers (e.g. "Items[0].Text").
    /// </summary>
    public static PathSegment[] ParseSegments(string path) => PathSegmentParser.ParseSegments(path);

    public static bool CanWritePath(Type targetType, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        PathSegment[] segments = ParseSegments(path);
        Type currentType = targetType;

        for (int i = 0; i < segments.Length - 1; i++)
        {
            if (!TryGetMember(currentType, segments[i].Name, out _, out Type? memberType))
            {
                return false;
            }

            if (segments[i].Index.HasValue)
            {
                currentType = GetElementType(memberType);
            }
            else
            {
                currentType = memberType;
            }
        }

        PathSegment leaf = segments[^1];

        if (leaf.Index.HasValue)
        {
            // The leaf is an indexed access like "Items[0]" — check the indexer has a setter
            if (!TryGetMember(currentType, leaf.Name, out _, out Type? collectionType))
            {
                return false;
            }

            PropertyInfo? indexer = FindIntIndexer(collectionType);
            return indexer is not null && indexer.SetMethod is not null && indexer.SetMethod.IsPublic;
        }

        if (!TryGetMember(currentType, leaf.Name, out MemberInfo? leafMember, out _))
        {
            return false;
        }

        return leafMember switch
        {
            PropertyInfo propertyInfo => propertyInfo.SetMethod is not null && propertyInfo.SetMethod.IsPublic,
            FieldInfo fieldInfo => !fieldInfo.IsInitOnly && !fieldInfo.IsLiteral,
            _ => false
        };
    }

    private static bool TryGetMember(Type declaringType, string memberName, out MemberInfo? member, out Type memberType)
    {
        member = declaringType.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
        if (member is PropertyInfo propertyInfo)
        {
            memberType = propertyInfo.PropertyType;
            return true;
        }

        member = declaringType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
        if (member is FieldInfo fieldInfo)
        {
            memberType = fieldInfo.FieldType;
            return true;
        }

        memberType = typeof(object);
        return false;
    }

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
        PathSegment[] segments = ParseSegments(path);
        foreach (PathSegment segment in segments)
        {
            // raw access to the next property/field
            Expression rawAccess = Expression.PropertyOrField(current, segment.Name);
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

            // If the segment has an index, apply indexer access
            if (segment.Index.HasValue)
            {
                current = BuildIndexAccess(current, segment.Index.Value);
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
        PathSegment[] segments = ParseSegments(path);
        for (int i = 0; i < segments.Length - 1; i++)
        {
            Expression rawAccess = Expression.PropertyOrField(current, segments[i].Name);
            if (rawAccess.Type.IsValueType)
            {
                // value types can't be null ⇒ just take it
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

            // If the intermediate segment has an index, apply indexer access with null propagation
            if (segments[i].Index.HasValue)
            {
                current = BuildIndexAccessWithNullPropagation(current, segments[i].Index.Value);
            }
        }

        PathSegment lastSegment = segments[segments.Length - 1];

        if (lastSegment.Index.HasValue)
        {
            // The leaf is an indexed access like "Items[0]" — set via indexer
            Expression rawAccess = Expression.PropertyOrField(current, lastSegment.Name);
            if (!rawAccess.Type.IsValueType)
            {
                rawAccess = Expression.Condition(
                    test: Expression.Equal(current, Expression.Constant(null, current.Type)),
                    ifTrue: Expression.Constant(null, rawAccess.Type),
                    ifFalse: rawAccess
                );
            }

            Expression collection = rawAccess;
            Type elementType = GetElementType(collection.Type);

            Expression valueExpr = BuildValueConversion(valueParam, elementType);

            Expression indexerSet = BuildIndexSetExpression(collection, lastSegment.Index.Value, valueExpr);

            // guard: collection != null
            Expression guard = Expression.NotEqual(
                collection,
                Expression.Constant(null, collection.Type)
            );
            Expression ifAssign = Expression.IfThen(guard, indexerSet);

            return Expression
                .Lambda<Action<object, object?>>(ifAssign, instanceParam, valueParam)
                .Compile();
        }

        // now 'current' owns the final member
        string lastName = lastSegment.Name;
        MemberExpression memberExpr = Expression.PropertyOrField(current, lastName);

        // figure out the member's declared type
        Type memberType = memberExpr.Member switch
        {
            PropertyInfo pi => pi.PropertyType,
            FieldInfo fi => fi.FieldType,
            _ => throw new InvalidOperationException()
        };

        Expression leafValueExpr = BuildValueConversion(valueParam, memberType);

        // assignment
        Expression assign = Expression.Assign(memberExpr, leafValueExpr);

        // guard: owner != null (for reference‐type owners); for value-type owner this is always true
        Expression ownerGuard = Expression.NotEqual(
            current,
            Expression.Constant(null, current.Type)
        );
        Expression ifSetAssign = Expression.IfThen(ownerGuard, assign);

        // compile to Action<object, object?>
        return Expression
            .Lambda<Action<object, object?>>(ifSetAssign, instanceParam, valueParam)
            .Compile();
    }

    public static string ExtractPath(LambdaExpression expression)
    {
        Expression? body = expression.Body;

        if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary)
        {
            body = unary.Operand;
        }

        Stack<string> segments = new();

        while (body is MemberExpression || IsIndexerAccess(body))
        {
            if (body is MemberExpression member)
            {
                segments.Push(member.Member.Name);
                body = member.Expression;
            }
            else
            {
                // Indexer access: get_Item(int) call, IndexExpression, or ArrayIndex
                int index = ExtractIndexFromExpression(body!);
                Expression inner = GetObjectFromExpression(body!);

                if (inner is MemberExpression collectionMember)
                {
                    segments.Push($"{collectionMember.Member.Name}[{index}]");
                    body = collectionMember.Expression;
                }
                else
                {
                    throw new InvalidOperationException(
                        "Unsupported expression. Indexer must be applied to a property or field.");
                }
            }
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

    #region Private helpers

    private static bool IsIndexerAccess(Expression? expr)
    {
        if (expr is MethodCallExpression mce && mce.Method.Name == "get_Item"
            && mce.Arguments.Count == 1 && mce.Arguments[0] is ConstantExpression ce
            && ce.Type == typeof(int))
        {
            return true;
        }

        if (expr is IndexExpression idx && idx.Arguments.Count == 1
            && idx.Arguments[0] is ConstantExpression ce2 && ce2.Type == typeof(int))
        {
            return true;
        }

        if (expr is BinaryExpression { NodeType: ExpressionType.ArrayIndex } bin
            && bin.Right is ConstantExpression ce3 && ce3.Type == typeof(int))
        {
            return true;
        }

        return false;
    }

    private static int ExtractIndexFromExpression(Expression expr)
    {
        if (expr is MethodCallExpression mce)
        {
            return (int)((ConstantExpression)mce.Arguments[0]).Value!;
        }

        if (expr is IndexExpression idx)
        {
            return (int)((ConstantExpression)idx.Arguments[0]).Value!;
        }

        if (expr is BinaryExpression bin)
        {
            return (int)((ConstantExpression)bin.Right).Value!;
        }

        throw new InvalidOperationException("Unsupported indexer expression type.");
    }

    private static Expression GetObjectFromExpression(Expression expr)
    {
        if (expr is MethodCallExpression mce)
        {
            return mce.Object!;
        }

        if (expr is IndexExpression idx)
        {
            return idx.Object!;
        }

        if (expr is BinaryExpression bin)
        {
            return bin.Left;
        }

        throw new InvalidOperationException("Unsupported indexer expression type.");
    }

    /// <summary>
    /// Builds an expression that accesses an element by integer index, with null propagation
    /// on the collection. If the collection is null, the result is default(elementType).
    /// </summary>
    private static Expression BuildIndexAccess(Expression collection, int index)
    {
        Type collectionType = collection.Type;
        ConstantExpression indexExpr = Expression.Constant(index);

        Expression rawAccess;
        Type elementType;

        if (collectionType.IsArray)
        {
            elementType = collectionType.GetElementType()!;
            rawAccess = Expression.ArrayIndex(collection, indexExpr);
        }
        else
        {
            PropertyInfo? indexer = FindIntIndexer(collectionType);
            if (indexer == null)
            {
                throw new InvalidOperationException(
                    $"Type '{collectionType.Name}' does not have an integer indexer.");
            }
            elementType = indexer.PropertyType;
            rawAccess = Expression.MakeIndex(collection, indexer, new[] { indexExpr });
        }

        // Null propagation on the collection
        if (!collectionType.IsValueType)
        {
            Expression testNull = Expression.Equal(collection, Expression.Constant(null, collectionType));
            Expression ifNull = Expression.Default(elementType);
            return Expression.Condition(testNull, ifNull, rawAccess);
        }

        return rawAccess;
    }

    private static Expression BuildIndexAccessWithNullPropagation(Expression collection, int index)
    {
        Type collectionType = collection.Type;
        ConstantExpression indexExpr = Expression.Constant(index);

        Expression rawAccess;
        Type elementType;

        if (collectionType.IsArray)
        {
            elementType = collectionType.GetElementType()!;
            rawAccess = Expression.ArrayIndex(collection, indexExpr);
        }
        else
        {
            PropertyInfo? indexer = FindIntIndexer(collectionType);
            if (indexer == null)
            {
                throw new InvalidOperationException(
                    $"Type '{collectionType.Name}' does not have an integer indexer.");
            }
            elementType = indexer.PropertyType;
            rawAccess = Expression.MakeIndex(collection, indexer, new[] { indexExpr });
        }

        // Null propagation: if collection is null, return null (for ref types) or default
        if (!collectionType.IsValueType)
        {
            Expression testNull = Expression.Equal(collection, Expression.Constant(null, collectionType));
            Expression ifNull = Expression.Default(elementType);
            return Expression.Condition(testNull, ifNull, rawAccess);
        }

        return rawAccess;
    }

    private static Expression BuildIndexSetExpression(Expression collection, int index, Expression value)
    {
        Type collectionType = collection.Type;
        ConstantExpression indexExpr = Expression.Constant(index);

        if (collectionType.IsArray)
        {
            Expression arrayAccess = Expression.ArrayAccess(collection, indexExpr);
            return Expression.Assign(arrayAccess, value);
        }
        else
        {
            PropertyInfo? indexer = FindIntIndexer(collectionType);
            if (indexer == null)
            {
                throw new InvalidOperationException(
                    $"Type '{collectionType.Name}' does not have an integer indexer.");
            }
            MethodInfo setter = indexer.SetMethod
                ?? throw new InvalidOperationException(
                    $"Indexer on type '{collectionType.Name}' does not have a setter.");
            return Expression.Call(collection, setter, indexExpr, value);
        }
    }

    private static Expression BuildValueConversion(ParameterExpression valueParam, Type memberType)
    {
        if (memberType.IsValueType && Nullable.GetUnderlyingType(memberType) == null)
        {
            // T is a non-nullable value type
            Expression isNullValue = Expression.Equal(
                valueParam,
                Expression.Constant(null, typeof(object))
            );
            Expression defaultValue = Expression.Default(memberType);
            Expression converted = Expression.Convert(valueParam, memberType);
            return Expression.Condition(isNullValue, defaultValue, converted);
        }
        else
        {
            // nullable<T> or reference: just convert
            return Expression.Convert(valueParam, memberType);
        }
    }

    private static PropertyInfo? FindIntIndexer(Type type)
    {
        // Look for a property with an int index parameter (the standard C# indexer named "Item")
        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            ParameterInfo[] indexParams = prop.GetIndexParameters();
            if (indexParams.Length == 1 && indexParams[0].ParameterType == typeof(int))
            {
                return prop;
            }
        }

        return null;
    }

    private static Type GetElementType(Type collectionType)
    {
        if (collectionType.IsArray)
        {
            return collectionType.GetElementType()!;
        }

        PropertyInfo? indexer = FindIntIndexer(collectionType);
        if (indexer != null)
        {
            return indexer.PropertyType;
        }

        return typeof(object);
    }

    #endregion
}
