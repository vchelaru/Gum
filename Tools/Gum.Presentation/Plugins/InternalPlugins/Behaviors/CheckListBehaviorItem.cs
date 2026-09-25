using Gum.Mvvm;

namespace Gum.Plugins.Behaviors
{
    public class CheckListBehaviorItem : ViewModel
    {
        /// <summary>The behavior's name, or null for a malformed component reference with no name.</summary>
        public string? Name
        {
            get => Get<string?>();
            set => Set(value);
        }

        public bool IsChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        /// <summary>
        /// True when the behavior is referenced by the component but no longer exists in the project.
        /// Surfaced so the user can uncheck stale references.
        /// </summary>
        public bool IsOrphaned
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(Name))]
        [DependsOn(nameof(IsOrphaned))]
        public string DisplayText => IsOrphaned ? $"{Name} (missing)" : Name ?? "";
    }
}
