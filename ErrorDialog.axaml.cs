using System;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace KNCSDecomp
{
    public partial class ErrorDialog : Window
    {
        public string ErrorMessage { get; set; }
        public string ExceptionDetails { get; set; }

        public ErrorDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        public ErrorDialog(string errorMessage, Exception exception)
            : this()
        {
            ErrorMessage = errorMessage ?? "An unexpected error occurred.";
            ExceptionDetails = FormatException(exception);
        }

        public static void Show(Window parent, string errorMessage, Exception exception)
        {
            var dialog = new ErrorDialog(errorMessage, exception);
            dialog.ShowDialog(parent);
        }

        public static void ShowAsync(Window parent, string errorMessage, Exception exception)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var dialog = new ErrorDialog(errorMessage, exception);
                dialog.ShowDialog(parent);
            });
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void OnCopyClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    if (desktop.MainWindow?.Clipboard != null)
                    {
                        await desktop.MainWindow.Clipboard.SetTextAsync(ExceptionDetails);
                    }
                }
            }
            catch
            {
                // Ignore clipboard errors
            }
        }

        private static string FormatException(Exception exception)
        {
            if (exception == null)
            {
                return "No exception details available.";
            }

            var sb = new StringBuilder();
            sb.AppendLine("Exception Type: " + exception.GetType().FullName);
            sb.AppendLine("Message: " + exception.Message);
            sb.AppendLine();
            sb.AppendLine("Stack Trace:");
            sb.AppendLine(exception.StackTrace ?? "No stack trace available.");

            if (exception.InnerException != null)
            {
                sb.AppendLine();
                sb.AppendLine("Inner Exception:");
                sb.AppendLine(FormatException(exception.InnerException));
            }

            return sb.ToString();
        }
    }
}

