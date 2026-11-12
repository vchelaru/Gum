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

/// <summary>
/// A loop. GUM0013 (variable references, as <c>Element.Variable</c>), GUM0014 (base types), GUM0018 (an element
/// containing itself, as element names) and GUM0024 (Parent values, as instance names).
/// </summary>
public class CycleDetails : ErrorDetails
{
    /// <summary>The names on the loop, in order, ending with the first name again.</summary>
    public IReadOnlyList<string> Path { get; set; } = new List<string>();

    /// <summary>The state the loop is in, or null when it is not in one state.</summary>
    public string? StateName { get; set; }
}

/// <summary>
/// A variable in a state. GUM0003 (a category state sets its own category), GUM0007 (enum value not defined),
/// GUM0019 (value does not fit the type), GUM0026 (state the category does not have), GUM0027 (variable of a
/// missing instance) and GUM0028 (unknown type).
/// </summary>
public class StateVariableDetails : ErrorDetails
{
    /// <summary>The state that sets the variable.</summary>
    public string StateName { get; set; } = "";

    /// <summary>The category of the state, or null for an uncategorized state or when the code does not report it.</summary>
    public string? CategoryName { get; set; }

    /// <summary>The variable's name, qualified with its instance when it has one (<c>Instance.Variable</c>).</summary>
    public string VariableName { get; set; } = "";

    /// <summary>The variable's type, or null when the code does not report it.</summary>
    public string? VariableType { get; set; }

    /// <summary>The variable's value as text, or null when the code does not report it.</summary>
    public string? Value { get; set; }
}

/// <summary>What kind of thing has a duplicate name in <see cref="DuplicateNameDetails"/>.</summary>
public enum DuplicateKind
{
    Instance,
    State,
    Category,
    Variable,
    Screen,
    Component,
    StandardElement,
    Behavior
}

/// <summary>
/// A name used more than once. GUM0020 (instances), GUM0021 (states or categories), GUM0022 (variables in a state)
/// and GUM0023 (references in the project file).
/// </summary>
public class DuplicateNameDetails : ErrorDetails
{
    /// <summary>What has the duplicate name.</summary>
    public DuplicateKind Kind { get; set; }

    /// <summary>The duplicated name.</summary>
    public string Name { get; set; } = "";

    /// <summary>The category that holds the duplicate states, or null.</summary>
    public string? CategoryName { get; set; }

    /// <summary>The state that holds the duplicate variables, or null.</summary>
    public string? StateName { get; set; }
}

/// <summary>A file the project expects that is not on disk. GUM0004 (element file) and GUM0031 (behavior file).</summary>
public class MissingFileDetails : ErrorDetails
{
    /// <summary>The expected path, relative to the project folder.</summary>
    public string ExpectedPath { get; set; } = "";
}

/// <summary>GUM0001: a component lacks an instance its behavior requires.</summary>
public class BehaviorInstanceRequirementDetails : ErrorDetails
{
    /// <summary>The behavior that requires the instance.</summary>
    public string BehaviorName { get; set; } = "";

    /// <summary>The required instance name.</summary>
    public string InstanceName { get; set; } = "";

    /// <summary>The required instance type, or null when any type will do.</summary>
    public string? InstanceType { get; set; }

    /// <summary>The behaviors the instance's type must have.</summary>
    public IReadOnlyList<string> RequiredBehaviors { get; set; } = new List<string>();
}

/// <summary>
/// A font range problem. GUM0010 (entry matches no font: <see cref="Font"/>), GUM0011 (invalid range:
/// <see cref="Font"/>, null for the project-wide range, and <see cref="Ranges"/>) and GUM0012 (Text outside its
/// font's range: <see cref="InstanceName"/>, <see cref="Font"/> and <see cref="CodePoints"/>).
/// </summary>
public class FontRangeDetails : ErrorDetails
{
    /// <summary>The font family or font file, or null for the project-wide range.</summary>
    public string? Font { get; set; }

    /// <summary>The range text, or null.</summary>
    public string? Ranges { get; set; }

    /// <summary>The Text instance, or null when the element itself is the Text or the code is not GUM0012.</summary>
    public string? InstanceName { get; set; }

    /// <summary>The Unicode code points the range leaves out.</summary>
    public IReadOnlyList<int> CodePoints { get; set; } = new List<int>();
}

/// <summary>GUM0009: a VariableReferences line reads something the project does not have.</summary>
public class VariableReferenceLineDetails : ErrorDetails
{
    /// <summary>The state that has the line.</summary>
    public string StateName { get; set; } = "";

    /// <summary>The VariableReferences variable, qualified with its instance when it has one.</summary>
    public string VariableListName { get; set; } = "";

    /// <summary>The line as written.</summary>
    public string Line { get; set; } = "";
}

/// <summary>GUM0016: a derived element's file does not list an instance its base defines.</summary>
public class MissingBaseInstanceDetails : ErrorDetails
{
    /// <summary>The base element that defines the instance.</summary>
    public string BaseElementName { get; set; } = "";

    /// <summary>The instance's name.</summary>
    public string InstanceName { get; set; } = "";

    /// <summary>The instance's type.</summary>
    public string InstanceType { get; set; } = "";
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

/// <summary>
/// A type an element or instance names that it cannot use. GUM0025 (a Screen used as a type), GUM0030 (an
/// element's base type does not exist) and GUM0035 (an instance's type does not exist).
/// </summary>
public class TypeReferenceDetails : ErrorDetails
{
    /// <summary>The instance whose type it is, or null when it is the element's own base type.</summary>
    public string? InstanceName { get; set; }

    /// <summary>The type as written.</summary>
    public string Type { get; set; } = "";
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
