using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Decompiler = BioWare.Resource.Formats.NCS.Decomp.Decompiler;

namespace KNCSDecomp
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            var settings = Decompiler.settings;
            OutputDirectoryTextBox.Text = settings.GetProperty("Output Directory");
            K1NWScriptTextBox.Text = settings.GetProperty("K1 nwscript Path");
            K2NWScriptTextBox.Text = settings.GetProperty("K2 nwscript Path");
            EncodingTextBox.Text = settings.GetProperty("Encoding", "Windows-1252");
            FileExtensionTextBox.Text = settings.GetProperty("File Extension", ".nss");
            FilenamePrefixTextBox.Text = settings.GetProperty("Filename Prefix", "");
            FilenameSuffixTextBox.Text = settings.GetProperty("Filename Suffix", "");
            OverwriteFilesCheckBox.IsChecked = bool.Parse(settings.GetProperty("Overwrite Files", "false"));
            PreferSwitchesCheckBox.IsChecked = bool.Parse(settings.GetProperty("Prefer Switches", "false"));
            StrictSignaturesCheckBox.IsChecked = bool.Parse(settings.GetProperty("Strict Signatures", "false"));

            string gameVariant = settings.GetProperty("Game Variant", "k1").ToLowerInvariant();
            GameVariantComboBox.SelectedIndex = gameVariant == "k2" || gameVariant == "tsl" || gameVariant == "2" ? 1 : 0;
        }

        private async void OnBrowseOutputClick(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null)
                return;

            string initialDirectory = OutputDirectoryTextBox.Text;
            if (string.IsNullOrEmpty(initialDirectory))
            {
                initialDirectory = Environment.CurrentDirectory;
            }

            System.Collections.Generic.IReadOnlyList<IStorageFolder> folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select output directory",
                SuggestedStartLocation = !string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory)
                    ? await topLevel.StorageProvider.TryGetFolderFromPathAsync(initialDirectory)
                    : null
            });

            if (folder != null && folder.Count > 0)
            {
                OutputDirectoryTextBox.Text = folder[0].Path.LocalPath;
            }
        }

        private async void OnBrowseK1Click(object sender, RoutedEventArgs e)
        {
            string selected = await BrowseForNwscript(K1NWScriptTextBox.Text);
            if (!string.IsNullOrEmpty(selected))
            {
                K1NWScriptTextBox.Text = selected;
            }
        }

        private async void OnBrowseK2Click(object sender, RoutedEventArgs e)
        {
            string selected = await BrowseForNwscript(K2NWScriptTextBox.Text);
            if (!string.IsNullOrEmpty(selected))
            {
                K2NWScriptTextBox.Text = selected;
            }
        }

        private async System.Threading.Tasks.Task<string> BrowseForNwscript(string currentPath)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null)
            {
                return null;
            }

            string initialDirectory = currentPath;
            if (string.IsNullOrEmpty(initialDirectory))
            {
                initialDirectory = Environment.CurrentDirectory;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select nwscript.nss",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("NWScript files")
                    {
                        Patterns = new[] { "*.nss" }
                    }
                },
                SuggestedStartLocation = !string.IsNullOrEmpty(initialDirectory) && Directory.Exists(Path.GetDirectoryName(initialDirectory) ?? initialDirectory)
                    ? await topLevel.StorageProvider.TryGetFolderFromPathAsync(
                        Directory.Exists(initialDirectory) ? initialDirectory : Path.GetDirectoryName(initialDirectory))
                    : null
            });

            if (files != null && files.Count > 0)
            {
                return files[0].Path.LocalPath;
            }

            return null;
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var settings = Decompiler.settings;
            settings.SetProperty("Output Directory", OutputDirectoryTextBox.Text ?? "");
            settings.SetProperty("K1 nwscript Path", K1NWScriptTextBox.Text ?? "");
            settings.SetProperty("K2 nwscript Path", K2NWScriptTextBox.Text ?? "");
            settings.SetProperty("Encoding", string.IsNullOrWhiteSpace(EncodingTextBox.Text) ? "Windows-1252" : EncodingTextBox.Text.Trim());
            settings.SetProperty("File Extension", string.IsNullOrWhiteSpace(FileExtensionTextBox.Text) ? ".nss" : FileExtensionTextBox.Text.Trim());
            settings.SetProperty("Filename Prefix", FilenamePrefixTextBox.Text ?? "");
            settings.SetProperty("Filename Suffix", FilenameSuffixTextBox.Text ?? "");
            settings.SetProperty("Overwrite Files", (OverwriteFilesCheckBox.IsChecked ?? false).ToString().ToLowerInvariant());
            settings.SetProperty("Prefer Switches", (PreferSwitchesCheckBox.IsChecked ?? false).ToString().ToLowerInvariant());
            settings.SetProperty("Strict Signatures", (StrictSignaturesCheckBox.IsChecked ?? false).ToString().ToLowerInvariant());

            string gameVariant = GameVariantComboBox.SelectedIndex == 1 ? "k2" : "k1";
            settings.SetProperty("Game Variant", gameVariant);
            settings.Save();

            // Keep static runtime flags in sync with saved settings.
            BioWare.Resource.Formats.NCS.Decomp.FileDecompiler.isK2Selected = gameVariant == "k2";
            BioWare.Resource.Formats.NCS.Decomp.FileDecompiler.preferSwitches = PreferSwitchesCheckBox.IsChecked ?? false;
            BioWare.Resource.Formats.NCS.Decomp.FileDecompiler.strictSignatures = StrictSignaturesCheckBox.IsChecked ?? false;

            this.Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

