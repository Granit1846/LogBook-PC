using System.ComponentModel;

namespace logbook.Models
{
    public class Chast1Record : INotifyPropertyChanged
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

        private string _flightType = "";
        public string FlightType
        {
            get => _flightType;
            set { _flightType = value; OnPropertyChanged(nameof(FlightType)); }
        }

        private string _groundHours = "";
        public string GroundHours
        {
            get => _groundHours;
            set { _groundHours = value; OnPropertyChanged(nameof(GroundHours)); }
        }

        private string _flightDuration = "";
        public string FlightDuration
        {
            get => _flightDuration;
            set { _flightDuration = value; OnPropertyChanged(nameof(FlightDuration)); }
        }

        private string _col5 = "";
        public string Col5
        {
            get => _col5;
            set { _col5 = value; OnPropertyChanged(nameof(Col5)); }
        }

        private string _colSNE = "";
        public string ColSNE
        {
            get => _colSNE;
            set { _colSNE = value; OnPropertyChanged(nameof(ColSNE)); }
        }

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

        private string _landings = "";
        public string Landings
        {
            get => _landings;
            set { _landings = value; OnPropertyChanged(nameof(Landings)); }
        }

        private string _col11 = "";
        public string Col11
        {
            get => _col11;
            set { _col11 = value; OnPropertyChanged(nameof(Col11)); }
        }

        private string _landings2 = "";
        public string Landings2
        {
            get => _landings2;
            set { _landings2 = value; OnPropertyChanged(nameof(Landings2)); }
        }

        private string _pilot = "";
        public string Pilot
        {
            get => _pilot;
            set { _pilot = value; OnPropertyChanged(nameof(Pilot)); }
        }

        private string _mechanic = "";
        public string Mechanic
        {
            get => _mechanic;
            set { _mechanic = value; OnPropertyChanged(nameof(Mechanic)); }
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