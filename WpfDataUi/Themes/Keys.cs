using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfDataUi;

public static class Keys
{
    /// <summary>
    /// The row template (one <see cref="Controls.SingleDataUiContainer"/> per member). Keyed by
    /// component resource rather than by type: WPF looks for a type-keyed template in the theme of
    /// the assembly that defines the type, and the member types live in DataUi.Core, which has no
    /// theme. Consumers that re-template the categories use this key for their rows.
    /// </summary>
    public static ComponentResourceKey InstanceMemberTemplateKey { get; } =
        new ComponentResourceKey(typeof(DataUiGrid), "DataUi.InstanceMemberTemplate");
    public static ComponentResourceKey AlternatingRowBackgroundEvenKey { get; } =
        new ComponentResourceKey(typeof(DataUiGrid), "DataUi.AlternatingRowBackgroundEven");
    public static ComponentResourceKey AlternatingRowBackgroundOddKey { get; } =
        new ComponentResourceKey(typeof(DataUiGrid), "DataUi.AlternatingRowBackgroundOdd");
}