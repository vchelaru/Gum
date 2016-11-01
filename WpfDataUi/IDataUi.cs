using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        InstanceMember InstanceMember { get; set; }
        bool SuppressSettingProperty { get; set; }
        void Refresh(bool forceRefreshEvenIfFocused = false);

        ApplyValueResult TryGetValueOnUi(out object result);
        ApplyValueResult TrySetValueOnUi(object value);

    }

}
