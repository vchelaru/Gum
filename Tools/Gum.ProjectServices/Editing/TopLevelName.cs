namespace Gum.Managers;

/// <summary>
/// A name declared directly on an element (an instance, a category, an exposed variable, or what a tool plugin
/// adds such as an animation). No two may share a name. It keeps this namespace because tool plugins use it;
/// <c>Gum.Presentation</c> forwards the type here.
/// </summary>
public record TopLevelName(string Name, string Type, object? SourceObject);
