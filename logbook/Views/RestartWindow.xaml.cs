using System.Diagnostics;
using System.Windows;

namespace logbook.Views
{
    public partial class RestartWindow : Window
    {
        public RestartWindow()
        {
            InitializeComponent();

            Loaded += (s, e) => logbook.Helpers.DarkTitleBarHelper.Apply(this);
        }

        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            // Перезапуск приложения
            Process.Start(Process.GetCurrentProcess().MainModule.FileName);
            Application.Current.Shutdown();
        }
    }
}