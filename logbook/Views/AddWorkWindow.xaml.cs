using System;
using System.Collections.Generic;
using System.Windows;
using logbook.Helpers;
using logbook.Services;

namespace logbook.Views
{
    public partial class AddWorkWindow : Window
    {
        public class KindItem
        {
            public string Kind { get; set; } = "";
            public string Title { get; set; } = "";
        }

        public string SelectedKind { get; private set; } = "";
        public DateTime SelectedDate { get; private set; } = DateTime.Today;
        public int CounterMinutes { get; private set; }
        public string Description { get; private set; } = "";

        public AddWorkWindow(int currentFuselageMinutes)
        {
            InitializeComponent();

            var list = new List<KindItem>();
            foreach (var k in MaintenanceJournalService.Kinds)
            {
                list.Add(new KindItem { Kind = k, Title = MaintenanceJournalService.GetKindTitle(k) });
            }
            KindComboBox.ItemsSource = list;
            KindComboBox.SelectedIndex = 0;

            WorkDatePicker.SelectedDate = DateTime.Today;
            CounterTextBox.Text = MaintenanceJournalService.FormatHours(currentFuselageMinutes);

            Loaded += (s, e) => DarkTitleBarHelper.Apply(this);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var item = KindComboBox.SelectedItem as KindItem;
            if (item == null)
            {
                MessageBox.Show(LocalizationManager.Get("Journal_ChooseWork"), LocalizationManager.Get("Msg_Attention"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var parsed = MaintenanceJournalService.ParseHours(CounterTextBox.Text);
            if (parsed == null)
            {
                MessageBox.Show(LocalizationManager.Get("Journal_InvalidInput"), LocalizationManager.Get("Msg_Attention"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedKind = item.Kind;
            SelectedDate = WorkDatePicker.SelectedDate ?? DateTime.Today;
            CounterMinutes = parsed.Value;
            Description = DescriptionTextBox.Text ?? "";

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}