using System.Collections.Generic;
using System.Windows;
using logbook.Models;

namespace logbook.Views
{
    public partial class OilAlertWindow : Window
    {
        public OilAlertWindow(List<AlertMessage> messages)
        {
            InitializeComponent();

            Loaded += (s, e) => logbook.Helpers.DarkTitleBarHelper.Apply(this);

            MessagesItems.ItemsSource = messages;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}