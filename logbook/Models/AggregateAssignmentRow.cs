using System.ComponentModel;

namespace logbook.Models
{
    /// <summary>
    /// Строка таблицы «Агрегаты на борту».
    /// Используется для отображения в ResursyTab.
    /// </summary>
    public class AggregateAssignmentRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Ключ привязки: "Chast1", "Vr80", "Ta14", "Vk250001", "Vk250002".
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// Отображаемое имя: "Часть 1 (борт)", "ВР-80", ...
        /// </summary>
        public string DisplayName { get; set; } = "";

        private string _number = "";
        public string Number
        {
            get => _number;
            set { _number = value; OnPropertyChanged(nameof(Number)); }
        }

        private string _status = "";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        /// <summary>
        /// Можно ли заменять этот агрегат.
        /// Для Части 1 — false (не меняется).
        /// </summary>
        public bool CanReplace { get; set; } = true;

        /// <summary>
        /// Показывать ли кнопку «Заменить» (false для Части 1).
        /// </summary>
        public bool ShowReplaceButton => CanReplace;
    }
}