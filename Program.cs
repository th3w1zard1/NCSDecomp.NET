using System;
using Avalonia;

namespace KNCSDecomp
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            // Any command-line arguments trigger CLI mode.
            // GUI mode is only used when launched without arguments.
            if (args != null && args.Length > 0)
            {
                int exitCode = NCSDecompCLI.Run(args);
                Environment.Exit(exitCode);
                return;
            }

            // GUI mode by default
            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                // If GUI is unavailable, print error and exit gracefully
                Console.Error.WriteLine("[Warning] Display driver not available, cannot run in GUI mode: " + ex.Message);
                Console.Error.WriteLine("[Info] Use CLI arguments for command-line mode");
                Environment.Exit(1);
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
