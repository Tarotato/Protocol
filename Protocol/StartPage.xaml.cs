using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Protocol
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StartPage : Page
    {
        private List<InkStrokeContainer> strokes;
        public StartPage()
        {
            strokes = new List<InkStrokeContainer>();
            this.InitializeComponent();
        }

        private void OnNewProjectClick(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainCanvas), strokes);
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
                    if (f != null)
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
                        this.Frame.Navigate(typeof(MainCanvas), strokes);
                    }
                    // User selects Cancel and picker returns null.
                    else
                    {
                        // Operation cancelled.
                    }
                }
            }
        }
    }
}
