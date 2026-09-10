using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace PHelper
{
    public enum TargetMode
    {
        Silent,
        Balanced,
        Turbo
    }

    public class AppProfile
    {
        public string ProcessName { get; set; } = string.Empty;
        public TargetMode Mode { get; set; }
        
        public string FullPath { get; set; } = string.Empty; 
    }

    public class IconConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? path = value as string;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

            try
            {
                using (Icon? sysicon = Icon.ExtractAssociatedIcon(path))
                {
                    if (sysicon != null)
                    {
                        return Imaging.CreateBitmapSourceFromHIcon(
                            sysicon.Handle,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                    }
                }
            }
            catch { }
            
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}