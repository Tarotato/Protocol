using Shared.Models;
using Shared.Utils;
using Shared.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace Protocol
{
    /// <summary>
    /// The startpage holding the new project and open project options
    /// </summary>
    public sealed partial class StartPage : Page
    {
        private List<InkStrokeContainer> strokes = new List<InkStrokeContainer>();
        private List<CanvasComponent> components = new List<CanvasComponent>();

        public StartPage()
        {
            this.InitializeComponent();
        }

        private async void OnNewProjectClick(object sender, RoutedEventArgs e)
        {
            // Open dialog to let users chose template
            PreloadTemplateDialog templateDialog = new PreloadTemplateDialog();
            await templateDialog.ShowAsync();
            if (templateDialog.result == ContentDialogResult.Primary)
            {
                var template = templateDialog.template;
                this.Frame.Navigate(typeof(MainCanvas), new MainCanvasParams(strokes, null, template, components));
            }
        }

        private async void OnOpenProjectClick(object sender, RoutedEventArgs e)
        {
            TemplateChoice templateChoice = TemplateChoice.None;
            //Let the user pick a project folder to open
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                // Get all the files in the project folder and loop through them
                IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();
                foreach (var f in files)
                {
                    // For each file that matches the files we are looking for, do operations
                    if (f.Name.Equals("metadata.txt")) // Templates
                    {
                        string text = await FileIO.ReadTextAsync(f);
                        templateChoice = (TemplateChoice) Enum.Parse(typeof(TemplateChoice), text);
                    }
                    else if (f.Name.Equals("components.txt")) // Shapes
                    {
                        // read file load shapes
                        string text = await FileIO.ReadTextAsync(f);
                        string[] xmlComponents = text.Split('\n');
                        
                        foreach (string component in xmlComponents)
                        {
                            if (component.Length > 0)
                            {
                                components.Add(Serializer.Deserialize<CanvasComponent>(component));
                            }
                        }

                    }
                    else if(f != null && f.FileType.Equals(".gif")) // .gif are strokes
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
                this.Frame.Navigate(typeof(MainCanvas), new MainCanvasParams(strokes, folder, templateChoice, components));
            }
        }
    }
}
