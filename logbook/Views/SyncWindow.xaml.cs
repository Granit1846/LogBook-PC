using System.Windows;

namespace logbook.Views
{
    public partial class SyncWindow : Window
    {
        /// <summary>
        /// Что выбрал пользователь: Push, Pull, Phone или ничего.
        /// </summary>
        public SyncTarget Target { get; private set; } = SyncTarget.None;

        /// <summary>
        /// Показывать ли уведомления о конфликтах агрегатов.
        /// </summary>
        public bool ShowNotifications => NotificationsCheckBox.IsChecked == true;

        public SyncWindow(bool initialNotifications)
        {
            InitializeComponent();

            NotificationsCheckBox.IsChecked = initialNotifications;

            Loaded += (s, e) => logbook.Helpers.DarkTitleBarHelper.Apply(this);
        }

        private void Push_Click(object sender, RoutedEventArgs e)
        {
            Target = SyncTarget.Push;
            DialogResult = true;
            Close();
        }

        private void Pull_Click(object sender, RoutedEventArgs e)
        {
            Target = SyncTarget.Pull;
            DialogResult = true;
            Close();
        }

        private void Phone_Click(object sender, RoutedEventArgs e)
        {
            Target = SyncTarget.Phone;
            DialogResult = true;
            Close();
        }
    }

    public enum SyncTarget
    {
        None,
        Push,
        Pull,
        Phone
    }
}