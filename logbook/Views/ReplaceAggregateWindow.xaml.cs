using System.Linq;
using System.Windows;
using System.Windows.Controls;
using logbook.Helpers;
using logbook.Services;

namespace logbook.Views
{
    public partial class ReplaceAggregateWindow : Window
    {
        private readonly StorageService _storage;
        private readonly string _profile;
        private readonly string _type;

        /// <summary>
        /// true — пользователь подтвердил замену.
        /// </summary>
        public bool Replaced { get; private set; }

        /// <summary>
        /// Новый номер (после подтверждения).
        /// </summary>
        public string NewNumber { get; private set; } = "";

        /// <summary>
        /// Причина замены.
        /// </summary>
        public string Reason { get; private set; } = "";

        public ReplaceAggregateWindow(StorageService storage, string profile, string type)
        {
            InitializeComponent();

            _storage = storage;
            _profile = profile;
            _type = type;

            SourceInitialized += (s, e) => DarkTitleBarHelper.Apply(this);
            Loaded += ReplaceAggregateWindow_Loaded;
        }

        private void ReplaceAggregateWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DarkTitleBarHelper.Apply(this);

            string displayName = GetDisplayName(_type);
            string oldNumber = _storage.GetAggregateAssignment(_profile, _type) ?? "";

            HeaderText.Text = LocalizationManager.Format("ReplaceAgg_TitleFormat", displayName);
            OldNumberText.Text = string.IsNullOrWhiteSpace(oldNumber) ? "—" : oldNumber;

            TxtNewNumber.Focus();
        }

        // ============================================================
        // ==================== ОБРАБОТЧИКИ ===========================
        // ============================================================

        private void OnAnyTextChanged(object sender, TextChangedEventArgs e)
        {
            // Пока ничего — можно было бы подсвечивать статус
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Replaced = false;
            DialogResult = false;
        }

        private void OnConfirmClick(object sender, RoutedEventArgs e)
        {
            string newNumber = TxtNewNumber.Text?.Trim() ?? "";
            string reason = TxtReason.Text?.Trim() ?? "";

            // 1. Новый номер обязателен
            if (string.IsNullOrWhiteSpace(newNumber))
            {
                MessageBox.Show(
                    LocalizationManager.Get("ReplaceAgg_NewNumberRequired"),
                    LocalizationManager.Get("Msg_Attention"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Нельзя заменить на тот же номер
            string oldNumber = _storage.GetAggregateAssignment(_profile, _type) ?? "";
            if (newNumber == oldNumber)
            {
                MessageBox.Show(
                    LocalizationManager.Get("ReplaceAgg_SameNumber"),
                    LocalizationManager.Get("Msg_Attention"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Проверка: не занят ли номер другим профилем
            var conflicts = _storage.GetAllProfiles()
                .Where(p => p != _profile)
                .Where(p => _storage.GetAggregateAssignment(p, _type) == newNumber)
                .ToList();

            if (conflicts.Count > 0)
            {
                var result = MessageBox.Show(
                    LocalizationManager.Format("ReplaceAgg_ConflictWarning",
                        string.Join(", ", conflicts)),
                    LocalizationManager.Get("Msg_Attention"),
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            // 4. Всё ок
            NewNumber = newNumber;
            Reason = reason;
            Replaced = true;
            DialogResult = true;
        }

        // ============================================================
        // ==================== ВСПОМОГАТЕЛЬНЫЕ =======================
        // ============================================================

        private string GetDisplayName(string type)
        {
            return type switch
            {
                "Vr80" => "ВР-80",
                "Ta14" => "Та-14",
                "Vk250001" => "ВК-2500-01",
                "Vk250002" => "ВК-2500-02",
                _ => type
            };
        }
    }
}