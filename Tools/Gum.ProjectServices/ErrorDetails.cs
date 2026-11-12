using System.Collections.Generic;

namespace Gum.ProjectServices;

/// <summary>
/// The values an <see cref="ErrorResult"/> is about, as data. Each code that carries values has its own
/// subclass, so a tool can read them without parsing <see cref="ErrorResult.Message"/>, whose wording may change.
/// </summary>
public abstract class ErrorDetails
{
}

/// <summary>GUM0006: a file an element references is not on disk.</summary>
public class MissingExternalFileDetails : ErrorDetails
{
    /// <summary>The element or instance whose variable references the file.</summary>
    public string ReferencedFrom { get; set; } = "";

    /// <summary>The referenced path, relative to the project folder.</summary>
    public string ReferencedPath { get; set; } = "";
}

/// <summary>GUM0008: a referenced file name differs from the file on disk only by case.</summary>
public class FileNameCaseMismatchDetails : ErrorDetails
{
    /// <summary>The element or instance that references the file.</summary>
    public string ReferencedFrom { get; set; } = "";

    /// <summary>The path as the project spells it, relative to the project folder.</summary>
    public string ReferencedPath { get; set; } = "";

    /// <summary>The path as the file on disk spells it, relative to the project folder.</summary>
    public string OnDiskPath { get; set; } = "";
}

/// <summary>GUM0018: an element contains an instance of itself.</summary>
public class ElementContainsItselfDetails : ErrorDetails
{
    /// <summary>The element names on the loop, starting and ending with the element itself.</summary>
    public IReadOnlyList<string> ElementPath { get; set; } = new List<string>();
}

/// <summary>GUM0032: a component uses a behavior the project does not have.</summary>
public class MissingBehaviorDetails : ErrorDetails
{
    /// <summary>The name of the missing behavior.</summary>
    public string BehaviorName { get; set; } = "";
}

/// <summary>GUM0033: a component lacks a variable its behavior requires, or has it with the wrong type.</summary>
public class BehaviorVariableRequirementDetails : ErrorDetails
{
    /// <summary>The behavior that requires the variable.</summary>
    public string BehaviorName { get; set; } = "";

    /// <summary>The name of the required variable.</summary>
    public string VariableName { get; set; } = "";

    /// <summary>The type the behavior requires.</summary>
    public string RequiredType { get; set; } = "";

    /// <summary>The type the component's variable has, or null when the component has no such variable.</summary>
    public string? ActualType { get; set; }
}

/// <summary>GUM0034: an instance's Parent names an instance the element does not have.</summary>
public class MissingParentDetails : ErrorDetails
{
    /// <summary>The instance whose Parent is set.</summary>
    public string InstanceName { get; set; } = "";

    /// <summary>The Parent value, which may name a child of the parent (<c>Parent.Child</c>).</summary>
    public string ParentValue { get; set; } = "";

    /// <summary>The state that sets the Parent.</summary>
    public string StateName { get; set; } = "";
}

/// <summary>GUM0035: an instance's type is an element the project does not have.</summary>
public class MissingInstanceTypeDetails : ErrorDetails
{
    /// <summary>The instance whose type is missing.</summary>
    public string InstanceName { get; set; } = "";

    /// <summary>The missing type.</summary>
    public string BaseType { get; set; } = "";
}

/// <summary>GUM0036: a state variable's type or name lacks the <c>State</c> suffix.</summary>
public class StateSuffixDetails : ErrorDetails
{
    /// <summary>The variable's name.</summary>
    public string VariableName { get; set; } = "";

    /// <summary>The variable's type.</summary>
    public string VariableType { get; set; } = "";

    /// <summary>The type the variable should have, or null when the name is what lacks the suffix.</summary>
    public string? SuggestedVariableType { get; set; }

    /// <summary>The name the variable should have, or null when the type is what lacks the suffix.</summary>
    public string? SuggestedVariableName { get; set; }
}

/// <summary>GUM0037: a Sprite uses an .achx with per-frame offsets but its origin is not Center.</summary>
public class AnimationChainOriginDetails : ErrorDetails
{
    /// <summary>The Sprite instance, or null when the element itself is the Sprite.</summary>
    public string? InstanceName { get; set; }

    /// <summary>The .achx file, as the variable stores it.</summary>
    public string SourceFile { get; set; } = "";
}

/// <summary>GUM0038: an element file could not be read as XML or JSON.</summary>
public class MalformedFileDetails : ErrorDetails
{
    /// <summary><c>XML</c> or <c>JSON</c>.</summary>
    public string Format { get; set; } = "";

    /// <summary>The parser's own message, which usually names the line and column.</summary>
    public string ParserMessage { get; set; } = "";
}

/// <summary>GUM0040: an element file has content that loading ignores.</summary>
public class IgnoredContentDetails : ErrorDetails
{
    /// <summary>The ignored XML element (for example <c>&lt;States&gt;</c>) or JSON property name.</summary>
    public string Content { get; set; } = "";
}

/// <summary>GUM0041: an element file has unresolved git conflict markers.</summary>
public class ConflictMarkerDetails : ErrorDetails
{
    /// <summary>The file's name with its extension.</summary>
    public string FileName { get; set; } = "";
}

/// <summary>GUM0043: an animation keyframe uses a state the element does not have.</summary>
public class KeyframeMissingStateDetails : ErrorDetails
{
    /// <summary>The animation that has the keyframe.</summary>
    public string AnimationName { get; set; } = "";

    /// <summary>The keyframe's time, in seconds.</summary>
    public float Time { get; set; }

    /// <summary>The missing state, qualified with its category when it has one.</summary>
    public string StateName { get; set; } = "";
}

/// <summary>GUM0044: an animation keyframe plays an animation that does not exist.</summary>
public class KeyframeMissingAnimationDetails : ErrorDetails
{
    /// <summary>The animation that has the keyframe.</summary>
    public string AnimationName { get; set; } = "";

    /// <summary>The keyframe's time, in seconds.</summary>
    public float Time { get; set; }

    /// <summary>The missing animation, as the keyframe names it.</summary>
    public string MissingAnimationName { get; set; } = "";
}
