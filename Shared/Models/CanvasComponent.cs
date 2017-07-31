using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml.Shapes;

namespace Shared.Models
{
    class CanvasComponent
    {
        public enum ComponentType { Ellipse, Polygon }

        public ComponentType type { get; set; }
        public Shape shape { get; set; }

        // ellipse specific fields
        public Point center { get; set; }
        public double a { get; set; }
        public double b { get; set; }
        public double rotAngle { get; set; }

        // polygon specific fields
        public List<Point> points = new List<Point>();

        public CanvasComponent(ComponentType type)
        {
            this.type = type;
        }
    }
}
