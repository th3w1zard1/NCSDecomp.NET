using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Decompiler = BioWare.Resource.Formats.NCS.Decomp.Decompiler;
using File = BioWare.Resource.Formats.NCS.Decomp.NcsFile;
using JavaSystem = BioWare.Resource.Formats.NCS.Decomp.JavaSystem;
using MsgBoxIcon = MsBox.Avalonia.Enums.Icon;

namespace KNCSDecomp
{
    public partial class MainWindow : Window
    {
        private readonly BioWare.Resource.Formats.NCS.Decomp.FileDecompiler fileDecompiler;
        private readonly Dictionary<object, File> hash_TabComponent2File;
        private readonly Dictionary<object, Dictionary<object, object>> hash_TabComponent2Func2VarVec;
        private readonly Dictionary<object, object> hash_TabComponent2TreeModel;
        private static readonly List<File> unsavedFiles;
        private Dictionary<object, object> hash_Func2VarVec;
        private bool isInitialized;

        static MainWindow()
        {
            unsavedFiles = new List<File>();
            Decompiler.settings = new BioWare.Resource.Formats.NCS.Decomp.Settings();
            Decompiler.settings.Load();
            // Output directory validation will be done when window is shown
        }

        public MainWindow()
        {
            InitializeComponent();

            // Initialize FileDecompiler - now lazy-loads nwscript.nss, so won't crash if file is missing
            // The default constructor uses Decompiler.settings automatically
            fileDecompiler = new BioWare.Resource.Formats.NCS.Decomp.FileDecompiler();

            hash_TabComponent2File = new Dictionary<object, File>();
            hash_TabComponent2Func2VarVec = new Dictionary<object, Dictionary<object, object>>();
            hash_TabComponent2TreeModel = new Dictionary<object, object>();

            // Center window on screen
            Avalonia.Platform.Screen screen = Screens.Primary;
            if (screen != null)
            {
                PixelRect workingArea = screen.WorkingArea;
                WindowStartupLocation = WindowStartupLocation.Manual;
                Position = new PixelPoint(
                    (int)(workingArea.Width / 2 - Width / 2),
                    (int)(workingArea.Height / 2 - Height / 2)
                );
            }

            // Enable drag and drop
            AddHandler(DragDrop.DropEvent, OnDrop);
            AddHandler(DragDrop.DragOverEvent, OnDragOver);

            // Mark initialization complete after controls are loaded
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                isInitialized = true;
                // Update menu states now that everything is loaded
                UpdateMenuStates(FilesTabControl != null && FilesTabControl.SelectedItem != null);
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(this, "Error during window initialization", ex);
            }
        }

        public static void Exit()
        {
            JavaSystem.Exit(0);
        }

        public static async Task<string> ChooseOutputDirectoryAsync(Window parent)
        {
            var topLevel = TopLevel.GetTopLevel(parent);
            if (topLevel is null)
            {
                return JavaSystem.GetProperty("user.dir");
            }

            string initialDirectory = Decompiler.settings.GetProperty("Output Directory");
            if (string.IsNullOrEmpty(initialDirectory))
            {
                initialDirectory = JavaSystem.GetProperty("user.dir");
            }

            IReadOnlyList<IStorageFolder> folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select output directory",
                SuggestedStartLocation = !string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory)
                    ? await topLevel.StorageProvider.TryGetFolderFromPathAsync(initialDirectory)
                    : null
            });

            if (folder != null && folder.Count > 0)
            {
                return folder[0].Path.LocalPath;
            }

            return Decompiler.settings.GetProperty("Output Directory").Equals("")
                ? JavaSystem.GetProperty("user.dir")
                : Decompiler.settings.GetProperty("Output Directory");
        }

        public static string ChooseOutputDirectory()
        {
            // Synchronous version for compatibility - returns current setting
            return Decompiler.settings.GetProperty("Output Directory").Equals("")
                ? JavaSystem.GetProperty("user.dir")
                : Decompiler.settings.GetProperty("Output Directory");
        }

        private void OnWindowClosing(object sender, WindowClosingEventArgs e)
        {
            try
            {
                Decompiler.settings.Save();
                Exit();
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(this, "Error closing window", ex);
            }
        }

        private void OnMenuOpenClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Open();
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(this, "Error opening menu", ex);
            }
        }

        private void OnMenuCloseClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FilesTabControl.SelectedItem != null)
                {
                    int index = FilesTabControl.SelectedIndex;
                    Dispose(index);
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(this, "Error closing file", ex);
            }
        }

        private void OnMenuCloseAllClick(object sender, RoutedEventArgs e)
        {
            try
            {
                CloseAll();
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(this, "Error closing all files", ex);
            }
        }

        private void OnMenuSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FilesTabControl.SelectedItem != null)
                {
                    Save(FilesTabControl.SelectedIndex);
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(this, "Error saving file", ex);
            }
        }

        private void OnMenuSaveAllClick(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveAll();
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(this, "Error saving all files", ex);
            }
        }

        private void OnMenuExitClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Exit();
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(this, "Error exiting application", ex);
            }
        }

        private async void OnMenuSettingsClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new SettingsWindow();
                await settingsWindow.ShowDialog(this);
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(this, "Error opening settings", ex);
            }
        }

        private void OnStatusClearClick(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusTextBox.Text = "";
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(this, "Error clearing status", ex);
            }
        }

        private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Skip handling during initialization when controls may not be ready
                if (!isInitialized || FilesTabControl == null)
                {
                    return;
                }

                UpdateMenuStates(FilesTabControl.SelectedItem != null);
                if (FilesTabControl.SelectedItem != null)
                {
                    if (FilesTabControl.SelectedItem is TabItem tabItem && hash_TabComponent2TreeModel.ContainsKey(tabItem))
                    {
                        // Update tree view with the model for this tab
                        UpdateTreeView(hash_TabComponent2TreeModel[tabItem]);
                    }
                }
                else
                {
                    if (SubroutinesTreeView != null)
                    {
                        SubroutinesTreeView.ItemsSource = null;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(this, "Error handling tab selection change", ex);
            }
        }

        private void OnTreeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (SubroutinesTreeView.SelectedItem != null && FilesTabControl.SelectedItem != null)
                {
                    object selectedItem = SubroutinesTreeView.SelectedItem;
                    // Handle tree selection change - update decompiled code if needed
                    // This matches the KeyReleased logic from the original
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(this, "Error handling tree selection change", ex);
            }
        }

        private void OnTreeKeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                // Handle tree editing - matches KeyReleased from original
                if (FilesTabControl.SelectedItem is null)
                {
                    return;
                }

                if (e.Source is TreeView)
                {
                    object selectedItem = SubroutinesTreeView.SelectedItem;
                    if (selectedItem != null)
                    {
                        // Handle variable/subroutine name changes
                        // This matches the original KeyReleased logic
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(this, "Error handling tree key event", ex);
            }
        }

        private void OnCloseTabClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button)
                {
                    if (button.DataContext is TabItem tabItem)
                    {
                        int index = FilesTabControl.Items.OfType<TabItem>().ToList().IndexOf(tabItem);
                        Dispose(index);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(this, "Error closing tab", ex);
            }
        }

        private void OnDecompiledCodeChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (FilesTabControl.SelectedItem != null)
                {
                    if (FilesTabControl.SelectedItem is TabItem tabItem && hash_TabComponent2File.ContainsKey(tabItem))
                    {
                        File file = hash_TabComponent2File[tabItem];
                        if (!unsavedFiles.Contains(file))
                        {
                            unsavedFiles.Add(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(this, "Error handling code change", ex);
            }
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.Contains(DataFormats.FileNames))
                {
                    if (e.Data.Get(DataFormats.FileNames) is IEnumerable<string> fileNames)
                    {
                        File[] fileInfos = fileNames.Select(path => new File(path)).ToArray();
                        OpenFiles(fileInfos);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(this, "Error handling file drop", ex);
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.Contains(DataFormats.FileNames))
                {
                    e.DragEffects = DragDropEffects.Copy;
                }
                else
                {
                    e.DragEffects = DragDropEffects.None;
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(this, "Error handling drag over", ex);
                e.DragEffects = DragDropEffects.None;
            }
        }

        private async void Open()
        {
            try
            {
                TopLevel topLevel = GetTopLevel(this);
                if (topLevel is null)
                {
                    return;
                }

                string initialDirectory = Decompiler.settings.GetProperty("Open Directory");
                if (string.IsNullOrEmpty(initialDirectory))
                {
                    initialDirectory = Decompiler.settings.GetProperty("Output Directory");
                }

                IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Open NCS Files",
                    AllowMultiple = true,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("NCS Files")
                        {
                            Patterns = new[] { "*.ncs" }
                        },
                        new FilePickerFileType("All Files")
                        {
                            Patterns = new[] { "*.*" }
                        }
                    },
                    SuggestedStartLocation = !string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory)
                        ? await topLevel.StorageProvider.TryGetFolderFromPathAsync(initialDirectory)
                        : null
                });

                if (files != null && files.Count > 0)
                {
                    File[] fileInfos = files.Select(f => new File(f.Path.LocalPath)).ToArray();
                    if (fileInfos.Length > 0)
                    {
                        Decompiler.settings.SetProperty("Open Directory", fileInfos[0].DirectoryName);
                    }
                    OpenFiles(fileInfos);
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(this, "Error opening files", ex);
            }
        }

        private void OpenFiles(File[] files)
        {
            try
            {
                if (FilesTabControl.Items.Count == 0)
                {
                    UpdateMenuStates(true);
                }

                foreach (File file in files)
                {
                    try
                    {
                        string name = file.Name;
                        if (name.Length >= 3 && name.Substring(name.Length - 3).Equals("ncs", StringComparison.OrdinalIgnoreCase))
                        {
                            Decompile(file);
                            if (FilesTabControl.SelectedItem != null)
                            {
                                if (FilesTabControl.SelectedItem is TabItem tabItem && hash_TabComponent2File.ContainsKey(tabItem))
                                {
                                    File fileForTab = hash_TabComponent2File[tabItem];
                                    if (!unsavedFiles.Contains(fileForTab))
                                    {
                                        unsavedFiles.Add(fileForTab);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorDialog.Show(this, "Error processing file: " + file.Name, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorDialog.Show(this, "Error opening files", ex);
            }
        }

        private void Decompile(File file)
        {
            AppendStatus("Decompiling...");
            AppendStatus(file.Name + ": ");
            int result = 2;
            try
            {
                result = fileDecompiler.Decompile(file);
            }
            catch (BioWare.Resource.Formats.NCS.Decomp.DecompilerException ex)
            {
                string errorMsg = ex.Message;
                // Check if it's a missing nwscript.nss error
                if (errorMsg.Contains("nwscript.nss") || errorMsg.Contains("cannot find") || errorMsg.Contains("Error: cannot"))
                {
                    errorMsg += "\n\nPlease configure the nwscript.nss path in Settings (Options > Settings).";
                }
                ShowMessageDialog(errorMsg);
                AppendStatus("error: " + ex.Message + "\n");
                return;
            }

            switch (result)
            {
                case 1: // SUCCESS
                    {
                        TabItem panels = NewNCSTab(file.Name.Substring(0, file.Name.Length - 4));
                        if (FilesTabControl.SelectedItem is TabItem tabItem)
                        {
                            string decompiledCode = fileDecompiler.GetGeneratedCode(file);
                            string originalByteCode = fileDecompiler.GetOriginalByteCode(file);
                            string newByteCode = fileDecompiler.GetNewByteCode(file);

                            SetTabContent(tabItem, decompiledCode, originalByteCode, newByteCode);

                            hash_Func2VarVec = fileDecompiler.GetVariableData(file);
                            UpdateTreeView(BioWare.Resource.Formats.NCS.Decomp.TreeModelFactory.CreateTreeModel(hash_Func2VarVec));
                            fileDecompiler.GetOriginalByteCode(file);
                            hash_TabComponent2File[tabItem] = file;
                            hash_TabComponent2Func2VarVec[tabItem] = hash_Func2VarVec;
                            hash_TabComponent2TreeModel[tabItem] = BioWare.Resource.Formats.NCS.Decomp.TreeModelFactory.CreateTreeModel(hash_Func2VarVec);
                        }
                        AppendStatus("success\n");
                        break;
                    }
                case 0: // FAILURE
                    {
                        AppendStatus("failure\n");
                        break;
                    }
                case 2: // PARTIAL_COMPILE
                    {
                        TabItem panels = NewNCSTab(file.Name.Substring(0, file.Name.Length - 4));
                        if (FilesTabControl.SelectedItem is TabItem tabItem)
                        {
                            string decompiledCode = fileDecompiler.GetGeneratedCode(file);
                            string originalByteCode = fileDecompiler.GetOriginalByteCode(file);

                            SetTabContent(tabItem, decompiledCode, originalByteCode, null);

                            hash_Func2VarVec = fileDecompiler.GetVariableData(file);
                            UpdateTreeView(BioWare.Resource.Formats.NCS.Decomp.TreeModelFactory.CreateTreeModel(hash_Func2VarVec));
                            fileDecompiler.GetOriginalByteCode(file);
                            hash_TabComponent2File[tabItem] = file;
                            hash_TabComponent2Func2VarVec[tabItem] = hash_Func2VarVec;
                            hash_TabComponent2TreeModel[tabItem] = BioWare.Resource.Formats.NCS.Decomp.TreeModelFactory.CreateTreeModel(hash_Func2VarVec);
                            SetTabComponentPanel(tabItem, 1);
                        }
                        AppendStatus("partial-could not recompile\n");
                        break;
                    }
                case 3: // PARTIAL_COMPARE
                    {
                        TabItem panels = NewNCSTab(file.Name.Substring(0, file.Name.Length - 4));
                        if (FilesTabControl.SelectedItem is TabItem tabItem)
                        {
                            string decompiledCode = fileDecompiler.GetGeneratedCode(file);
                            string originalByteCode = fileDecompiler.GetOriginalByteCode(file);
                            string newByteCode = fileDecompiler.GetNewByteCode(file);

                            SetTabContent(tabItem, decompiledCode, originalByteCode, newByteCode);

                            hash_Func2VarVec = fileDecompiler.GetVariableData(file);
                            UpdateTreeView(BioWare.Resource.Formats.NCS.Decomp.TreeModelFactory.CreateTreeModel(hash_Func2VarVec));
                            fileDecompiler.GetOriginalByteCode(file);
                            hash_TabComponent2File[tabItem] = file;
                            hash_TabComponent2Func2VarVec[tabItem] = hash_Func2VarVec;
                            hash_TabComponent2TreeModel[tabItem] = BioWare.Resource.Formats.NCS.Decomp.TreeModelFactory.CreateTreeModel(hash_Func2VarVec);
                            SetTabComponentPanel(tabItem, 1);
                        }
                        AppendStatus("partial-byte code does not match\n");
                        break;
                    }
            }
        }

        private TabItem NewNCSTab(string text)
        {
            return NewNCSTab(text, FilesTabControl.Items.Count);
        }

        private TabItem NewNCSTab(string text, int index)
        {
            var tabItem = new TabItem
            {
                Header = text,
                Content = CreateTabContent()
            };

            FilesTabControl.Items.Insert(index, tabItem);
            FilesTabControl.SelectedItem = tabItem;
            return tabItem;
        }

        private Grid CreateTabContent()
        {
            var grid = new Grid();

            // Decompiled code view (view 0)
            var decompiledScrollViewer = new ScrollViewer
            {
                Name = "DecompiledCodeScrollViewer",
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            var decompiledBorder = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5),
                Child = new TextBox
                {
                    Name = "DecompiledCodeTextBox",
                    AcceptsReturn = true,
                    AcceptsTab = true,
                    TextWrapping = TextWrapping.NoWrap,
                    FontFamily = new FontFamily("Courier New"),
                    FontSize = 12,
                    IsReadOnly = false
                }
            };
            decompiledScrollViewer.Content = decompiledBorder;
            grid.Children.Add(decompiledScrollViewer);

            // Byte code view (view 1) - initially hidden
            var byteCodeGrid = new Grid
            {
                Name = "ByteCodeGrid",
                IsVisible = false
            };
            byteCodeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            byteCodeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
            byteCodeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var originalScrollViewer = new ScrollViewer
            {
                Name = "OriginalByteCodeScrollViewer",
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            var originalBorder = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "Original Byte Code", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 5) },
                        new TextBox
                        {
                            Name = "OriginalByteCodeTextBox",
                            AcceptsReturn = true,
                            TextWrapping = TextWrapping.NoWrap,
                            FontFamily = new FontFamily("Courier New"),
                            FontSize = 12,
                            IsReadOnly = true
                        }
                    }
                }
            };
            originalScrollViewer.Content = originalBorder;
            Grid.SetColumn(originalScrollViewer, 0);
            byteCodeGrid.Children.Add(originalScrollViewer);

            var splitter = new GridSplitter { ShowsPreview = true };
            Grid.SetColumn(splitter, 1);
            byteCodeGrid.Children.Add(splitter);

            var recompiledScrollViewer = new ScrollViewer
            {
                Name = "RecompiledByteCodeScrollViewer",
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            var recompiledBorder = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "Recompiled Byte Code", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 5) },
                        new TextBox
                        {
                            Name = "RecompiledByteCodeTextBox",
                            AcceptsReturn = true,
                            TextWrapping = TextWrapping.NoWrap,
                            FontFamily = new FontFamily("Courier New"),
                            FontSize = 12,
                            IsReadOnly = true
                        }
                    }
                }
            };
            recompiledScrollViewer.Content = recompiledBorder;
            Grid.SetColumn(recompiledScrollViewer, 2);
            byteCodeGrid.Children.Add(recompiledScrollViewer);

            grid.Children.Add(byteCodeGrid);
            return grid;
        }

        private void SetTabContent(TabItem tabItem, string decompiledCode, string originalByteCode, string recompiledByteCode)
        {
            if (tabItem.Content is Grid grid)
            {
                ScrollViewer decompiledScrollViewer = grid.Children.OfType<ScrollViewer>().FirstOrDefault(s => s.Name == "DecompiledCodeScrollViewer");
                if (decompiledScrollViewer?.Content is Border decompiledBorder && decompiledBorder.Child is TextBox decompiledTextBox)
                {
                    decompiledTextBox.Text = decompiledCode ?? "";
                }

                Grid byteCodeGrid = grid.Children.OfType<Grid>().FirstOrDefault(g => g.Name == "ByteCodeGrid");
                if (byteCodeGrid != null)
                {
                    ScrollViewer originalScrollViewer = byteCodeGrid.Children.OfType<ScrollViewer>().FirstOrDefault(s => s.Name == "OriginalByteCodeScrollViewer");
                    if (originalScrollViewer?.Content is Border originalBorder && originalBorder.Child is StackPanel originalPanel)
                    {
                        TextBox originalTextBox = originalPanel.Children.OfType<TextBox>().FirstOrDefault(t => t.Name == "OriginalByteCodeTextBox");
                        if (originalTextBox != null)
                        {
                            originalTextBox.Text = originalByteCode ?? "";
                        }
                    }

                    ScrollViewer recompiledScrollViewer = byteCodeGrid.Children.OfType<ScrollViewer>().FirstOrDefault(s => s.Name == "RecompiledByteCodeScrollViewer");
                    if (recompiledScrollViewer?.Content is Border recompiledBorder && recompiledBorder.Child is StackPanel recompiledPanel)
                    {
                        TextBox recompiledTextBox = recompiledPanel.Children.OfType<TextBox>().FirstOrDefault(t => t.Name == "RecompiledByteCodeTextBox");
                        if (recompiledTextBox != null)
                        {
                            recompiledTextBox.Text = recompiledByteCode ?? "";
                        }
                    }
                }
            }
        }

        private void SetTabComponentPanel(TabItem tabItem, int index)
        {
            if (tabItem.Content is Grid grid)
            {
                ScrollViewer decompiledScrollViewer = grid.Children.OfType<ScrollViewer>().FirstOrDefault(s => s.Name == "DecompiledCodeScrollViewer");
                Grid byteCodeGrid = grid.Children.OfType<Grid>().FirstOrDefault(g => g.Name == "ByteCodeGrid");

                if (index == 0)
                {
                    if (decompiledScrollViewer != null)
                    {
                        decompiledScrollViewer.IsVisible = true;
                    }

                    if (byteCodeGrid != null)
                    {
                        byteCodeGrid.IsVisible = false;
                    }
                }
                else if (index == 1)
                {
                    if (decompiledScrollViewer != null)
                    {
                        decompiledScrollViewer.IsVisible = false;
                    }

                    if (byteCodeGrid != null)
                    {
                        byteCodeGrid.IsVisible = true;
                    }
                }
            }
        }

        private void UpdateTreeView(object model)
        {
            // Convert the tree model to Avalonia TreeView items
            if (model == null)
            {
                SubroutinesTreeView.ItemsSource = null;
                return;
            }

            try
            {
                var treeItems = new List<TreeViewItem>();

                if (model is Dictionary<object, object> variableData)
                {
                    foreach (KeyValuePair<object, object> kvp in variableData)
                    {
                        string subroutineName = kvp.Key?.ToString() ?? "Unknown";
                        var subroutineItem = new TreeViewItem
                        {
                            Header = subroutineName,
                            IsExpanded = false
                        };

                        // Add variables as children
                        var variableList = new List<TreeViewItem>();
                        if (kvp.Value is List<object> variables)
                        {
                            foreach (object variable in variables)
                            {
                                string varName = variable?.ToString() ?? "Unknown";
                                variableList.Add(new TreeViewItem
                                {
                                    Header = varName,
                                    IsExpanded = false
                                });
                            }
                        }
                        else if (kvp.Value is Dictionary<object, object> varDict)
                        {
                            foreach (KeyValuePair<object, object> varKvp in varDict)
                            {
                                string varName = varKvp.Key?.ToString() ?? "Unknown";
                                variableList.Add(new TreeViewItem
                                {
                                    Header = varName,
                                    IsExpanded = false
                                });
                            }
                        }

                        if (variableList.Count > 0)
                        {
                            subroutineItem.ItemsSource = variableList;
                        }

                        treeItems.Add(subroutineItem);
                    }
                }

                SubroutinesTreeView.ItemsSource = treeItems.Count > 0 ? treeItems : null;
            }
            catch (Exception ex)
            {
                AppendStatus($"Error updating tree view: {ex.Message}\n");
                SubroutinesTreeView.ItemsSource = null;
            }
        }

        private void UpdateMenuStates(bool enabled = false)
        {
            // Skip if not initialized - controls may not be available
            if (!isInitialized)
            {
                return;
            }

            // In Avalonia, nested menu items need to be found using FindControl
            MenuItem menuClose = this.FindControl<MenuItem>("MenuClose");
            MenuItem menuCloseAll = this.FindControl<MenuItem>("MenuCloseAll");
            MenuItem menuSave = this.FindControl<MenuItem>("MenuSave");
            MenuItem menuSaveAll = this.FindControl<MenuItem>("MenuSaveAll");

            if (menuClose != null)
            {
                menuClose.IsEnabled = enabled;
            }

            if (menuCloseAll != null)
            {
                menuCloseAll.IsEnabled = enabled;
            }

            if (menuSave != null)
            {
                menuSave.IsEnabled = enabled;
            }

            if (menuSaveAll != null)
            {
                menuSaveAll.IsEnabled = enabled;
            }
        }

        private void AppendStatus(string text)
        {
            Dispatcher.UIThread.Post(() =>
            {
                StatusTextBox.Text += text;
                if (StatusTextBox.Parent is ScrollViewer scrollViewer)
                {
                    scrollViewer.ScrollToEnd();
                }
            });
        }

        private async void ShowMessageDialog(string message)
        {
            MsBox.Avalonia.Base.IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard(
                "KNCSDecomp",
                message,
                ButtonEnum.Ok,
                MsgBoxIcon.Info);
            await box.ShowAsync();
        }

        private File SaveBuffer(string text, string canonicalPath)
        {
            try
            {
                System.IO.File.WriteAllText(canonicalPath, text);
                return new File(canonicalPath);
            }
            catch (DirectoryNotFoundException)
            {
                ShowMessageDialog("Error saving " + Path.GetFileName(canonicalPath) + "\nOutput directory does not exist; change in settings");
                if (System.IO.File.Exists(canonicalPath))
                {
                    try
                    { System.IO.File.Delete(canonicalPath); }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                ShowMessageDialog("Error saving " + Path.GetFileName(canonicalPath) + ": " + ex.Message);
                if (System.IO.File.Exists(canonicalPath))
                {
                    try
                    { System.IO.File.Delete(canonicalPath); }
                    catch { }
                }
            }
            return null;
        }

        private async void Dispose(int index)
        {
            if (index < 0 || index >= FilesTabControl.Items.Count)
            {
                return;
            }

            if (!(FilesTabControl.Items[index] is TabItem tabItem))
            {
                return;
            }

            File file = null;
            if (hash_TabComponent2File.ContainsKey(tabItem))
            {
                file = hash_TabComponent2File[tabItem];
                if (unsavedFiles.Contains(file))
                {
                    bool? result = await ShowConfirmDialog(
                        Path.GetFileNameWithoutExtension(file.Name) + ".nss is unsaved. Would you like to save it?");
                    if (result == true)
                    {
                        Save(index);
                    }
                    else if (result is null)
                    {
                        // User cancelled
                        return;
                    }
                }
            }

            if (file != null)
            {
                fileDecompiler.CloseFile(file);
                hash_TabComponent2File.Remove(tabItem);
                hash_TabComponent2Func2VarVec.Remove(tabItem);
                hash_TabComponent2TreeModel.Remove(tabItem);
            }

            FilesTabControl.Items.RemoveAt(index);

            if (FilesTabControl.Items.Count == 0)
            {
                UpdateMenuStates(false);
            }
        }

        private void CloseAll()
        {
            while (FilesTabControl.Items.Count > 0)
            {
                Dispose(0);
            }
        }

        private void Save(int index)
        {
            if (index < 0 || index >= FilesTabControl.Items.Count)
            {
                return;
            }

            if (!(FilesTabControl.Items[index] is TabItem tabItem))
            {
                return;
            }

            string decompiledCode = "";
            if (tabItem.Content is Grid grid)
            {
                ScrollViewer decompiledScrollViewer = grid.Children.OfType<ScrollViewer>().FirstOrDefault(s => s.Name == "DecompiledCodeScrollViewer");
                if (decompiledScrollViewer?.Content is Border decompiledBorder && decompiledBorder.Child is TextBox decompiledTextBox)
                {
                    decompiledCode = decompiledTextBox.Text;
                }
            }

            string fileName = (tabItem.Header as string) ?? "untitled";
            string outputPath = Path.Combine(
                Decompiler.settings.GetProperty("Output Directory"),
                fileName + ".nss");

            File newFile = SaveBuffer(decompiledCode, outputPath);
            if (newFile is null)
            {
                return;
            }

            File file = null;
            if (hash_TabComponent2File.ContainsKey(tabItem))
            {
                file = hash_TabComponent2File[tabItem];
            }

            if (file != null && unsavedFiles.Contains(file))
            {
                AppendStatus("Recompiling..." + file.Name + ": ");
                int result = 2;
                try
                {
                    result = fileDecompiler.CompileAndCompare(file, newFile);
                }
                catch (BioWare.Resource.Formats.NCS.Decomp.DecompilerException ex)
                {
                    ShowMessageDialog(ex.Message);
                    return;
                }

                switch (result)
                {
                    case 1: // SUCCESS
                        {
                            string originalByteCode = fileDecompiler.GetOriginalByteCode(file);
                            string newByteCode = fileDecompiler.GetNewByteCode(file);
                            SetTabContent(tabItem, decompiledCode, originalByteCode, newByteCode);

                            hash_Func2VarVec = fileDecompiler.GetVariableData(file);
                            UpdateTreeView(BioWare.Resource.Formats.NCS.Decomp.TreeModelFactory.CreateTreeModel(hash_Func2VarVec));
                            fileDecompiler.GetOriginalByteCode(file);
                            hash_TabComponent2File[tabItem] = file;
                            hash_TabComponent2Func2VarVec[tabItem] = hash_Func2VarVec;
                            hash_TabComponent2TreeModel[tabItem] = BioWare.Resource.Formats.NCS.Decomp.TreeModelFactory.CreateTreeModel(hash_Func2VarVec);
                            AppendStatus("success\n");
                            break;
                        }
                    case 0: // FAILURE
                        {
                            AppendStatus("failure\n");
                            break;
                        }
                    case 2: // PARTIAL_COMPILE
                        {
                            string originalByteCode = fileDecompiler.GetOriginalByteCode(file);
                            SetTabContent(tabItem, decompiledCode, originalByteCode, null);

                            hash_Func2VarVec = fileDecompiler.GetVariableData(file);
                            UpdateTreeView(BioWare.Resource.Formats.NCS.Decomp.TreeModelFactory.CreateTreeModel(hash_Func2VarVec));
                            fileDecompiler.GetOriginalByteCode(file);
                            hash_TabComponent2File[tabItem] = file;
                            hash_TabComponent2Func2VarVec[tabItem] = hash_Func2VarVec;
                            hash_TabComponent2TreeModel[tabItem] = BioWare.Resource.Formats.NCS.Decomp.TreeModelFactory.CreateTreeModel(hash_Func2VarVec);
                            SetTabComponentPanel(tabItem, 1);
                            AppendStatus("partial-could not recompile\n");
                            break;
                        }
                    case 3: // PARTIAL_COMPARE
                        {
                            string originalByteCode = fileDecompiler.GetOriginalByteCode(file);
                            string newByteCode = fileDecompiler.GetNewByteCode(file);
                            SetTabContent(tabItem, decompiledCode, originalByteCode, newByteCode);

                            hash_Func2VarVec = fileDecompiler.GetVariableData(file);
                            UpdateTreeView(BioWare.Resource.Formats.NCS.Decomp.TreeModelFactory.CreateTreeModel(hash_Func2VarVec));
                            fileDecompiler.GetOriginalByteCode(file);
                            hash_TabComponent2File[tabItem] = file;
                            hash_TabComponent2Func2VarVec[tabItem] = hash_Func2VarVec;
                            hash_TabComponent2TreeModel[tabItem] = BioWare.Resource.Formats.NCS.Decomp.TreeModelFactory.CreateTreeModel(hash_Func2VarVec);
                            SetTabComponentPanel(tabItem, 1);
                            AppendStatus("partial-byte code does not match\n");
                            break;
                        }
                }

                unsavedFiles.Remove(file);
            }
        }

        private void SaveAll()
        {
            for (int i = 0; i < FilesTabControl.Items.Count; ++i)
            {
                Save(i);
            }
        }

        private async Task<bool?> ShowConfirmDialog(string message)
        {
            MsBox.Avalonia.Base.IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard(
                "KNCSDecomp",
                message,
                ButtonEnum.YesNoCancel,
                MsgBoxIcon.Question);
            ButtonResult result = await box.ShowAsync();
            if (result == ButtonResult.Yes)
            {
                return true;
            }

            if (result == ButtonResult.No)
            {
                return false;
            }

            return null;
        }
    }
}
