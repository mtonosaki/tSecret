// (c) 2019 Manabu Tonosaki
// Licensed under the MIT license.

using System;
using Tono;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace tSecretUwp
{
    public class Boolean2VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return DbUtil.ToBoolean(value, true) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is Visibility vis ? vis == Visibility.Visible : (object)Visibility.Visible;
        }
    }
}
