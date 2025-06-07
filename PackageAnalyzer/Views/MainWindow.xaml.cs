using PackageAnalyzer.Core;
using PackageAnalyzer.Core.FileSystem;
using PackageAnalyzer.ViewModels;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace PackageAnalyzer.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel, string[] args)
        {
            ApplicationManager.GetStartedLogMainInfo();
            InitializeComponent();
            ArchiveProvider.CleanupTempFiles();
            _viewModel = viewModel;
            DataContext = _viewModel;
            _viewModel.HandleStartupArgs(args);
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            ApplicationManager.GetFinishedLogMainInfo();
            ArchiveProvider.CleanupTempFiles();
        }

        private void FileOrFolder_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                if (e.Data.GetData(DataFormats.FileDrop, true) is string[] droppedFiles && droppedFiles.Any())
                {
                    _viewModel.ProcessUploadedFilesOrFolder(droppedFiles);
                }
            }
        }

        private void FileListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox lb && lb.SelectedItem is string name)
            {
                _viewModel.LoadSelected(name);
            }
        }


        private void SelectSettingsCategories_Click(object sender, RoutedEventArgs e)
        {
            SettingsPopup.IsOpen = true;
        }

        private void ClosePopup(object sender, RoutedEventArgs e)
        {
            SettingsPopup.IsOpen = false;
            _viewModel.RefreshCommand.Execute(null);
        }
    }
}
