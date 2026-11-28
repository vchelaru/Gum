using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    /// <summary>
    /// The message for a binding that would write to a target property with no setter. A null
    /// <paramref name="modeName"/> is a binding with no modes (a visual's SetBinding).
    /// </summary>
    public static string GetReadOnlyTargetMessage(string ownerTypeName, string propertyName, string? modeName)
    {
        string bindingName = modeName == null ? "a binding" : $"a {modeName} binding";
        return $"{ownerTypeName}.{propertyName} has no setter, so it cannot be the target of " +
            $"{bindingName}, which writes the view model's value to it. Bind a property that has a public setter.";
    }

    /// <summary>
    /// The message for a <see cref="Binding.StringFormat"/> with no <c>{0}</c> placeholder, whose output never
    /// shows the value, or null when the format has one (any <c>{0:...}</c> or <c>{0,n}</c> form counts; an
    /// escaped <c>{{0}}</c> does not). A format string.Format rejects outright is left to fail when it is used.
    /// </summary>
    public static string? GetMissingPlaceholderMessage(string? stringFormat)
    {
        if (stringFormat is null)
        {
            return null;
        }

        string formatted;
        try
        {
            formatted = string.Format(System.Globalization.CultureInfo.InvariantCulture, stringFormat, PlaceholderProbe.Instance);
        }
        catch (FormatException)
        {
            return null;
        }

        if (formatted.Contains(PlaceholderProbe.Token))
        {
            return null;
        }

        return $"Binding.StringFormat '{stringFormat}' has no {{0}} placeholder, so the bound value never appears. " +
            "Put {0} where the value goes, in a plain string: an interpolated $\"...\" string fills {0} in before the binding sees it.";
    }

    /// <summary>
    /// Formats as a token no real format string contains, whatever format item it fills.
    /// </summary>
    private sealed class PlaceholderProbe : IFormattable
    {
        public const string Token = "\u0001GumBindingValue\u0001";
        public static readonly PlaceholderProbe Instance = new();

        public string ToString(string? format, IFormatProvider? formatProvider) => Token;

        public override string ToString() => Token;
    }

    /// <summary>
    /// Converts a string (a member name, any case, as WPF's EnumConverter does) or an integral number to
    /// <paramref name="enumType"/>. Returns <see cref="GumProperty.UnsetValue"/> when the value matches no member;
    /// a number on a [Flags] enum only has to be a combination of bits.
    /// </summary>
    public static object ConvertToEnum(object value, Type enumType)
    {
        object? result = null;
        if (value is string text)
        {
            try
            {
                result = Enum.Parse(enumType, text, ignoreCase: true);
            }
            catch (Exception exception) when (exception is ArgumentException or OverflowException)
            {
                return GumProperty.UnsetValue;
            }
        }
        else if (Type.GetTypeCode(value.GetType()) is TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or
            TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64)
        {
            result = Enum.ToObject(enumType, value);
        }

        if (result is null)
        {
            return GumProperty.UnsetValue;
        }

        bool isFlags = enumType.IsDefined(typeof(FlagsAttribute), inherit: false);
        return isFlags || Enum.IsDefined(enumType, result) ? result : GumProperty.UnsetValue;
    }

    /// <summary>
    /// The message for a source value that matches no member of the enum target it is bound to.
    /// </summary>
    public static string GetUnmatchedEnumMessage(object value, Type enumType, string ownerTypeName, string propertyName) =>
        $"Cannot set {ownerTypeName}.{propertyName}: the bound value '{value}' ({value.GetType().Name}) matches no member " +
        $"of enum {enumType.Name}. Bind a member name or a number defined on {enumType.Name}.";

    /// <summary>
    /// The message for a binding that would write to a view model member with no setter, or null when the
    /// path's leaf can be written or does not resolve on declared types (a missing member is reported
    /// elsewhere, and a member only the runtime type has is checked at write time).
    /// </summary>
    [RequiresUnreferencedCode("Calls " + nameof(GetPathWritability) + ".")]
    public static string? GetReadOnlySourceMessage(Type sourceType, string path, string modeName,
        string targetTypeName, string targetPropertyName)
    {
        if (GetPathWritability(sourceType, path, out Type leafOwner) != PathWritability.NoSetter)
        {
            return null;
        }

        PathSegment[] segments = ParseSegments(path);
        string leafName = $"{leafOwner.Name}.{FormatSegment(segments[^1])}";
        string problem = $"so it cannot be the source of a {modeName} binding, " +
            $"which writes {targetTypeName}.{targetPropertyName} to it.";
        if (segments.Length == 1)
        {
            return $"{leafName} has no setter, {problem} Give the property a setter.";
        }

        // Past the root the leaf's owner is the declared type of the member holding it (often an interface or a
        // base class), which the object held there may not match.
        TryWalkToLeafOwner(sourceType, segments[..^1], out Type holderOwner, out _);
        string holderName = $"{holderOwner.Name}.{FormatSegment(segments[^2])}";
        return $"{leafName} has no setter ({leafOwner.Name} is the declared type of {holderName}), {problem} " +
            $"Give {leafName} a setter, or declare {holderName} as a type whose {segments[^1].Name} has one.";
    }

    private static string FormatSegment(PathSegment segment) =>
        segment.Index is int index ? $"{segment.Name}[{index}]" : segment.Name;

    [RequiresUnreferencedCode("Calls " + nameof(GetPathWritability) + ".")]
    public static bool CanWritePath(Type targetType, string path) =>
        GetPathWritability(targetType, path, out _) == PathWritability.Writable;

    // A property reflected from a derived type hides a private accessor its base class declares, so setters are
    // looked up on the declaring type.
    public static PropertyInfo AsDeclared(PropertyInfo property)
    {
        if (property.DeclaringType is not { } declaringType || declaringType == property.ReflectedType)
        {
            return property;
        }

        Type[] indexTypes = Array.ConvertAll(property.GetIndexParameters(), parameter => parameter.ParameterType);
        return declaringType.GetProperty(property.Name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
            binder: null, property.PropertyType, indexTypes, modifiers: null) ?? property;
    }

    private enum PathWritability
    {
        Writable,
        // The leaf resolves on declared types and has no setter.
        NoSetter,
        // The path does not resolve, or the leaf cannot be written for another reason.
        NotWritable
    }

    // A setter of any visibility counts: the compiled setter and PropertyInfo.SetValue both reach a non-public
    // one, as a visual's SetBinding already does. leafOwner is the declared type the leaf is looked up on.
    [RequiresUnreferencedCode(
        "Walks the path on targetType by name (GetProperty/GetField). Those members may be removed " +
        "under PublishTrimmed if nothing else in the app references them.")]
    private static PathWritability GetPathWritability(Type targetType, string path, out Type leafOwner)
    {
        leafOwner = targetType;
        if (string.IsNullOrWhiteSpace(path))
        {
            return PathWritability.NotWritable;
        }

        PathSegment[] segments = ParseSegments(path);
        if (!TryWalkToLeafOwner(targetType, segments, out leafOwner, out int failedIndex))
        {
            // Past the root, a declared type that lacks the member may hold a derived object that has it;
            // BuildSetter resolves those segments on the runtime type at write time.
            return failedIndex > 0 ? PathWritability.Writable : PathWritability.NotWritable;
        }

        PathSegment leaf = segments[^1];
        if (!TryGetMember(leafOwner, leaf.Name, out MemberInfo? leafMember, out Type memberType))
        {
            return segments.Length > 1 ? PathWritability.Writable : PathWritability.NotWritable;
        }

        if (leaf.Index.HasValue)
        {
            // The leaf is an indexed access like "Items[0]"; the collection's indexer must have a setter.
            PropertyInfo? indexer = FindIntIndexer(memberType);
            if (indexer is null)
            {
                return PathWritability.NotWritable;
            }
            return AsDeclared(indexer).SetMethod is not null ? PathWritability.Writable : PathWritability.NoSetter;
        }

        return leafMember switch
        {
            PropertyInfo propertyInfo => AsDeclared(propertyInfo).SetMethod is not null
                ? PathWritability.Writable
                : PathWritability.NoSetter,
            FieldInfo fieldInfo => !fieldInfo.IsInitOnly && !fieldInfo.IsLiteral
                ? PathWritability.Writable
                : PathWritability.NoSetter,
            _ => PathWritability.NotWritable
        };
    }

    // Walks every segment but the leaf along declared member types. On success, owner is the type the leaf is
    // looked up on. On failure, owner is the type that lacks segments[failedIndex].
    [RequiresUnreferencedCode("Calls " + nameof(TryGetMember) + ".")]
    private static bool TryWalkToLeafOwner(Type rootType, PathSegment[] segments, out Type owner, out int failedIndex)
    {
        owner = rootType;
        for (int i = 0; i < segments.Length - 1; i++)
        {
            if (!TryGetMember(owner, segments[i].Name, out _, out Type memberType))
            {
                failedIndex = i;
                return false;
            }

            owner = segments[i].Index.HasValue ? GetElementType(memberType) : memberType;
        }

        failedIndex = -1;
        return true;
    }

    /// <summary>
    /// Returns the message reporting that <paramref name="path"/> names a member that the type of
    /// <paramref name="bindingContext"/> (or of an intermediate segment) does not have, or null when every
    /// segment resolves. Segments are checked on declared member types. Past the root, a segment the declared type
    /// lacks is judged on the object the path holds there at the time of the call; when that object is null, the
    /// message is reported only if the declared type is sealed, since a derived value could still have the member.
    /// </summary>
    [RequiresUnreferencedCode(
        "Walks the path on bindingContext's own type by name (GetProperty/GetField/GetEvent). Those members " +
        "may be removed under PublishTrimmed if nothing else in the app references them.")]
    public static string? GetMissingPathMessage(object bindingContext, string path, bool allowEventLeaf,
        string targetTypeName, string targetPropertyName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        PathSegment[] segments = ParseSegments(path);
        Type contextType = bindingContext.GetType();
        int missingIndex = TryWalkToLeafOwner(contextType, segments, out Type owner, out int failedIndex)
            ? segments.Length - 1
            : failedIndex;
        bool allowEvent = allowEventLeaf && missingIndex == segments.Length - 1;
        if (HasMember(owner, segments[missingIndex], allowEvent))
        {
            return null;
        }

        if (missingIndex > 0)
        {
            if (TryWalkRuntime(bindingContext, segments, missingIndex, out object? runtimeOwner))
            {
                if (HasMember(runtimeOwner!.GetType(), segments[missingIndex], allowEvent))
                {
                    return null;
                }
                owner = runtimeOwner.GetType();
            }
            else if (!owner.IsSealed && !owner.IsValueType)
            {
                return null;
            }
        }

        string missingName = segments[missingIndex].Name;
        if (string.IsNullOrEmpty(missingName))
        {
            return null;
        }

        return $"Binding path '{path}' for {targetTypeName}.{targetPropertyName} does not resolve: " +
            $"{owner.Name} has no property '{missingName}' (BindingContext: {contextType.Name}).";
    }

    [RequiresUnreferencedCode(
        "Looks up a property or field on declaringType by name. That member may be removed under " +
        "PublishTrimmed if nothing else in the app references it.")]
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
    /// Builds a getter for <paramref name="path"/>. It returns <see cref="GumProperty.UnsetValue"/> when the
    /// path is broken (a null intermediate or collection, or a member the held object does not have) and null
    /// when the path resolves to a null leaf. Segments compile against declared types; from the first segment
    /// the declared type lacks (an intermediate declared as <c>object</c> or a base class), the rest of the path
    /// is resolved on the runtime type of the held object at every call.
    /// </summary>
    [RequiresUnreferencedCode(
        "Walks the path on targetType by name (Expression.PropertyOrField), and on runtime types by " +
        "reflection past a segment the declared type lacks. Those members may be removed under " +
        "PublishTrimmed if nothing else in the app references them.")]
    public static Func<object, object?> BuildGetter(Type targetType, string path)
    {
        ParameterExpression instanceParam = Expression.Parameter(typeof(object), "instance");
        LabelTarget returnTarget = Expression.Label(typeof(object), "result");
        Expression unset = Expression.Constant(GumProperty.UnsetValue, typeof(object));
        List<ParameterExpression> variables = new();
        List<Expression> statements = new();

        Expression current = Expression.Convert(instanceParam, targetType);
        PathSegment[] segments = ParseSegments(path);
        Expression? result = null;

        for (int i = 0; i < segments.Length; i++)
        {
            if (i > 0)
            {
                current = ReturnUnsetIfNull(current, returnTarget, unset, variables, statements);
            }

            PathSegment segment = segments[i];
            // At the root (already its runtime type) this reads as a broken path, so the target shows FallbackValue.
            if (!TryGetMember(current.Type, segment.Name, out _, out _))
            {
                result = Expression.Call(GetOnRuntimeTypeMethod,
                    Expression.Convert(current, typeof(object)), Expression.Constant(segments), Expression.Constant(i));
                break;
            }

            current = Expression.PropertyOrField(current, segment.Name);

            if (segment.Index is int index)
            {
                current = ReturnUnsetIfNull(current, returnTarget, unset, variables, statements);
                current = BuildIndexAccess(current, index);
            }
        }

        result ??= Expression.Convert(current, typeof(object));
        statements.Add(Expression.Label(returnTarget, result));

        Expression body = Expression.Block(typeof(object), variables, statements);
        return Expression.Lambda<Func<object, object?>>(body, instanceParam).Compile();
    }

    // Stores value in a local and returns early with unset when it is null, so a broken path is told apart from
    // a null leaf. Non-nullable value types pass through unchanged.
    private static Expression ReturnUnsetIfNull(Expression value, LabelTarget returnTarget, Expression unset,
        List<ParameterExpression> variables, List<Expression> statements)
    {
        if (value.Type.IsValueType && Nullable.GetUnderlyingType(value.Type) == null)
        {
            return value;
        }

        ParameterExpression local = Expression.Variable(value.Type);
        variables.Add(local);
        statements.Add(Expression.Assign(local, value));
        statements.Add(Expression.IfThen(
            Expression.Equal(local, Expression.Constant(null, local.Type)),
            Expression.Return(returnTarget, unset)));
        return local;
    }

    // A setter that compiles the declared-type prefix and resolves segments[startIndex..] on runtime types.
    private static Action<object, object?> BuildRuntimeTypeSetter(Expression holder, PathSegment[] segments,
        int startIndex, ParameterExpression instanceParam, ParameterExpression valueParam)
    {
        Expression call = Expression.Call(SetOnRuntimeTypeMethod,
            Expression.Convert(holder, typeof(object)), Expression.Constant(segments), Expression.Constant(startIndex),
            valueParam);
        return Expression.Lambda<Action<object, object?>>(call, instanceParam, valueParam).Compile();
    }

    private static readonly MethodInfo GetOnRuntimeTypeMethod =
        typeof(BinderHelpers).GetMethod(nameof(GetOnRuntimeType), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo SetOnRuntimeTypeMethod =
        typeof(BinderHelpers).GetMethod(nameof(SetOnRuntimeType), BindingFlags.NonPublic | BindingFlags.Static)!;

    // Resolves segments[startIndex..] on the runtime type of each held object. Same return contract as
    // BuildGetter's delegate.
    [RequiresUnreferencedCode("Looks up members by name on runtime types.")]
    private static object? GetOnRuntimeType(object? holder, PathSegment[] segments, int startIndex)
    {
        object? cursor = holder;
        for (int i = startIndex; i < segments.Length; i++)
        {
            if (cursor == null || !TryGetRuntimeValue(cursor, segments[i], out cursor))
            {
                return GumProperty.UnsetValue;
            }
        }
        return cursor;
    }

    // Writes value to the leaf of segments[startIndex..], resolving each segment on the runtime type of the
    // object that holds it. Does nothing when the path is broken or the leaf is read-only.
    [RequiresUnreferencedCode("Looks up members by name on runtime types.")]
    private static void SetOnRuntimeType(object? holder, PathSegment[] segments, int startIndex, object? value)
    {
        object? owner = holder;
        for (int i = startIndex; i < segments.Length - 1; i++)
        {
            if (owner == null || !TryGetRuntimeValue(owner, segments[i], out owner))
            {
                return;
            }
        }

        if (owner == null)
        {
            return;
        }

        PathSegment leaf = segments[^1];
        if (leaf.Index is int index)
        {
            if (TryGetMember(owner.GetType(), leaf.Name, out MemberInfo? collectionMember, out _) &&
                GetMemberValue(collectionMember!, owner) is IList list && index >= 0 && index < list.Count)
            {
                list[index] = value;
            }
            return;
        }

        if (!TryGetMember(owner.GetType(), leaf.Name, out MemberInfo? member, out Type memberType))
        {
            return;
        }

        if (value == null && memberType.IsValueType && Nullable.GetUnderlyingType(memberType) == null)
        {
            value = Activator.CreateInstance(memberType);
        }

        switch (member)
        {
            case PropertyInfo property when AsDeclared(property) is { SetMethod: not null } declared:
                declared.SetValue(owner, value);
                break;
            case FieldInfo { IsInitOnly: false, IsLiteral: false } field:
                field.SetValue(owner, value);
                break;
        }
    }

    [RequiresUnreferencedCode("Looks up members by name on runtime types.")]
    private static bool HasMember(Type owner, PathSegment segment, bool allowEvent) =>
        TryGetMember(owner, segment.Name, out _, out _) ||
        (allowEvent && !segment.Index.HasValue && owner.GetEvent(segment.Name) != null);

    // Walks segments[0..count) on runtime values. False when a value along the way is null or lacks the member.
    [RequiresUnreferencedCode("Looks up members by name on runtime types.")]
    private static bool TryWalkRuntime(object root, PathSegment[] segments, int count, out object? holder)
    {
        holder = root;
        for (int i = 0; i < count; i++)
        {
            if (holder == null || !TryGetRuntimeValue(holder, segments[i], out holder))
            {
                return false;
            }
        }
        return holder != null;
    }

    [RequiresUnreferencedCode("Looks up members by name on runtime types.")]
    private static bool TryGetRuntimeValue(object holder, PathSegment segment, out object? value)
    {
        value = null;
        if (!TryGetMember(holder.GetType(), segment.Name, out MemberInfo? member, out _))
        {
            return false;
        }

        value = GetMemberValue(member!, holder);
        if (segment.Index is int index)
        {
            if (value is not IList list || index < 0 || index >= list.Count)
            {
                return false;
            }
            value = list[index];
        }
        return true;
    }

    private static object? GetMemberValue(MemberInfo member, object holder) => member switch
    {
        PropertyInfo property => property.GetValue(holder),
        FieldInfo field => field.GetValue(holder),
        _ => null
    };

    /// <summary>
    /// Null-safe setter: no-op if any intermediate is null.
    /// </summary>
    [RequiresUnreferencedCode(
        "Walks the path on targetType by name (Expression.PropertyOrField). Those members may be " +
        "removed under PublishTrimmed if nothing else in the app references them.")]
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
            if (i > 0 && !TryGetMember(current.Type, segments[i].Name, out _, out _))
            {
                return BuildRuntimeTypeSetter(current, segments, i, instanceParam, valueParam);
            }

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
            if (segments[i].Index is int index)
            {
                current = BuildIndexAccessWithNullPropagation(current, index);
            }
        }

        PathSegment lastSegment = segments[segments.Length - 1];

        if (segments.Length > 1 && !TryGetMember(current.Type, lastSegment.Name, out _, out _))
        {
            return BuildRuntimeTypeSetter(current, segments, segments.Length - 1, instanceParam, valueParam);
        }

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
        if (memberExpr.Member is PropertyInfo leafProperty)
        {
            memberExpr = Expression.Property(current, AsDeclared(leafProperty));
        }

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
    [RequiresUnreferencedCode("Calls " + nameof(FindIntIndexer) + " for non-array collections.")]
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

    [RequiresUnreferencedCode("Calls " + nameof(FindIntIndexer) + " for non-array collections.")]
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

    [RequiresUnreferencedCode("Calls " + nameof(FindIntIndexer) + " for non-array collections.")]
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
            MethodInfo setter = AsDeclared(indexer).SetMethod
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

    [RequiresUnreferencedCode(
        "Searches type's own properties by reflection for an int indexer. That indexer may be removed " +
        "under PublishTrimmed if nothing else in the app references it.")]
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

    [RequiresUnreferencedCode("Calls " + nameof(FindIntIndexer) + ".")]
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
