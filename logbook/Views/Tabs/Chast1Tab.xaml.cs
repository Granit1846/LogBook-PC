using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using logbook.Helpers;

namespace logbook.Views.Tabs
{
    public partial class Chast1Tab : UserControl
    {
        public Chast1Tab()
        {
            InitializeComponent();

            Loaded += Chast1Tab_Loaded;
            Unloaded += Chast1Tab_Unloaded;
        }

        private void Chast1Tab_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyLocalization();
            LocalizationManager.LanguageChanged += ApplyLocalization;

            // Синхронизируем положение шапки с таблицей
            HeaderScroll.ScrollToHorizontalOffset(DataScroll.HorizontalOffset);
        }

        private void Chast1Tab_Unloaded(object sender, RoutedEventArgs e)
        {
            LocalizationManager.LanguageChanged -= ApplyLocalization;
        }

        private void ApplyLocalization()
        {
            // Многострочные заголовки шапки
            Run_Ground1.Text = LocalizationManager.Get("Header_Chast1_Ground1");
            Run_Ground2.Text = LocalizationManager.Get("Header_Chast1_Ground2");
            Run_Ground3.Text = LocalizationManager.Get("Header_Chast1_Ground3");

            Run_Duration1.Text = LocalizationManager.Get("Header_Chast1_Duration1");
            Run_Duration2.Text = LocalizationManager.Get("Header_Chast1_Duration2");

            Run_AfterRepair1.Text = LocalizationManager.Get("Header_Chast1_AfterRepair1");
            Run_AfterRepair2.Text = LocalizationManager.Get("Header_Chast1_AfterRepair2");

            Run_SinceOp1.Text = LocalizationManager.Get("Header_Chast1_SinceOp1");
            Run_SinceOp2.Text = LocalizationManager.Get("Header_Chast1_SinceOp2");
        }

        private void DataScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange != 0)
            {
                HeaderScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }

        private void HeaderScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Ctrl + колесо: ZoomBehavior
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                return;

            // Shift + колесо: горизонтальный скролл
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (e.Delta < 0)
                {
                    for (int i = 0; i < 3; i++) DataScroll.LineRight();
                }
                else
                {
                    for (int i = 0; i < 3; i++) DataScroll.LineLeft();
                }
                e.Handled = true;
                return;
            }

            // Обычное колесо над шапкой: скроллим строки таблицы по вертикали
            if (e.Delta < 0)
            {
                for (int i = 0; i < 3; i++) DataScroll.LineDown();
            }
            else
            {
                for (int i = 0; i < 3; i++) DataScroll.LineUp();
            }
            e.Handled = true;
        }

        private void DataScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Ctrl + колесо: ZoomBehavior
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                return;

            // Shift + колесо: горизонтальный скролл
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (e.Delta < 0)
                {
                    for (int i = 0; i < 3; i++) DataScroll.LineRight();
                }
                else
                {
                    for (int i = 0; i < 3; i++) DataScroll.LineLeft();
                }
                e.Handled = true;
            }
        }
    }
}