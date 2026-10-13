using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using logbook.Helpers;
using logbook.Services;

namespace logbook.Views
{
    public partial class ProfileSelectWindow : Window
    {
        private readonly StorageService _storage;

        public string SelectedProfile { get; private set; }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

        public ProfileSelectWindow(StorageService storage)
        {
            InitializeComponent();
            _storage = storage;

            Loaded += (s, e) => ApplyDarkTitleBar();

            var profiles = _storage.GetAllProfiles();

            if (profiles == null || profiles.Count == 0)
            {
                _storage.CreateProfile(LocalizationManager.Get("Main_NewAircraft"), "1.ico");
                profiles = _storage.GetAllProfiles();
            }

            var items = profiles
                .Select(p => new
                {
                    ProfileName = p,
                    IconPath = GetIconPath(_storage.GetProfileIcon(p))
                })
                .ToList();

            ProfilesItems.ItemsSource = items;
        }

        private void ApplyDarkTitleBar()
        {
            try
            {
                int value = logbook.Helpers.ThemeManager.IsDarkTheme ? 1 : 0;

                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero) return;

                int result = DwmSetWindowAttribute(
                    hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));

                if (result != 0)
                {
                    DwmSetWindowAttribute(
                        hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref value, sizeof(int));
                }
            }
            catch { }
        }

        private string GetIconPath(string iconName)
        {
            return $"pack://application:,,,/Resources/Icons/{iconName}";
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string profile)
            {
                SelectedProfile = profile;
                DialogResult = true;
                Close();
            }
        }
    }
}