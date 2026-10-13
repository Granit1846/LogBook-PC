using System.Windows;
using logbook.Helpers;
using logbook.Services;

namespace logbook.Views
{
    public partial class DeleteProfileWindow : Window
    {
        private readonly StorageService _storage;

        public DeleteProfileWindow(StorageService storage, string currentProfile)
        {
            InitializeComponent();

            Loaded += (s, e) => logbook.Helpers.DarkTitleBarHelper.Apply(this);

            _storage = storage;

            var profiles = _storage.GetAllProfiles();
            ProfilesListBox.ItemsSource = profiles;

            ProfilesListBox.SelectedItem = currentProfile;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            string profileToDelete = ProfilesListBox.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(profileToDelete))
            {
                MessageBox.Show(
                    LocalizationManager.Get("Msg_SelectProfileToDelete"),
                    LocalizationManager.Get("Msg_Attention"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var all = _storage.GetAllProfiles();
            if (all.Count <= 1)
            {
                MessageBox.Show(
                    LocalizationManager.Get("Msg_DeleteLastProfile"),
                    LocalizationManager.Get("Msg_Attention"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"{LocalizationManager.Format("Msg_DeleteProfileConfirm", profileToDelete)}\n" +
                LocalizationManager.Get("Msg_DeleteProfileWarning"),
                LocalizationManager.Get("Msg_ConfirmDeletion"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            _storage.DeleteProfile(profileToDelete);

            var profiles = _storage.GetAllProfiles();
            ProfilesListBox.ItemsSource = profiles;

            if (profiles.Count > 0)
                ProfilesListBox.SelectedItem = profiles[0];

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