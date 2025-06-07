namespace PackageAnalyzer.ViewModels
{
    public class AnalysisOption : ObservableObject
    {
        public string Name { get; }
        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }

        public AnalysisOption(string name, bool isChecked = true)
        {
            Name = name;
            _isChecked = isChecked;
        }
    }
}
