using System.Collections.Generic;
using System.Linq;
using System.Windows;
using logbook.Models;
using logbook.ViewModels;

namespace logbook.Views
{
    public partial class NotificationsWindow : Window
    {
        public NotificationsWindow(MainViewModel mainVm)
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                logbook.Helpers.DarkTitleBarHelper.Apply(this);

                var messages = new List<AlertMessage>();
                messages.AddRange(mainVm.BuildResourceAlerts());
                messages.AddRange(mainVm.BuildOilAlerts());

                if (messages.Count == 0)
                {
                    messages.Add(new AlertMessage
                    {
                        Prefix = logbook.Helpers.LocalizationManager.Get("Notifications_Empty"),
                        Name = "",
                        Middle = "",
                        Time = "",
                        Suffix = ""
                    });
                }

                MessagesItems.ItemsSource = messages;
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}