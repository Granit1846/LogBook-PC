using System.Windows;
using System.Windows.Media.Imaging;
using logbook.Helpers;

namespace logbook.Views
{
    /// <summary>
    /// Базовое окно с автоматическим тёмным заголовком и иконкой приложения.
    /// </summary>
    public class ThemedWindow : Window
    {
        public ThemedWindow()
        {
            // Иконка приложения
            try
            {
                Icon = new BitmapImage(new System.Uri(
                    "pack://application:,,,/Resources/Icons/logbook.ico",
                    System.UriKind.Absolute));
            }
            catch { }

            Loaded += (s, e) => DarkTitleBarHelper.Apply(this);
        }
    }
}