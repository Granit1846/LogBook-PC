using System.ComponentModel;

namespace logbook.Models
{
    public class Launch : INotifyPropertyChanged
    {
        public string ExchangeId { get; set; } = "";

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int _vsu;
        public int Vsu
        {
            get => _vsu;
            set { _vsu = value; OnPropertyChanged(nameof(Vsu)); }
        }

        private int _zemlya;
        public int Zemlya
        {
            get => _zemlya;
            set { _zemlya = value; OnPropertyChanged(nameof(Zemlya)); }
        }

        private int _vozduh;
        public int Vozduh
        {
            get => _vozduh;
            set { _vozduh = value; OnPropertyChanged(nameof(Vozduh)); }
        }

        /// <summary>
        /// Время работы от турбопривода (в минутах).
        /// </summary>
        private int _tp;
        public int TP
        {
            get => _tp;
            set { _tp = value; OnPropertyChanged(nameof(TP)); }
        }

        /// <summary>
        /// Количество посадок по самолётному за день.
        /// Суммируется в Часть 1 (колонка «По самолётному»).
        /// </summary>
        private int _samoletLandings;
        public int SamoletLandings
        {
            get => _samoletLandings;
            set { _samoletLandings = value; OnPropertyChanged(nameof(SamoletLandings)); }
        }
    }
}