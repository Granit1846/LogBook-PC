using System.ComponentModel;

namespace logbook.Models
{
    public class Ta14Record : INotifyPropertyChanged
    {
        public string ExchangeId { get; set; } = "";

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 0: Дата
        private string _date = "";
        public string Date
        {
            get => _date;
            set { _date = value; OnPropertyChanged(nameof(Date)); }
        }

        // 1: Количество запусков (за день)
        private string _launchCount = "";
        public string LaunchCount
        {
            get => _launchCount;
            set { _launchCount = value; OnPropertyChanged(nameof(LaunchCount)); }
        }

        // 2: Показания счётчиков (пусто)
        private string _col3 = "";
        public string Col3
        {
            get => _col3;
            set { _col3 = value; OnPropertyChanged(nameof(Col3)); }
        }

        // 3: Время (ВСУ) — наработка двигателя на земле
        private string _colVremya = "";
        public string ColVremya
        {
            get => _colVremya;
            set { _colVremya = value; OnPropertyChanged(nameof(ColVremya)); }
        }

        // 4: «5» (пусто)
        private string _col5 = "";
        public string Col5
        {
            get => _col5;
            set { _col5 = value; OnPropertyChanged(nameof(Col5)); }
        }

        // 5: СНЭ (накопительное, суммарная наработка двигателя)
        private string _colSNE = "";
        public string ColSNE
        {
            get => _colSNE;
            set { _colSNE = value; OnPropertyChanged(nameof(ColSNE)); }
        }

        // 6: «7» (накопительное, после кап. ремонта — ч.мин)
        private string _col7 = "";
        public string Col7
        {
            get => _col7;
            set { _col7 = value; OnPropertyChanged(nameof(Col7)); }
        }

        // 7: «8» (накопительное, после кап. ремонта — запуски)
        private string _col8 = "";
        public string Col8
        {
            get => _col8;
            set { _col8 = value; OnPropertyChanged(nameof(Col8)); }
        }

        // 8: СНЭ2 (с начала эксплуатации — ч.мин) — дублирует ColSNE
        private string _colSNE2 = "";
        public string ColSNE2
        {
            get => _colSNE2;
            set { _colSNE2 = value; OnPropertyChanged(nameof(ColSNE2)); }
        }

        // 9: Запуски2 (с начала эксплуатации — накопительное)
        private string _launchCount2 = "";
        public string LaunchCount2
        {
            get => _launchCount2;
            set { _launchCount2 = value; OnPropertyChanged(nameof(LaunchCount2)); }
        }

        // 10: «11» (Подпись ведущего формуляр, пусто)
        private string _col11 = "";
        public string Col11
        {
            get => _col11;
            set { _col11 = value; OnPropertyChanged(nameof(Col11)); }
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