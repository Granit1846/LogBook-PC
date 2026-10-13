using System.ComponentModel;

namespace logbook.Models
{
    public class Vr80Record : INotifyPropertyChanged
    {
        public string ExchangeId { get; set; } = "";

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _date = "";
        public string Date
        {
            get => _date;
            set { _date = value; OnPropertyChanged(nameof(Date)); }
        }

        private string _launchCount = "";
        public string LaunchCount
        {
            get => _launchCount;
            set { _launchCount = value; OnPropertyChanged(nameof(LaunchCount)); }
        }

        private string _groundHours = "";
        public string GroundHours
        {
            get => _groundHours;
            set { _groundHours = value; OnPropertyChanged(nameof(GroundHours)); }
        }

        private string _col4 = "";
        public string Col4
        {
            get => _col4;
            set { _col4 = value; OnPropertyChanged(nameof(Col4)); }
        }

        private string _col5 = "";
        public string Col5
        {
            get => _col5;
            set { _col5 = value; OnPropertyChanged(nameof(Col5)); }
        }

        // Колонка 6 — «Взлётный» (пусто, не используется)
        private string _col6 = "";
        public string Col6
        {
            get => _col6;
            set { _col6 = value; OnPropertyChanged(nameof(Col6)); }
        }

        // Колонка 7 — «Возд» (Воздух)
        private string _col7 = "";
        public string Col7
        {
            get => _col7;
            set { _col7 = value; OnPropertyChanged(nameof(Col7)); }
        }

        private string _col8 = "";
        public string Col8
        {
            get => _col8;
            set { _col8 = value; OnPropertyChanged(nameof(Col8)); }
        }

        private string _col9 = "";
        public string Col9
        {
            get => _col9;
            set { _col9 = value; OnPropertyChanged(nameof(Col9)); }
        }

        private string _col10 = "";
        public string Col10
        {
            get => _col10;
            set { _col10 = value; OnPropertyChanged(nameof(Col10)); }
        }

        // Col24 — ППР (после последнего капремонта) → работа ГР от двигателей
        private string _col24 = "";
        public string Col24
        {
            get => _col24;
            set { _col24 = value; OnPropertyChanged(nameof(Col24)); }
        }

        // ColSNE — СНЭ (с начала эксплуатации) → работа ГР от двигателей
        private string _colSNE = "";
        public string ColSNE
        {
            get => _colSNE;
            set { _colSNE = value; OnPropertyChanged(nameof(ColSNE)); }
        }

        // Col26 — ППР → от турбопривода
        private string _col26 = "";
        public string Col26
        {
            get => _col26;
            set { _col26 = value; OnPropertyChanged(nameof(Col26)); }
        }

        // Col27 — СНЭ → от турбопривода
        private string _col27 = "";
        public string Col27
        {
            get => _col27;
            set { _col27 = value; OnPropertyChanged(nameof(Col27)); }
        }

        private string _col28 = "";
        public string Col28
        {
            get => _col28;
            set { _col28 = value; OnPropertyChanged(nameof(Col28)); }
        }

        private string _col15 = "";
        public string Col15
        {
            get => _col15;
            set { _col15 = value; OnPropertyChanged(nameof(Col15)); }
        }

        private bool _needsManualRecalc;
        public bool NeedsManualRecalc
        {
            get => _needsManualRecalc;
            set { _needsManualRecalc = value; OnPropertyChanged(nameof(NeedsManualRecalc)); }
        }

        /// <summary>
        /// true — это итоговая строка за месяц ("Итого за ..."),
        /// а не запись за конкретный день.
        /// </summary>
        public bool IsTotal { get; set; } = false;
    }
}