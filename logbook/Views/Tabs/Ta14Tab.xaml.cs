using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using logbook.Helpers;

namespace logbook.Views.Tabs
{
    public partial class Ta14Tab : UserControl
    {
        public Ta14Tab()
        {
            InitializeComponent();

            Loaded += Ta14Tab_Loaded;
            Unloaded += Ta14Tab_Unloaded;
        }

        private void Ta14Tab_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyLocalization();
            LocalizationManager.LanguageChanged += ApplyLocalization;

            // Синхронизируем положение шапки с таблицей
            Ta14HeaderScroll.ScrollToHorizontalOffset(Ta14DataScroll.HorizontalOffset);
        }

        private void Ta14Tab_Unloaded(object sender, RoutedEventArgs e)
        {
            LocalizationManager.LanguageChanged -= ApplyLocalization;
        }

        private void ApplyLocalization()
        {
            // Колонка 2: Показания счётчиков
            Run_Ta14_Counter1.Text = LocalizationManager.Get("Header_Ta14_Counter1");
            Run_Ta14_Counter2.Text = LocalizationManager.Get("Header_Ta14_Counter2");
            Run_Ta14_Counter3.Text = LocalizationManager.Get("Header_Ta14_Counter3");

            // Колонка 3: Наработка на земле
            Run_Ta14_GroundWork1.Text = LocalizationManager.Get("Header_Ta14_GroundWork1");
            Run_Ta14_GroundWork2.Text = LocalizationManager.Get("Header_Ta14_GroundWork2");
            Run_Ta14_GroundWork3.Text = LocalizationManager.Get("Header_Ta14_GroundWork3");

            // Колонка 4: Наработка в полёте
            Run_Ta14_AirWork1.Text = LocalizationManager.Get("Header_Ta14_AirWork1");
            Run_Ta14_AirWork2.Text = LocalizationManager.Get("Header_Ta14_AirWork2");
            Run_Ta14_AirWork3.Text = LocalizationManager.Get("Header_Ta14_AirWork3");

            // Колонка 5: Суммарная наработка
            Run_Ta14_Total1.Text = LocalizationManager.Get("Header_Ta14_Total1");
            Run_Ta14_Total2.Text = LocalizationManager.Get("Header_Ta14_Total2");
            Run_Ta14_Total3.Text = LocalizationManager.Get("Header_Ta14_Total3");

            // Колонки 6-9: Суммарная наработка (верх)
            Run_Ta14_Grand1.Text = LocalizationManager.Get("Header_Ta14_Grand1");
            Run_Ta14_Grand2.Text = LocalizationManager.Get("Header_Ta14_Grand2");

            // Колонки 6-7: После капитального ремонта
            Run_Ta14_AfterCap1.Text = LocalizationManager.Get("Header_Ta14_AfterCap1");
            Run_Ta14_AfterCap2.Text = LocalizationManager.Get("Header_Ta14_AfterCap2");

            // Колонки 8-9: С начала эксплуатации
            Run_Ta14_Since1.Text = LocalizationManager.Get("Header_Ta14_Since1");
            Run_Ta14_Since2.Text = LocalizationManager.Get("Header_Ta14_Since2");
        }

        private void Ta14DataScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange != 0)
            {
                Ta14HeaderScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }

        private void Ta14HeaderScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Ctrl + колесо: ZoomBehavior
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                return;

            // Shift + колесо: горизонтальный скролл
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (e.Delta < 0)
                {
                    for (int i = 0; i < 3; i++) Ta14DataScroll.LineRight();
                }
                else
                {
                    for (int i = 0; i < 3; i++) Ta14DataScroll.LineLeft();
                }
                e.Handled = true;
                return;
            }

            // Обычное колесо над шапкой: скроллим строки таблицы по вертикали
            if (e.Delta < 0)
            {
                for (int i = 0; i < 3; i++) Ta14DataScroll.LineDown();
            }
            else
            {
                for (int i = 0; i < 3; i++) Ta14DataScroll.LineUp();
            }
            e.Handled = true;
        }

        private void Ta14DataScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Ctrl + колесо: ZoomBehavior
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                return;

            // Shift + колесо: горизонтальный скролл
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (e.Delta < 0)
                {
                    for (int i = 0; i < 3; i++) Ta14DataScroll.LineRight();
                }
                else
                {
                    for (int i = 0; i < 3; i++) Ta14DataScroll.LineLeft();
                }
                e.Handled = true;
            }
        }
    }
}