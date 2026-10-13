using System.ComponentModel;

namespace logbook.Models
{
    public class ResourceRecord : INotifyPropertyChanged
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

        private string _sne = "";
        public string SNE
        {
            get => _sne;
            set { _sne = value; OnPropertyChanged(nameof(SNE)); }
        }

        private string _extendedTo1 = "";
        public string ExtendedTo1
        {
            get => _extendedTo1;
            set { _extendedTo1 = value; OnPropertyChanged(nameof(ExtendedTo1)); }
        }

        private string _remaining1 = "";
        public string Remaining1
        {
            get => _remaining1;
            set { _remaining1 = value; OnPropertyChanged(nameof(Remaining1)); }
        }

        private string _yearsResource = "";
        public string YearsResource
        {
            get => _yearsResource;
            set { _yearsResource = value; OnPropertyChanged(nameof(YearsResource)); }
        }

        private string _issueDate = "";
        public string IssueDate
        {
            get => _issueDate;
            set { _issueDate = value; OnPropertyChanged(nameof(IssueDate)); }
        }

        private string _extendedTo2 = "";
        public string ExtendedTo2
        {
            get => _extendedTo2;
            set { _extendedTo2 = value; OnPropertyChanged(nameof(ExtendedTo2)); }
        }

        private string _remaining2 = "";
        public string Remaining2
        {
            get => _remaining2;
            set { _remaining2 = value; OnPropertyChanged(nameof(Remaining2)); }
        }

        private string _installedAtSNE1 = "";
        public string InstalledAtSNE1
        {
            get => _installedAtSNE1;
            set { _installedAtSNE1 = value; OnPropertyChanged(nameof(InstalledAtSNE1)); }
        }

        private string _installedAtSNE2 = "";
        public string InstalledAtSNE2
        {
            get => _installedAtSNE2;
            set { _installedAtSNE2 = value; OnPropertyChanged(nameof(InstalledAtSNE2)); }
        }

        private string _coefficient = "";
        public string Coefficient
        {
            get => _coefficient;
            set { _coefficient = value; OnPropertyChanged(nameof(Coefficient)); }
        }

        // Флаги для колонки 3 (Остаток чч:мм)
        private bool _remaining1IsYellowBg;
        public bool Remaining1IsYellowBg
        {
            get => _remaining1IsYellowBg;
            set { _remaining1IsYellowBg = value; OnPropertyChanged(nameof(Remaining1IsYellowBg)); }
        }

        private bool _remaining1IsRedText;
        public bool Remaining1IsRedText
        {
            get => _remaining1IsRedText;
            set { _remaining1IsRedText = value; OnPropertyChanged(nameof(Remaining1IsRedText)); }
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

        // Флаги для колонки 6 (Остаток дд.мм.гггг)
        private bool _remaining2IsYellowBg;
        public bool Remaining2IsYellowBg
        {
            get => _remaining2IsYellowBg;
            set { _remaining2IsYellowBg = value; OnPropertyChanged(nameof(Remaining2IsYellowBg)); }
        }

        private bool _remaining2IsRedText;
        public bool Remaining2IsRedText
        {
            get => _remaining2IsRedText;
            set { _remaining2IsRedText = value; OnPropertyChanged(nameof(Remaining2IsRedText)); }
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
    }
}