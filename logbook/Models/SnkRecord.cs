using System.ComponentModel;

namespace logbook.Models
{
    public class SnkRecord : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _label = "";
        public string Label
        {
            get => _label;
            set { _label = value; OnPropertyChanged(nameof(Label)); }
        }

        private string _leftValue = "";
        public string LeftValue
        {
            get => _leftValue;
            set { _leftValue = value; OnPropertyChanged(nameof(LeftValue)); }
        }

        private string _rightValue = "";
        public string RightValue
        {
            get => _rightValue;
            set { _rightValue = value; OnPropertyChanged(nameof(RightValue)); }
        }
    }
}