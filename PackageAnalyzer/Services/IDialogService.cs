using System.Windows;
namespace PackageAnalyzer.Services
{
    public interface IDialogService
    {
        void ShowMessage(string message, string caption, MessageBoxButton button, MessageBoxImage icon);
    }
}
