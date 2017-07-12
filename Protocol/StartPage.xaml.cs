using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Shared.ViewModels;
using Shared.Models;

namespace Protocol
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StartPage : Page
    {
        private List<InkStrokeContainer> strokes;
        private DialogFactory dialogFactory = new DialogFactory();

        public StartPage()
        {
            strokes = new List<InkStrokeContainer>();
            this.InitializeComponent();
        }

        private void OnNewProjectClick(object sender, RoutedEventArgs e)
        {
            ProjectButtons.Visibility = Visibility.Collapsed;
            CanvasSizeButtons.Visibility = Visibility.Visible;
            Title.Text = "Select Desired Platform";
        }

        private async void OnOpenProjectClick(object sender, RoutedEventArgs e)
        {
            //Let the user pick a project folder to open
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();
                foreach (var f in files)
                {
                    if (f != null && f.FileType.Equals(".gif"))
                    {
                        // Open a file stream for reading.
                        IRandomAccessStream stream = await f.OpenAsync(FileAccessMode.Read);
                        // Read from file.
                        using (var inputStream = stream.GetInputStreamAt(0))
                        {
                            var container = new InkStrokeContainer();
                            await container.LoadAsync(inputStream);
                            //Add the strokes stored in the files
                            strokes.Add(container);
                        }
                        stream.Dispose();
                    }
                }
                // TODO size is hard coded
                this.Frame.Navigate(typeof(MainCanvas), new MainCanvasParams(strokes, folder, CanvasSize.Hub));
            }
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            ProjectButtons.Visibility = Visibility.Visible;
            CanvasSizeButtons.Visibility = Visibility.Collapsed;
            Title.Text = "Welcome to Protocol";
        }

        private void OnMobileSizeClick(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainCanvas), new MainCanvasParams(strokes, null, CanvasSize.Mobile));
        }

        private void OnDesktopSizeClick(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainCanvas), new MainCanvasParams(strokes, null, CanvasSize.Desktop));
        }

        private void OnHubSizeClick(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainCanvas), new MainCanvasParams(strokes, null, CanvasSize.Hub));
        }
    }
}
