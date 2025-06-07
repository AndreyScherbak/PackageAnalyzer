using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Serilog;
using Serilog.Core;

namespace PackageAnalyzer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Get the directory where the executable resides
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var exeDirectory = System.IO.Path.GetDirectoryName(exePath);
            var logFilePath = System.IO.Path.Combine(exeDirectory, "logs", "log..txt");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(
                    path: logFilePath,
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                    rollOnFileSizeLimit: true)
                .CreateLogger();
            if (e.Args != null && (e.Args.Length == 0 || (e.Args.Length > 0 && !e.Args[0].Contains("-ClearRegistry") && !e.Args[0].Contains("-UpdateRegistry"))))
            {
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                DispatcherUnhandledException += App_DispatcherUnhandledException;
            }
            MainWindow mainWindow = new MainWindow(e.Args);
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            Log.Error(ex.Message);
            Log.Error(ex.StackTrace);
            MessageBox.Show("A fatal error occurred. The application will close.");
            Current.Shutdown();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception.Message);
            Log.Error(e.Exception.StackTrace);
            e.Handled = true;
            MessageBox.Show("An unexpected error occurred: " + e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Current.Shutdown();
        }
    }
}
