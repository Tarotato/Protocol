using Microsoft.Graphics.Canvas;
using Shared.Models;
using Shared.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
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

        public async void SaveAsImage(double width, double height, List<InkStrokeContainer> _strokes)
        {
            // Set up and launch the Save Picker
            //FileSavePicker fileSavePicker = new FileSavePicker();
            //fileSavePicker.FileTypeChoices.Add("JPEG", new string[] { ".jpeg" });
            //fileSavePicker.FileTypeChoices.Add("PNG", new string[] { ".png" });
            //var InstallationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation.CreateFolderAsync(@"Assets\ProfilePictures", CreationCollisionOption.OpenIfExists);
            //StorageFile file = await fileSavePicker.PickSaveFileAsync();
            StorageFolder pictureFolder = ApplicationData.Current.LocalFolder;
            var file = await pictureFolder.CreateFileAsync("tempImage.jpeg", CreationCollisionOption.ReplaceExisting);

            if (file != null)
            {
                // At this point, the app can begin writing to the provided save file
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
                    if (file.FileType.Equals(".jpeg"))
                    {
                        await renderTarget.SaveAsync(fileStream, CanvasBitmapFileFormat.Jpeg, 1f);
                        ShowFlyoutAboveInkToolbar?.Invoke("Image Saved");
                    }
                    else
                    {
                        await renderTarget.SaveAsync(fileStream, CanvasBitmapFileFormat.Png, 1f);
                        ShowFlyoutAboveInkToolbar?.Invoke("Image Saved");
                    }
                }
            var bitmap = new WriteableBitmap(1, 1);
            var savedfiles = await pictureFolder.GetFilesAsync();
             BitmapImage bitmapImage = new BitmapImage();
            WriteableBitmap w;
            foreach (var imageFile in savedfiles)
            {
                if (imageFile.Name.StartsWith("tempImage"))
                {
                    using (IRandomAccessStream fileStream = await imageFile.OpenAsync(FileAccessMode.Read))
                    {
                        bitmapImage.DecodePixelHeight = 100;
                        bitmapImage.DecodePixelWidth = 100;
                        await bitmapImage.SetSourceAsync(fileStream);                        
                    }

                    int h = bitmapImage.PixelHeight;
                    int wd = bitmapImage.PixelWidth;

                    using (var stream = await file.OpenReadAsync())
                    {
                        w = new WriteableBitmap(wd, h);
                        await w.SetSourceAsync(stream);
                        //Other code
                    thingAsync(w);
                    }
                }
            }
            }

        }

        private async void thingAsync(WriteableBitmap wBitmap)
        {
            var writeableBmp = new WriteableBitmap(1, 1);

            var image1 = wBitmap;
            var image2 = await writeableBmp.FromContent(new Uri("ms-appx:///Assets/browser.png"));

            image1.Blit(new Rect(0, 0, image1.PixelWidth, image1.PixelHeight), image2, new Rect(0, 0, image2.PixelWidth, image2.PixelHeight));

            // Save the writeableBitmap object to JPG Image file 
            FileSavePicker picker = new FileSavePicker();
            picker.FileTypeChoices.Add("JPG File", new List<string>() { ".jpg" });
            StorageFile savefile = await picker.PickSaveFileAsync();
            if (savefile == null)
                return;
            IRandomAccessStream stream = await savefile.OpenAsync(FileAccessMode.ReadWrite);
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
            // Get pixels of the WriteableBitmap object 
            Stream pixelStream = image1.PixelBuffer.AsStream();
            byte[] pixels = new byte[pixelStream.Length];
            await pixelStream.ReadAsync(pixels, 0, pixels.Length);
            // Save the image file with jpg extension 
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)image1.PixelWidth, (uint)image1.PixelHeight, 96.0, 96.0, pixels);
            await encoder.FlushAsync();

        }

        public async Task<StorageFolder> SaveProject(StorageFolder storageFolder, List<InkStrokeContainer> strokes, ProjectMetaData metaData)
        {
            //Use existing folder
            if (storageFolder != null)
            {
                SaveStrokes(storageFolder, strokes);
                SaveMetaData(metaData, storageFolder);
                return storageFolder;
            }

            //Create new folder to save files
            SaveDialog save = new SaveDialog();
            await save.ShowAsync();
            if (save.result == ContentDialogResult.Primary)
            {
                //Save
                SaveStrokes(save.folder, strokes);
                SaveMetaData(metaData, storageFolder);
                storageFolder = save.folder;
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
            var file = await storageFolder.CreateFileAsync($"{storageFolder.Name}Protocol.txt", CreationCollisionOption.ReplaceExisting);
            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            using (var outputStream = stream.GetOutputStreamAt(0))
            {
                // We'll add more code here in the next step.
                using (var dataWriter = new DataWriter(outputStream))
                {
                    dataWriter.WriteString($"Template Visible: {metaData.templateVisibility.ToString()}");
                    dataWriter.WriteString($"Template Choice: {metaData.templateChoice.ToString()}");
                    await dataWriter.StoreAsync();
                    await outputStream.FlushAsync();
                }
            }
            stream.Dispose(); // Or use the stream variable (see previous code snippet) with a using statement as well.
        }

        public async Task<ContentDialogResult> ConfirmSave(List<InkStrokeContainer> strokes, StorageFolder folder, ProjectMetaData metaData)
        {
            TernaryButtonDialog t = new TernaryButtonDialog();
            await t.ShowAsync();
            if (t.result == ContentDialogResult.Primary)
            {
                await SaveProject(folder, strokes, metaData);
            }
            return t.result;
        }

        public async Task<MainCanvasParams> OpenProject(List<InkStrokeContainer> currentStrokes, StorageFolder currentFolder, ProjectMetaData metaData)
        {
            List<InkStrokeContainer> newStrokes = new List<InkStrokeContainer>();
            //Let the user pick a project folder to open
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder newFolder = await folderPicker.PickSingleFolderAsync();
            if (newFolder != null)
            {
                IReadOnlyList<StorageFile> files = await newFolder.GetFilesAsync();
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
                            newStrokes.Add(container);
                        }
                        stream.Dispose();
                    }
                }
                var result = await ConfirmSave(currentStrokes, currentFolder, metaData);
                if (result != ContentDialogResult.None)
                {
                    return new MainCanvasParams(newStrokes, newFolder, TemplateChoice.None);
                }
            }
            return null;
        }
    }
}
