using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.Storage.Streams;
using Shared.ViewModels;
using Shared.Views;
using System.Threading.Tasks;

namespace Protocol
{
	public class MainCanvasViewModel
	{
		public delegate void ChangedEventHandler();
		public event ChangedEventHandler DrawCanvasInvalidated;

		// fields for dry erasing
		private bool _isErasing;
		private Point _lastPoint;
		private List<InkStrokeContainer> _strokes;
        private StorageFolder _storageFolder;
        private DialogFactory _dialogFactory = new DialogFactory();

        public MainCanvasViewModel(MainCanvasParams parameters)
        {
            _strokes = parameters.strokes;
            _storageFolder = parameters.folder; 
        }

		internal void AddStroke(InkStrokeContainer container)
		{
			_strokes.Add(container);
		}

		internal void ClearStokes()
		{
			_strokes.Clear();
		}

		internal void DrawInk(CanvasDrawingSession session)
		{
			foreach (var item in _strokes)
			{
				var strokes = item.GetStrokes();

				using (var list = new CanvasCommandList(session))
				{
					using (var listSession = list.CreateDrawingSession())
					{
						listSession.DrawInk(strokes);
					}
				}

				session.DrawInk(strokes);
			}
		}

		internal void StartErasing(Point point)
		{
			_lastPoint = point;
			_isErasing = true;
		}

		internal void AddListeners(InkUnprocessedInput unprocessedInput)
		{
			unprocessedInput.PointerMoved += UnprocessedInput_PointerMoved;
			unprocessedInput.PointerReleased += UnprocessedInput_PointerReleased;
			unprocessedInput.PointerExited += UnprocessedInput_PointerExited;
			unprocessedInput.PointerLost += UnprocessedInput_PointerLost;
		}

		internal void RemoveListeners(InkUnprocessedInput unprocessedInput)
		{
			unprocessedInput.PointerMoved -= UnprocessedInput_PointerMoved;
			unprocessedInput.PointerReleased -= UnprocessedInput_PointerReleased;
			unprocessedInput.PointerExited -= UnprocessedInput_PointerExited;
			unprocessedInput.PointerLost -= UnprocessedInput_PointerLost;
		}

		private void UnprocessedInput_PointerMoved(InkUnprocessedInput sender, PointerEventArgs args)
		{
			if (!_isErasing)
			{
				return;
			}

			var invalidate = false;

			foreach (var item in _strokes.ToArray())
			{
				var rect = item.SelectWithLine(_lastPoint, args.CurrentPoint.Position);

				if (rect.IsEmpty)
				{
					continue;
				}

				if (rect.Width * rect.Height > 0)
				{
					_strokes.Remove(item);

					invalidate = true;
				}
			}

			_lastPoint = args.CurrentPoint.Position;

			args.Handled = true;

			if (invalidate)
			{
				if (DrawCanvasInvalidated != null)
					DrawCanvasInvalidated();
			}
		}

		private void UnprocessedInput_PointerLost(InkUnprocessedInput sender, PointerEventArgs args)
		{
			if (_isErasing)
			{
				args.Handled = true;
			}

			_isErasing = false;
		}

		private void UnprocessedInput_PointerExited(InkUnprocessedInput sender, PointerEventArgs args)
		{
			if (_isErasing)
			{
				args.Handled = true;
			}

			_isErasing = true;
		}

		private void UnprocessedInput_PointerReleased(InkUnprocessedInput sender, PointerEventArgs args)
		{
			if (_isErasing)
			{
				args.Handled = true;
			}

			_isErasing = false;
		}

        public async void SaveAsImage(double width, double height)
        {
            bool confirmSave = await _dialogFactory.BooleanDialogAsync("Export File as an Image?");

            if (confirmSave)
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
                        }
                        else
                        {
                            await renderTarget.SaveAsync(fileStream, CanvasBitmapFileFormat.Png, 1f);
                        }
                    }

                }
            }            
        }

        public async Task<bool> SaveProject()
        {
            //Use existing folder
            if (_storageFolder != null)
            {
                SaveStrokes(_storageFolder);
                return true;
            }

            //Create new folder to save files
            SaveDialog save = new SaveDialog();
            await save.ShowAsync();
            //Save
            SaveStrokes(save.folder);
            _storageFolder = save.folder;
            return true;
        }

        private bool IsValidName(string name)
        {
            // TODO: Input Checking
            return true;
        }        

        private async void SaveStrokes(StorageFolder storageFolder)
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

                        if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
                        {
                            // File saved.
                        }
                        else
                        {
                            // File couldn't be saved.
                        }
                    }
                    // User selects Cancel and picker returns null.
                    else
                    {
                        // Operation cancelled.
                    }
                    i++;
                }
            }
        }
    }
}
