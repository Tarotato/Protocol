using Microsoft.Graphics.Canvas;
using Shared.Models;
using Shared.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Shared.Utils
{
    public class Save
    {
        public delegate void ChangedEventHandler(String message);
        public event ChangedEventHandler ShowFlyoutAboveInkToolbar;

        public async void SaveAsImage(double width, double height, List<InkStrokeContainer> _strokes)
        {
            // Set up and launch the Save Picker
            FileSavePicker fileSavePicker = new FileSavePicker();
            fileSavePicker.FileTypeChoices.Add("JPEG", new string[] { ".jpeg" });
            fileSavePicker.FileTypeChoices.Add("PNG", new string[] { ".png" });

            StorageFile file = await fileSavePicker.PickSaveFileAsync();
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
            }
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
