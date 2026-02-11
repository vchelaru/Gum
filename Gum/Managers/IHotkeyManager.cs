namespace Gum.Managers;

/// <summary>
/// Interface for HotkeyManager to enable dependency injection and testing.
/// Provides access to keyboard shortcuts used throughout the application.
/// </summary>
public interface IHotkeyManager
{
    KeyCombination MultiSelect { get; }
    KeyCombination SnapRotationTo15Degrees { get; }
    KeyCombination ResizeFromCenter { get; }
    KeyCombination LockMovementToAxis { get; }
}
