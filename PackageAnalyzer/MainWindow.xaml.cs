using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using PackageAnalyzer.Core.Readers;
using System.Collections.ObjectModel;
using System;
using PackageAnalyzer.Core;
using Serilog;
using Ookii.Dialogs.Wpf;
using PackageAnalyzer.Core.FileSystem;
using PackageAnalyzer.Data;
using System.Windows;
using System.Diagnostics;
using PSS.Telemetry;
using PackageAnalyzer.Telemetry;
using PackageAnalyzer.Configuration;
using System.Windows.Documents;
using System.Threading.Tasks;
using System.Collections.Specialized;

namespace PackageAnalyzer
{
    public interface ISelectableItem
    {
        string Name { get; set; }
        bool IsChecked { get; set; }
    }
    public class KeySettingItem : ISelectableItem
    {
        public string Name { get; set; }
        public bool IsChecked { get; set; }
    }
    public class ExcludedCustomTypeItem : ISelectableItem
    {
        public string Name { get; set; }
        public bool IsChecked { get; set; }
    }
    public partial class MainWindow : Window
    {
        ObservableCollection<SitecoreData> dataToShow;
        List<string> uploadedFiles = new List<string>();
        public ObservableCollection<KeySettingItem> KeySettings { get; set; } = new ObservableCollection<KeySettingItem>();
        public ObservableCollection<ExcludedCustomTypeItem> ExcludedCustomTypes { get; set; } = new ObservableCollection<ExcludedCustomTypeItem>();
        private string currentConfigSection = "DefaultSettingsFiles";

        public MainWindow()
        {
            dataToShow = new ObservableCollection<SitecoreData>();
            ApplicationManager.GetStartedLogMainInfo();
            InitializeComponent();

            SitecoreDataGrid.Loaded += (_, __) => AutoSizeColumns(SitecoreDataGrid);
            SitecoreDataGrid.SizeChanged += (_, __) => AutoSizeColumns(SitecoreDataGrid);

            if (SitecoreDataGrid.Items is INotifyCollectionChanged incc)
                incc.CollectionChanged += (_, __) => AutoSizeColumns(SitecoreDataGrid);
            TrackAppRun();
            ArchiveProvider.CleanupTempFiles();
            DataContext = this;  // Set the DataContext here
            LoadKeySettings();
        }

        public async Task InitializeAsync(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                if (args[0].Contains("-UpdateRegistry"))
                {
                    UpdateRegistry_Click(new object(), new RoutedEventArgs());
                    MessageBox.Show("Added/Updated registry entries to enable context menu option to 'Open with Package Analyzer' on *.zip, *.7z, *.rar files and folder in windows explorer", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else if (args[0].Contains("-ClearRegistry"))
                {
                    ClearRegistry_Click(new object(), new RoutedEventArgs());
                    MessageBox.Show("Cleared registry entries to enable context menu option to 'Open with Package Analyzer' on *.zip, *.7z, *.rar files and folder in windows explorer", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    uploadedFiles.AddRange(args);
                    await ProcessUploadedFilesOrFolderAsync(args);
                }
            }
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Clean up and remove the temporary folder
            ApplicationManager.GetFinishedLogMainInfo();
            ArchiveProvider.CleanupTempFiles();
        }

        private void TrackAppRun()
        {
            try
            {
                TelemetryMetadataProvider telemetryMetadataProvider = new TelemetryMetadataProvider();
                TelemetryLogger telemetryLogger = new TelemetryLogger();
                TelemetryManager telemetryManager = new TelemetryManager(telemetryMetadataProvider, telemetryLogger);
                telemetryManager.TrackAppRun();
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
            }
        }

        private async void FileOrFolder_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                string[] droppedFiles = e.Data.GetData(DataFormats.FileDrop, true) as string[];

                if (droppedFiles != null)
                {
                    await ProcessUploadedFilesOrFolderAsync(droppedFiles);
                    uploadedFiles.AddRange(droppedFiles);
                }

            }
        }

        #region ProcessCheckboxes
        private async Task ProcessCheckboxesAsync(string filePath)
        {
            string fileOrFolderPath = filePath;
            if (dataToShow.Count() > 0)
            {
                dataToShow.Add(new SitecoreData());
            }
            SitecoreData data = new SitecoreData
            {
                Identifier = "Package",
                Value = Path.GetFileName(filePath)
            };
            dataToShow.Add(data);

            var selected = CheckBoxPanel.Children.OfType<CheckBox>().Where(c => c.IsChecked == true).Select(c => c.Content.ToString()).ToList();

            foreach (var option in selected)
            {
                switch (option)
                {
                    case "Sitecore (pre-release) version":
                        await ProcessCheckboxAsync("Sitecore (pre-release) version", fileOrFolderPath, PackageAnalyzerAdapter.GetSitecoreVersions);
                        break;

                    case "Sitecore roles from web.config":
                        await ProcessCheckboxAsync("Sitecore roles from web.config", fileOrFolderPath, PackageAnalyzerAdapter.GetSitecoreRoles);
                        break;

                    case "Installed modules":
                        await ProcessCheckboxAsync("Installed modules", fileOrFolderPath, PackageAnalyzerAdapter.GetSitecoreModules);
                        break;

                    case "Hotfixes installed":
                        break;

                    case "Assembly versions":
                        await ProcessCheckboxAsync("Sitecore assembly versions list", fileOrFolderPath, PackageAnalyzerAdapter.GetSitecoreAssemblyVersions);
                        break;

                    case "Key settings values":
                        await ProcessCheckboxAsync("Sitecore settings list", fileOrFolderPath, PackageAnalyzerAdapter.GetSitecoreSettings);
                        break;

                    case "Topology (XM/XP)":
                        await ProcessCheckboxWithExceptionHandlingAsync("Topology (XM/XP)", fileOrFolderPath, CheckTopology);
                        break;

                    default:
                        break;
                }
            }

            ConnectionStringsRichTextBox.Document.Blocks.Clear();
            CustomTypesXmlRichTextBox.Document.Blocks.Clear();
            var prettifyXml = new PrettifyXml();
            var customTypes = await Task.Run(() => PackageAnalyzerAdapter.GetCustomTypes(fileOrFolderPath));
            var connectionStrings = await Task.Run(() => PackageAnalyzerAdapter.GetConfigFile(fileOrFolderPath, "ConnectionStrings.config"));
            CustomTypesXmlRichTextBox.Document.Blocks.Add(prettifyXml.DisplayXmlWithHighlighting(customTypes));
            ConnectionStringsRichTextBox.Document.Blocks.Add(prettifyXml.DisplayXmlWithHighlighting(connectionStrings));
        }

        private async Task ProcessCheckboxAsync(string identifier, string filePath, Func<string, object> valueProvider)
        {
            object value = await Task.Run(() => valueProvider(filePath));
            SitecoreData data = new SitecoreData
            {
                Identifier = identifier,
                Value = value
            };
            dataToShow.Add(data);
        }

        private async Task ProcessCheckboxWithExceptionHandlingAsync(string identifier, string filePath, Func<string, string> valueProvider)
        {
            try
            {
                await ProcessCheckboxAsync(identifier, filePath, fp => valueProvider(fp));
            }
            catch (Exception ex)
            {
                Log.Error($"Error analyzing the provided package: {ex.Message}:\r\n{ex.StackTrace}");
                MessageBox.Show($"Error analyzing the provided package: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private string CheckTopology(string filePath)
        {
            TopologyChecker topologyChecker = new TopologyChecker();
            return topologyChecker.CheckTopology(filePath);
        }

        #endregion

        private async void FileListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            dataToShow.Clear();
            var selectedFile = fileListBox.SelectedItem?.ToString();
            if (selectedFile != null)
            {
                var matchingFile = uploadedFiles.FirstOrDefault(file => file.Contains(selectedFile));
                if (matchingFile != null)
                {
                    string filePath = matchingFile;
                    if (Directory.Exists(Path.Combine(Path.GetTempPath(), "SearchArchiveTemp", Path.GetFileNameWithoutExtension(selectedFile))))  
                    {
                        filePath = Path.Combine(Path.GetTempPath(), "SearchArchiveTemp", Path.GetFileNameWithoutExtension(selectedFile));
                    }
                    else
                    {
                        filePath = PackageAnalyzerAdapter.InitialUnzipping(matchingFile);
                    } 
                    if (!IsMultipleRolesPackage(filePath))
                    {
                        await ProcessCheckboxesAsync(filePath);
                    }
                }
            }
        }


        private void AddFilesToListBox(IEnumerable<string> files)
        {
            List<string> currentFileNames = fileListBox.Items.Cast<string>().ToList();

            // Extract new file names from the provided files
            List<string> newFileNames = files.Select(file => Path.GetFileName(file)).ToList();

            // Merge existing and new file names
            List<string> mergedFileNames = currentFileNames.Union(newFileNames).ToList();

            // Bind the merged file names to the ListBox
            Binding binding = new Binding();
            binding.Source = mergedFileNames;
            fileListBox.SetBinding(ItemsControl.ItemsSourceProperty, binding);
            fileListBox.SelectedIndex = fileListBox.Items.Count - 1;
        }

        private async void UploadFiles_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog openFileDialog = new VistaOpenFileDialog();
            openFileDialog.Multiselect = true; // Allow multiple file selection
            if (openFileDialog.ShowDialog() == true)
            {
                string[] filePaths = openFileDialog.FileNames;
                await ProcessUploadedFilesOrFolderAsync(filePaths);
                uploadedFiles.AddRange(filePaths);
            }
        }

        private async void UploadFolders_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog folderBrowserDialog = new VistaFolderBrowserDialog();
            folderBrowserDialog.Multiselect = true;
            folderBrowserDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads" + @"\"; // Default starting directory
            folderBrowserDialog.ShowNewFolderButton = false; // Hide the "New Folder" button

            if (folderBrowserDialog.ShowDialog() == true)
            {
                string[] selectedFolders =  folderBrowserDialog.SelectedPaths;
                await ProcessUploadedFilesOrFolderAsync(selectedFolders);
                uploadedFiles.AddRange(selectedFolders);
            }
        }

        private async Task ProcessUploadedFilesOrFolderAsync(string[] uploadedFilesOrFolders)
        {
            // Guard clause for null or empty input
            if (uploadedFilesOrFolders == null || uploadedFilesOrFolders.Length == 0)
            {
                throw new ArgumentException("No files or folders uploaded.", nameof(uploadedFilesOrFolders));
            }

            // Add files or folders to the list box
            AddFilesToListBox(uploadedFilesOrFolders);

            // Get the first item in the array
            string firstItem = uploadedFilesOrFolders.FirstOrDefault();

            // Guard clause for null firstItem
            if (firstItem == null)
            {
                throw new InvalidOperationException("First item in the array is null.");
            }
            // Process checkboxes only if the package is not a multiple roles package
            string filePath = await Task.Run(() => PackageAnalyzerAdapter.InitialUnzipping(firstItem));
            if (!IsMultipleRolesPackage(filePath))
            {
                await ProcessCheckboxesAsync(filePath);
            }
            AutoSizeColumns(SitecoreDataGrid);
        }
        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            dataToShow.Clear();
            SitecoreRoleComboBox.ItemsSource = null;
            var matchingFile = uploadedFiles.FirstOrDefault();
            if (matchingFile != null)
            {
                string filePath = PackageAnalyzerAdapter.InitialUnzipping(matchingFile);
                if (!IsMultipleRolesPackage(filePath))
                {
                    await ProcessCheckboxesAsync(filePath);
                }
            }
        }
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            dataToShow.Clear();
            SitecoreRoleComboBox.ItemsSource = null;
            CustomTypesXmlRichTextBox.Document.Blocks.Clear();
        }
        private void ClearRegistry_Click(object sender, RoutedEventArgs e)
        {
            string errorMessage = string.Empty;
            try
            {
                RegistryManager.TryClearShellIntegration(out errorMessage);
            }
            catch (Exception ex)
            {
                Log.Error(errorMessage);
                Log.Error(ex.Message);
                MessageBox.Show($"Error clearing the registry: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateRegistry_Click(object sender, RoutedEventArgs e)
        {
            string errorMessage = string.Empty;
            try
            {
                RegistryManager.TryUpdateShellIntegration(out errorMessage);
            }
            catch (Exception ex)
            {
                Log.Error(errorMessage);
                Log.Error(ex.Message);
                MessageBox.Show($"Error updating the registry: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }
        private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OpenLogAnalyzerButton.IsEnabled = fileListBox.SelectedItem != null;
            OpenShowconfig.IsEnabled = fileListBox.SelectedItem != null;
        }
        private void OpenLogAnalyzer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string filePath = LogAnalyzerManager.GetSelectedFilePath(SitecoreRoleComboBox.SelectedValue?.ToString(), fileListBox.SelectedItem?.ToString(), uploadedFiles);
                if (string.IsNullOrEmpty(filePath))
                {
                    MessageBox.Show("Logs folder not found", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var logsFolder = LogAnalyzerManager.FindLogsFolder(filePath, uploadedFiles);
                if (string.IsNullOrEmpty(logsFolder))
                {
                    MessageBox.Show("Logs folder not found", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                MessageBox.Show($"Error opening Log Analyzer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenShowconfig_Click(object sender, RoutedEventArgs e)
        {
            var filePath = SitecoreRoleComboBox.SelectedValue?.ToString();
            var filePathFromListBox = uploadedFiles.FirstOrDefault(file => file.Contains(fileListBox.SelectedItem?.ToString()));

            string targetFilePath = null;

            if (!string.IsNullOrEmpty(filePath))
            {
                // Check "ShowConfig.xml" in the selected directory
                targetFilePath = new FileManager(filePath).FindFiles("ShowConfig.xml", true).FirstOrDefault()?.ToString();
            }
            else if (!string.IsNullOrEmpty(filePathFromListBox))
            {
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
            }

            if (!string.IsNullOrEmpty(targetFilePath))
            {
                // Open the existing ShowConfig.xml
                Process.Start(new ProcessStartInfo(targetFilePath) { UseShellExecute = true });
            }
            else
            {
                MessageBox.Show("Invalid file path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsMultipleRolesPackage(string filePath)
        {
            var sitecoreRolesAndFolders = PackageAnalyzerAdapter.GetSitecoreRolesForComboBox(filePath);
            // Ensure the list is not empty before populating the ComboBox
            if (sitecoreRolesAndFolders.Count > 0)
            {
                SitecoreRoleComboBox.ItemsSource = sitecoreRolesAndFolders;
                SitecoreRoleComboBox.DisplayMemberPath = "Value";
                SitecoreRoleComboBox.SelectedValuePath = "Key";
                SitecoreRoleComboBox.SelectedIndex = 0;
                return true;
            }
            else
            {
                return false;
            }
        }

        private async void SitecoreRoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SitecoreRoleComboBox.SelectedItem != null)
            {
                dataToShow.Clear();
                await ProcessCheckboxesAsync(SitecoreRoleComboBox.SelectedValue.ToString());
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

        private void LoadExcludedCustomTypes()
        {
            ExcludedCustomTypes.Clear();
            var types = new PackageAnalyzerConfiguration().GetSection("ExcludeAssembliesThatStartFrom");
            foreach (var type in types)
            {
                ExcludedCustomTypes.Add(new ExcludedCustomTypeItem
                {
                    Name = type,
                    IsChecked = true
                });
            }
        }

        private void PopupItem_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is ISelectableItem item)
            {
                item.IsChecked = true;
                new PackageAnalyzerConfiguration().AddToSection(currentConfigSection, item.Name);
            }
        }

        private void PopupItem_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is ISelectableItem item)
            {
                item.IsChecked = false;
                new PackageAnalyzerConfiguration().RemoveFromSection(currentConfigSection, item.Name);
            }
        }

        private void SelectSettingsCategories_Click(object sender, RoutedEventArgs e)
        {
            SettingsListBox.ItemsSource = KeySettings;
            currentConfigSection = "DefaultSettingsFiles";
            AddCustomTypePanel.Visibility = Visibility.Collapsed;
            SettingsPopup.IsOpen = true;
        }

        private void ClosePopup(object sender, RoutedEventArgs e)
        {
            // Close the popup
            SettingsPopup.IsOpen = false;
            Refresh_Click(sender, e);
        }
        private void AutoSizeColumns(ListView listView)
        {
            SitecoreDataGrid.ItemsSource = dataToShow;
            SitecoreDataGrid.UpdateLayout();
            if (listView.View is GridView gv)
            {
                foreach (var col in gv.Columns)
                {
                    col.Width = 0;              // force re-measure
                    col.Width = double.NaN;     // autosize to content
                }
            }
        }

        private void ExcludedCustomTypesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LoadExcludedCustomTypes();
            SettingsListBox.ItemsSource = ExcludedCustomTypes;
            currentConfigSection = "ExcludeAssembliesThatStartFrom";
            AddCustomTypePanel.Visibility = Visibility.Visible;
            SettingsPopup.IsOpen = true;
        }

        private void AddCustomType_Click(object sender, RoutedEventArgs e)
        {
            var newType = NewCustomTypeTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(newType))
            {
                var config = new PackageAnalyzerConfiguration();
                config.AddToSection("ExcludeAssembliesThatStartFrom", newType);
                ExcludedCustomTypes.Add(new ExcludedCustomTypeItem { Name = newType, IsChecked = true });
                NewCustomTypeTextBox.Clear();
            }
        }
    }

}
