using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfDataUi;

public static class Keys
{
    public static ComponentResourceKey AlternatingRowBackgroundEvenKey { get; } =
        new ComponentResourceKey(typeof(DataUiGrid), "DataUi.AlternatingRowBackgroundEven");
    public static ComponentResourceKey AlternatingRowBackgroundOddKey { get; } =
        new ComponentResourceKey(typeof(DataUiGrid), "DataUi.AlternatingRowBackgroundOdd");
}