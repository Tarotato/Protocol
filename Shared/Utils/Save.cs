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
    /// <summary>
    ///     This save class handles saving the canvas as an image and as a project
    /// </summary>
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
            
            //Create a second bitmap if the pixel stream is too large
            WriteableBitmap recogWBitmap;

            if (IsTooLarge)
            {
                recogWBitmap = await SaveRecognitionCanvas2(recognitionCanvas, width, height);
            }
            else
            {
                recogWBitmap = new WriteableBitmap((int)width, (int)height);
            }

            //Create an image to save the InkCanvas strokes to
            StorageFolder pictureFolder = ApplicationData.Current.LocalFolder;
            var file = await pictureFolder.CreateFileAsync("tempImage.png", CreationCollisionOption.ReplaceExisting);

            if (file != null)
            {
                //Write to the provided save file tempImage.png
                CanvasDevice device = CanvasDevice.GetSharedDevice();
                CanvasRenderTarget renderTarget = new CanvasRenderTarget(device, (int)width, (int)height, 96);

                //Add strokes to be drawn to the drawing session
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
                    //Save the image to tempImage.png
                    await renderTarget.SaveAsync(fileStream, CanvasBitmapFileFormat.Png, 1f);
                }

                //Load tempImage.png as a BitmapImage then convert to WriteableBitmap
                BitmapImage bitmapImage = new BitmapImage();
                WriteableBitmap w;
                var savedfiles = await pictureFolder.GetFilesAsync();
                foreach (var imageFile in savedfiles)
                {
                    if (imageFile.Name.StartsWith("tempImage"))
                    {
                        //Read the image file
                        using (IRandomAccessStream fileStream = await imageFile.OpenAsync(FileAccessMode.Read))
                        {
                            bitmapImage.DecodePixelHeight = 100;
                            bitmapImage.DecodePixelWidth = 100;
                            //Write the image into a bitmap
                            await bitmapImage.SetSourceAsync(fileStream);
                        }

                        int h = bitmapImage.PixelHeight;
                        int wd = bitmapImage.PixelWidth;
                        
                        using (var stream = await file.OpenReadAsync())
                        {
                            //Obtain the bitmap dimensions for the WritatbleBitmap
                            w = new WriteableBitmap(wd, h);
                            //Write the contents of tempImage.png to the WritableBitmap
                            await w.SetSourceAsync(stream);

                            //Merge the inkcanvas image with the shapes on the recognition canvas
                            SaveImageFile(await MergeImages(w, metaData, recogWBitmap1, recogWBitmap));                            
                        }
                    }
                }
            }
        }        
        
        private async Task<WriteableBitmap> SaveRecognitionCanvas1(Canvas canvas, double width, double height)
        {
            //Create a temporary image file for the recognition canvas - recogCanvas.png
            StorageFolder pictureFolder = ApplicationData.Current.LocalFolder;
            var savefile = await pictureFolder.CreateFileAsync("recogCanvas.png", CreationCollisionOption.GenerateUniqueName);
            if (savefile == null)
                return null;
            IRandomAccessStream stream = await savefile.OpenAsync(FileAccessMode.ReadWrite);

            //Retrieve the pixel data from the canvas
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap();
            await renderBitmap.RenderAsync(canvas, (int)width, (int)height);
            IBuffer x = await renderBitmap.GetPixelsAsync();

            //Set the IsTooLarge flag if the IBuffer is too long for writing. This prevents an exception and writes only half the image.
            //7664640 - max buffer size
            int value;
            if ((int)x.Length > 7664640)
            {
                //Write only the max length of data
                IsTooLarge = true;
                value = 7664640;
            }
            else
            {
                //Write the whole lenght of data
                value = (int)x.Length;
            }

            //Write the image pixels to a bitmap
            WriteableBitmap WBitmap = new WriteableBitmap((int)width, (int)height);
            using (Stream pb = WBitmap.PixelBuffer.AsStream())
            {
                //This method will throw and exception if he pixel array is too large
                await pb.WriteAsync(WindowsRuntimeBufferExtensions.ToArray(x), 0, value);
            }

            Stream pixelStream = WBitmap.PixelBuffer.AsStream();
            byte[] pixels = new byte[pixelStream.Length];
            var length = pixels.Length;
            await pixelStream.ReadAsync(pixels, 0, pixels.Length);

            // Save the image file with png extension so the image is transparent for merging
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)WBitmap.PixelWidth, (uint)WBitmap.PixelHeight, 96.0, 96.0, pixels);
            await encoder.FlushAsync();

            return WBitmap;
        }

        private async Task<WriteableBitmap> SaveRecognitionCanvas2(Canvas canvas, double width, double height)
        {
            //Create a temporary image file for the recognition canvas - recogCanvas.png
            StorageFolder pictureFolder = ApplicationData.Current.LocalFolder;
            var savefile = await pictureFolder.CreateFileAsync("recogCanvas.png", CreationCollisionOption.GenerateUniqueName);
            if (savefile == null)
                return null;
            IRandomAccessStream stream = await savefile.OpenAsync(FileAccessMode.ReadWrite);

            //Retrieve the pixel data from the canvas
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap();
            await renderBitmap.RenderAsync(canvas, (int)width, (int)height);
            IBuffer x = await renderBitmap.GetPixelsAsync();

            //Write the image pixels to a bitmap
            WriteableBitmap WBitmap = new WriteableBitmap((int)width, (int)height);
            byte[] pixelArray = WindowsRuntimeBufferExtensions.ToArray(x);
            //Reverse the array to write the rest of the pixel data, since writing can only start from the beginning of an array
            Array.Reverse(pixelArray);
            using (Stream pb = WBitmap.PixelBuffer.AsStream())
            {
                //Write the remaing length of the pixels (the original array - first 7664640 values that were written)
                await pb.WriteAsync(pixelArray, 7664640, (int)x.Length - 7664640);
            }
            
            Stream pixelStream = WBitmap.PixelBuffer.AsStream();
            byte[] pixels = new byte[pixelStream.Length];
            var length = pixels.Length;
            await pixelStream.ReadAsync(pixels, 0, pixels.Length);

            // Save the image file with png extension so the image is transparent for merging
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)WBitmap.PixelWidth, (uint)WBitmap.PixelHeight, 96.0, 96.0, pixels);
            //Rotate the image 180 degreees, as it was written upside down due to reversing the pixel array
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

            // Save the image file with png extension 
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)finalImage.PixelWidth, (uint)finalImage.PixelHeight, 96.0, 96.0, pixels);
            await encoder.FlushAsync();

            ShowFlyoutAboveInkToolbar?.Invoke("Image Saved");
        }
        public async Task<StorageFolder> SaveProject(StorageFolder storageFolder, List<InkStrokeContainer> strokes, ProjectMetaData metaData, List<CanvasComponent> components)
        {
            //Use existing folder
            if (storageFolder != null)
            {
                //Save the strokes drawn, the project metadata so the current state of the eapplication is saved, and the shapes
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
                //Store the chosen folder so it is automatically saved to the location specified in the future
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
            //Create a text file to store the project metadata
            var file = await storageFolder.CreateFileAsync("metadata.txt", CreationCollisionOption.ReplaceExisting);
            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            using (var outputStream = stream.GetOutputStreamAt(0))
            {
                // Store information about the template chosen if it was enabled
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
            stream.Dispose();
        }

        private async void SaveCanvasComponents(List<CanvasComponent> components,StorageFolder storageFolder)
        {
            //Create a text file to store the computer generated shapes (components) in the project
            var file = await storageFolder.CreateFileAsync("components.txt", CreationCollisionOption.ReplaceExisting);
            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            using (var outputStream = stream.GetOutputStreamAt(0))
            {
                // Store information about each computer generated shape in the project
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
            stream.Dispose(); 
        }

        public async Task<ContentDialogResult> ConfirmSave(List<InkStrokeContainer> strokes, StorageFolder folder, ProjectMetaData metaData, List<CanvasComponent> components)
        {
            //Open the dialog for confirming save before navigation away from the curent project
            TernaryButtonDialog t = new TernaryButtonDialog();
            await t.ShowAsync();
            if (t.result == ContentDialogResult.Primary)
            {
                //If they confirm save, execute save method
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
