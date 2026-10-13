using System.Windows;
using logbook.Helpers;

namespace logbook.Views
{
    public partial class AddDefectWindow : Window
    {
        public string Description { get; private set; } = "";

        public AddDefectWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => DarkTitleBarHelper.Apply(this);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var text = DescriptionTextBox.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show(LocalizationManager.Get("Journal_InvalidInput"), LocalizationManager.Get("Msg_Attention"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Description = text;
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