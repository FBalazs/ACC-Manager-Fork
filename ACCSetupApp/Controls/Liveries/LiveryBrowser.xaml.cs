﻿using ACCSetupApp.LiveryParser;
using Newtonsoft.Json;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Cache;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ACCSetupApp.Controls
{
    /// <summary>
    /// Interaction logic for LiveryBrowser.xaml
    /// </summary>
    public partial class LiveryBrowser : UserControl
    {
        private string AccPath => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + "Assetto Corsa Competizione\\";
        private string CustomsPath => AccPath + "Customs\\";
        private string CarsPath => CustomsPath + "Cars\\";
        private string LiveriesPath => CustomsPath + "Liveries\\";

        private LiveryBrowser Instance;

        Regex carsJsonRegex = new Regex("^[0-9]*-[0-9]*-[0-9]*.json");

        public LiveryBrowser()
        {
            InitializeComponent();

            Instance = this;
            ThreadPool.QueueUserWorkItem(x => FetchAllCars());

            liveriesTreeView.SelectedItemChanged += LiveriesTreeView_SelectedItemChanged;

            buttonImportLiveries.Click += ButtonImportLiveries_Click;
        }

        private void ButtonImportLiveries_Click(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(x => ImportLiveryZips());
        }

        private void LiveriesTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue != null)
            {
                if (e.NewValue.GetType() == typeof(TreeViewItem))
                {
                    TreeViewItem item = (TreeViewItem)e.NewValue;
                    if (item.DataContext != null)
                    {
                        if (item.DataContext.GetType() == typeof(LiveryTreeCar))
                        {
                            LiveryTreeCar treeCar = (LiveryTreeCar)item.DataContext;
                            

                            LiveryDisplayer.Instance.SetLivery(treeCar);
                        }
                    }
                }
            }
        }

        private Label GetInfoLabel(string text, HorizontalAlignment allignmment = HorizontalAlignment.Left, int size = 13)
        {
            Label label = new Label()
            {
                Content = text,
                Style = Resources["MaterialDesignLabel"] as Style,
                FontSize = size,
                HorizontalAlignment = allignmment
            };
            return label;
        }

        internal class LiveryTreeCar
        {
            public FileInfo carsFile { get; set; }
            public CarsJson.Root carsRoot { get; set; }
        }

        private void FetchAllCars()
        {
            Instance.Dispatcher.BeginInvoke(new Action(() =>
            {
                liveriesTreeView.Items.Clear();

                DirectoryInfo customsCarsDirectory = new DirectoryInfo(CarsPath);

                List<TreeViewItem> carsTreeViews = new List<TreeViewItem>();

                List<LiveryTreeCar> liveryTreeCars = new List<LiveryTreeCar>();

                foreach (var carsFile in customsCarsDirectory.GetFiles())
                {
                    if (carsFile.Extension.Equals(".json"))
                    {
                        CarsJson.Root carsRoot = GetLivery(carsFile);

                        if (carsRoot != null)
                        {
                            LiveryTreeCar treeCar = new LiveryTreeCar() { carsFile = carsFile, carsRoot = carsRoot };

                            if (!treeCar.carsRoot.customSkinName.Equals(String.Empty))
                                liveryTreeCars.Add(treeCar);
                        }
                    }
                }

                var groupedLiveryCars = liveryTreeCars.GroupBy(g => g.carsRoot.teamName);

                foreach (IGrouping<string, LiveryTreeCar> tItem in groupedLiveryCars)
                {
                    TextBlock teamHeader = new TextBlock()
                    {
                        Text = $"{tItem.Key}",
                        Style = Resources["MaterialDesignSubtitle1TextBlock"] as Style,
                        TextTrimming = TextTrimming.WordEllipsis,
                        Width = liveriesTreeView.Width - 5
                    };
                    TreeViewItem teamItem = new TreeViewItem() { Header = teamHeader };

                    foreach (LiveryTreeCar car in tItem)
                    {
                        TextBlock skinHeader = new TextBlock()
                        {
                            Text = $"{car.carsRoot.customSkinName}",
                            Style = Resources["MaterialDesignDataGridTextColumnStyle"] as Style,
                            TextTrimming = TextTrimming.WordEllipsis,
                            Width = liveriesTreeView.Width - 5
                        };
                        TreeViewItem skinItem = new TreeViewItem() { Header = skinHeader, DataContext = car };
                        skinItem.ContextMenu = GetSkinContextMenu(car);

                        teamItem.Items.Add(skinItem);
                    }

                    teamItem.ContextMenu = GetTeamContextMenu(teamItem);
                    carsTreeViews.Add(teamItem);
                }

                carsTreeViews.Sort((a, b) =>
                {
                    TextBlock aCar = a.Header as TextBlock;
                    TextBlock bCar = b.Header as TextBlock;
                    return $"{aCar.Text}".CompareTo($"{bCar.Text}");
                });

                carsTreeViews.ForEach(x => liveriesTreeView.Items.Add(x));
            }));
        }

        private ContextMenu GetSkinContextMenu(LiveryTreeCar directory)
        {
            ContextMenu menu = new ContextMenu()
            {
                Style = Resources["MaterialDesignContextMenu"] as Style,
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                UsesItemContainerTemplate = true,
                Background = new SolidColorBrush(Color.FromArgb(220, 0, 0, 0))
            };


            Button createZipButton = new Button()
            {
                Content = $"Save Skin as zip archive",
                CommandParameter = directory,
                Style = Resources["MaterialDesignRaisedButton"] as Style,
                Margin = new Thickness(0),
                Height = 30,
                VerticalAlignment = VerticalAlignment.Center,
            };
            createZipButton.Click += CreateZipButton_Click;

            menu.Items.Add(createZipButton);

            return menu;
        }

        private ContextMenu GetTeamContextMenu(TreeViewItem teamItem)
        {
            ContextMenu menu = new ContextMenu()
            {
                Style = Resources["MaterialDesignContextMenu"] as Style,
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                UsesItemContainerTemplate = true,
                Background = new SolidColorBrush(Color.FromArgb(220, 0, 0, 0))
            };

            Button createZipButton = new Button()
            {
                Content = $"Save team as zip archive",
                CommandParameter = teamItem,
                Style = Resources["MaterialDesignRaisedButton"] as Style,
                Margin = new Thickness(0),
                Height = 30,
                ToolTip = "Saves all the skins for this team",
                VerticalAlignment = VerticalAlignment.Center,
            };
            createZipButton.Click += CreateTeamZipButton_Click;

            menu.Items.Add(createZipButton);

            return menu;
        }

        private void ImportLiveryZips()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.AddExtension = true;
            dlg.CheckPathExists = true;

            dlg.Filter = "Livery zip(s)|*.zip|Livery rar(s)|*.rar|Livery 7z(s)|*.7z";
            dlg.Multiselect = true;
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                string[] fileNames = dlg.FileNames;

                if (fileNames != null && fileNames.Length > 0)
                {
                    for (int i = 0; i < fileNames.Length; i++)
                    {
                        FileInfo fi = new FileInfo(fileNames[i]);
                        if (fi.Exists && fi.OpenRead() != null)
                        {
                            IArchive archive = null;

                            switch (fi.Extension)
                            {
                                case ".7z":
                                case ".zip":
                                case ".rar": archive = ArchiveFactory.Open(fileNames[i]); break;
                                default:
                                    {
                                        return;
                                    }
                            }

                            List<IArchiveEntry> archiveEntries = archive.Entries.ToList();
                            archiveEntries.ForEach(x =>
                            {
                                //Debug.WriteLine(x.Key);
                                if (!x.Key.StartsWith("__MACOSX") && (x.Key.ToLower().Contains("cars/") || x.Key.ToLower().Contains("cars\\")))
                                {
                                    CarsJson.Root carRoot = GetLivery(x.OpenEntryStream());
                                    if (carRoot != null && carRoot.customSkinName != string.Empty)
                                    {
                                        Debug.WriteLine($"Found livery {carRoot.teamName} / {carRoot.customSkinName}");

                                        string carsJsonFileName = $"{CarsPath}{GetFileName(x.Key)}";


                                        List<IArchiveEntry> skinFiles = archiveEntries.FindAll(s =>
                                        {
                                            return s.Key.Contains($"/{carRoot.customSkinName}/") && !GetFileName(s.Key).StartsWith(".") && !s.Key.Contains("__MACOSX");
                                        });

                                        if (skinFiles.Count == 0)
                                        {
                                            skinFiles = archiveEntries.FindAll(s => s.Key.Contains($"\\{carRoot.customSkinName}\\"));
                                        }
                                        if (skinFiles.Count == 0)
                                        {
                                            skinFiles = archiveEntries.FindAll(s => s.Key.Contains($"\\\\{carRoot.customSkinName}\\\\"));
                                        }


                                        if (skinFiles.Count > 0)
                                        {
                                            x.WriteToFile(carsJsonFileName);

                                            string[] validSkinFiles = new string[] { "sponsors.json", "sponsors.png", "decals.json", "decals.png", ".dds" };
                                            foreach (IArchiveEntry skinFile in skinFiles)
                                            {
                                                for (int s = 0; s < validSkinFiles.Length; s++)
                                                {
                                                    if (skinFile.Key.ToLower().EndsWith(validSkinFiles[s]))
                                                    {
                                                        string liveryFolder = $"{LiveriesPath}{carRoot.customSkinName}\\";
                                                        Directory.CreateDirectory(liveryFolder);

                                                        string skinFileName = $"{liveryFolder}{GetFileName(skinFile.Key)}";
                                                        skinFile.WriteToFile(skinFileName);
                                                        Debug.WriteLine($"Imported livery file: {skinFileName}");
                                                    }
                                                }

                                            }

                                            MainWindow.Instance.EnqueueSnackbarMessage($"Imported {carRoot.teamName} / {carRoot.customSkinName}");
                                        }
                                    }
                                };
                            });
                        }

                    }
                }

            }

            MainWindow.Instance.EnqueueSnackbarMessage($"Finished importing liveries");
            FetchAllCars();
        }

        private string GetFileName(string fullName)
        {
            string[] split = fullName.Split('/');

            if (split.Length == 1 && split[0].Contains("\\"))
            {
                split = fullName.Split('\\');
            }

            return split[split.Length - 1].Replace("\\", "");
        }

        private void CreateZipButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender.GetType() == typeof(Button))
            {
                Button button = (Button)sender;

                LiveryTreeCar liveryTreeCar = (LiveryTreeCar)button.CommandParameter;

                if (liveryTreeCar.carsRoot.customSkinName == null || liveryTreeCar.carsRoot.customSkinName.Length == 0)
                {
                    goto closeMenu;
                }

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

                // Set filter for file extension and default file extension 
                dlg.DefaultExt = ".zip";
                dlg.AddExtension = true;
                dlg.CheckPathExists = true;
                dlg.FileName = $"{liveryTreeCar.carsRoot.customSkinName}";
                dlg.DefaultExt = ".zip";
                dlg.Filter = "Livery zip|*.zip";
                Nullable<bool> result = dlg.ShowDialog();


                // Get the selected file name and display in a TextBox 
                if (result == true)
                {
                    // Open document 
                    string filename = dlg.FileName;

                    if (filename == null)
                        goto closeMenu;

                    using (ZipArchive zipArchive = ZipArchive.Create())
                    {
                        string liveriesFolder = $"Liveries\\{liveryTreeCar.carsRoot.customSkinName}\\";
                        string carsFolder = "Cars\\";
                        zipArchive.AddEntry($"{carsFolder}{liveryTreeCar.carsFile.Name}", liveryTreeCar.carsFile);

                        DirectoryInfo customSkinDir = new DirectoryInfo(LiveriesPath + liveryTreeCar.carsRoot.customSkinName);
                        if (customSkinDir.Exists)
                        {

                            FileInfo decalsPng = new FileInfo(customSkinDir.FullName + "\\" + "decals.png");
                            FileInfo decalsJson = new FileInfo(customSkinDir.FullName + "\\" + "decals.json");
                            FileInfo sponsorsPng = new FileInfo(customSkinDir.FullName + "\\" + "sponsors.png");
                            FileInfo sponsorsJson = new FileInfo(customSkinDir.FullName + "\\" + "sponsors.json"); ;

                            if (decalsPng.Exists)
                                zipArchive.AddEntry($"{liveriesFolder}{decalsPng.Name}", decalsPng);
                            if (decalsJson.Exists)
                                zipArchive.AddEntry($"{liveriesFolder}{decalsJson.Name}", decalsJson);
                            if (sponsorsPng.Exists)
                                zipArchive.AddEntry($"{liveriesFolder}{sponsorsPng.Name}", sponsorsPng);
                            if (sponsorsJson.Exists)
                                zipArchive.AddEntry($"{liveriesFolder}{sponsorsJson.Name}", sponsorsJson);

                            using (FileStream outputStream = new FileStream(filename, FileMode.Create))
                            {
                                zipArchive.SaveTo(outputStream);
                                MainWindow.Instance.snackbar.MessageQueue.Enqueue($"Livery \"{liveryTreeCar.carsRoot.teamName}\" saved as: {filename}");
                            }
                        }
                    }
                }

            closeMenu:
                (button.Parent as ContextMenu).IsOpen = false;
            }
        }

        private void CreateTeamZipButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender.GetType() == typeof(Button))
            {
                Button button = (Button)sender;

                TreeViewItem treeItem = (TreeViewItem)button.CommandParameter;
                List<LiveryTreeCar> treeCars = new List<LiveryTreeCar>();
                treeItem.Items.OfType<TreeViewItem>().ToList().ForEach(x =>
                {
                    treeCars.Add((LiveryTreeCar)x.DataContext);
                });

                //if (liveryTreeCar.carsRoot.customSkinName == null || liveryTreeCar.carsRoot.customSkinName.Length == 0)
                //{
                //    goto closeMenu;
                //}

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

                // Set filter for file extension and default file extension 
                dlg.DefaultExt = ".zip";
                dlg.AddExtension = true;
                dlg.CheckPathExists = true;
                dlg.FileName = $"{((TextBlock)treeItem.Header).Text}";
                dlg.DefaultExt = ".zip";
                dlg.Filter = "Livery zip|*.zip";
                Nullable<bool> result = dlg.ShowDialog();


                // Get the selected file name and display in a TextBox 
                if (result == true)
                {
                    // Open document 
                    string filename = dlg.FileName;

                    if (filename == null)
                        goto closeMenu;

                    using (ZipArchive zipArchive = ZipArchive.Create())
                    {
                        foreach (LiveryTreeCar liveryTreeCar in treeCars)
                        {
                            string liveriesFolder = $"Liveries\\{liveryTreeCar.carsRoot.customSkinName}\\";
                            string carsFolder = "Cars\\";
                            zipArchive.AddEntry($"{carsFolder}{liveryTreeCar.carsFile.Name}", liveryTreeCar.carsFile);

                            DirectoryInfo customSkinDir = new DirectoryInfo(LiveriesPath + liveryTreeCar.carsRoot.customSkinName);
                            if (customSkinDir.Exists)
                            {

                                FileInfo decalsPng = new FileInfo(customSkinDir.FullName + "\\" + "decals.png");
                                FileInfo decalsJson = new FileInfo(customSkinDir.FullName + "\\" + "decals.json");
                                FileInfo sponsorsPng = new FileInfo(customSkinDir.FullName + "\\" + "sponsors.png");
                                FileInfo sponsorsJson = new FileInfo(customSkinDir.FullName + "\\" + "sponsors.json"); ;

                                if (decalsPng.Exists)
                                    zipArchive.AddEntry($"{liveriesFolder}{decalsPng.Name}", decalsPng);
                                if (decalsJson.Exists)
                                    zipArchive.AddEntry($"{liveriesFolder}{decalsJson.Name}", decalsJson);
                                if (sponsorsPng.Exists)
                                    zipArchive.AddEntry($"{liveriesFolder}{sponsorsPng.Name}", sponsorsPng);
                                if (sponsorsJson.Exists)
                                    zipArchive.AddEntry($"{liveriesFolder}{sponsorsJson.Name}", sponsorsJson);

                            }

                        }

                        using (FileStream outputStream = new FileStream(filename, FileMode.Create))
                        {
                            zipArchive.SaveTo(outputStream);
                            MainWindow.Instance.snackbar.MessageQueue.Enqueue($"Team {((TextBlock)treeItem.Header).Text} saved as: {filename}");
                        }
                    }
                }

            closeMenu:
                (button.Parent as ContextMenu).IsOpen = false;
            }
        }


        public CarsJson.Root GetLivery(FileInfo file)
        {
            if (!file.Exists)
                return null;

            using (FileStream fileStream = file.OpenRead())
            {
                return GetLivery(fileStream);
            }
        }

        public CarsJson.Root GetLivery(Stream stream)
        {
            CarsJson.Root carLiveryRoot = null;
            string jsonString = string.Empty;
            try
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    jsonString = reader.ReadToEnd();
                    jsonString = jsonString.Replace("\0", "");
                    reader.Close();
                    stream.Close();
                }

                carLiveryRoot = JsonConvert.DeserializeObject<CarsJson.Root>(jsonString);
            }
            catch (Exception e)
            {
                //Debug.WriteLine(e);
            }

            return carLiveryRoot;
        }
    }
}
