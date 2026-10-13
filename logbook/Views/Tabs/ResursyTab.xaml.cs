using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using logbook.Helpers;

namespace logbook.Views.Tabs
{
    public partial class ResursyTab : UserControl
    {
        public ResursyTab()
        {
            InitializeComponent();

            Loaded += ResursyTab_Loaded;
            Unloaded += ResursyTab_Unloaded;
        }

        private void ResursyTab_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyLocalization();
            LocalizationManager.LanguageChanged += ApplyLocalization;

            // Синхронизируем положение шапок с таблицами
            AggregatesHeaderScroll.ScrollToHorizontalOffset(AggregatesDataScroll.HorizontalOffset);
            OilsHeaderScroll.ScrollToHorizontalOffset(OilsDataScroll.HorizontalOffset);

            // Обновить таблицу «Агрегаты на борту» при загрузке
            if (DataContext is logbook.ViewModels.MainViewModel vm)
            {
                vm.RefreshAggregateRows();
            }
        }

        private void ResursyTab_Unloaded(object sender, RoutedEventArgs e)
        {
            LocalizationManager.LanguageChanged -= ApplyLocalization;
        }

        private void ApplyLocalization()
        {
            // ===== Агрегаты =====
            Run_Resursy_ItemName1.Text = LocalizationManager.Get("Resursy_ItemName1");
            Run_Resursy_ItemName2.Text = LocalizationManager.Get("Resursy_ItemName2");

            Run_Resursy_Assigned1.Text = LocalizationManager.Get("Resursy_Assigned1");
            Run_Resursy_Assigned2.Text = LocalizationManager.Get("Resursy_Assigned2");
            Run_Resursy_Assigned3.Text = LocalizationManager.Get("Resursy_Assigned3");

            Run_Resursy_RemainingHh1.Text = LocalizationManager.Get("Resursy_RemainingHh1");
            Run_Resursy_RemainingHh2.Text = LocalizationManager.Get("Resursy_RemainingHh2");

            Run_Resursy_AssignedYears1.Text = LocalizationManager.Get("Resursy_AssignedYears1");
            Run_Resursy_AssignedYears2.Text = LocalizationManager.Get("Resursy_AssignedYears2");

            Run_Resursy_RemainingDate1.Text = LocalizationManager.Get("Resursy_RemainingDate1");
            Run_Resursy_RemainingDate2.Text = LocalizationManager.Get("Resursy_RemainingDate2");

            Run_Resursy_InstalledResource1.Text = LocalizationManager.Get("Resursy_InstalledResource1");
            Run_Resursy_InstalledResource2.Text = LocalizationManager.Get("Resursy_InstalledResource2");
            Run_Resursy_InstalledResource3.Text = LocalizationManager.Get("Resursy_InstalledResource3");

            Run_Resursy_BeforeInstall1.Text = LocalizationManager.Get("Resursy_BeforeInstall1");
            Run_Resursy_BeforeInstall2.Text = LocalizationManager.Get("Resursy_BeforeInstall2");
            Run_Resursy_BeforeInstall3.Text = LocalizationManager.Get("Resursy_BeforeInstall3");

            // ===== Масла =====
            Run_Resursy_OilSNE1.Text = LocalizationManager.Get("Resursy_OilSNE1");
            Run_Resursy_OilSNE2.Text = LocalizationManager.Get("Resursy_OilSNE2");

            Run_Resursy_OilDate1.Text = LocalizationManager.Get("Resursy_OilDate1");
            Run_Resursy_OilDate2.Text = LocalizationManager.Get("Resursy_OilDate2");

            Run_Resursy_OilRem1.Text = LocalizationManager.Get("Resursy_OilRem1");
            Run_Resursy_OilRem2.Text = LocalizationManager.Get("Resursy_OilRem2");

            Run_Resursy_OilRemDate1.Text = LocalizationManager.Get("Resursy_OilRemDate1");
            Run_Resursy_OilRemDate2.Text = LocalizationManager.Get("Resursy_OilRemDate2");

            Run_Resursy_OilRes1.Text = LocalizationManager.Get("Resursy_OilRes1");
            Run_Resursy_OilRes2.Text = LocalizationManager.Get("Resursy_OilRes2");

            Run_Resursy_OilResY1.Text = LocalizationManager.Get("Resursy_OilResY1");
            Run_Resursy_OilResY2.Text = LocalizationManager.Get("Resursy_OilResY2");
        }

        // ===== Агрегаты scroll handlers =====
        private void AggregatesDataScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange != 0)
            {
                AggregatesHeaderScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }

        private void AggregatesHeaderScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Ctrl + колесо: ZoomBehavior
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                return;

            // Shift + колесо: горизонтальный скролл
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (e.Delta < 0)
                {
                    for (int i = 0; i < 3; i++) AggregatesDataScroll.LineRight();
                }
                else
                {
                    for (int i = 0; i < 3; i++) AggregatesDataScroll.LineLeft();
                }
                e.Handled = true;
                return;
            }

            // Обычное колесо над шапкой: скроллим строки таблицы по вертикали
            if (e.Delta < 0)
            {
                for (int i = 0; i < 3; i++) AggregatesDataScroll.LineDown();
            }
            else
            {
                for (int i = 0; i < 3; i++) AggregatesDataScroll.LineUp();
            }
            e.Handled = true;
        }

        private void AggregatesDataScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Ctrl + колесо: ZoomBehavior
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                return;

            // Shift + колесо: горизонтальный скролл
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (e.Delta < 0)
                {
                    for (int i = 0; i < 3; i++) AggregatesDataScroll.LineRight();
                }
                else
                {
                    for (int i = 0; i < 3; i++) AggregatesDataScroll.LineLeft();
                }
                e.Handled = true;
            }
        }

        // ===== Масла scroll handlers =====
        private void OilsDataScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange != 0)
            {
                OilsHeaderScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }

        private void OilsHeaderScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Ctrl + колесо: ZoomBehavior
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                return;

            // Shift + колесо: горизонтальный скролл
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (e.Delta < 0)
                {
                    for (int i = 0; i < 3; i++) OilsDataScroll.LineRight();
                }
                else
                {
                    for (int i = 0; i < 3; i++) OilsDataScroll.LineLeft();
                }
                e.Handled = true;
                return;
            }

            // Обычное колесо над шапкой: скроллим строки таблицы по вертикали
            if (e.Delta < 0)
            {
                for (int i = 0; i < 3; i++) OilsDataScroll.LineDown();
            }
            else
            {
                for (int i = 0; i < 3; i++) OilsDataScroll.LineUp();
            }
            e.Handled = true;
        }

        private void OilsDataScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Ctrl + колесо: ZoomBehavior
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                return;

            // Shift + колесо: горизонтальный скролл
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (e.Delta < 0)
                {
                    for (int i = 0; i < 3; i++) OilsDataScroll.LineRight();
                }
                else
                {
                    for (int i = 0; i < 3; i++) OilsDataScroll.LineLeft();
                }
                e.Handled = true;
            }
        }
    }
}