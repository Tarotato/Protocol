using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.Storage;
using Shared.Models;
using System.Threading.Tasks;
using Shared.Utils;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;

namespace Protocol
{
    public class MainCanvasViewModel
    {
        public delegate void ChangedEventHandler();
        public event ChangedEventHandler DrawCanvasInvalidated;

        public delegate void FlyoutEventHandler(string message);
        public event FlyoutEventHandler ShowFlyoutAboveToolbar;

        public delegate void ShapeHandler(Shape shape);
        public event ShapeHandler AddShapeToCanvas;
        public event ShapeHandler RemoveShapeFromCanvas;

        private Save save = new Save();

        // fields for dry erasing
        private bool _isErasing;
        private Point _lastPoint;
        private List<InkStrokeContainer> _strokes;
        private StorageFolder _storageFolder;

        // stroke recognition
        ShapeRecognitionUtil shapeHelper = new ShapeRecognitionUtil();
        InkAnalyzer inkAnalyzer = new InkAnalyzer();
        InkAnalysisResult inkAnalysisResults = null;
        InkDrawingAttributes currentBrush;

        List<CanvasComponent> components = new List<CanvasComponent>();

        public MainCanvasViewModel(MainCanvasParams parameters)
        {
            _strokes = parameters.strokes;
            _storageFolder = parameters.folder;
            components = parameters.components;
            LoadShapes();

            save.ShowFlyoutAboveInkToolbar += ShowFlyout;
        }

        private void LoadShapes()
        {
            foreach (Shape shape in shapeHelper.BuildComponents(components))
            {

            }
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

        internal async void RecognizeStrokes(IEnumerable<InkStroke> strokes)
        {
            inkAnalyzer.AddDataForStrokes(strokes);
            inkAnalysisResults = await inkAnalyzer.AnalyzeAsync();

            // Find all strokes that are recognized as a drawing and create a corresponding ink analysis InkDrawing node.
            var inkdrawingNodes = inkAnalyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing);
            foreach (InkAnalysisInkDrawing node in inkdrawingNodes)
            {
                // Draw an Ellipse object on the recognitionCanvas (circle is a specialized ellipse).
                if (node.DrawingKind == InkAnalysisDrawingKind.Circle || node.DrawingKind == InkAnalysisDrawingKind.Ellipse)
                {
                    DrawEllipse(node);
                    inkAnalyzer.RemoveDataForStrokes(node.GetStrokeIds());
                }
                else if (node.DrawingKind != InkAnalysisDrawingKind.Drawing) // Draw a Polygon object on the inkCanvas.
                {
                    DrawPolygon(node);
                    inkAnalyzer.RemoveDataForStrokes(node.GetStrokeIds());
                }
            }
        }

        internal void StopRecognizingStrokes()
        {
            inkAnalyzer.ClearDataForAllStrokes();
        }

        private void DrawEllipse(InkAnalysisInkDrawing shape)
        {
            CanvasComponent ellipse = shapeHelper.BuildEllipse(shape);

            RemoveStrokes(shape);
            ellipse.shape.Stroke = new SolidColorBrush(currentBrush.Color);
            ellipse.shape.StrokeThickness = currentBrush.Size.Width;

            components.Add(ellipse);
            AddShapeToCanvas?.Invoke(ellipse.shape);
        }

        private void DrawPolygon(InkAnalysisInkDrawing shape)
        {
            CanvasComponent polygon = shapeHelper.BuildPolygon(shape);

            RemoveStrokes(shape);
            polygon.shape.Stroke = new SolidColorBrush(currentBrush.Color);
            polygon.shape.StrokeThickness = currentBrush.Size.Width;

            components.Add(polygon);
            AddShapeToCanvas?.Invoke(polygon.shape);
        }

        private void RemoveStrokes(InkAnalysisInkDrawing shape)
        {
            foreach (var strokeId in shape.GetStrokeIds())
            {
                RemoveStroke(strokeId);
            }
        }

        private void RemoveStroke(uint strokeId)
        {
            foreach (var item in _strokes.ToArray())
            {
                var stroke = item.GetStrokeById(strokeId -1); // -1 is hard coded
                if (stroke != null)
                {
                    currentBrush = stroke.DrawingAttributes;
                    _strokes.Remove(item);
                    break;
                }
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

            // canvas: CanvasControl
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

            if (invalidate)
            {
                DrawCanvasInvalidated?.Invoke();
            }

            // Recognition canvas
            foreach (var component in components)
            {
                if (shapeHelper.ShouldDelete(_lastPoint, args.CurrentPoint.Position, component))
                {
                    RemoveShapeFromCanvas?.Invoke(component.shape);
                }
            }

            _lastPoint = args.CurrentPoint.Position;
            args.Handled = true;
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

        public void SaveAsImage(double width, double height, ProjectMetaData metaData)
        {
            save.SaveAsImage(width, height, _strokes, metaData);
        }

        public async Task<bool> SaveProject(ProjectMetaData metaData)
        {
            var savedFolder = await save.SaveProject(_storageFolder, _strokes, metaData, components);
            if(savedFolder != null)
            {
                _storageFolder = savedFolder;
            }
            return true;
        }

        public async Task<MainCanvasParams> OpenExistingProject(ProjectMetaData metaData)
        {
            return await save.OpenProject(_strokes, _storageFolder, metaData, components);
        }

        public async Task<ContentDialogResult> OpenNewProject(ProjectMetaData metaData)
        {
            return await save.ConfirmSave(_strokes, _storageFolder, metaData, components);
        }
    }
}
