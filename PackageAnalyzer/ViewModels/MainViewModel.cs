using Ookii.Dialogs.Wpf;
using PackageAnalyzer.Commands;
using PackageAnalyzer.Configuration;
using PackageAnalyzer.Core;
using PackageAnalyzer.Core.FileSystem;
using PackageAnalyzer.Core.Readers;
using PackageAnalyzer.Data;
using PackageAnalyzer.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace PackageAnalyzer.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly TelemetryService _telemetryService;
        private readonly IDialogService _dialogService;
        private readonly PrettifyXml _prettify = new();
        private readonly List<string> uploadedFiles = new();

        public ObservableCollection<SitecoreData> DataToShow { get; } = new();
        public ObservableCollection<string> UploadedFileNames { get; } = new();
        public ObservableCollection<KeySettingItem> KeySettings { get; } = new();

        private ObservableCollection<KeyValuePair<string, string>> _sitecoreRoles = new();
        public ObservableCollection<KeyValuePair<string, string>> SitecoreRoles
        {
            get => _sitecoreRoles;
            set => SetProperty(ref _sitecoreRoles, value);
        }

        private FlowDocument _customTypes = new();
        public FlowDocument CustomTypes
        {
            get => _customTypes;
            set => SetProperty(ref _customTypes, value);
        }

        private FlowDocument _connectionStrings = new();
        public FlowDocument ConnectionStrings
        {
            get => _connectionStrings;
            set => SetProperty(ref _connectionStrings, value);
        }

        public ICommand UploadFilesCommand { get; }
        public ICommand UploadFoldersCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand UpdateRegistryCommand { get; }
        public ICommand ClearRegistryCommand { get; }
        public ICommand OpenLogAnalyzerCommand { get; }
        public ICommand OpenShowconfigCommand { get; }

        public MainViewModel(TelemetryService telemetryService, IDialogService dialogService)
        {
            _telemetryService = telemetryService;
            _dialogService = dialogService;
            UploadFilesCommand = new RelayCommand(_ => UploadFiles());
            UploadFoldersCommand = new RelayCommand(_ => UploadFolders());
            RefreshCommand = new RelayCommand(_ => Refresh());
            ClearCommand = new RelayCommand(_ => Clear());
            UpdateRegistryCommand = new RelayCommand(_ => UpdateRegistry());
            ClearRegistryCommand = new RelayCommand(_ => ClearRegistry());
            OpenLogAnalyzerCommand = new RelayCommand(_ => OpenLogAnalyzer(), _ => uploadedFiles.Any());
            OpenShowconfigCommand = new RelayCommand(_ => OpenShowconfig(), _ => uploadedFiles.Any());
            LoadKeySettings();
            _telemetryService.TrackAppRun();
        }

        public void HandleStartupArgs(string[] args)
        {
            if (args == null || args.Length == 0)
                return;

            if (args[0].Contains("-UpdateRegistry"))
            {
                UpdateRegistry();
                _dialogService.ShowMessage("Added/Updated registry entries to enable context menu option to 'Open with Package Analyzer' on *.zip, *.7z, *.rar files and folder in windows explorer", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Shutdown();
            }
            else if (args[0].Contains("-ClearRegistry"))
            {
                ClearRegistry();
                _dialogService.ShowMessage("Cleared registry entries to enable context menu option to 'Open with Package Analyzer' on *.zip, *.7z, *.rar files and folder in windows explorer", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Shutdown();
            }
            else
            {
                uploadedFiles.AddRange(args);
                ProcessUploadedFilesOrFolder(args);
            }
        }

        private void UploadFiles()
        {
            var dialog = new VistaOpenFileDialog { Multiselect = true };
            if (dialog.ShowDialog() == true)
            {
                ProcessUploadedFilesOrFolder(dialog.FileNames);
                uploadedFiles.AddRange(dialog.FileNames);
            }
        }

        private void UploadFolders()
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Multiselect = true,
                SelectedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads") + Path.DirectorySeparatorChar,
                ShowNewFolderButton = false
            };
            if (dialog.ShowDialog() == true)
            {
                ProcessUploadedFilesOrFolder(dialog.SelectedPaths);
                uploadedFiles.AddRange(dialog.SelectedPaths);
            }
        }

        public void ProcessUploadedFilesOrFolder(string[] uploadedFilesOrFolders)
        {
            if (uploadedFilesOrFolders == null || uploadedFilesOrFolders.Length == 0)
                throw new ArgumentException("No files or folders uploaded.", nameof(uploadedFilesOrFolders));

            AddFilesToListBox(uploadedFilesOrFolders);
            string firstItem = uploadedFilesOrFolders.First();
            string filePath = PackageAnalyzerAdapter.InitialUnzipping(firstItem);
            var roles = PackageAnalyzerAdapter.GetSitecoreRolesForComboBox(filePath);
            SitecoreRoles = new ObservableCollection<KeyValuePair<string, string>>(roles);
            if (roles.Count == 0)
            {
                ProcessCheckboxes(filePath);
            }
        }

        private void AddFilesToListBox(IEnumerable<string> files)
        {
            foreach (var f in files.Select(Path.GetFileName))
            {
                if (!UploadedFileNames.Contains(f))
                    UploadedFileNames.Add(f);
            }
        }

        public void LoadSelected(string displayName)
        {
            var matchingFile = uploadedFiles.FirstOrDefault(file => file.Contains(displayName));
            if (matchingFile == null)
                return;

            string filePath = matchingFile;
            var tempDir = Path.Combine(Path.GetTempPath(), "SearchArchiveTemp", Path.GetFileNameWithoutExtension(displayName));
            if (Directory.Exists(tempDir))
            {
                filePath = tempDir;
            }
            else
            {
                filePath = PackageAnalyzerAdapter.InitialUnzipping(matchingFile);
            }

            var roles = PackageAnalyzerAdapter.GetSitecoreRolesForComboBox(filePath);
            SitecoreRoles = new ObservableCollection<KeyValuePair<string, string>>(roles);
            if (roles.Count == 0)
            {
                DataToShow.Clear();
                ProcessCheckboxes(filePath);
            }
        }

        public void ProcessCheckboxes(string filePath)
        {
            if (DataToShow.Count > 0)
                DataToShow.Add(new SitecoreData());
            DataToShow.Add(new SitecoreData { Identifier = "Package", Value = Path.GetFileName(filePath) });
            foreach (var item in GetEnabledCheckboxes())
            {
                switch (item)
                {
                    case "Sitecore (pre-release) version":
                        ProcessCheckbox("Sitecore (pre-release) version", filePath, PackageAnalyzerAdapter.GetSitecoreVersions);
                        break;
                    case "Sitecore roles from web.config":
                        ProcessCheckbox("Sitecore roles from web.config", filePath, PackageAnalyzerAdapter.GetSitecoreRoles);
                        break;
                    case "Installed modules":
                        ProcessCheckbox("Installed modules", filePath, PackageAnalyzerAdapter.GetSitecoreModules);
                        break;
                    case "Assembly versions":
                        ProcessCheckbox("Sitecore assembly versions list", filePath, PackageAnalyzerAdapter.GetSitecoreAssemblyVersions);
                        break;
                    case "Key settings values":
                        ProcessCheckbox("Sitecore settings list", filePath, PackageAnalyzerAdapter.GetSitecoreSettings);
                        break;
                    case "Topology (XM/XP)":
                        ProcessCheckboxWithExceptionHandling("Topology (XM/XP)", filePath, CheckTopology);
                        break;
                }
            }
            CustomTypes = new FlowDocument(_prettify.DisplayXmlWithHighlighting(PackageAnalyzerAdapter.GetCustomTypes(filePath)));
            ConnectionStrings = new FlowDocument(_prettify.DisplayXmlWithHighlighting(PackageAnalyzerAdapter.GetConfigFile(filePath, "ConnectionStrings.config")));
        }

        private IEnumerable<string> GetEnabledCheckboxes()
        {
            return new[]
            {
                "Sitecore (pre-release) version",
                "Sitecore roles from web.config",
                "Installed modules",
                "Assembly versions",
                "Key settings values",
                "Topology (XM/XP)"
            };
        }

        private void ProcessCheckbox(string identifier, string filePath, Func<string, object> valueProvider)
        {
            DataToShow.Add(new SitecoreData { Identifier = identifier, Value = valueProvider(filePath) });
        }

        private void ProcessCheckboxWithExceptionHandling(string identifier, string filePath, Func<string, string> valueProvider)
        {
            try
            {
                ProcessCheckbox(identifier, filePath, valueProvider);
            }
            catch (Exception ex)
            {
                Log.Error($"Error analyzing the provided package: {ex.Message}:\n{ex.StackTrace}");
                _dialogService.ShowMessage($"Error analyzing the provided package: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string CheckTopology(string filePath)
        {
            return new TopologyChecker().CheckTopology(filePath);
        }

        private void Refresh()
        {
            DataToShow.Clear();
            CustomTypes = new FlowDocument();
            ConnectionStrings = new FlowDocument();
            if (uploadedFiles.FirstOrDefault() is string matchingFile)
            {
                string filePath = PackageAnalyzerAdapter.InitialUnzipping(matchingFile);
                var roles = PackageAnalyzerAdapter.GetSitecoreRolesForComboBox(filePath);
                SitecoreRoles = new ObservableCollection<KeyValuePair<string, string>>(roles);
                if (roles.Count == 0)
                {
                    ProcessCheckboxes(filePath);
                }
            }
        }

        private void Clear()
        {
            DataToShow.Clear();
            CustomTypes = new FlowDocument();
            ConnectionStrings = new FlowDocument();
        }

        private void UpdateRegistry()
        {
            RegistryManager.TryUpdateShellIntegration(out _);
        }

        private void ClearRegistry()
        {
            RegistryManager.TryClearShellIntegration(out _);
        }

        private void OpenLogAnalyzer()
        {
            try
            {
                string filePath = LogAnalyzerManager.GetSelectedFilePath(null, UploadedFileNames.FirstOrDefault(), uploadedFiles);
                if (string.IsNullOrEmpty(filePath))
                {
                    _dialogService.ShowMessage("Logs folder not found", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var logsFolder = LogAnalyzerManager.FindLogsFolder(filePath, uploadedFiles);
                if (string.IsNullOrEmpty(logsFolder))
                {
                    _dialogService.ShowMessage("Logs folder not found", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string logAnalyzerPath = RegistryManager.GetLogAnalyzerCommandData();
                if (string.IsNullOrEmpty(logAnalyzerPath))
                {
                    return;
                }

                LogAnalyzerManager.LaunchLogAnalyzer(logAnalyzerPath, logsFolder);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                _dialogService.ShowMessage($"Error opening Log Analyzer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenShowconfig()
        {
            var filePathFromListBox = uploadedFiles.FirstOrDefault();
            if (string.IsNullOrEmpty(filePathFromListBox))
                return;

            string targetFilePath = null;
            var fileManager = new FileManager(filePathFromListBox);
            if (fileManager.IsArchive(Path.GetExtension(filePathFromListBox)))
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "SearchArchiveTemp", Path.GetFileNameWithoutExtension(filePathFromListBox));
                targetFilePath = new FileManager(tempDir).FindFiles("ShowConfig.xml", true).FirstOrDefault()?.ToString();
            }
            else
            {
                targetFilePath = fileManager.FindFiles("ShowConfig.xml", true).FirstOrDefault()?.ToString();
            }

            if (!string.IsNullOrEmpty(targetFilePath))
            {
                Process.Start(new ProcessStartInfo(targetFilePath) { UseShellExecute = true });
            }
            else
            {
                _dialogService.ShowMessage("Invalid file path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadKeySettings()
        {
            KeySettings.Clear();
            var pathToSettings = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings");
            var enabledSettings = new SettingsReader().ReadAllXmlFiles(pathToSettings);
            foreach (var file in Directory.GetFiles(pathToSettings))
            {
                var fileName = Path.GetFileName(file);
                KeySettings.Add(new KeySettingItem
                {
                    Name = fileName,
                    IsChecked = enabledSettings.ContainsKey(fileName)
                });
            }
        }
    }
}
