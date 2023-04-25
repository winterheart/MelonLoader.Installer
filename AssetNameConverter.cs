using Avalonia.Data;
using Avalonia.Data.Converters;
using System.Globalization;
using System;

namespace MelonLoader
{
    public class AssetNameConverter : IValueConverter
    {
        public static readonly AssetNameConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string sourceText && targetType.IsAssignableTo(typeof(string)))
            {
                return sourceText switch
                {
                    "MelonLoader.x64.zip" => "Windows x64",
                    "MelonLoader.x86.zip" => "Windows x86",
                    "MelonLoader.Linux.x64.zip" => "Linux x64",
                    _ => sourceText,
                };
            }
            // converter used for the wrong type
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
