using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls
{
    public class TextBoxDisplayLogic
    {
        TextBox mAssociatedTextBox;
        IDataUi mContainer;

        public bool HasUserChangedAnything { get; set; }
        public string TextAtStartOfEditing { get; set; }
        public InstanceMember InstanceMember { get; set; }
        public Type InstancePropertyType { get; set; }

        public TextBoxDisplayLogic(IDataUi container, TextBox textBox)
        {
            mAssociatedTextBox = textBox;
            mContainer = container;
            mAssociatedTextBox.GotFocus += HandleTextBoxGotFocus;
            mAssociatedTextBox.PreviewKeyDown += HandlePreviewKeydown;
            mAssociatedTextBox.TextChanged += HandleTextChanged;
        }

        private void HandleTextChanged(object sender, TextChangedEventArgs e)
        {
            HasUserChangedAnything = true;
        }

        private void HandlePreviewKeydown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                TryApplyToInstance();
                TextAtStartOfEditing = mAssociatedTextBox.Text;

            }
            else if (e.Key == Key.Escape)
            {
                HasUserChangedAnything = false;
                mAssociatedTextBox.Text = TextAtStartOfEditing;
            }
        }

        void HandleTextBoxGotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            TextAtStartOfEditing = mAssociatedTextBox.Text;
            HasUserChangedAnything = false;
        }

        public ApplyValueResult TryApplyToInstance()
        {
            object newValue;

            if (HasUserChangedAnything)
            {
                if (mContainer.TryGetValueOnUi(out newValue) == ApplyValueResult.Success)
                {
                    if (InstanceMember.BeforeSetByUi != null)
                    {
                        InstanceMember.CallBeforeSetByUi(mContainer);
                    }

                    // Hold on, the Before set may have actually changed the value, so we should get the value again.
                    mContainer.TryGetValueOnUi(out newValue);


                    return mContainer.TrySetValueOnInstance(newValue);
                }
            }
            return ApplyValueResult.Success;
        }

        public string ConvertStringToUsableValue()
        {

            string text = mAssociatedTextBox.Text;

            if (InstancePropertyType.Name == "Vector3" ||
                InstancePropertyType.Name == "Vector2")
            {
                text = text.Replace("{", "").Replace("}", "").Replace("X:", "").Replace("Y:", "").Replace("Z:", "").Replace(" ", ",");

            }
            if (InstancePropertyType.Name == "Color")
            {
                // I think this expects byte values, so we gotta make sure it's not giving us floats
                text = text.Replace("{", "").Replace("}", "").Replace("A:", "").Replace("R:", "").Replace("G:", "").Replace("B:", "").Replace(" ", ",");

            }

            return text;
            
        }


        private bool GetIfConverterCanConvert(TypeConverter converter)
        {
            string converterTypeName = converter.GetType().Name;
            if (converterTypeName == "MatrixConverter" ||
                converterTypeName == "CollectionConverter"
                )
            {
                return false;
            }
            return true;
        }

        public ApplyValueResult TryGetValueOnUi(out object value)
        {
            var result = ApplyValueResult.UnknownError;



            value = null;
            if (!mContainer.HasEnoughInformationToWork() || InstancePropertyType == null)
            {
                result = ApplyValueResult.NotEnoughInformation;
            }
            else
            {
                try
                {
                    var usableString = ConvertStringToUsableValue();

                    var converter = TypeDescriptor.GetConverter(InstancePropertyType);

                    bool canConverterConvert = GetIfConverterCanConvert(converter);

                    if (canConverterConvert)
                    {
                        // The user may have put in a bad value
                        try
                        {
                            value = converter.ConvertFromInvariantString(usableString);
                            result = ApplyValueResult.Success;
                        }
                        catch (FormatException)
                        {
                            result = ApplyValueResult.InvalidSyntax;
                        }
                        catch
                        {
                            result = ApplyValueResult.InvalidSyntax;
                        }
                    }
                    else
                    {
                        result = ApplyValueResult.NotSupported;
                    }
                }
                catch
                {
                    result = ApplyValueResult.UnknownError;
                }
            }

            return result;
        }

        public void RefreshDisplay()
        {
            if (mContainer.HasEnoughInformationToWork())
            {
                Type type = mContainer.GetPropertyType();

                InstancePropertyType = type;
            }

            object valueOnInstance;
            bool successfulGet = mContainer.TryGetValueOnInstance(out valueOnInstance);
            if (successfulGet)
            {
                if (valueOnInstance != null)
                {
                    mContainer.TrySetValueOnUi(valueOnInstance);
                }
                else
                {
                    mAssociatedTextBox.Text = null;
                }
            }


            bool isDefault = InstanceMember.IsDefault;
            if (isDefault)
            {
                mAssociatedTextBox.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                mAssociatedTextBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }
    }
}
