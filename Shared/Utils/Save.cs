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
        private bool IsTooLarge = false;

        public async void SaveAsImage(double width, double height, List<InkStrokeContainer> _strokes, ProjectMetaData metaData, Canvas recognitionCanvas)
        {
            IsTooLarge = false;
            //Create a bitmap of the recognition canvas so it can be merged with the inkcanvas image
            WriteableBitmap recogWBitmap1 = await SaveRecognitionCanvas1(recognitionCanvas, width, height);

            WriteableBitmap recogWBitmap;

            if (IsTooLarge)
            {
                recogWBitmap = await SaveRecognitionCanvas2(recognitionCanvas, width, height);
            }
            else
            {
                recogWBitmap = new WriteableBitmap((int)width, (int)height);
            }

            //Create a temporary inkcanvas image of the inkcanvas
            StorageFolder pictureFolder = ApplicationData.Current.LocalFolder;
            var file = await pictureFolder.CreateFileAsync("tempImage.png", CreationCollisionOption.ReplaceExisting);

            if (file != null)
            {
                //Write to the provided save file
                CanvasDevice device = CanvasDevice.GetSharedDevice();
                CanvasRenderTarget renderTarget = new CanvasRenderTarget(device, (int)width, (int)height, 96);

                //Add strokes to be drawn
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

                            //Merge inkcanvas image with other images
                            SaveImageFile(await MergeImages(w, metaData, recogWBitmap1, recogWBitmap));                            
                        }
                    }
                }
            }
        }

        private async Task<WriteableBitmap> SaveRecognitionCanvas1(Canvas canvas, double width, double height)
        {
            StorageFolder pictureFolder = ApplicationData.Current.LocalFolder;
            var savefile = await pictureFolder.CreateFileAsync("recogCanvas1.png", CreationCollisionOption.ReplaceExisting);
            if (savefile == null)
                return null;
            IRandomAccessStream stream = await savefile.OpenAsync(FileAccessMode.ReadWrite);

            RenderTargetBitmap renderBitmap = new RenderTargetBitmap();
            await renderBitmap.RenderAsync(canvas, (int)width, (int)height);
            IBuffer x = await renderBitmap.GetPixelsAsync();

            //Only draw second bitmap when canvas is full screen (large)
            //7664640 - max buffer size
            int value;
            if ((int)x.Length > 7664640)
            {
                IsTooLarge = true;
                value = 7664640;
            }
            else
            {
                value = (int)x.Length;
            }

            //Write the image pixels to a bitmap
            WriteableBitmap WBitmap = new WriteableBitmap((int)width, (int)height);
            using (Stream pb = WBitmap.PixelBuffer.AsStream())
            {
                await pb.WriteAsync(WindowsRuntimeBufferExtensions.ToArray(x), 0, value);
            }

            Stream pixelStream = WBitmap.PixelBuffer.AsStream();
            byte[] pixels = new byte[pixelStream.Length];
            var length = pixels.Length;
            await pixelStream.ReadAsync(pixels, 0, pixels.Length);

            // Save the image file with jpg extension 
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)WBitmap.PixelWidth, (uint)WBitmap.PixelHeight, 96.0, 96.0, pixels);
            await encoder.FlushAsync();

            return WBitmap;
        }

        private async Task<WriteableBitmap> SaveRecognitionCanvas2(Canvas canvas, double width, double height)
        {
            StorageFolder pictureFolder = ApplicationData.Current.LocalFolder;
            var savefile = await pictureFolder.CreateFileAsync("recogCanvas2.png", CreationCollisionOption.ReplaceExisting);
            if (savefile == null)
                return null;
            IRandomAccessStream stream = await savefile.OpenAsync(FileAccessMode.ReadWrite);

            RenderTargetBitmap renderBitmap = new RenderTargetBitmap();
            await renderBitmap.RenderAsync(canvas, (int)width, (int)height);
            IBuffer x = await renderBitmap.GetPixelsAsync();

            //Write the image pixels to a bitmap
            WriteableBitmap WBitmap = new WriteableBitmap((int)width, (int)height);
            byte[] lol = WindowsRuntimeBufferExtensions.ToArray(x);
            Array.Reverse(lol);
            using (Stream pb = WBitmap.PixelBuffer.AsStream())
            {
                await pb.WriteAsync(lol, 7664640, (int)x.Length - 7664640);
            }
            
            Stream pixelStream = WBitmap.PixelBuffer.AsStream();
            byte[] pixels = new byte[pixelStream.Length];
            var length = pixels.Length;
            await pixelStream.ReadAsync(pixels, 0, pixels.Length);

            // Save the image file with jpg extension 
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)WBitmap.PixelWidth, (uint)WBitmap.PixelHeight, 96.0, 96.0, pixels);
            encoder.BitmapTransform.Rotation = BitmapRotation.Clockwise180Degrees;
            await encoder.FlushAsync();

            return WBitmap;
        }


        private async Task<WriteableBitmap> MergeImages(WriteableBitmap originalImage, ProjectMetaData metaData, WriteableBitmap shapes1, WriteableBitmap shapes)
        {
            // Merge inkcanvas with recognition canvas
            var writeableBmp = new WriteableBitmap(1, 1);
            var mergedImage = originalImage;
            var background = metaData.bgImage;

            // background.Blit(new Rect(0, 0, background.PixelWidth, background.PixelHeight), mergedImage, new Rect(0, 0, mergedImage.PixelWidth, mergedImage.PixelHeight));
            mergedImage.Blit(new Rect(0, 0, mergedImage.PixelWidth, mergedImage.PixelHeight), shapes1, new Rect(0, 0, shapes1.PixelWidth, shapes1.PixelHeight));
            mergedImage.Blit(new Rect(0, 0, mergedImage.PixelWidth, mergedImage.PixelHeight), shapes, new Rect(0, 0, shapes.PixelWidth, shapes.PixelHeight));

            //Merge image with template if visible
            if (metaData.templateVisibility == Visibility.Visible)
            {
                var templateImage = await writeableBmp.FromContent(new Uri($"ms-appx:///Assets/{metaData.templateChoice.ToString()}.png"));
                mergedImage.Blit(new Rect(0, 0, mergedImage.PixelWidth, mergedImage.PixelHeight), templateImage, new Rect(0, 0, templateImage.PixelWidth, templateImage.PixelHeight));
            }
            //mergedImage.Blit(new Rect(0, 0, mergedImage.PixelWidth, mergedImage.PixelHeight), background, new Rect(0, 0, background.PixelWidth, background.PixelHeight));

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
