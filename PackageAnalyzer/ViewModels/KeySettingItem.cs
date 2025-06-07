using PackageAnalyzer.Configuration;

namespace PackageAnalyzer.ViewModels
{
    public class KeySettingItem : ObservableObject
    {
        public string Name { get; set; } = string.Empty;
        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (SetProperty(ref _isChecked, value))
                {
                    var config = new PackageAnalyzerConfiguration();
                    if (value)
                        config.AddToSection("DefaultSettingsFiles", Name);
                    else
                        config.RemoveFromSection("DefaultSettingsFiles", Name);
                }
            }
        }
    }
}
