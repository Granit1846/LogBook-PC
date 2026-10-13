using System.Linq;
using System.Windows;
using logbook.Helpers;

namespace logbook.Views
{
    public partial class AddProfileWindow : Window
    {
        private static readonly string[] IconNames = new[]
        {
            "1.ico", "2.ico", "3.ico", "4.ico", "5.ico", "6.ico"
        };

        public string ProfileName { get; private set; }
        public string IconName { get; private set; } = "1.ico";

        public AddProfileWindow()
        {
            InitializeComponent();

            Loaded += (s, e) => logbook.Helpers.DarkTitleBarHelper.Apply(this);

            var items = IconNames
                .Select(icon => new
                {
                    IconName = icon,
                    IconPath = $"pack://application:,,,/Resources/Icons/{icon}"
                })
                .ToList();

            IconsListBox.ItemsSource = items;

            IconsListBox.SelectedIndex = 0;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(
                    LocalizationManager.Get("Msg_EnterAircraftName"),
                    LocalizationManager.Get("Msg_Attention"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (IconsListBox.SelectedItem == null)
            {
                MessageBox.Show(
                    LocalizationManager.Get("Msg_SelectIcon"),
                    LocalizationManager.Get("Msg_Attention"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ProfileName = name;

            dynamic selected = IconsListBox.SelectedItem;
            IconName = selected.IconName;

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