using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace KNCSDecomp
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            OutputDirectoryTextBox.Text = CSharpKOTOR.Formats.NCS.KNCSDecomp.Decompiler.settings.GetProperty("Output Directory");
        }

        private async void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null) return;

            string initialDirectory = OutputDirectoryTextBox.Text;
            if (string.IsNullOrEmpty(initialDirectory))
            {
                initialDirectory = CSharpKOTOR.Formats.NCS.KNCSDecomp.JavaSystem.GetProperty("user.dir");
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

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            CSharpKOTOR.Formats.NCS.KNCSDecomp.Decompiler.settings.SetProperty("Output Directory", OutputDirectoryTextBox.Text);
            CSharpKOTOR.Formats.NCS.KNCSDecomp.Decompiler.settings.Save();
            this.Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

