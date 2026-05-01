using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace Gum.PropertyGridHelpers.Converters
{
    /// <summary>
    /// A type converter for enums which can restrict certain values from appearing.
    /// </summary>
    public class RestrictiveEnumConverter : EnumConverter
    {
        public List<object> ValuesToExclude = new List<object>();

        public RestrictiveEnumConverter(Type type)
            : base(type)
        {

        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            // Iterate by NAME (not value) so we can:
            //   1. Skip names tagged with [Obsolete] — these share an underlying value with
            //      their canonical replacement (e.g. DimensionUnitType.Percentage = 1 =
            //      PercentageOfParent), so excluding one by value would also hide the other.
            //      Iterating Enum.GetValues would surface BOTH names ("Percentage" twice in
            //      the dropdown — once for each declared alias).
            //   2. Dedupe alias pairs that aren't marked obsolete but still share a value.
            // The first non-obsolete name wins for any given underlying value.
            var seenValues = new HashSet<object>();
            List<object> valuesToReturn = new List<object>();

            foreach (string name in Enum.GetNames(EnumType))
            {
                var member = EnumType.GetMember(name);
                if (member.Length > 0 && member[0].GetCustomAttribute<ObsoleteAttribute>() != null)
                {
                    continue;
                }

                object value = Enum.Parse(EnumType, name);

                if (!seenValues.Add(value))
                {
                    continue;
                }

                if (ValuesToExclude.Contains(value))
                {
                    continue;
                }

                valuesToReturn.Add(value);
            }

            return new StandardValuesCollection(valuesToReturn);
        }
    }
}
