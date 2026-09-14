using System;
using System.Reflection;

namespace WpfDataUi
{
    public static class MemberInfoExtensionMethods
    {
        public static Type GetMemberType(this MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo)
            {
                return ((FieldInfo)memberInfo).FieldType;
            }
            else if (memberInfo is PropertyInfo)
            {
                return ((PropertyInfo)memberInfo).PropertyType;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
