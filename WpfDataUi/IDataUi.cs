using WpfDataUi.DataTypes;

namespace WpfDataUi
{
    public enum ApplyValueResult
    {
        Success,
        NotSupported,
        InvalidSyntax,
        NotEnoughInformation,
        NotEnabled,
        UnknownError,
        Skipped
    }

    public interface IDataUi
    {
        InstanceMember? InstanceMember { get; set; }
        bool SuppressSettingProperty { get; set; }
        void Refresh(bool forceRefreshEvenIfFocused = false);

        bool IsEnabled { get; set; }

        ApplyValueResult TryGetValueOnUi(out object result);
        ApplyValueResult TrySetValueOnUi(object value);

        /// <summary>
        /// Called when the control is returned to the pool.
        /// Override to reset any state that was set via
        /// PropertiesToSetOnDisplayer or other external configuration
        /// so the control doesn't leak state to its next consumer.
        /// </summary>
        void ResetForPooling() { }
    }

}
