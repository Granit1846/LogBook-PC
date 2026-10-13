using System.ComponentModel;

namespace logbook.Models
{
    public class OilRecord : INotifyPropertyChanged
    {
        public string ExchangeId { get; set; } = "";
        public string ExchangeExtras { get; set; } = "";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _name = "";
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        private string _byHours = "";
        public string ByHours
        {
            get => _byHours;
            set { _byHours = value; OnPropertyChanged(nameof(ByHours)); }
        }

        private string _byYears = "";
        public string ByYears
        {
            get => _byYears;
            set { _byYears = value; OnPropertyChanged(nameof(ByYears)); }
        }

        private string _remaining1 = "";
        public string Remaining1
        {
            get => _remaining1;
            set { _remaining1 = value; OnPropertyChanged(nameof(Remaining1)); }
        }

        private bool _remaining1IsWarning;
        public bool Remaining1IsWarning
        {
            get => _remaining1IsWarning;
            set { _remaining1IsWarning = value; OnPropertyChanged(nameof(Remaining1IsWarning)); }
        }

        private bool _remaining1IsCritical;
        public bool Remaining1IsCritical
        {
            get => _remaining1IsCritical;
            set { _remaining1IsCritical = value; OnPropertyChanged(nameof(Remaining1IsCritical)); }
        }

        private bool _remaining1IsNegative;
        public bool Remaining1IsNegative
        {
            get => _remaining1IsNegative;
            set { _remaining1IsNegative = value; OnPropertyChanged(nameof(Remaining1IsNegative)); }
        }

        private string _remaining2 = "";
        public string Remaining2
        {
            get => _remaining2;
            set { _remaining2 = value; OnPropertyChanged(nameof(Remaining2)); }
        }

        private bool _remaining2IsWarning;
        public bool Remaining2IsWarning
        {
            get => _remaining2IsWarning;
            set { _remaining2IsWarning = value; OnPropertyChanged(nameof(Remaining2IsWarning)); }
        }

        private bool _remaining2IsCritical;
        public bool Remaining2IsCritical
        {
            get => _remaining2IsCritical;
            set { _remaining2IsCritical = value; OnPropertyChanged(nameof(Remaining2IsCritical)); }
        }

        private bool _remaining2IsNegative;
        public bool Remaining2IsNegative
        {
            get => _remaining2IsNegative;
            set { _remaining2IsNegative = value; OnPropertyChanged(nameof(Remaining2IsNegative)); }
        }

        private string _coefficient = "";
        public string Coefficient
        {
            get => _coefficient;
            set { _coefficient = value; OnPropertyChanged(nameof(Coefficient)); }
        }

        private string _term = "1";
        public string Term
        {
            get => _term;
            set { _term = value; OnPropertyChanged(nameof(Term)); }
        }
    }
}