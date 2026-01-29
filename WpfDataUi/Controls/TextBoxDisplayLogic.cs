using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls
{
    public class TextBoxDisplayLogic
    {
        #region Properties

        TextBox mAssociatedTextBox;
        IDataUi mContainer;

        public bool HasUserChangedAnything { get; set; }
        public string TextAtStartOfEditing { get; set; }
        public InstanceMember? InstanceMember { get; set; }
        public Type InstancePropertyType { get; set; }

        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }

        public bool HandlesEnter { get; set; } = true;

        public bool IsNumeric
        {
            get
            {
                var type = InstanceMember?.PropertyType;

                return
                    type == typeof(float) ||
                    type == typeof(double) ||
                    type == typeof(decimal) ||
                    type == typeof(int) ||
                    type == typeof(long) ||
                    type == typeof(byte) ||
                    type == typeof(short) ||


                    type == typeof(float?) ||
                    type == typeof(double?) ||
                    type == typeof(decimal?) ||
                    type == typeof(int?) ||
                    type == typeof(long?) ||
                    type == typeof(byte?) ||
                    type == typeof(short?)
                    ;
            }
        }


        #endregion

        public TextBoxDisplayLogic(IDataUi container, TextBox textBox)
        {
            mAssociatedTextBox = textBox;
            mContainer = container;
            mAssociatedTextBox.GotFocus += HandleTextBoxGotFocus;
            mAssociatedTextBox.PreviewKeyDown += HandlePreviewKeydown;
            mAssociatedTextBox.TextChanged += HandleTextChanged;
        }

        private void HandleTextChanged(object? sender, TextChangedEventArgs e)
        {
            HasUserChangedAnything = true;
        }

        public void ClampTextBoxValuesToMinMax()
        {
            bool shouldClamp = MinValue.HasValue || MaxValue.HasValue;

            if (shouldClamp)
            {
                decimal parsedDecimal;

                if (decimal.TryParse(mAssociatedTextBox.Text, out parsedDecimal))
                {
                    if (MinValue.HasValue && parsedDecimal < MinValue)
                    {
                        mAssociatedTextBox.Text = MinValue.ToString();
                    }
                    if (MaxValue.HasValue && parsedDecimal > MaxValue)
                    {
                        mAssociatedTextBox.Text = MaxValue.ToString();
                    }
                }
            }
        }

        private void HandlePreviewKeydown(object? sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (HandlesEnter))
            {
                e.Handled = true;

                ClampTextBoxValuesToMinMax();

                var result = TryApplyToInstance();

                if (result == ApplyValueResult.Success)
                {
                    TextAtStartOfEditing = mAssociatedTextBox.Text;
                    mContainer.Refresh(forceRefreshEvenIfFocused: true);
                }
                else
                {
                    mAssociatedTextBox.Text = TextAtStartOfEditing;
                }

            }
            else if (e.Key == Key.Escape)
            {
                HasUserChangedAnything = false;
                mAssociatedTextBox.Text = TextAtStartOfEditing;
            }
        }

        void HandleTextBoxGotFocus(object? sender, System.Windows.RoutedEventArgs e)
        {
            TextAtStartOfEditing = mAssociatedTextBox.Text;

            mAssociatedTextBox.SelectAll();

            HasUserChangedAnything = false;
        }

        public bool IsInApplicationToInstance { get; private set; } = false;
        public ApplyValueResult TryApplyToInstance(SetPropertyCommitType commitType = SetPropertyCommitType.Full)
        {
            IsInApplicationToInstance = true;
            try
            {

                object newValue;

                if (HasUserChangedAnything || commitType == SetPropertyCommitType.Full)
                {
                    var result = mContainer.TryGetValueOnUi(out newValue);

                    if (result == ApplyValueResult.Success)
                    {
                        if (InstanceMember?.BeforeSetByUi != null)
                        {
                            InstanceMember.CallBeforeSetByUi(mContainer);
                        }

                        // Hold on, the Before set may have actually changed the value, so we should get the value again.
                        mContainer.TryGetValueOnUi(out newValue);

                        if (newValue is string)
                        {
                            newValue = (newValue as string).Replace("\r", "");
                        }
                        // get rid of \r
                        return mContainer.TrySetValueOnInstance(newValue, commitType);
                    }
                    else
                    {
                        InstanceMember?.SetValueError?.Invoke(mAssociatedTextBox.Text);

                        return result;
                    }
                }
                return ApplyValueResult.Success;
            }
            finally
            {
                IsInApplicationToInstance = false;
            }
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

        public string ConvertNumberToString(object value, int? numberOfDecimals = null)
        {
            string text = value?.ToString();

            if (value is float)
            {

                // I came to this method through a lot of trial and error.
                // Initially I just used ToString, but that introduces exponential
                // notation, which is confusing and weird for tools to display.
                // I then tried ToString("f0"), which gets rid of exponents, but also
                // truncates at 0 decimals.
                // So I did ToString("#.##############") which will display as many decimals
                // as it can, but this takes numbers like 21.2 and instead shows 21.199997
                // or something. I really want ToString to do ToString unless there is an exponent, and if so
                // then let's fall back to a version that does not show exponents. Which we do depends on if
                // the shown exponent is positive (really large number, abs greater than 1) or really small
                float floatValue = (float)value;
                text = floatValue.ToString();
                if (text.Contains("E"))
                {
                    if (Math.Abs(floatValue) > 1)
                    {
                        // truncating decimals:
                        text = (floatValue).ToString("f0");
                    }
                    else if (numberOfDecimals != null)
                    {
                        text = (floatValue).ToString($"f{numberOfDecimals}");
                    }
                    else
                    {
                        text = (floatValue).ToString("0.################################");
                    }
                }
                else if (numberOfDecimals != null)
                {
                    text = (floatValue).ToString($"f{numberOfDecimals}");
                }
            }
            if (value is double)
            {
                double doubleValue = (double)value;
                text = doubleValue.ToString();
                if (text.Contains("e"))
                {
                    if (Math.Abs(doubleValue) > 1)
                    {
                        // truncating decimals:
                        text = (doubleValue).ToString("f0");
                    }
                    else if (numberOfDecimals != null)
                    {
                        text = (doubleValue).ToString($"f{numberOfDecimals}");
                    }
                    else
                    {
                        text = (doubleValue).ToString("#.################################");
                    }
                }
                else if (numberOfDecimals != null)
                {
                    text = (doubleValue).ToString($"f{numberOfDecimals}");
                }
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
                            if (string.IsNullOrEmpty(usableString))
                            {
                                if (InstancePropertyType == typeof(float))
                                {
                                    value = 0.0f;
                                    result = ApplyValueResult.Success;
                                }
                                else if (InstancePropertyType == typeof(int))
                                {
                                    value = 0;
                                    result = ApplyValueResult.Success;
                                }
                                else if (InstancePropertyType == typeof(double))
                                {
                                    value = 0.0;
                                    result = ApplyValueResult.Success;
                                }
                                else if (InstancePropertyType == typeof(long))
                                {
                                    value = (long)0;
                                    result = ApplyValueResult.Success;
                                }
                                else if (InstancePropertyType == typeof(byte))
                                {
                                    value = (byte)0;
                                    result = ApplyValueResult.Success;
                                }
                            }

                            if (result != ApplyValueResult.Success)
                            {
                                // This used to convert from invariant string, but we want to use commas if the native 
                                // computer settings use commas
                                try
                                {
                                    if(usableString == "-0" && (InstancePropertyType == typeof(float) || InstancePropertyType == typeof(double)))
                                    {
                                        if(InstancePropertyType == typeof(float))
                                        {
                                            value = 0f;
                                        }
                                        else
                                        {
                                            value = 0.0;
                                        }
                                    }
                                    else
                                    {
                                        value = converter.ConvertFromString(usableString);
                                    }
                                    result = ApplyValueResult.Success;
                                }
                                catch (NotSupportedException)
                                {
                                    // if we got here, then suppress the error if we are already working with a string:
                                    value = usableString;
                                    result = ApplyValueResult.Success;
                                }
                            }
                        }
                        catch (FormatException)
                        {
                            result = ApplyValueResult.InvalidSyntax;
                        }
                        catch (Exception e)
                        {
                            var wasMathOperation = false;

                            var succeeded = TryConvertingToIntThroughDouble(ref value, ref result, usableString);

                            if(!succeeded)
                            {
                                if (e.InnerException is FormatException)
                                {
                                    try
                                    {
                                        var computedValue = TryHandleMathOperation(usableString, InstancePropertyType);
                                        wasMathOperation = computedValue != null;
                                        value = computedValue;
                                    }
                                    catch
                                    {
                                        result = ApplyValueResult.InvalidSyntax;

                                        // It's possible this is an integer value that is either too long, or that has a decimal point. Try that:
                                        TryConvertingToIntThroughDouble(ref value, ref result, usableString);

                                    }
                                }
                                if (wasMathOperation)
                                {
                                    result = ApplyValueResult.Success;
                                }
                                else
                                {
                                    result = ApplyValueResult.InvalidSyntax;
                                }
                            }
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

        private bool TryConvertingToIntThroughDouble(ref object value, ref ApplyValueResult result, string usableString)
        {
            bool succeeded = false;
            if (InstancePropertyType == typeof(int))
            {
                // see if a double can handle this:
                var doubleConverter = TypeDescriptor.GetConverter(typeof(double));

                try
                {
                    var doubleValue = (double)doubleConverter.ConvertFromString(usableString);

                    if (doubleValue > int.MaxValue)
                    {
                        value = int.MaxValue;
                        result = ApplyValueResult.Success;
                    }
                    else if (doubleValue < int.MinValue)
                    {
                        value = int.MinValue;
                        result = ApplyValueResult.Success;
                    }
                    else
                    {
                        value = (int)doubleValue;
                        result = ApplyValueResult.Success;
                    }
                    succeeded = true;
                }
                catch
                {
                    // oh well we tried
                }
            }

            return succeeded;
        }

        public static object TryHandleMathOperation(string usableString, Type instancePropertyType)
        {
            if (instancePropertyType == typeof(float) ||
                instancePropertyType == typeof(float?) ||
                instancePropertyType == typeof(int?) ||
                instancePropertyType == typeof(int) ||
                instancePropertyType == typeof(double) ||
                instancePropertyType == typeof(double?) ||
                instancePropertyType == typeof(decimal) ||
                instancePropertyType == typeof(decimal?)
                )
            {
                var result = new DataTable().Compute(usableString, null);

                if (result is float || result is int || result is decimal || result is double)
                {
                    var converter = TypeDescriptor.GetConverter(instancePropertyType);

                    return converter.ConvertFrom(result.ToString());
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public static SolidColorBrush DefaultValueBackground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 255, 180)) { Opacity = 0.5 };
        public static SolidColorBrush IndeterminateValueBackground = new SolidColorBrush(System.Windows.Media.Colors.LightGray);
        public static SolidColorBrush CustomValueBackground = System.Windows.Media.Brushes.White;


        public object GetValueInDirection(int direction, object value)
        {
            if (value is int asInt)
            {
                return asInt + direction;
            }
            else if (value is long asLong)
            {
                return asLong + direction;
            }
            else if (value is float asFloat)
            {
                return asFloat + direction;
            }
            else if (value is double asDouble)
            {
                return asDouble + direction;
            }
            else if (value is decimal asDecimal)
            {
                return asDecimal + direction;
            }
            else
            {
                return value;
            }
        }

        public object GetValueInDirection(double direction, object value)
        {
            if (value is int asInt)
            {
                return (int)(asInt + direction);
            }
            else if (value is long asLong)
            {
                return (long)(asLong + direction);
            }
            else if (value is float asFloat)
            {
                return (float)(asFloat + direction);
            }
            else if (value is double asDouble)
            {
                return asDouble + direction;
            }
            else if (value is decimal asDecimal)
            {
                return (double)asDecimal + direction;
            }
            else
            {
                return value;
            }

        }

        public void RefreshDisplay(out object valueOnInstance)
        {
            if (mContainer.HasEnoughInformationToWork())
            {
                Type type = mContainer.GetPropertyType();

                InstancePropertyType = type;
            }

            bool successfulGet = mContainer.TryGetValueOnInstance(out valueOnInstance);
            if (successfulGet)
            {
                mContainer.TrySetValueOnUi(valueOnInstance);
            }

            RefreshBackgroundColor();
        }

        public void RefreshBackgroundColor()
        {
            mAssociatedTextBox.Dispatcher.BeginInvoke(() =>
            {
                
                if (DataUiGrid.GetOverridesIsDefaultStyling(mAssociatedTextBox))
                {
                    return;
                }

                if (InstanceMember.IsDefault)
                {
                    mAssociatedTextBox.Background = DefaultValueBackground;
                }
                else if (InstanceMember.IsIndeterminate)
                {
                    mAssociatedTextBox.Background = IndeterminateValueBackground;
                }
                else
                {
                    if (mAssociatedTextBox.TryFindResource("Frb.Brushes.Field.Background") != null)
                    {
                        mAssociatedTextBox.SetResourceReference(TextBox.BackgroundProperty,
                            "Frb.Brushes.Field.Background");
                    }
                    else
                    {
                        mAssociatedTextBox.ClearValue(TextBox.BackgroundProperty);
                    }
                }
            });

        }
    }
}
