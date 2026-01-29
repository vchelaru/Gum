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

    }

}
