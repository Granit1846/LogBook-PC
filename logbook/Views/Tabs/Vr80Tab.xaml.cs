using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using logbook.Helpers;

namespace logbook.Views.Tabs
{
    public partial class Vr80Tab : UserControl
    {
        public Vr80Tab()
        {
            InitializeComponent();

            Loaded += Vr80Tab_Loaded;
            Unloaded += Vr80Tab_Unloaded;
        }

        private void Vr80Tab_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyLocalization();
            LocalizationManager.LanguageChanged += ApplyLocalization;

            // Синхронизируем положение шапки с таблицей
            Vr80HeaderScroll.ScrollToHorizontalOffset(Vr80DataScroll.HorizontalOffset);
        }

        private void Vr80Tab_Unloaded(object sender, RoutedEventArgs e)
        {
            LocalizationManager.LanguageChanged -= ApplyLocalization;
        }

        private void ApplyLocalization()
        {
            // Продолжительность работы ГР на земле
            Run_Vr80_GroundWork1.Text = LocalizationManager.Get("Header_Vr80_GroundWork1");
            Run_Vr80_GroundWork2.Text = LocalizationManager.Get("Header_Vr80_GroundWork2");

            // Продолжительность работы ГР в полете
            Run_Vr80_AirWork1.Text = LocalizationManager.Get("Header_Vr80_AirWork1");
            Run_Vr80_AirWork2.Text = LocalizationManager.Get("Header_Vr80_AirWork2");

            // Сумма работы ГР от двигателей
            Run_Vr80_TotalEngine1.Text = LocalizationManager.Get("Header_Vr80_TotalEngine1");
            Run_Vr80_TotalEngine2.Text = LocalizationManager.Get("Header_Vr80_TotalEngine2");
            Run_Vr80_TotalEngine3.Text = LocalizationManager.Get("Header_Vr80_TotalEngine3");

            // Сумма работы ГР от турбопривода
            Run_Vr80_TotalTurbine1.Text = LocalizationManager.Get("Header_Vr80_TotalTurbine1");
            Run_Vr80_TotalTurbine2.Text = LocalizationManager.Get("Header_Vr80_TotalTurbine2");
            Run_Vr80_TotalTurbine3.Text = LocalizationManager.Get("Header_Vr80_TotalTurbine3");
        }

        private void Vr80DataScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange != 0)
            {
                Vr80HeaderScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }

        private void Vr80HeaderScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Ctrl + колесо: ZoomBehavior
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                return;

            // Shift + колесо: горизонтальный скролл
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (e.Delta < 0)
                {
                    for (int i = 0; i < 3; i++) Vr80DataScroll.LineRight();
                }
                else
                {
                    for (int i = 0; i < 3; i++) Vr80DataScroll.LineLeft();
                }
                e.Handled = true;
                return;
            }

            // Обычное колесо над шапкой: скроллим строки таблицы по вертикали
            if (e.Delta < 0)
            {
                for (int i = 0; i < 3; i++) Vr80DataScroll.LineDown();
            }
            else
            {
                for (int i = 0; i < 3; i++) Vr80DataScroll.LineUp();
            }
            e.Handled = true;
        }

        private void Vr80DataScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Ctrl + колесо: ZoomBehavior
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                return;

            // Shift + колесо: горизонтальный скролл
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (e.Delta < 0)
                {
                    for (int i = 0; i < 3; i++) Vr80DataScroll.LineRight();
                }
                else
                {
                    for (int i = 0; i < 3; i++) Vr80DataScroll.LineLeft();
                }
                e.Handled = true;
            }
        }
    }
}