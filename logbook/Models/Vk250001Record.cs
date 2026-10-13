using System.ComponentModel;

namespace logbook.Models
{
    public class Vk250001Record : INotifyPropertyChanged
    {
        public string ExchangeId { get; set; } = "";

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _date = "";
        public string Date { get => _date; set { _date = value; OnPropertyChanged(nameof(Date)); } }

        private string _a = "";
        public string A { get => _a; set { _a = value; OnPropertyChanged(nameof(A)); } }

        private string _aPpr = "";
        public string A_PPR { get => _aPpr; set { _aPpr = value; OnPropertyChanged(nameof(A_PPR)); } }

        private string _b = "";
        public string B { get => _b; set { _b = value; OnPropertyChanged(nameof(B)); } }

        private string _d = "";
        public string D { get => _d; set { _d = value; OnPropertyChanged(nameof(D)); } }

        private string _e = "";
        public string E { get => _e; set { _e = value; OnPropertyChanged(nameof(E)); } }

        private string _g = "";
        public string G { get => _g; set { _g = value; OnPropertyChanged(nameof(G)); } }

        private string _h = "";
        public string H { get => _h; set { _h = value; OnPropertyChanged(nameof(H)); } }

        private string _hPpr = "";
        public string H_PPR { get => _hPpr; set { _hPpr = value; OnPropertyChanged(nameof(H_PPR)); } }

        private string _i = "";
        public string I { get => _i; set { _i = value; OnPropertyChanged(nameof(I)); } }

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