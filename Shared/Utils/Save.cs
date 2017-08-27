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
        public async void SaveAsImage(double width, double height, List<InkStrokeContainer> _strokes, ProjectMetaData metaData, Canvas recognitionCanvas)
        {

            WriteableBitmap wb = await SaveRecognitionCanvas(recognitionCanvas);
            //Create a temporary file of the image so it can be merged with the template
            StorageFolder pictureFolder = ApplicationData.Current.LocalFolder;
            var file = await pictureFolder.CreateFileAsync("tempImage.png", CreationCollisionOption.ReplaceExisting);

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
                    await renderTarget.SaveAsync(fileStream, CanvasBitmapFileFormat.Png, 1f);
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
                            SaveImageFile(await MergeImages(w, wb));                            
                        }
                    }
                }
            }
        }

        private async Task<WriteableBitmap> SaveRecognitionCanvas(Canvas canvas)
        {
            // Set up and launch the Save Picker
            FileSavePicker fileSavePicker = new FileSavePicker();
            fileSavePicker.FileTypeChoices.Add("PNG", new string[] { ".png" });
            StorageFile savefile = await fileSavePicker.PickSaveFileAsync();
            if (savefile == null)
                return null;
            IRandomAccessStream stream = await savefile.OpenAsync(FileAccessMode.ReadWrite);

            RenderTargetBitmap renderBitmap = new RenderTargetBitmap();
            // needed otherwise the image output is black
            canvas.Measure(new Size((int)canvas.ActualWidth, (int)canvas.ActualHeight));
            canvas.Arrange(new Rect(0,0, (int)canvas.ActualWidth, (int)canvas.ActualHeight));

            await renderBitmap.RenderAsync(canvas, (int)canvas.ActualWidth, (int)canvas.ActualHeight);
            IBuffer x = await renderBitmap.GetPixelsAsync();

            //Write the image pixels to a bitmap
            WriteableBitmap wb = new WriteableBitmap((int)canvas.ActualWidth, (int)canvas.ActualHeight);
            using (Stream str = wb.PixelBuffer.AsStream())
            {
                await str.WriteAsync(WindowsRuntimeBufferExtensions.ToArray(x), 0, (int)x.Length);
            }

            Stream pixelStream = wb.PixelBuffer.AsStream();
            byte[] pixels = new byte[pixelStream.Length];
            await pixelStream.ReadAsync(pixels, 0, pixels.Length);

            // Save the image file with jpg extension 
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)wb.PixelWidth, (uint)wb.PixelHeight, 96.0, 96.0, pixels);
            await encoder.FlushAsync();

            return wb;
        }    

        private async Task<WriteableBitmap> MergeImages(WriteableBitmap originalImage, WriteableBitmap shapes)
        {
            //if (metaData.templateVisibility == Visibility.Collapsed)
            //    return originalImage;

            //Merge image with template if visible
            var writeableBmp = new WriteableBitmap(1, 1);
            var mergedImage = originalImage;
            //var templateImage = await writeableBmp.FromContent(new Uri($"ms-appx:///Assets/{metaData.templateChoice.ToString()}.png"));
            //var background = await writeableBmp.FromContent(new Uri($"ms-appx:///Assets/DefaultBackground.png"));

            //background.Blit(new Rect(0, 0, background.PixelWidth, background.PixelHeight), templateImage, new Rect(0, 0, templateImage.PixelWidth, templateImage.PixelHeight));
            mergedImage.Blit(new Rect(0, 0, mergedImage.PixelWidth, mergedImage.PixelHeight), shapes, new Rect(0, 0, shapes.PixelWidth, shapes.PixelHeight));
            //shapes.Blit(new Rect(0, 0, shapes.PixelWidth, shapes.PixelHeight), mergedImage, new Rect(0, 0, mergedImage.PixelWidth, mergedImage.PixelHeight));

            return mergedImage;
        }

        private async void SaveImageFile(WriteableBitmap finalImage)
        {
            // Set up and launch the Save Picker
            FileSavePicker fileSavePicker = new FileSavePicker();
            fileSavePicker.FileTypeChoices.Add("PNG", new string[] { ".png" });
            StorageFile savefile = await fileSavePicker.PickSaveFileAsync();
            if (savefile == null)
                return;
            IRandomAccessStream stream = await savefile.OpenAsync(FileAccessMode.ReadWrite);
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

            // Get pixels of the WriteableBitmap object 
            Stream pixelStream = finalImage.PixelBuffer.AsStream();
            byte[] pixels = new byte[pixelStream.Length];
            await pixelStream.ReadAsync(pixels, 0, pixels.Length);

            // Save the image file with jpg extension 
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)finalImage.PixelWidth, (uint)finalImage.PixelHeight, 96.0, 96.0, pixels);
            await encoder.FlushAsync();

            ShowFlyoutAboveInkToolbar?.Invoke("Image Saved");
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
                    dataWriter.WriteString($"Template Visible: {metaData.templateVisibility.ToString()}\n");
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
