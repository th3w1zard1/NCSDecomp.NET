using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace KNCSDecomp
{
    public partial class App : Application
    {
        static App()
        {
            // Disable Avalonia telemetry to avoid crashes when the default telemetry
            // directory is missing (seen in headless test runs).
            const string telemetryOptOut = "AVALONIA_TELEMETRY_OPTOUT";
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(telemetryOptOut)))
            {
                Environment.SetEnvironmentVariable(telemetryOptOut, "1");
            }

            // Ensure the telemetry folder exists in case the opt-out is ignored.
            try
            {
                string telemetryDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ".avalonia-build-tasks");
                if (!Directory.Exists(telemetryDir))
                {
                    Directory.CreateDirectory(telemetryDir);
                }
            }
            catch
            {
                // Ignore failures; the opt-out should prevent telemetry access.
            }
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            SetupGlobalExceptionHandling();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void SetupGlobalExceptionHandling()
        {
            // Handle unhandled exceptions from main thread
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // Handle unobserved task exceptions
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // Handle Avalonia-specific exceptions
            Dispatcher.UIThread.UnhandledException += OnUIThreadUnhandledException;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                HandleException("An unhandled exception occurred", exception);
            }
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved(); // Prevent application crash
            if (e.Exception?.InnerException != null)
            {
                HandleException("An unobserved task exception occurred", e.Exception.InnerException);
            }
            else if (e.Exception != null)
            {
                HandleException("An unobserved task exception occurred", e.Exception);
            }
        }

        private void OnUIThreadUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true; // Prevent application crash
            HandleException("An unhandled UI exception occurred", e.Exception);
        }

        private void HandleException(string context, Exception exception)
        {
            // Ensure we're on the UI thread
            if (Dispatcher.UIThread.CheckAccess())
            {
                ShowErrorDialog(context, exception);
            }
            else
            {
                Dispatcher.UIThread.Post(() => ShowErrorDialog(context, exception));
            }
        }

        private void ShowErrorDialog(string context, Exception exception)
        {
            try
            {
                Window parentWindow = null;
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    parentWindow = desktop.MainWindow;
                }

                ErrorDialog.Show(parentWindow, context, exception);
            }
            catch
            {
                // If showing dialog fails, at least try to log to console
                System.Console.WriteLine("FATAL ERROR: " + context);
                System.Console.WriteLine(exception?.ToString() ?? "No exception details");
            }
        }
    }
}
