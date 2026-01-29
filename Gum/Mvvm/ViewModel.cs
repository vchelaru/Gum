using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Gum.Mvvm
{
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
    public class DependsOnAttribute : Attribute
    {
        public string ParentProperty { get; set; }

        public DependsOnAttribute(string parentPropertyName)
        {
            ParentProperty = parentPropertyName;
        }

    }

    public class ViewModel : INotifyPropertyChanged
    {
        Dictionary<string, List<string>> notifyRelationships = new Dictionary<string, List<string>>();
        private Dictionary<string, object> propertyDictionary = new Dictionary<string, object>();

        public int GetPropertyChangeCount() => PropertyChanged?.GetInvocationList().Length ?? 0;

        protected T Get<T>([CallerMemberName]string propertyName = null)
        {
            T toReturn = default(T);

            if (propertyName != null && propertyDictionary.ContainsKey(propertyName))
            {
                toReturn = (T)propertyDictionary[propertyName];
            }

            return toReturn;
        }

        protected bool Set<T>(T propertyValue, [CallerMemberName]string propertyName = null)
        {
            if (propertyValue is INotifyCollectionChanged collection)
            {
                var oldValue = Get<T>(propertyName);

                if (oldValue is INotifyCollectionChanged oldCollection)
                {
                    oldCollection.CollectionChanged -= CollectionChangedInternal;
                }
                collection.CollectionChanged += CollectionChangedInternal;
            }

            bool didSet = SetWithoutNotifying(propertyValue, propertyName);

            if (didSet)
            {
                NotifyPropertyChanged(propertyName);
            }

            return didSet;


            void CollectionChangedInternal(object? sender, NotifyCollectionChangedEventArgs e)
            {
                var shouldNotify = true;
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    // weirdly enough in Gum, resetting has nulls here even when we 
                    // call clear. Not sure why so let's return true even if these are null
                    //shouldNotify = e.OldItems != null || e.NewItems != null;
                }
                if (shouldNotify)
                {
                    NotifyPropertyChanged(propertyName);
                }
            }
        }

        protected bool SetWithoutNotifying<T>(T propertyValue, [CallerMemberName]string propertyName = null)
        {
            var didSet = false;

            if (propertyDictionary.ContainsKey(propertyName))
            {
                var storage = (T)propertyDictionary[propertyName];
                if (EqualityComparer<T>.Default.Equals(storage, propertyValue) == false)
                {
                    didSet = true;
                    propertyDictionary[propertyName] = propertyValue;
                }
            }
            else
            {
                propertyDictionary[propertyName] = propertyValue;

                // Even though the user is setting a new value, we want to make sure it's
                // not the same:
                var defaultValue = default(T);
                var isSettingDefault =
                    EqualityComparer<T>.Default.Equals(defaultValue, propertyValue);

                didSet = isSettingDefault == false;
            }

            return didSet;
        }


        public ViewModel()
        {
            var derivedType = this.GetType();

            var properties = derivedType.GetRuntimeProperties();

            foreach (var property in properties)
            {
                var attributes = property.GetCustomAttributes(true);

                string child = property.Name;
                foreach (var uncastedAttribute in attributes)
                {
                    if (uncastedAttribute is DependsOnAttribute)
                    {
                        var attribute = uncastedAttribute as DependsOnAttribute;

                        string parent = attribute.ParentProperty;

                        List<string> childrenProps = null;
                        if (notifyRelationships.ContainsKey(parent) == false)
                        {
                            childrenProps = new List<string>();
                            notifyRelationships[parent] = childrenProps;
                        }
                        else
                        {
                            childrenProps = notifyRelationships[parent];
                        }

                        childrenProps.Add(child);
                    }
                }
            }

        }
        protected void ChangeAndNotify<T>(ref T property, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(property, value) == false)
            {
                property = value;
                NotifyPropertyChanged(propertyName);
            }
        }

        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

            if (notifyRelationships.ContainsKey(propertyName))
            {
                var childPropertyNames = notifyRelationships[propertyName];

                foreach (var childPropertyName in childPropertyNames)
                {
                    // todo - worry about recursive notifications?
                    NotifyPropertyChanged(childPropertyName);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected T Clone<T>()
        {
            var asObject = this.MemberwiseClone();
            var asViewModel = (ViewModel)asObject;
            asViewModel.propertyDictionary = new Dictionary<string, object>(this.propertyDictionary);
            foreach(var kvp in propertyDictionary)
            {
                asViewModel.propertyDictionary[kvp.Key] = kvp.Value;
            }
            var asT = (T)asObject;

            return asT;
        }

    }
}
