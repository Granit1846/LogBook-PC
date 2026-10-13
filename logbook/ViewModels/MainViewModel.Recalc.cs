using System.Collections.ObjectModel;
using System.ComponentModel;

namespace logbook.ViewModels
{
    public partial class MainViewModel
    {
        // ============================================================
        // ==================== ПОДПИСКИ НА ИЗМЕНЕНИЯ =================
        // ============================================================

        /// <summary>
        /// Подписаться на все источники изменений, влияющие на формуляры.
        /// </summary>
        private void SubscribeToRecalcSources()
        {
            Chast1Records.CollectionChanged += OnCollectionChanged_Recalc;
            Vr80Records.CollectionChanged += OnCollectionChanged_Recalc;
            Ta14Records.CollectionChanged += OnCollectionChanged_Recalc;
            Vk250001Records.CollectionChanged += OnCollectionChanged_Recalc;
            Vk250002Records.CollectionChanged += OnCollectionChanged_Recalc;
            ResourceRecords.CollectionChanged += OnCollectionChanged_Recalc;
            OilRecords.CollectionChanged += OnCollectionChanged_Recalc;

            SubscribeStartRecord(Chast1StartRecord);
            SubscribeStartRecord(Vr80StartRecord);
            SubscribeStartRecord(Ta14StartRecord);
            SubscribeStartRecord(Vk250001StartRecord);
            SubscribeStartRecord(Vk250002StartRecord);

            SubscribeItems(Chast1Records);
            SubscribeItems(Vr80Records);
            SubscribeItems(Ta14Records);
            SubscribeItems(Vk250001Records);
            SubscribeItems(Vk250002Records);
            SubscribeItems(ResourceRecords);
            SubscribeItems(OilRecords);
        }

        private void SubscribeItems<T>(ObservableCollection<T> collection)
            where T : INotifyPropertyChanged
        {
            foreach (var item in collection)
                item.PropertyChanged += OnItemPropertyChanged_Recalc;
        }

        private void UnsubscribeItems<T>(ObservableCollection<T> collection)
            where T : INotifyPropertyChanged
        {
            foreach (var item in collection)
                item.PropertyChanged -= OnItemPropertyChanged_Recalc;
        }

        private void SubscribeStartRecord(INotifyPropertyChanged startRecord)
        {
            if (startRecord != null)
                startRecord.PropertyChanged += OnItemPropertyChanged_Recalc;
        }

        private void UnsubscribeStartRecord(INotifyPropertyChanged startRecord)
        {
            if (startRecord != null)
                startRecord.PropertyChanged -= OnItemPropertyChanged_Recalc;
        }

        // ============================================================
        // ==================== ОБРАБОТЧИКИ ===========================
        // ============================================================

        private void OnCollectionChanged_Recalc(
    object sender,
    System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (_isRecalculating) return;

            if (e.NewItems != null)
                foreach (INotifyPropertyChanged item in e.NewItems)
                    item.PropertyChanged += OnItemPropertyChanged_Recalc;

            if (e.OldItems != null)
                foreach (INotifyPropertyChanged item in e.OldItems)
                    item.PropertyChanged -= OnItemPropertyChanged_Recalc;

            if (sender == Chast1Records)
                UpdateDaysInProfile();

            IsDirty = true;      // <-- ДОБАВЛЕНО
            ScheduleRecalc();
        }

        private void OnItemPropertyChanged_Recalc(object sender, PropertyChangedEventArgs e)
        {
            if (_isRecalculating) return;
            IsDirty = true;
            ScheduleRecalc();
        }

        // ============================================================
        // ==================== DEBOUNCE ==============================
        // ============================================================

        private void ScheduleRecalc()
        {
            if (_exchangeOpen || _loadingProfile) return;
            _recalcTimer?.Stop();
            _recalcTimer?.Start();
        }

        // ============================================================
        // ==================== ПОЛНЫЙ ПЕРЕСЧЁТ =======================
        // ============================================================

        private void RecalculateAll()
        {
            if (_isRecalculating) return;

            _isRecalculating = true;
            try
            {
                RecalculateChast1Records();
                EnsureMonthlyTotals();
                RecalculateMonthlyTotals();

                RecalculateVr80Records();
                EnsureMonthlyTotalsVr80();
                RecalculateVr80MonthlyTotals();

                RecalculateTa14Records();
                EnsureMonthlyTotalsTa14();
                RecalculateTa14MonthlyTotals();

                RecalculateVk250001Records();
                RecalculateVk250002Records();

                EnsureMonthlyTotalsVk2500(Vk250001Records);
                EnsureMonthlyTotalsVk2500(Vk250002Records);

                RefreshSNE();
                RecalculateResources();
            }
            finally
            {
                _isRecalculating = false;
            }
        }
    }
}