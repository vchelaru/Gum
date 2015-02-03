using System;
using System.Collections.Generic;
using System.Reflection;
#if !XBOX360 && !ZUNE && !WINDOWS_PHONE
using System.Reflection.Emit;
#endif

namespace Gum.Reflection
{
    public abstract class LateBinder
    {
        static Dictionary<Type, LateBinder> mLateBinders = new Dictionary<Type, LateBinder>();

        public static LateBinder GetInstance(Type type)
        {
            if (!mLateBinders.ContainsKey(type))
            {
                Type t = typeof(LateBinder<>).MakeGenericType(
                    type);
                object obj = Activator.CreateInstance(t);

                mLateBinders.Add(type, obj as LateBinder);
            }
            return mLateBinders[type];

        }

        public abstract object GetValue(object target, string name);
        public abstract void SetValue(object target, string name, object value);
    }


    /// <summary>
    /// Provides a simple interface to late bind a class.
    /// </summary>
    /// <remarks>The first time you attempt to get or set a property, it will dynamically generate the get and/or set 
    /// methods and cache them internally.  Subsequent gets uses the dynamic methods without having to query the type's 
    /// meta data.</remarks>
    public sealed class LateBinder<T> : LateBinder
    {
        #region Fields

        HashSet<string> mFieldsSet = new HashSet<string>();
        HashSet<string> mPropertieSet = new HashSet<string>();

        private Type mType;
        private Dictionary<string, GetHandler> mPropertyGet;
        private Dictionary<string, SetHandler> mPropertySet;

        private Dictionary<Type, List<string>> mFields;

        private T mTarget = default(T);

        private static LateBinder<T> _instance;

        #endregion

        #region Properties

        public static LateBinder<T> Instance
        {
            get { return _instance; }
        }


        /// <summary>
        /// The instance that this binder operates on by default
        /// </summary>
        /// <remarks>This can be overridden by the caller explicitly passing a target to the indexer</remarks>
        public T Target
        {
            get { return mTarget; }
            set { mTarget = value; }
        }

        /// <summary>
        /// Gets or Sets the supplied property on the contained <seealso cref="Instance"/>
        /// </summary>
        /// <exception cref="InvalidOperationException">Throws if the contained Instance is null.</exception>
        public object this[string propertyName]
        {
            get
            {
                ValidateInstance();
                return this[mTarget, propertyName];
            }
            set
            {
                ValidateInstance();
                this[mTarget, propertyName] = value;
            }
        }

        /// <summary>
        /// Gets or Sets the supplied property on the supplied target
        /// </summary>
        public object this[T target, string propertyName]
        {
            get
            {
                ValidateGetter(ref propertyName);
                return mPropertyGet[propertyName](target);
            }
            set
            {
                ValidateSetter(ref propertyName);
                mPropertySet[propertyName](target, value);
            }
        }


        #endregion

        #region Methods

        #region Constructors

        static LateBinder()
        {
            _instance = new LateBinder<T>();
        }


        public LateBinder(T instance)
            : this()
        {
            mTarget = instance;
        }

        public LateBinder()
        {
            mType = typeof(T);
            mPropertyGet = new Dictionary<string, GetHandler>();
            mPropertySet = new Dictionary<string, SetHandler>();

            mFields = new Dictionary<Type, List<string>>();
        }
        #endregion

        #endregion

        #region Public Accessors

        public override object GetValue(object target, string name)
        {
            if (mFieldsSet.Contains(name))
            {
                return GetField(target, name);
            }
            else if (mPropertieSet.Contains(name))
            {
                return GetProperty(target, name);
            }
            else
            {
                if (mType.GetField(name, mGetFieldBindingFlags) != null)
                {
                    mFieldsSet.Add(name);
                    return GetField(target, name);
                }
                else
                {
                    mPropertieSet.Add(name);
                    return GetProperty(target, name);
                }
            }
        }

        public override void SetValue(object target, string name, object value)
        {
            if (mFieldsSet.Contains(name))
            {
                // do nothing currently
                SetField(target, name, value);
            }
            else if (mPropertieSet.Contains(name))
            {
                SetProperty(target, name, value);
            }
            else
            {
                if (mType.GetField(name, mGetFieldBindingFlags) != null)
                {
                    mFieldsSet.Add(name);

                    SetField(target, name, value);
                }
                else
                {
                    mPropertieSet.Add(name);
                    SetProperty(target, name, value);
                }
            }
        }

        private void SetField(object target, string name, object value)
        {
#if XBOX360 || WINDOWS_PHONE || SILVERLIGHT
            throw new NotImplementedException();
#else
            FieldInfo fieldInfo = target.GetType().GetField(
                name);


#if DEBUG
            try
            {
#endif



                fieldInfo.SetValueDirect(
                    __makeref(target), value);


#if DEBUG
            }
            catch (Exception)
            {
                if(fieldInfo == null)
                {
                    throw new Exception("Could nto find field by the name " + name );
                }
                else
                {

                    throw new Exception("Error trying to set field " + name + " which is of type " + fieldInfo.FieldType + ".\nTrying to set to " + value + " of type " + value.GetType());
                }
            }
#endif


#endif       
        }


        /// <summary>
        /// Sets the supplied property on the supplied target
        /// </summary>
        /// <typeparam name="K">the type of the value</typeparam>
        public void SetProperty<K>(object target, string propertyName, K value)
        {
#if XBOX360 || SILVERLIGHT || ZUNE || WINDOWS_PHONE

            // find out if this is a property or field
            Type type = typeof(T);

            PropertyInfo propertyInfo = type.GetProperty(propertyName);

            if (propertyInfo != null)
            {
                propertyInfo.SetValue(target, value, null);
            }

            else
            {
                FieldInfo fieldInfo = type.GetField(propertyName);

                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(target, value);
                }
                else
                {
                    throw new ArgumentException("Cannot find property or field with the name " + propertyName);
                }

            }


#else
            ValidateSetter(ref propertyName);

            if (mPropertySet.ContainsKey(propertyName))
            {
                mPropertySet[propertyName](target, value);
            }
            else
            {
                // This is probably not a property so see if it is a field.

                FieldInfo fieldInfo = mType.GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

                if (fieldInfo == null)
                {
                    string errorMessage =
                        "LateBinder could not find a field or property by the name of " + propertyName +
                        ".  Check the name of the property to verify if it is correct.";
                    throw new System.MemberAccessException(errorMessage);
                }
                else
                {

                    if (!fieldInfo.IsPublic)
                    {
                        fieldInfo.SetValue(target, value);
                    }
                    else
                    {
                        object[] args = { value };
                        mType.InvokeMember(propertyName, BindingFlags.SetField, null, target, args);
                    }
                }
            }
#endif
        }

        static BindingFlags mGetFieldBindingFlags = BindingFlags.GetField | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public object GetField(object target, string fieldName)
        {


            if (target == null)
            {
                FieldInfo fieldInfo = mType.GetField(fieldName, mGetFieldBindingFlags);
                return fieldInfo.GetValue(null);

            }
            else
            {
                Binder binder = null;
                object[] args = null; 
                
                return mType.InvokeMember(
                   fieldName,
                   mGetFieldBindingFlags,
                   binder,
                   target,
                   args
                   );
            }
        }

        public ReturnType GetField<ReturnType>(T target, string propertyName)
        {
            return (ReturnType)GetField(target, propertyName);
        }

        /// <summary>
        /// Gets  the supplied property on the supplied target
        /// </summary>
        /// <typeparam name="K">The type of the property being returned</typeparam>
        public K GetProperty<K>(T target, string propertyName)
        {
            return (K)GetProperty(target, propertyName);
        }

        public object GetProperty(object target, string propertyName)
        {
#if XBOX360 || SILVERLIGHT || ZUNE || WINDOWS_PHONE || XNA4
            // SLOW, but still works
            return GetPropertyThroughReflection(target, propertyName);
#else
            // June 11, 2011
            // Turns out that
            // getters for value
            // types don't work properly.
            // I found this out by trying to
            // get the X value on a System.Drawing.Rectangle
            // which was 0, but it kept returning a value of
            // 2 billion.  Checking for value types and using 
            // regular reflection fixes this problem.
            if (target == null || typeof(T).IsValueType)
            {
                // SLOW, but still works
                return GetPropertyThroughReflection(target, propertyName);
            }
            else
            {
                ValidateGetter(ref propertyName);

                GetHandler getHandler = mPropertyGet[propertyName];

                return getHandler(target);
            }
#endif


        }

        private static object GetPropertyThroughReflection(object target, string propertyName)
        {
            PropertyInfo pi = typeof(T).GetProperty(propertyName, mGetterBindingFlags);

            if (pi == null)
            {
                string message = "Could not find the property " + propertyName + "\n\nAvailableProperties:\n\n";

                PropertyInfo[] properties = typeof(T).GetProperties(mGetterBindingFlags);

                foreach (PropertyInfo containedProperty in properties)
                {
                    message += containedProperty.Name + "\n";

                }

                throw new InvalidOperationException(message);
            }

            return pi.GetValue(target, null);
        }

        #endregion

        #region Private Helpers
        private void ValidateInstance()
        {
            if (mTarget == null)
            {
                throw new InvalidOperationException("Instance property must not be null");
            }
        }
        private void ValidateSetter(ref string propertyName)
        {
            if (!mPropertySet.ContainsKey(propertyName))
            {
                BindingFlags bindingFlags =
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Static;

                PropertyInfo propertyInfo = mType.GetProperty(propertyName, bindingFlags);

                if (propertyInfo != null)
                {
                    mPropertySet.Add(propertyName, DynamicMethodCompiler.CreateSetHandler(mType, propertyInfo));
                }

            }
        }

        static BindingFlags mGetterBindingFlags = 
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Static;                

        private void ValidateGetter(ref string propertyName)
        {
            if (!mPropertyGet.ContainsKey(propertyName))
            {


                PropertyInfo propertyInfo = mType.GetProperty(propertyName, mGetterBindingFlags);

                if (propertyInfo != null)
                {

                    mPropertyGet.Add(propertyName, DynamicMethodCompiler.CreateGetHandler(mType, propertyInfo));
                }
            }
        }
        #endregion

        #region Contained Classes
        internal delegate object GetHandler(object source);
        internal delegate void SetHandler(object source, object value);
        internal delegate object InstantiateObjectHandler();

        /// <summary>
        /// provides helper functions for late binding a class
        /// </summary>
        /// <remarks>
        /// Class found here:
        /// http://www.codeproject.com/useritems/Dynamic_Code_Generation.asp
        /// </remarks>
        internal sealed class DynamicMethodCompiler
        {
            // DynamicMethodCompiler
            private DynamicMethodCompiler() { }

            // CreateInstantiateObjectDelegate
            internal static InstantiateObjectHandler CreateInstantiateObjectHandler(Type type)
            {
#if !XBOX360 && !SILVERLIGHT && !ZUNE && !WINDOWS_PHONE
                ConstructorInfo constructorInfo = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[0], null);
                if (constructorInfo == null)
                {
                    throw new ApplicationException(string.Format("The type {0} must declare an empty constructor (the constructor may be private, internal, protected, protected internal, or public).", type));
                }

                DynamicMethod dynamicMethod = new DynamicMethod("InstantiateObject", MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, typeof(object), null, type, true);
                ILGenerator generator = dynamicMethod.GetILGenerator();
                generator.Emit(OpCodes.Newobj, constructorInfo);
                generator.Emit(OpCodes.Ret);
                return (InstantiateObjectHandler)dynamicMethod.CreateDelegate(typeof(InstantiateObjectHandler));
#else
                throw new NotSupportedException();
#endif
            }

            // CreateGetDelegate
            internal static GetHandler CreateGetHandler(Type type, PropertyInfo propertyInfo)
            {
#if !XBOX360 && !SILVERLIGHT && !ZUNE && !WINDOWS_PHONE
                MethodInfo getMethodInfo = propertyInfo.GetGetMethod(true);
                DynamicMethod dynamicGet = CreateGetDynamicMethod(type);
                ILGenerator getGenerator = dynamicGet.GetILGenerator();

                getGenerator.Emit(OpCodes.Ldarg_0);
                getGenerator.Emit(OpCodes.Call, getMethodInfo);
                BoxIfNeeded(getMethodInfo.ReturnType, getGenerator);
                getGenerator.Emit(OpCodes.Ret);

                return (GetHandler)dynamicGet.CreateDelegate(typeof(GetHandler));
#else
                throw new NotSupportedException();
#endif
            }

            // CreateGetDelegate
            internal static GetHandler CreateGetHandler(Type type, FieldInfo fieldInfo)
            {
#if !XBOX360 && !SILVERLIGHT && !ZUNE && !WINDOWS_PHONE
                DynamicMethod dynamicGet = CreateGetDynamicMethod(type);
                ILGenerator getGenerator = dynamicGet.GetILGenerator();

                getGenerator.Emit(OpCodes.Ldarg_0);
                getGenerator.Emit(OpCodes.Ldfld, fieldInfo);
                BoxIfNeeded(fieldInfo.FieldType, getGenerator);
                getGenerator.Emit(OpCodes.Ret);

                return (GetHandler)dynamicGet.CreateDelegate(typeof(GetHandler));
#else
                throw new NotSupportedException();
#endif
            }

            // CreateSetDelegate
            internal static SetHandler CreateSetHandler(Type type, PropertyInfo propertyInfo)
            {
#if !XBOX360 && !SILVERLIGHT && !ZUNE && !WINDOWS_PHONE
                MethodInfo setMethodInfo = propertyInfo.GetSetMethod(true);

                DynamicMethod dynamicSet = CreateSetDynamicMethod(type);
                ILGenerator setGenerator = dynamicSet.GetILGenerator();

                setGenerator.Emit(OpCodes.Ldarg_0);
                setGenerator.Emit(OpCodes.Ldarg_1);
                UnboxIfNeeded(setMethodInfo.GetParameters()[0].ParameterType, setGenerator);
                setGenerator.Emit(OpCodes.Call, setMethodInfo);
                setGenerator.Emit(OpCodes.Ret);

                return (SetHandler)dynamicSet.CreateDelegate(typeof(SetHandler));
#else
                throw new NotSupportedException();
#endif
            }

            // CreateSetDelegate
            internal static SetHandler CreateSetHandler(Type type, FieldInfo fieldInfo)
            {
#if !XBOX360 && !SILVERLIGHT && !ZUNE && !WINDOWS_PHONE
                DynamicMethod dynamicSet = CreateSetDynamicMethod(type);
                ILGenerator setGenerator = dynamicSet.GetILGenerator();

                setGenerator.Emit(OpCodes.Ldarg_0);
                setGenerator.Emit(OpCodes.Ldarg_1);
                UnboxIfNeeded(fieldInfo.FieldType, setGenerator);
                setGenerator.Emit(OpCodes.Stfld, fieldInfo);
                setGenerator.Emit(OpCodes.Ret);

                return (SetHandler)dynamicSet.CreateDelegate(typeof(SetHandler));
#else
                throw new NotSupportedException();
#endif
            }

#if !XBOX360 && !SILVERLIGHT && !ZUNE && !WINDOWS_PHONE
            // CreateGetDynamicMethod
            private static DynamicMethod CreateGetDynamicMethod(Type type)
            {
                return new DynamicMethod("DynamicGet", typeof(object), new Type[] { typeof(object) }, type, true);
            }

            // CreateSetDynamicMethod
            private static DynamicMethod CreateSetDynamicMethod(Type type)
            {
                return new DynamicMethod("DynamicSet", typeof(void), new Type[] { typeof(object), typeof(object) }, type, true);
            }

            // BoxIfNeeded
            private static void BoxIfNeeded(Type type, ILGenerator generator)
            {
                if (type.IsValueType)
                {
                    generator.Emit(OpCodes.Box, type);
                }
            }

            // UnboxIfNeeded
            private static void UnboxIfNeeded(Type type, ILGenerator generator)
            {
                if (type.IsValueType)
                {
                    generator.Emit(OpCodes.Unbox_Any, type);
                }
            }
#endif
        }
        #endregion
    }
}
