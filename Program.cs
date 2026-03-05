using System;
using System.IO;
using Avalonia;
using BioWare.Resource.Formats.NCS.Decomp;
using File = BioWare.Resource.Formats.NCS.Decomp.NcsFile;

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
            // Parse command line arguments
            CommandLineArgs cmdlineArgs = ParseArgs(args);

            // Determine if we should run in CLI mode
            // CLI mode is triggered when file paths are provided as arguments
            bool forceCli = cmdlineArgs.Files != null && cmdlineArgs.Files.Length > 0;

            if (forceCli)
            {
                // CLI mode explicitly requested - no GUI
                ExecuteCli(cmdlineArgs);
            }
            else
            {
                // GUI mode by default
                try
                {
                    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
                }
                catch (Exception ex)
                {
                    // If GUI is unavailable, print error and exit gracefully
                    Console.Error.WriteLine($"[Warning] Display driver not available, cannot run in GUI mode: {ex.Message}");
                    Console.Error.WriteLine("[Info] Use file paths as arguments for CLI mode");
                    Environment.Exit(1);
                }
            }
        }

        private static CommandLineArgs ParseArgs(string[] args)
        {
            var result = new CommandLineArgs();
            var fileList = new System.Collections.Generic.List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--output-dir" when i + 1 < args.Length:
                        result.OutputDir = args[++i];
                        break;
                    case "--help":
                        result.Help = true;
                        break;
                    default:
                        // Treat as file path if it doesn't start with --
                        if (!args[i].StartsWith("--"))
                        {
                            fileList.Add(args[i]);
                        }
                        break;
                }
            }

            result.Files = fileList.ToArray();
            return result;
        }

        private static void ExecuteCli(CommandLineArgs args)
        {
            if (args.Help)
            {
                Console.WriteLine("KNCSDecomp - NCS File Decompiler");
                Console.WriteLine("Usage: KNCSDecomp [options] <file1.ncs> [file2.ncs ...]");
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine("  --output-dir <path>  Output directory for decompiled files");
                Console.WriteLine("  --help               Show this help message");
                Console.WriteLine();
                Console.WriteLine("If no file arguments are provided, GUI mode will be used.");
                Environment.Exit(0);
                return;
            }

            if (args.Files == null || args.Files.Length == 0)
            {
                Console.Error.WriteLine("[Error] No files specified for CLI mode");
                Console.Error.WriteLine("Use --help for usage information");
                Environment.Exit(1);
                return;
            }

            // Initialize settings
            Decompiler.settings = new Settings();
            Decompiler.settings.Load();

            // Set output directory if provided
            if (!string.IsNullOrEmpty(args.OutputDir))
            {
                if (!Directory.Exists(args.OutputDir))
                {
                    try
                    {
                        Directory.CreateDirectory(args.OutputDir);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Error] Cannot create output directory '{args.OutputDir}': {ex.Message}");
                        Environment.Exit(1);
                        return;
                    }
                }
                Decompiler.settings.SetProperty("Output Directory", args.OutputDir);
            }

            // Create decompiler
            var decompiler = new FileDecompiler();

            int successCount = 0;
            int errorCount = 0;

            foreach (string filePath in args.Files)
            {
                try
                {
                    if (!System.IO.File.Exists(filePath))
                    {
                        Console.Error.WriteLine($"[Error] File not found: {filePath}");
                        errorCount++;
                        continue;
                    }

                    File ncsFile = new File(filePath);
                    Console.WriteLine($"[Info] Decompiling: {filePath}");

                    int result = decompiler.Decompile(ncsFile);
                    if (result >= 1)
                    {
                        string generatedCode = decompiler.GetGeneratedCode(ncsFile);
                        if (generatedCode != null)
                        {
                            // Output to file or console
                            string outputPath = Path.ChangeExtension(filePath, ".nss");
                            if (!string.IsNullOrEmpty(args.OutputDir))
                            {
                                string fileName = Path.GetFileName(outputPath);
                                outputPath = Path.Combine(args.OutputDir, fileName);
                            }

                            System.IO.File.WriteAllText(outputPath, generatedCode);
                            Console.WriteLine($"[Info] Decompiled code written to: {outputPath}");
                            successCount++;
                        }
                        else
                        {
                            Console.Error.WriteLine($"[Warning] No code generated for: {filePath}");
                            errorCount++;
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine($"[Error] Decompilation failed for: {filePath}");
                        errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Error] Exception decompiling {filePath}: {ex.GetType().Name}: {ex.Message}");
                    errorCount++;
                }
            }

            Console.WriteLine($"[Info] Completed: {successCount} succeeded, {errorCount} failed");
            Environment.Exit(errorCount > 0 ? 1 : 0);
        }

        private class CommandLineArgs
        {
            public string[] Files { get; set; }
            public string OutputDir { get; set; }
            public bool Help { get; set; }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
