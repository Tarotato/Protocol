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
using Shared.Utils;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Protocol
{
    public class MainCanvasViewModel
    {
        public delegate void ChangedEventHandler();
        public event ChangedEventHandler DrawCanvasInvalidated;
        public delegate void FlyoutEventHandler(string message);
        public event FlyoutEventHandler ShowFlyoutAboveToolbar;
        private Save save = new Save();

        // fields for dry erasing
        private bool _isErasing;
        private Point _lastPoint;
        private List<InkStrokeContainer> _strokes;
        private StorageFolder _storageFolder;

        public MainCanvasViewModel(MainCanvasParams parameters)
        {
            _strokes = parameters.strokes;
            _storageFolder = parameters.folder;

            save.ShowFlyoutAboveInkToolbar += ShowFlyout;
        }

        internal void ShowFlyout(string message)
        {
            ShowFlyoutAboveToolbar?.Invoke(message);
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
                DrawCanvasInvalidated?.Invoke();
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

        public void SaveAsImage(double width, double height)
        {
            save.SaveAsImage(width, height, _strokes);
        }

        public async Task<bool> SaveProject()
        {
            var savedFolder = await save.SaveProject(_storageFolder, _strokes);
            if(savedFolder != null)
            {
                _storageFolder = savedFolder;
            }
            return true;
        }

        public async Task<MainCanvasParams> OpenExistingProject()
        {
            return await save.OpenProject(_strokes, _storageFolder);
        }

        public async Task<ContentDialogResult> OpenNewProject()
        {
            return await save.ConfirmSave(_strokes, _storageFolder);
        }
    }
}
