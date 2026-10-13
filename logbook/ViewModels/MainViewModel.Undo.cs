using logbook.Helpers;
using logbook.Models;
using System;
using System.Linq;
using System.Windows;

namespace logbook.ViewModels
{
    public partial class MainViewModel
    {
        // ============================================================
        // ==================== UNDO ==================================
        // ============================================================

        private int MaxHistorySize => Math.Max(1, _settingsService.Settings.UndoSize);

        /// <summary>
        /// Создаёт глубокую копию записи через рефлексию.
        /// </summary>
        private static T CloneRecord<T>(T source) where T : class, new()
        {
            if (source == null) return null;
            var clone = new T();
            foreach (var prop in typeof(T).GetProperties())
            {
                if (prop.CanRead && prop.CanWrite)
                    prop.SetValue(clone, prop.GetValue(source));
            }
            return clone;
        }

        /// <summary>
        /// Сохраняет текущее состояние в стек (глубокая копия).
        /// </summary>
        private void TakeSnapshot()
        {
            if (_isRestoringHistory) return;

            var state = new AppState
            {
                SelectedProfile = SelectedProfile,
                SelectedDate = SelectedDate,
                Crew = Crew,
                Launches = Launches.Select(l => new Launch
                {
                    ExchangeId = l.ExchangeId,
                    Vsu = l.Vsu,
                    Zemlya = l.Zemlya,
                    Vozduh = l.Vozduh,
                    TP = l.TP,
                    SamoletLandings = l.SamoletLandings
                }).ToList(),
                SnkValues = SnkRecords.Select(s => new SnkValue
                {
                    Label = s.Label,
                    LeftValue = s.LeftValue,
                    RightValue = s.RightValue
                }).ToList(),

                // Глубокие копии всех коллекций записей
                Chast1Records = Chast1Records.Select(r => CloneRecord(r)).ToList(),
                Chast1StartRecord = CloneRecord(Chast1StartRecord),

                Vr80Records = Vr80Records.Select(r => CloneRecord(r)).ToList(),
                Vr80StartRecord = CloneRecord(Vr80StartRecord),

                Ta14Records = Ta14Records.Select(r => CloneRecord(r)).ToList(),
                Ta14StartRecord = CloneRecord(Ta14StartRecord),

                Vk250001Records = Vk250001Records.Select(r => CloneRecord(r)).ToList(),
                Vk250001StartRecord = CloneRecord(Vk250001StartRecord),
                Vk250001CoeffA = Vk250001CoeffA,
                Vk250001CoeffH = Vk250001CoeffH,

                Vk250002Records = Vk250002Records.Select(r => CloneRecord(r)).ToList(),
                Vk250002StartRecord = CloneRecord(Vk250002StartRecord),
                Vk250002CoeffA = Vk250002CoeffA,
                Vk250002CoeffH = Vk250002CoeffH,

                ResourceRecords = ResourceRecords.Select(r => CloneRecord(r)).ToList(),
                OilRecords = OilRecords.Select(r => CloneRecord(r)).ToList()
            };

            // Удаляем самый старый элемент, если стек переполнен
            while (_history.Count >= MaxHistorySize)
            {
                var temp = _history.ToArray();   // [0] = верх, [last] = низ
                _history.Clear();
                for (int i = temp.Length - 2; i >= 0; i--)
                    _history.Push(temp[i]);
            }

            _history.Push(state);
        }

        /// <summary>
        /// Отменяет последнее действие.
        /// </summary>
        private void Undo()
        {
            if (_history.Count == 0)
            {
                MessageBox.Show(
                    LocalizationManager.Get("Msg_NoUndo"),
                    LocalizationManager.Get("Msg_Undo"),
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var state = _history.Pop();

            _isRestoringHistory = true;
            try
            {
                SelectedProfile = state.SelectedProfile;
                SelectedDate = state.SelectedDate;
                Crew = state.Crew;

                // === Launches ===
                Launches.Clear();
                foreach (var l in state.Launches)
                {
                    var launch = new Launch
                    {
                        ExchangeId = l.ExchangeId,
                    Vsu = l.Vsu,
                        Zemlya = l.Zemlya,
                        Vozduh = l.Vozduh,
                        TP = l.TP,
                        SamoletLandings = l.SamoletLandings
                    };
                    launch.PropertyChanged += Launch_PropertyChanged;
                    Launches.Add(launch);
                }

                // === SnkRecords ===
                SnkRecords.Clear();
                foreach (var s in state.SnkValues)
                {
                    SnkRecords.Add(new SnkRecord
                    {
                        Label = s.Label,
                        LeftValue = s.LeftValue,
                        RightValue = s.RightValue
                    });
                }

                // === Часть 1 ===
                Chast1Records.Clear();
                foreach (var r in state.Chast1Records)
                    Chast1Records.Add(r);
                Chast1StartRecord = state.Chast1StartRecord;

                // === ВР-80 ===
                Vr80Records.Clear();
                foreach (var r in state.Vr80Records)
                    Vr80Records.Add(r);
                Vr80StartRecord = state.Vr80StartRecord;

                // === ТА-14 ===
                Ta14Records.Clear();
                foreach (var r in state.Ta14Records)
                    Ta14Records.Add(r);
                Ta14StartRecord = state.Ta14StartRecord;

                // === ВК-2500-01 ===
                Vk250001Records.Clear();
                foreach (var r in state.Vk250001Records)
                    Vk250001Records.Add(r);
                Vk250001StartRecord = state.Vk250001StartRecord;
                Vk250001CoeffA = state.Vk250001CoeffA;
                Vk250001CoeffH = state.Vk250001CoeffH;

                // === ВК-2500-02 ===
                Vk250002Records.Clear();
                foreach (var r in state.Vk250002Records)
                    Vk250002Records.Add(r);
                Vk250002StartRecord = state.Vk250002StartRecord;
                Vk250002CoeffA = state.Vk250002CoeffA;
                Vk250002CoeffH = state.Vk250002CoeffH;

                // === Ресурсы / ГСМ ===
                ResourceRecords.Clear();
                foreach (var r in state.ResourceRecords)
                    ResourceRecords.Add(r);

                OilRecords.Clear();
                foreach (var o in state.OilRecords)
                    OilRecords.Add(o);

                // === Пересчёт итогов (без пересчёта записей) ===
                EnsureMonthlyTotals();
                RecalculateMonthlyTotals();

                EnsureMonthlyTotalsVr80();
                RecalculateVr80MonthlyTotals();

                EnsureMonthlyTotalsTa14();
                RecalculateTa14MonthlyTotals();

                EnsureMonthlyTotalsVk2500(Vk250001Records);
                RecalculateVk2500MonthlyTotals(Vk250001Records);

                EnsureMonthlyTotalsVk2500(Vk250002Records);
                RecalculateVk2500MonthlyTotals(Vk250002Records);

                RefreshSNE();

                // Подписываем восстановленные записи на изменения
                SubscribeItems(Chast1Records);
                SubscribeItems(Vr80Records);
                SubscribeItems(Ta14Records);
                SubscribeItems(Vk250001Records);
                SubscribeItems(Vk250002Records);
            }
            finally
            {
                _isRestoringHistory = false;
            }
        }

        /// <summary>
        /// Применяет новый размер Undo (из настроек).
        /// </summary>
        public void ApplyUndoSize(int newSize)
        {
            if (newSize < 1) newSize = 1;

            while (_history.Count > newSize)
            {
                var temp = _history.ToArray();
                _history.Clear();
                for (int i = temp.Length - 2; i >= 0; i--)
                    _history.Push(temp[i]);
            }
        }
    }
}
