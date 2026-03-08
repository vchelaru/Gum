namespace Gum.Managers;

/// <summary>
/// Common validation errors for name checks.
/// </summary>
public enum CommonValidationError
{
    None,
    IsEmpty,
    StartsWithSpace,
    EndsWithSpace,
    InvalidCharacter,
    InvalidStartingCharacterForCSharp,
    ReservedCSharpKeyword
}
