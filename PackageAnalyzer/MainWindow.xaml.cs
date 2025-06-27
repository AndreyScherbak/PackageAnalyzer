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

namespace PackageAnalyzer
{
    public class KeySettingItem
    {
        public string Name { get; set; }
        public bool IsChecked { get; set; }
    }
    public partial class MainWindow : Window
    {
        ObservableCollection<SitecoreData> dataToShow;
        List<string> uploadedFiles = new List<string>();
        public ObservableCollection<KeySettingItem> KeySettings { get; set; } = new ObservableCollection<KeySettingItem>();

        public MainWindow(string[] args)
        {  
            dataToShow = new ObservableCollection<SitecoreData>();
            ApplicationManager.GetStartedLogMainInfo();
            InitializeComponent();
            TrackAppRun();
            ArchiveProvider.CleanupTempFiles();
            DataContext = this;  // Set the DataContext here
            LoadKeySettings();
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
                    ProcessUploadedFilesOrFolder(args);
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

        private void FileOrFolder_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                string[] droppedFiles = e.Data.GetData(DataFormats.FileDrop, true) as string[];

                if (droppedFiles != null)
                {
                    ProcessUploadedFilesOrFolder(droppedFiles);
                    uploadedFiles.AddRange(droppedFiles);
                }

            }
        }

        #region ProcessCheckboxes
        private void ProcessCheckboxes(string filePath)
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
            foreach (var child in CheckBoxPanel.Children.OfType<CheckBox>().Where(c => c.IsChecked == true))
            {
                switch (child.Content.ToString())
                {
                    case "Sitecore (pre-release) version":
                        ProcessCheckbox("Sitecore (pre-release) version", fileOrFolderPath, PackageAnalyzerAdapter.GetSitecoreVersions);
                        break;

                    case "Sitecore roles from web.config":
                        ProcessCheckbox("Sitecore roles from web.config", fileOrFolderPath, PackageAnalyzerAdapter.GetSitecoreRoles);
                        break;

                    case "Installed modules":
                        ProcessCheckbox("Installed modules", fileOrFolderPath, PackageAnalyzerAdapter.GetSitecoreModules);
                        break;

                    case "Hotfixes installed":
                        // Process logic for "Hotfixes installed" checkbox
                        // Add your logic here
                        break;

                    case "Assembly versions":
                        ProcessCheckbox("Sitecore assembly versions list", fileOrFolderPath, PackageAnalyzerAdapter.GetSitecoreAssemblyVersions);
                        // Process logic for "Assembly versions" checkbox
                        // Add your logic here
                        break;

                    case "Key settings values":
                        ProcessCheckbox("Sitecore settings list", fileOrFolderPath, PackageAnalyzerAdapter.GetSitecoreSettings);
                        // Add your logic here
                        break;

                    case "Topology (XM/XP)":
                        ProcessCheckboxWithExceptionHandling("Topology (XM/XP)", fileOrFolderPath, CheckTopology);
                        break;

                    // Add more cases for additional checkboxes if needed

                    default:
                        // Handle any other checkboxes not covered in cases
                        break;
                }
            }
            ConnectionStringsRichTextBox.Document.Blocks.Clear();
            CustomTypesXmlRichTextBox.Document.Blocks.Clear();
            var prettifyXml = new PrettifyXml();
            CustomTypesXmlRichTextBox.Document.Blocks.Add(prettifyXml.DisplayXmlWithHighlighting(PackageAnalyzerAdapter.GetCustomTypes(fileOrFolderPath)));
            ConnectionStringsRichTextBox.Document.Blocks.Add(prettifyXml.DisplayXmlWithHighlighting(PackageAnalyzerAdapter.GetConfigFile(fileOrFolderPath, "ConnectionStrings.config")));

            //return dataToShow;
        }

        private void ProcessCheckbox(string identifier, string filePath, Func<string, object> valueProvider)
        {
            SitecoreData data = new SitecoreData
            {
                Identifier = identifier,
                Value = valueProvider(filePath)
            };
            dataToShow.Add(data);
        }

        private void ProcessCheckboxWithExceptionHandling(string identifier, string filePath, Func<string, string> valueProvider)
        {
            try
            {
                ProcessCheckbox(identifier, filePath, valueProvider);
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

        private void AddRowToDataGrid()
        {
            SitecoreDataGrid.ItemsSource = dataToShow;
        }

        private void FileListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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
                        ProcessCheckboxes(filePath);
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

        private void UploadFiles_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog openFileDialog = new VistaOpenFileDialog();
            openFileDialog.Multiselect = true; // Allow multiple file selection
            if (openFileDialog.ShowDialog() == true)
            {
                string[] filePaths = openFileDialog.FileNames;
                ProcessUploadedFilesOrFolder(filePaths);
                uploadedFiles.AddRange(filePaths);
            }
        }

        private void UploadFolders_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog folderBrowserDialog = new VistaFolderBrowserDialog();
            folderBrowserDialog.Multiselect = true;
            folderBrowserDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads" + @"\"; // Default starting directory
            folderBrowserDialog.ShowNewFolderButton = false; // Hide the "New Folder" button

            if (folderBrowserDialog.ShowDialog() == true)
            {
                string[] selectedFolders =  folderBrowserDialog.SelectedPaths;
                ProcessUploadedFilesOrFolder(selectedFolders);
                uploadedFiles.AddRange(selectedFolders);
            }
        }

        private void ProcessUploadedFilesOrFolder(string[] uploadedFilesOrFolders)
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
            string filePath = PackageAnalyzerAdapter.InitialUnzipping(firstItem);
            if (!IsMultipleRolesPackage(filePath))
            {
                ProcessCheckboxes(filePath);
            }

            // Add a row to the data grid
            AddRowToDataGrid();
        }
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            dataToShow.Clear();
            SitecoreRoleComboBox.ItemsSource = null;
            var matchingFile = uploadedFiles.FirstOrDefault();
            if (matchingFile != null)
            {
                string filePath = PackageAnalyzerAdapter.InitialUnzipping(matchingFile);
                if (!IsMultipleRolesPackage(filePath))
                {
                    ProcessCheckboxes(filePath);
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

        private void SitecoreRoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if there is a selected item
            if (SitecoreRoleComboBox.SelectedItem != null)
            {
                dataToShow.Clear();
                ProcessCheckboxes(SitecoreRoleComboBox.SelectedValue.ToString());
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

        private void KeySetting_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is KeySettingItem setting)
            {
                setting.IsChecked = true; // Update the setting state
                new PackageAnalyzerConfiguration().AddToSection("DefaultSettingsFiles", setting.Name); // Apply changes
            }
        }

        private void KeySetting_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is KeySettingItem setting)
            {
                setting.IsChecked = false; // Update the setting state
                new PackageAnalyzerConfiguration().RemoveFromSection("DefaultSettingsFiles", setting.Name); // Apply changes
            }
        }

        private void SelectSettingsCategories_Click(object sender, RoutedEventArgs e)
        {
            // Open the popup in the center
            SettingsPopup.IsOpen = true;
        }

        private void ClosePopup(object sender, RoutedEventArgs e)
        {
            // Close the popup
            SettingsPopup.IsOpen = false;
            Refresh_Click(sender, e);
        } 
    }

}
