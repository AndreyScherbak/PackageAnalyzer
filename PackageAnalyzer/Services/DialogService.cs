using System.Windows;

namespace PackageAnalyzer.Services
{
    public class DialogService : IDialogService
    {
        public void ShowMessage(string message, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            MessageBox.Show(message, caption, button, icon);
        }
    }
}
