using Microsoft.Graphics.Canvas;
using Shared.Models;
using Shared.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Shared.Utils
{
    public class Save
    {
        public delegate void ChangedEventHandler(String message);
        public event ChangedEventHandler ShowFlyoutAboveInkToolbar;

        public async void SaveAsImage(double width, double height, List<InkStrokeContainer> _strokes, ProjectMetaData metaData)
        {
            //Create a temporary file of the image so it can be merged with the template
            StorageFolder pictureFolder = ApplicationData.Current.LocalFolder;
            var file = await pictureFolder.CreateFileAsync("tempImage.jpeg", CreationCollisionOption.ReplaceExisting);

            if (file != null)
            {
                //Write to the provided save file
                CanvasDevice device = CanvasDevice.GetSharedDevice();
                CanvasRenderTarget renderTarget = new CanvasRenderTarget(device, (int)width, (int)height, 96);

                using (var ds = renderTarget.CreateDrawingSession())
                {
                    ds.Clear(Colors.White);
                    foreach (var item in _strokes)
                    {
                        ds.DrawInk(item.GetStrokes());
                    }
                }

                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await renderTarget.SaveAsync(fileStream, CanvasBitmapFileFormat.Jpeg, 1f);                    
                }

                //Load image as BitmapImage then convert to WriteableBitmap
                BitmapImage bitmapImage = new BitmapImage();
                WriteableBitmap w;
                var savedfiles = await pictureFolder.GetFilesAsync();
                foreach (var imageFile in savedfiles)
                {
                    if (imageFile.Name.StartsWith("tempImage"))
                    {
                        //Create BitmapImage
                        using (IRandomAccessStream fileStream = await imageFile.OpenAsync(FileAccessMode.Read))
                        {
                            bitmapImage.DecodePixelHeight = 100;
                            bitmapImage.DecodePixelWidth = 100;
                            await bitmapImage.SetSourceAsync(fileStream);                        
                        }

                        int h = bitmapImage.PixelHeight;
                        int wd = bitmapImage.PixelWidth;
                        //Create WriteableBitmap
                        using (var stream = await file.OpenReadAsync())
                        {
                            w = new WriteableBitmap(wd, h);
                            await w.SetSourceAsync(stream);
                            //Merge image with template selected
                            SaveImageFile(await MergeImages(w, metaData));
                        }
                    }
                }
            }
        }

        private async Task<WriteableBitmap> MergeImages(WriteableBitmap originalImage, ProjectMetaData metaData)
        {
            if (metaData.templateVisibility == Visibility.Collapsed)
                return originalImage;

            //Merge image with template if visible
            var writeableBmp = new WriteableBitmap(1, 1);
            var mergedImage = originalImage;
            var templateImage = await writeableBmp.FromContent(new Uri($"ms-appx:///Assets/{metaData.templateChoice.ToString()}.png"));

            mergedImage.Blit(new Rect(0, 0, mergedImage.PixelWidth, mergedImage.PixelHeight), templateImage, new Rect(0, 0, templateImage.PixelWidth, templateImage.PixelHeight));

            return mergedImage;
        }

        private async void SaveImageFile(WriteableBitmap finalImage)
        {
            // Set up and launch the Save Picker
            FileSavePicker fileSavePicker = new FileSavePicker();
            fileSavePicker.FileTypeChoices.Add("JPEG", new string[] { ".jpeg" });
            fileSavePicker.FileTypeChoices.Add("PNG", new string[] { ".png" });
            StorageFile savefile = await fileSavePicker.PickSaveFileAsync();
            if (savefile == null)
                return;
            IRandomAccessStream stream = await savefile.OpenAsync(FileAccessMode.ReadWrite);
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

            // Get pixels of the WriteableBitmap object 
            Stream pixelStream = finalImage.PixelBuffer.AsStream();
            byte[] pixels = new byte[pixelStream.Length];
            await pixelStream.ReadAsync(pixels, 0, pixels.Length);

            // Save the image file with jpg extension 
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)finalImage.PixelWidth, (uint)finalImage.PixelHeight, 96.0, 96.0, pixels);
            await encoder.FlushAsync();

            ShowFlyoutAboveInkToolbar?.Invoke("Image Saved");
        }
        public async Task<StorageFolder> SaveProject(StorageFolder storageFolder, List<InkStrokeContainer> strokes, ProjectMetaData metaData, List<CanvasComponent> components)
        {
            //Use existing folder
            if (storageFolder != null)
            {
                SaveStrokes(storageFolder, strokes);
                SaveMetaData(metaData, storageFolder);
                SaveCanvasComponents(components, storageFolder);
                return storageFolder;
            }

            //Create new folder to save files
            SaveDialog save = new SaveDialog();
            await save.ShowAsync();
            if (save.result == ContentDialogResult.Primary)
            {
                //Save
                storageFolder = save.folder;
                SaveStrokes(storageFolder, strokes);
                SaveMetaData(metaData, storageFolder);
                SaveCanvasComponents(components, storageFolder);
                return storageFolder;
            }
            return null;
        }

        private bool IsValidName(string name)
        {
            // TODO: Input Checking
            return true;
        }

        private async void SaveStrokes(StorageFolder storageFolder, List<InkStrokeContainer> _strokes)
        {
            // Remove existing files from storageFolder
            if (storageFolder != null)
            {
                IReadOnlyList<StorageFile> files = await storageFolder.GetFilesAsync();
                foreach (var f in files)
                {
                    if (f.FileType.Equals(".gif") && f.DisplayName.StartsWith("InkStroke"))
                    {
                        await f.DeleteAsync();
                    }
                }
            }

            // Get all strokes on the InkCanvas.
            IReadOnlyList<InkStroke> currentStrokes;
            int i = 0;
            foreach (var item in _strokes)
            {
                if (item.GetStrokes().Count > 0)
                {
                    // Strokes present on ink canvas.
                    currentStrokes = item.GetStrokes();

                    var file = await storageFolder.CreateFileAsync("InkStroke" + i.ToString() + ".gif", CreationCollisionOption.ReplaceExisting);
                    // When chosen, picker returns a reference to the selected file.
                    if (file != null)
                    {
                        // Prevent updates to the file until updates are finalized with call to CompleteUpdatesAsync.
                        CachedFileManager.DeferUpdates(file);

                        // Open a file stream for writing.
                        IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);

                        // Write the ink strokes to the output stream.
                        using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
                        {
                            await item.SaveAsync(outputStream);
                            await outputStream.FlushAsync();
                        }
                        stream.Dispose();

                        // Finalize write so other apps can update file.
                        Windows.Storage.Provider.FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                    }
                    i++;
                }
            }
            ShowFlyoutAboveInkToolbar?.Invoke("Project Saved");
        }

        private async void SaveMetaData(ProjectMetaData metaData, StorageFolder storageFolder)
        {
            var file = await storageFolder.CreateFileAsync("metadata.txt", CreationCollisionOption.ReplaceExisting);
            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            using (var outputStream = stream.GetOutputStreamAt(0))
            {
                // We'll add more code here in the next step.
                using (var dataWriter = new DataWriter(outputStream))
                {
                    if (metaData.templateVisibility == Visibility.Collapsed)
                    {
                        metaData.templateChoice = TemplateChoice.None;
                    }
                    dataWriter.WriteString($"{metaData.templateChoice.ToString()}");
                    await dataWriter.StoreAsync();
                    await outputStream.FlushAsync();
                }
            }
            stream.Dispose(); // Or use the stream variable (see previous code snippet) with a using statement as well.
        }

        private async void SaveCanvasComponents(List<CanvasComponent> components,StorageFolder storageFolder)
        {
            var file = await storageFolder.CreateFileAsync("components.txt", CreationCollisionOption.ReplaceExisting);
            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            using (var outputStream = stream.GetOutputStreamAt(0))
            {
                // We'll add more code here in the next step.
                using (var dataWriter = new DataWriter(outputStream))
                {
                    foreach (CanvasComponent component in components)
                    {
                        dataWriter.WriteString($"{Serializer.Serialize(component)}\n");
                    }

                    await dataWriter.StoreAsync();
                    await outputStream.FlushAsync();
                }
            }
            stream.Dispose(); // Or use the stream variable (see previous code snippet) with a using statement as well.
        }

        public async Task<ContentDialogResult> ConfirmSave(List<InkStrokeContainer> strokes, StorageFolder folder, ProjectMetaData metaData, List<CanvasComponent> components)
        {
            TernaryButtonDialog t = new TernaryButtonDialog();
            await t.ShowAsync();
            if (t.result == ContentDialogResult.Primary)
            {
                await SaveProject(folder, strokes, metaData, components);
            }
            return t.result;
        }

        public async Task<MainCanvasParams> OpenProject(List<InkStrokeContainer> currentStrokes, StorageFolder currentFolder, ProjectMetaData metaData, List<CanvasComponent> components)
        {
            List<InkStrokeContainer> newStrokes = new List<InkStrokeContainer>();
            TemplateChoice templateChoice = TemplateChoice.None;
            //Let the user pick a project folder to open
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder newFolder = await folderPicker.PickSingleFolderAsync();
            if (newFolder != null)
            {
                IReadOnlyList<StorageFile> files = await newFolder.GetFilesAsync();
                foreach (var f in files)
                {
                    if (f.Name.Equals("metadata.txt"))
                    {
                        string text = await FileIO.ReadTextAsync(f);
                        templateChoice = (TemplateChoice)Enum.Parse(typeof(TemplateChoice), text);
                    }
                    else if (f.Name.Equals("components.txt"))
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
                    else if (f != null && f.FileType.Equals(".gif"))
                    {
                        // Open a file stream for reading.
                        IRandomAccessStream stream = await f.OpenAsync(FileAccessMode.Read);
                        // Read from file.
                        using (var inputStream = stream.GetInputStreamAt(0))
                        {
                            var container = new InkStrokeContainer();
                            await container.LoadAsync(inputStream);
                            //Add the strokes stored in the files
                            newStrokes.Add(container);
                        }
                        stream.Dispose();
                    }
                }
                var result = await ConfirmSave(currentStrokes, currentFolder, metaData, components);
                if (result != ContentDialogResult.None)
                {
                    return new MainCanvasParams(newStrokes, newFolder, templateChoice, components);
                }
            }
            return null;
        }
    }
}
