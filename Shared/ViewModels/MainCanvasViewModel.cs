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

        public delegate void AddShapeHandler(Shape shape);
        public event AddShapeHandler AddShapeToCanvas;

        private Save save = new Save();

        // fields for dry erasing
        private bool _isErasing;
        private Point _lastPoint;
        private List<InkStrokeContainer> _strokes;
        private StorageFolder _storageFolder;

        // stroke recognition
        InkAnalyzer inkAnalyzer = new InkAnalyzer();
        InkAnalysisResult inkAnalysisResults = null;


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
            var points = shape.Points;
            Ellipse ellipse = new Ellipse();
            ellipse.Width = Math.Sqrt((points[0].X - points[2].X) * (points[0].X - points[2].X) +
                    (points[0].Y - points[2].Y) * (points[0].Y - points[2].Y));
            ellipse.Height = Math.Sqrt((points[1].X - points[3].X) * (points[1].X - points[3].X) +
                    (points[1].Y - points[3].Y) * (points[1].Y - points[3].Y));

            var rotAngle = Math.Atan2(points[2].Y - points[0].Y, points[2].X - points[0].X);
            RotateTransform rotateTransform = new RotateTransform();
            rotateTransform.Angle = rotAngle * 180 / Math.PI;
            rotateTransform.CenterX = ellipse.Width / 2.0;
            rotateTransform.CenterY = ellipse.Height / 2.0;

            TranslateTransform translateTransform = new TranslateTransform();
            translateTransform.X = shape.Center.X - ellipse.Width / 2.0;
            translateTransform.Y = shape.Center.Y - ellipse.Height / 2.0;

            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(rotateTransform);
            transformGroup.Children.Add(translateTransform);
            ellipse.RenderTransform = transformGroup;

            var brush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(255, 0, 0, 255));
            ellipse.Stroke = brush;
            ellipse.StrokeThickness = 2;

            RemoveStrokes(shape);
            AddShapeToCanvas?.Invoke(ellipse);
        }

        private void DrawPolygon(InkAnalysisInkDrawing shape)
        {
            var points = shape.Points;
            Polygon polygon = new Polygon();

            foreach (var point in points)
            {
                polygon.Points.Add(point);
            }

            var brush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(255, 0, 0, 255));
            polygon.Stroke = brush;
            polygon.StrokeThickness = 2;

            RemoveStrokes(shape);
            AddShapeToCanvas?.Invoke(polygon);
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
                if (item.GetStrokeById(strokeId - 1) != null)
                {
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
