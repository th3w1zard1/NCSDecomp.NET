using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BioWare.Resource.Formats.NCS.Decomp;
using File = BioWare.Resource.Formats.NCS.Decomp.NcsFile;

namespace KNCSDecomp
{
    /// <summary>
    /// Headless CLI entry point for NCS decompilation.
    /// Mirrors Java DeNCS CLI option set and behavior.
    /// </summary>
    internal static class NCSDecompCLI
    {
        internal static int Run(string[] args)
        {
            CliConfig cfg;
            try
            {
                cfg = ParseArgs(args);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                PrintUsage();
                return 1;
            }

            if (cfg.Help)
            {
                PrintUsage();
                return 0;
            }

            if (cfg.Version)
            {
                PrintVersion();
                return 0;
            }

            if (cfg.Inputs.Count == 0)
            {
                Console.Error.WriteLine("Error: at least one input .ncs file or directory is required.");
                PrintUsage();
                return 1;
            }

            File nwscriptFile = ResolveNwscript(cfg);
            if (nwscriptFile == null)
            {
                return 1;
            }

            // Apply CLI flags
            FileDecompiler.isK2Selected = cfg.IsK2;
            FileDecompiler.preferSwitches = cfg.PreferSwitches;
            FileDecompiler.strictSignatures = cfg.StrictSignatures;

            // Collect files
            List<InputFile> worklist = BuildWorklist(cfg);
            if (worklist.Count == 0)
            {
                Console.Error.WriteLine("No .ncs files found to decompile.");
                return 1;
            }

            FileSystemInfo outputFileOrDir = ResolveOutputRoot(cfg, worklist.Count);
            if (outputFileOrDir == null)
            {
                return 1;
            }

            int failureCount = 0;
            try
            {
                var decompiler = new FileDecompiler(nwscriptFile);
                foreach (InputFile input in worklist)
                {
                    try
                    {
                        File ncsFile = new File(input.Source.FullName);
                        if (cfg.Stdout)
                        {
                            string code = decompiler.DecompileToString(ncsFile);
                            Console.WriteLine("// " + input.Source.Name);
                            Console.WriteLine(code ?? string.Empty);
                        }
                        else
                        {
                            FileInfo outFile = ResolveOutput(input, outputFileOrDir, cfg);
                            if (outFile.Directory != null && !outFile.Directory.Exists)
                            {
                                outFile.Directory.Create();
                            }

                            decompiler.DecompileToFile(
                                ncsFile,
                                new File(outFile.FullName),
                                cfg.Encoding,
                                cfg.Overwrite);

                            if (!cfg.Quiet)
                            {
                                Console.WriteLine("Decompiled " + input.Source.FullName + " -> " + outFile.FullName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Failed to decompile " + input.Source.FullName + ": " + ex.Message);
                        failureCount++;
                        if (cfg.FailFast)
                        {
                            return 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Fatal: " + ex.Message);
                return 1;
            }

            return failureCount > 0 ? 1 : 0;
        }

        private static File ResolveNwscript(CliConfig cfg)
        {
            if (!string.IsNullOrEmpty(cfg.NwscriptPath))
            {
                var explicitFile = new File(Path.GetFullPath(cfg.NwscriptPath));
                if (!explicitFile.IsFile())
                {
                    Console.Error.WriteLine("Error: nwscript file does not exist: " + explicitFile.GetAbsolutePath());
                    return null;
                }
                return explicitFile;
            }

            string cwd = Environment.CurrentDirectory;
            if (!cfg.GameExplicitlySet)
            {
                var generic = new File(Path.Combine(cwd, "nwscript.nss"));
                if (generic.IsFile())
                {
                    return generic;
                }

                Console.Error.WriteLine("Error: nwscript.nss not found in current directory: " + cwd);
                Console.Error.WriteLine();
                Console.Error.WriteLine("Please use one of the following:");
                Console.Error.WriteLine("  - Use --nwscript <path> to specify the nwscript.nss file location");
                Console.Error.WriteLine("  - Use -g k1, -g k2, --k1, or --k2 to select a game");
                Console.Error.WriteLine("  - Ensure nwscript.nss exists in the current directory");
                return null;
            }

            string nssName = cfg.IsK2 ? "tsl_nwscript.nss" : "k1_nwscript.nss";
            var candidates = new List<string>
            {
                Path.Combine(cwd, "tools", nssName),
                Path.Combine(cwd, nssName),
                Path.Combine(AppContext.BaseDirectory, nssName),
                Path.Combine(AppContext.BaseDirectory, "tools", nssName)
            };

            foreach (string candidatePath in candidates)
            {
                var candidate = new File(candidatePath);
                if (candidate.IsFile())
                {
                    return candidate;
                }
            }

            Console.Error.WriteLine("Error: nwscript file not found: " + nssName);
            Console.Error.WriteLine("Searched in:");
            foreach (string candidate in candidates)
            {
                Console.Error.WriteLine("  - " + candidate);
            }
            Console.Error.WriteLine();
            Console.Error.WriteLine("Please use --nwscript <path> to specify the nwscript.nss file location.");
            return null;
        }

        private static List<InputFile> BuildWorklist(CliConfig cfg)
        {
            var worklist = new List<InputFile>();
            var inputDirs = new List<DirectoryInfo>();

            foreach (string input in cfg.Inputs)
            {
                string fullPath = Path.GetFullPath(input);
                if (System.IO.File.Exists(fullPath))
                {
                    var source = new FileInfo(fullPath);
                    if (source.Extension.Equals(".ncs", StringComparison.OrdinalIgnoreCase))
                    {
                        worklist.Add(new InputFile(source, source.Directory));
                    }
                    continue;
                }

                if (Directory.Exists(fullPath))
                {
                    inputDirs.Add(new DirectoryInfo(fullPath));
                    continue;
                }

                Console.Error.WriteLine("Warning: input does not exist, skipping: " + fullPath);
            }

            foreach (DirectoryInfo inputDir in inputDirs)
            {
                Collect(inputDir, cfg.Recursive, worklist, inputDir);
            }

            return worklist;
        }

        private static void Collect(DirectoryInfo directory, bool recursive, List<InputFile> outList, DirectoryInfo baseDir)
        {
            FileInfo[] files;
            try
            {
                files = directory.GetFiles("*.ncs");
            }
            catch
            {
                return;
            }

            foreach (FileInfo file in files)
            {
                outList.Add(new InputFile(file, baseDir));
            }

            if (!recursive)
            {
                return;
            }

            DirectoryInfo[] children;
            try
            {
                children = directory.GetDirectories();
            }
            catch
            {
                return;
            }

            foreach (DirectoryInfo child in children)
            {
                Collect(child, true, outList, baseDir);
            }
        }

        private static FileSystemInfo ResolveOutputRoot(CliConfig cfg, int inputCount)
        {
            if (!string.IsNullOrEmpty(cfg.Output))
            {
                string outputPath = Path.GetFullPath(cfg.Output);
                if (Directory.Exists(outputPath))
                {
                    return new DirectoryInfo(outputPath);
                }

                if (inputCount > 1)
                {
                    if (System.IO.File.Exists(outputPath))
                    {
                        Console.Error.WriteLine("Error: --output must be a directory when processing multiple files: " + outputPath);
                        return null;
                    }

                    try
                    {
                        Directory.CreateDirectory(outputPath);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Error: failed to create output directory: " + outputPath);
                        Console.Error.WriteLine("  " + ex.Message);
                        return null;
                    }

                    return new DirectoryInfo(outputPath);
                }

                if (!System.IO.File.Exists(outputPath))
                {
                    string parent = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
                    {
                        Console.Error.WriteLine("Error: output directory does not exist: " + parent);
                        return null;
                    }
                }

                return new FileInfo(outputPath);
            }

            return new DirectoryInfo(Environment.CurrentDirectory);
        }

        private static FileInfo ResolveOutput(InputFile input, FileSystemInfo outputFileOrDir, CliConfig cfg)
        {
            if (!string.IsNullOrEmpty(cfg.Output) && outputFileOrDir is FileInfo)
            {
                return (FileInfo)outputFileOrDir;
            }

            string baseName = Path.GetFileNameWithoutExtension(input.Source.Name);
            string finalName = cfg.Prefix + baseName + cfg.Suffix + cfg.Extension;

            DirectoryInfo outputDirectory;
            if (!string.IsNullOrEmpty(cfg.Output) && outputFileOrDir is DirectoryInfo)
            {
                outputDirectory = (DirectoryInfo)outputFileOrDir;
                string rel = Path.GetRelativePath(input.BaseDir.FullName, input.Source.FullName);
                string relParent = Path.GetDirectoryName(rel);
                if (!string.IsNullOrEmpty(relParent))
                {
                    outputDirectory = new DirectoryInfo(Path.Combine(outputDirectory.FullName, relParent));
                }
            }
            else if (!string.IsNullOrEmpty(cfg.OutDir))
            {
                outputDirectory = new DirectoryInfo(Path.GetFullPath(cfg.OutDir));
            }
            else
            {
                outputDirectory = outputFileOrDir as DirectoryInfo ?? new DirectoryInfo(Environment.CurrentDirectory);
                if (input.BaseDir != null &&
                    input.Source.DirectoryName != null &&
                    !input.BaseDir.FullName.Equals(input.Source.DirectoryName, StringComparison.OrdinalIgnoreCase))
                {
                    string rel = Path.GetRelativePath(input.BaseDir.FullName, input.Source.FullName);
                    string relParent = Path.GetDirectoryName(rel);
                    if (!string.IsNullOrEmpty(relParent))
                    {
                        outputDirectory = new DirectoryInfo(Path.Combine(outputDirectory.FullName, relParent));
                    }
                }
            }

            return new FileInfo(Path.Combine(outputDirectory.FullName, finalName));
        }

        private static CliConfig ParseArgs(string[] args)
        {
            var cfg = new CliConfig();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                switch (arg)
                {
                    case "-h":
                    case "--help":
                        cfg.Help = true;
                        break;
                    case "-v":
                    case "--version":
                        cfg.Version = true;
                        break;
                    case "-i":
                    case "--input":
                        RequireValue(args, i, arg);
                        cfg.Inputs.Add(args[++i]);
                        break;
                    case "-o":
                    case "--output":
                        RequireValue(args, i, arg);
                        cfg.Output = args[++i];
                        break;
                    case "-O":
                    case "--out-dir":
                    case "--output-dir":
                        RequireValue(args, i, arg);
                        cfg.OutDir = args[++i];
                        break;
                    case "--suffix":
                        RequireValue(args, i, arg);
                        cfg.Suffix = args[++i];
                        break;
                    case "--prefix":
                        RequireValue(args, i, arg);
                        cfg.Prefix = args[++i];
                        break;
                    case "--ext":
                        RequireValue(args, i, arg);
                        string ext = args[++i];
                        cfg.Extension = ext.StartsWith(".") ? ext : "." + ext;
                        break;
                    case "--encoding":
                        RequireValue(args, i, arg);
                        cfg.Encoding = ResolveEncoding(args[++i]);
                        break;
                    case "--nwscript":
                        RequireValue(args, i, arg);
                        cfg.NwscriptPath = args[++i];
                        break;
                    case "--stdout":
                        cfg.Stdout = true;
                        break;
                    case "--overwrite":
                        cfg.Overwrite = true;
                        break;
                    case "-r":
                    case "--recursive":
                        cfg.Recursive = true;
                        break;
                    case "--quiet":
                        cfg.Quiet = true;
                        break;
                    case "--fail-fast":
                        cfg.FailFast = true;
                        break;
                    case "-g":
                    case "--game":
                        RequireValue(args, i, arg);
                        cfg.IsK2 = ParseGame(args[++i]);
                        cfg.GameExplicitlySet = true;
                        break;
                    case "--k1":
                    case "--game=k1":
                        cfg.IsK2 = false;
                        cfg.GameExplicitlySet = true;
                        break;
                    case "--k2":
                    case "--tsl":
                    case "--game=k2":
                    case "--game=tsl":
                        cfg.IsK2 = true;
                        cfg.GameExplicitlySet = true;
                        break;
                    case "--prefer-switches":
                        cfg.PreferSwitches = true;
                        break;
                    case "--strict-signatures":
                        cfg.StrictSignatures = true;
                        break;
                    default:
                        if (arg.StartsWith("-", StringComparison.Ordinal))
                        {
                            throw new ArgumentException("Unknown option: " + arg);
                        }

                        cfg.Inputs.Add(arg);
                        break;
                }
            }

            return cfg;
        }

        private static bool ParseGame(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            string game = value.ToLowerInvariant();
            if (game == "k1" || game == "1" || game.Contains("kotor1"))
            {
                return false;
            }

            return game == "k2" || game == "tsl" || game == "2" || game.Contains("kotor2");
        }

        private static void RequireValue(string[] args, int index, string option)
        {
            if (index + 1 >= args.Length)
            {
                throw new ArgumentException("Option requires a value: " + option);
            }
        }

        private static Encoding ResolveEncoding(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DefaultEncoding();
            }

            try
            {
                return Encoding.GetEncoding(value.Trim());
            }
            catch
            {
                Console.Error.WriteLine("Warning: unknown encoding '" + value + "', falling back to Windows-1252/UTF-8.");
                return DefaultEncoding();
            }
        }

        private static Encoding DefaultEncoding()
        {
            try
            {
                return Encoding.GetEncoding("Windows-1252");
            }
            catch
            {
                return Encoding.UTF8;
            }
        }

        private static void PrintVersion()
        {
            Console.WriteLine("NCSDecomp CLI headless decompiler (Beta 2, May 30 2006)");
            Console.WriteLine("Modified by th3w1zard1 | https://bolabaden.org | https://github.com/bolabaden");
        }

        private static void PrintUsage()
        {
            Console.WriteLine("KotOR NCSDecomp headless decompiler (Beta 2, May 30 2006). Decompiles NCS -> NSS without external tools.");
            Console.WriteLine("Original: JdNoa (decompiler), Dashus (GUI); further mods: th3w1zard1 | https://bolabaden.org | https://github.com/bolabaden");
            Console.WriteLine();
            Console.WriteLine("Usage: KNCSDecomp [options] <files/dirs>");
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help                 Show help");
            Console.WriteLine("  -v, --version              Show version info");
            Console.WriteLine("  -i, --input <path>         Input .ncs file or directory (can repeat or pass positional)");
            Console.WriteLine("  -o, --output <path>        Output file or directory (defaults to current directory)");
            Console.WriteLine("  -O, --out-dir <dir>        Output directory");
            Console.WriteLine("      --output-dir <dir>     Alias for --out-dir");
            Console.WriteLine("      --prefix <text>        Prefix for generated filenames");
            Console.WriteLine("      --suffix <text>        Suffix for generated filenames");
            Console.WriteLine("      --ext <ext>            Output extension (default: .nss)");
            Console.WriteLine("      --encoding <name>      Output charset (default: Windows-1252)");
            Console.WriteLine("      --nwscript <path>      Path to nwscript.nss file");
            Console.WriteLine("      --stdout               Write decompiled source to stdout");
            Console.WriteLine("      --overwrite            Overwrite existing files");
            Console.WriteLine("  -r, --recursive            Recurse into directories when inputs are dirs");
            Console.WriteLine("  -g, --game <value>         Select game: k1, k2, tsl, 1, or 2");
            Console.WriteLine("      --k1 | --k2 | --tsl    Select game variant");
            Console.WriteLine("      --quiet                Suppress success logs");
            Console.WriteLine("      --fail-fast            Stop on first decompile failure");
            Console.WriteLine("      --prefer-switches      Prefer generating switch structures");
            Console.WriteLine("      --strict-signatures    Fail if signatures stay unknown");
        }

        private sealed class InputFile
        {
            public readonly FileInfo Source;
            public readonly DirectoryInfo BaseDir;

            public InputFile(FileInfo source, DirectoryInfo baseDir)
            {
                Source = source;
                BaseDir = baseDir ?? source.Directory;
            }
        }

        private sealed class CliConfig
        {
            public readonly List<string> Inputs = new List<string>();
            public string Output;
            public string OutDir;
            public string Prefix = string.Empty;
            public string Suffix = string.Empty;
            public string Extension = ".nss";
            public Encoding Encoding = DefaultEncoding();
            public bool Stdout;
            public bool Overwrite;
            public bool Recursive;
            public bool Help;
            public bool Version;
            public bool Quiet;
            public bool FailFast;
            public bool IsK2;
            public bool GameExplicitlySet;
            public bool PreferSwitches;
            public bool StrictSignatures;
            public string NwscriptPath;
        }
    }
}
