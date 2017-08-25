using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Windows.Foundation;
using Windows.UI.Xaml.Shapes;

namespace Shared.Models
{
    [DataContract]
    public class CanvasComponent
    {
        public enum ComponentType { Ellipse, Polygon }

        [DataMember]
        public ComponentType type { get; set; }

        public Shape shape { get; set; }

        // ellipse specific fields
        [DataMember]
        public Point center { get; set; }
        [DataMember]
        public double a { get; set; }
        [DataMember]
        public double b { get; set; }
        [DataMember]
        public double rotAngle { get; set; }

        // polygon specific fields
        [DataMember]
        public List<Point> points = new List<Point>();

        public CanvasComponent(ComponentType type)
        {
            this.type = type;
        }
    }
}
