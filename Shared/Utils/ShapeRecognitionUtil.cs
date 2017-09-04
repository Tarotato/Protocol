using System;
using System.Collections.Generic;
using System.Text;
using Shared.Models;
using Windows.Foundation;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Shared.Utils
{
    class ShapeRecognitionUtil
    {
        internal CanvasComponent BuildEllipse(InkAnalysisInkDrawing shape)
        {
            CanvasComponent component = new CanvasComponent(CanvasComponent.ComponentType.Ellipse);

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

            component.a = ellipse.Width / 2.0;
            component.b = ellipse.Height / 2.0;
            component.rotAngle = rotAngle;

            var point = new Point(shape.Center.X, shape.Center.Y);
            component.center = point;

            component.shape = ellipse;

            return component;
        }

        internal CanvasComponent BuildPolygon(InkAnalysisInkDrawing shape)
        {
            CanvasComponent component = new CanvasComponent(CanvasComponent.ComponentType.Polygon);

            var points = shape.Points;
            Polygon polygon = new Polygon();

            foreach (var point in points)
            {
                polygon.Points.Add(point);
                component.points.Add(point);
            }
            component.shape = polygon;

            return component;
        }

        internal bool ShouldDelete(Point start, Point finish, CanvasComponent component)
        {
            bool startValue = true;
            bool finishValue = true;
            switch (component.type)
            {
                case CanvasComponent.ComponentType.Ellipse:
                    startValue = PointInEllipse(start, component.a, component.b, component.center, component.rotAngle);
                    finishValue = PointInEllipse(finish, component.a, component.b, component.center, component.rotAngle);
                    break;
                case CanvasComponent.ComponentType.Polygon:
                    startValue = PointInPolygon(start, component.points);
                    finishValue = PointInPolygon(finish, component.points);
                    break;
                default:
                    break;
            }

            return startValue ^ finishValue; // xor means line started outside, ended inside (vice versa)
        }

        internal List<Shape> BuildComponents(List<CanvasComponent> components)
        {
            List<Shape> shapes = new List<Shape>();
            foreach (CanvasComponent component in components)
            {
                if (component.type == CanvasComponent.ComponentType.Ellipse)
                {
                    Ellipse e = BuildEllipseFromComponent(component);
                    shapes.Add(e);
                }
                else if (component.type == CanvasComponent.ComponentType.Polygon)
                {
                    Polygon p = BuildPolygonFromComponent(component);
                    shapes.Add(p);
                }
            }

            return shapes;
        }

        private Ellipse BuildEllipseFromComponent(CanvasComponent component)
        {
            Ellipse ellipse = new Ellipse();

            ellipse.Width = component.a * 2.0;
            ellipse.Height = component.b * 2.0;

            RotateTransform rotateTransform = new RotateTransform();
            rotateTransform.Angle = component.rotAngle * 180 / Math.PI;
            rotateTransform.CenterX = component.a;
            rotateTransform.CenterY = component.b;

            TranslateTransform translateTransform = new TranslateTransform();
            translateTransform.X = component.center.X - component.a;
            translateTransform.Y = component.center.Y - component.b;

            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(rotateTransform);
            transformGroup.Children.Add(translateTransform);
            ellipse.RenderTransform = transformGroup;

            component.shape = ellipse;

            return ellipse;
        }

        private Polygon BuildPolygonFromComponent(CanvasComponent component)
        {
            Polygon polygon = new Polygon();

            foreach (var point in component.points)
            {
                polygon.Points.Add(point);
            }
            component.shape = polygon;

            return polygon;
        }

        private bool PointInPolygon(Point point, List<Point> polygon)
        {
            // equation from https://codereview.stackexchange.com/questions/108857/point-inside-polygon-check

            int polygonLength = polygon.Count, i = 0;
            bool inside = false;

            // x, y for tested point.
            double pointX = point.X, pointY = point.Y;
            // start / end point for the current polygon segment.
            double startX, startY, endX, endY;
            Point endPoint = polygon[polygonLength - 1];
            endX = endPoint.X;
            endY = endPoint.Y;
            while (i < polygonLength)
            {
                startX = endX; startY = endY;
                endPoint = polygon[i++];
                endX = endPoint.X; endY = endPoint.Y;
                inside ^= (endY > pointY ^ startY > pointY) /* ? pointY inside [startY;endY] segment ? */
                          && /* if so, test if it is under the segment */
                          ((pointX - endX) < (pointY - endY) * (startX - endX) / (startY - endY));
            }
            return inside;
        }

        private bool PointInEllipse(Point point, double a, double b, Point center, double angle)
        {
            // equation from https://stackoverflow.com/questions/7946187/point-and-ellipse-rotated-position-test-algorithm

            var xEquation = point.X - center.X;
            var yEquation = point.Y - center.Y;
            var sine = Math.Sin(angle);
            var cosine = Math.Cos(angle);

            var topLeft = cosine * xEquation + sine * yEquation;
            var topRight = sine * xEquation - cosine * yEquation;

            var leftHandSide = topLeft * topLeft / (a * a) + topRight * topRight / (b * b);

            return leftHandSide <= 1;
        }
    }
}
