using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using logbook.Helpers;

namespace logbook.Views.Tabs
{
    public partial class Vk250002Tab : UserControl
    {
        public Vk250002Tab()
        {
            InitializeComponent();

            Loaded += Vk250002Tab_Loaded;
            Unloaded += Vk250002Tab_Unloaded;
        }

        private void Vk250002Tab_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyLocalization();
            LocalizationManager.LanguageChanged += ApplyLocalization;

            // Синхронизируем положение шапки с таблицей
            Vk250002HeaderScroll.ScrollToHorizontalOffset(Vk250002DataScroll.HorizontalOffset);
        }

        private void Vk250002Tab_Unloaded(object sender, RoutedEventArgs e)
        {
            LocalizationManager.LanguageChanged -= ApplyLocalization;
        }

        private void ApplyLocalization()
        {
            // Эквивалентная наработка (G)
            Run_Vk_Equivalent1.Text = LocalizationManager.Get("Header_Vk_Equivalent1");
            Run_Vk_Equivalent2.Text = LocalizationManager.Get("Header_Vk_Equivalent2");
            Run_Vk_Equivalent3.Text = LocalizationManager.Get("Header_Vk_Equivalent3");

            // Сумарное время работы (столбец 3)
            Run_Vk_Total1.Text = LocalizationManager.Get("Header_Vk_Total1");
            Run_Vk_Total2.Text = LocalizationManager.Get("Header_Vk_Total2");
            Run_Vk_Total3.Text = LocalizationManager.Get("Header_Vk_Total3");

            // Суммарная (общая) наработка (А)
            Run_Vk_TotalA1.Text = LocalizationManager.Get("Header_Vk_TotalA1");
            Run_Vk_TotalA2.Text = LocalizationManager.Get("Header_Vk_TotalA2");
            Run_Vk_TotalA3.Text = LocalizationManager.Get("Header_Vk_TotalA3");

            // С начала эксплуатации (столбец 5)
            Run_Vk_SinceOp1.Text = LocalizationManager.Get("Header_Vk_SinceOp1");
            Run_Vk_SinceOp2.Text = LocalizationManager.Get("Header_Vk_SinceOp2");
            Run_Vk_SinceOp3.Text = LocalizationManager.Get("Header_Vk_SinceOp3");

            // После последнего кап. ремонта (столбец 6)
            Run_Vk_AfterCap1.Text = LocalizationManager.Get("Header_Vk_AfterCap1");
            Run_Vk_AfterCap2.Text = LocalizationManager.Get("Header_Vk_AfterCap2");

            // Наработка на взлетном режиме (В)
            Run_Vk_TakeoffMode1.Text = LocalizationManager.Get("Header_Vk_TakeoffMode1");
            Run_Vk_TakeoffMode2.Text = LocalizationManager.Get("Header_Vk_TakeoffMode2");
            Run_Vk_TakeoffMode3.Text = LocalizationManager.Get("Header_Vk_TakeoffMode3");

            // От 6 до 15 мин
            Run_Vk_From6To15_1.Text = LocalizationManager.Get("Header_Vk_From6To15_1");
            Run_Vk_From6To15_2.Text = LocalizationManager.Get("Header_Vk_From6To15_2");
            Run_Vk_From6To15_3.Text = LocalizationManager.Get("Header_Vk_From6To15_3");

            // Сумарное время работы (столбец 8)
            Run_Vk_Total2_1.Text = LocalizationManager.Get("Header_Vk_Total2_1");
            Run_Vk_Total2_2.Text = LocalizationManager.Get("Header_Vk_Total2_2");
            Run_Vk_Total2_3.Text = LocalizationManager.Get("Header_Vk_Total2_3");

            // Наработка на максимально продолжительном режиме
            Run_Vk_MaxMode1.Text = LocalizationManager.Get("Header_Vk_MaxMode1");
            Run_Vk_MaxMode2.Text = LocalizationManager.Get("Header_Vk_MaxMode2");
            Run_Vk_MaxMode3.Text = LocalizationManager.Get("Header_Vk_MaxMode3");

            // Наработка при повышенной температуре газов (D)
            Run_Vk_HighTemp1.Text = LocalizationManager.Get("Header_Vk_HighTemp1");
            Run_Vk_HighTemp2.Text = LocalizationManager.Get("Header_Vk_HighTemp2");
            Run_Vk_HighTemp3.Text = LocalizationManager.Get("Header_Vk_HighTemp3");

            // Сумарное время работы (столбец 13)
            Run_Vk_Total3_1.Text = LocalizationManager.Get("Header_Vk_Total3_1");
            Run_Vk_Total3_2.Text = LocalizationManager.Get("Header_Vk_Total3_2");
            Run_Vk_Total3_3.Text = LocalizationManager.Get("Header_Vk_Total3_3");

            // Наработка при отключенном БАРК (Е)
            Run_Vk_BarkOff1.Text = LocalizationManager.Get("Header_Vk_BarkOff1");
            Run_Vk_BarkOff2.Text = LocalizationManager.Get("Header_Vk_BarkOff2");
            Run_Vk_BarkOff3.Text = LocalizationManager.Get("Header_Vk_BarkOff3");

            // Сумарное время работы (столбец 15)
            Run_Vk_Total4_1.Text = LocalizationManager.Get("Header_Vk_Total4_1");
            Run_Vk_Total4_2.Text = LocalizationManager.Get("Header_Vk_Total4_2");
            Run_Vk_Total4_3.Text = LocalizationManager.Get("Header_Vk_Total4_3");

            // Циклическая наработка (Н)
            Run_Vk_Cyclic1.Text = LocalizationManager.Get("Header_Vk_Cyclic1");
            Run_Vk_Cyclic2.Text = LocalizationManager.Get("Header_Vk_Cyclic2");
            Run_Vk_Cyclic3.Text = LocalizationManager.Get("Header_Vk_Cyclic3");

            // Циклическая — с начала эксплуатации
            Run_Vk_CyclicSince1.Text = LocalizationManager.Get("Header_Vk_CyclicSince1");
            Run_Vk_CyclicSince2.Text = LocalizationManager.Get("Header_Vk_CyclicSince2");
            Run_Vk_CyclicSince3.Text = LocalizationManager.Get("Header_Vk_CyclicSince3");

            // После последнего кап. ремонта (столбец 18)
            Run_Vk_AfterCap2_1.Text = LocalizationManager.Get("Header_Vk_AfterCap2_1");
            Run_Vk_AfterCap2_2.Text = LocalizationManager.Get("Header_Vk_AfterCap2_2");
        }

        private void Vk250002DataScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange != 0)
            {
                Vk250002HeaderScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }

        private void Vk250002HeaderScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Ctrl + колесо: ZoomBehavior
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                return;

            // Shift + колесо: горизонтальный скролл
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (e.Delta < 0)
                {
                    for (int i = 0; i < 3; i++) Vk250002DataScroll.LineRight();
                }
                else
                {
                    for (int i = 0; i < 3; i++) Vk250002DataScroll.LineLeft();
                }
                e.Handled = true;
                return;
            }

            // Обычное колесо над шапкой: скроллим строки таблицы по вертикали
            if (e.Delta < 0)
            {
                for (int i = 0; i < 3; i++) Vk250002DataScroll.LineDown();
            }
            else
            {
                for (int i = 0; i < 3; i++) Vk250002DataScroll.LineUp();
            }
            e.Handled = true;
        }

        private void Vk250002DataScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Ctrl + колесо: ZoomBehavior
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                return;

            // Shift + колесо: горизонтальный скролл
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (e.Delta < 0)
                {
                    for (int i = 0; i < 3; i++) Vk250002DataScroll.LineRight();
                }
                else
                {
                    for (int i = 0; i < 3; i++) Vk250002DataScroll.LineLeft();
                }
                e.Handled = true;
            }
        }
    }
}