using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using logbook.Helpers;
using logbook.Models;
using logbook.Services;

namespace logbook.Views
{
    public partial class AggregateHistoryWindow : Window
    {
        private readonly StorageService _storage;
        private readonly string _profile;

        public ObservableCollection<HistoryItem> Vr80History { get; set; } = new();
        public ObservableCollection<HistoryItem> Ta14History { get; set; } = new();
        public ObservableCollection<HistoryItem> Vk01History { get; set; } = new();
        public ObservableCollection<HistoryItem> Vk02History { get; set; } = new();

        public string Vr80Current { get; set; } = "";
        public string Ta14Current { get; set; } = "";
        public string Vk01Current { get; set; } = "";
        public string Vk02Current { get; set; } = "";

        public AggregateHistoryWindow(StorageService storage, string profile)
        {
            InitializeComponent();

            _storage = storage;
            _profile = profile;

            DataContext = this;

            SourceInitialized += (s, e) => DarkTitleBarHelper.Apply(this);
            Loaded += (s, e) => DarkTitleBarHelper.Apply(this);

            LoadHistory();
        }

        private void LoadHistory()
        {
            var history = _storage.GetAggregateHistory(_profile) ?? new();

            Vr80History.Clear();
            Ta14History.Clear();
            Vk01History.Clear();
            Vk02History.Clear();

            // Группируем по типу. В каждой группе — старые сверху, новые снизу.
            BuildColumn("Vr80", Vr80History, history);
            BuildColumn("Ta14", Ta14History, history);
            BuildColumn("Vk250001", Vk01History, history);
            BuildColumn("Vk250002", Vk02History, history);

            // Текущие номера
            Vr80Current = _storage.GetAggregateAssignment(_profile, "Vr80") ?? "—";
            Ta14Current = _storage.GetAggregateAssignment(_profile, "Ta14") ?? "—";
            Vk01Current = _storage.GetAggregateAssignment(_profile, "Vk250001") ?? "—";
            Vk02Current = _storage.GetAggregateAssignment(_profile, "Vk250002") ?? "—";

            ClearHistoryButton.Visibility = history.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            // Чтобы WPF увидел изменения
            DataContext = null;
            DataContext = this;
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                LocalizationManager.Get("AggHist_ClearConfirm"),
                LocalizationManager.Get("AggHist_Title"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _storage.ClearAggregateHistory(_profile);
                LoadHistory();
            }
        }

        private void BuildColumn(
            string type,
            ObservableCollection<HistoryItem> target,
            System.Collections.Generic.List<AggregateReplacement> history)
        {
            // Отбираем только записи этого типа
            var items = history
                .Where(h => h.Type == type)
                .ToList();

            // Старые сверху, новые снизу.
            // В истории (AddAggregateReplacement) записи идут в порядке добавления (старые → новые),
            // но нам нужен стек: сверху — самый старый, снизу — самый новый.
            // Если у вас новые добавляются в конец списка — порядок уже правильный.
            foreach (var r in items)
            {
                target.Add(new HistoryItem
                {
                    Number = r.OldNumber,
                    Reason = r.Reason
                });
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    /// <summary>
    /// Одна строка в колонке истории.
    /// </summary>
    public class HistoryItem
    {
        public string Number { get; set; } = "";
        public string Reason { get; set; } = "";
    }
}