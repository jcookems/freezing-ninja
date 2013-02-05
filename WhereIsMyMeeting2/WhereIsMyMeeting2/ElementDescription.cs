﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace WhereIsMyMeeting2
{
    class ElementDescription
    {
        public enum ElementType
        {
            Circle,
            Path,
            Text,
        }
        public enum ParentType
        {
            Rooms,
            Labels,
            Background,
        }

        public ElementType Type { get; set; }
        public ParentType Parent { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public string Stroke { get; set; }
        public string Fill { get; set; }
        public string Text { get; set; }
        public Boolean IsHall { get; set; }
        public List<PathInfo> Path { get; set; }
        public double FontSize { get; set; }

        private BoundingRectangle elementBounds = null;

        public override string ToString()
        {
            String ret =
                      "Type: " + this.Type + "\n" +
                      "Parent: " + this.Parent + "\n" +
                      "Width: " + this.Width + "\n" +
                      "Height: " + this.Height + "\n" +
                      "CenterX: " + this.CenterX + "\n" +
                      "CenterY: " + this.CenterY + "\n" +
                      "Stroke: " + this.Stroke + "\n" +
                      "Fill: " + this.Fill + "\n" +
                      "IsHall: " + this.IsHall + "\n" +
                      "FontSize: " + this.FontSize + "\n";
            if (this.Path == null)
            {
                ret += "Path: <null>\n";
            }
            else
            {
                ret += "Path.count: " + this.Path.Count + "\n";
                for (int i = 0; i < this.Path.Count; i++)
                {
                    var x = Path[i];
                    ret += "Path[" + i + "]: " + x + "\n";
                }
            }
            return ret;
        }


        public static ElementDescription ParseSvgElement(XElement node2, String parentName)
        {
            ElementDescription ret = new ElementDescription();
            string name = node2.Name.LocalName;

            if (parentName.ToLowerInvariant() == "rooms".ToLowerInvariant())
            {
                ret.Parent = ParentType.Rooms;
            }
            else if (parentName.ToLowerInvariant() == "labels".ToLowerInvariant())
            {
                ret.Parent = ParentType.Labels;
            }
            else if (parentName.ToLowerInvariant() == "background".ToLowerInvariant())
            {
                ret.Parent = ParentType.Background;
            }
            else if (parentName.ToLowerInvariant() == "tooltip".ToLowerInvariant())
            {
                return null;
            }
            else
            {
                throw new ArgumentOutOfRangeException("Unexpected node type: " + parentName);
            }

            var styleParts = new Dictionary<string, string>();
            // Fill out some defaults
            styleParts["stroke"] = "black";
            styleParts["font-size"] = "11";
            foreach (var part in node2.Attribute("style").Value.Split(new char[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).Select(p => p.Split(':')))
            {
                styleParts[part[0]] = part[1];
            }

            if (name == "circle")
            {
                ret.Type = ElementDescription.ElementType.Circle;
                ret.Width = double.Parse(node2.Attribute("r").Value) * 2;
                ret.Height = double.Parse(node2.Attribute("r").Value) * 2;
                ret.Fill = styleParts["fill"];
                ret.Stroke = styleParts["stroke"];
                ret.CenterX = double.Parse(node2.Attribute("cx").Value);
                ret.CenterY = double.Parse(node2.Attribute("cy").Value);
            }
            else if (name == "path")
            {
                ret.Type = ElementDescription.ElementType.Path;
                ret.IsHall = (node2.Attribute("desc") == null ? false : node2.Attribute("desc").Value.Contains("irculation"));
                ret.Stroke = styleParts["stroke"];
                if (ret.IsHall)
                {
                    ret.Fill = "lightgray";
                }
                else if (styleParts["fill"] != "none")
                {
                    ret.Fill = styleParts["fill"];
                }
                ret.Path = PathInfo.ProcessPathData(node2.Attribute("d").Value);

                if (node2.Attribute("id") != null)
                {
                    ret.Text = node2.Attribute("id").Value;
                }

                ret.elementBounds = BoundingRectangle.GetBoundsFromPath(ret.Path);
            }
            else if (name == "text")
            {
                ret.Type = ElementDescription.ElementType.Text;
                ret.CenterX = double.Parse(node2.Attribute("x").Value);
                ret.CenterY = double.Parse(node2.Attribute("y").Value);
                ret.Stroke = styleParts["stroke"];
                ret.FontSize = double.Parse(styleParts["font-size"]);
                ret.Text = node2.Value;
            }
            else
            {
                throw new InvalidOperationException("Unexpected node: " + name);
            }

            return ret;
        }

        internal Tuple<Point, double> GetClosestWallIntersection(Point testPoint)
        {
            if (this.Path == null)
            {
                throw new InvalidOperationException("Path not defined for this element");
            }

            var lineSegments = new List<Tuple<Point, Point>>();
            lineSegments.Add(new Tuple<Point, Point>(Path[Path.Count - 2].point, Path[0].point));
            for (int i = 1; i < this.Path.Count - 1; i++)
            {
                lineSegments.Add(new Tuple<Point, Point>(Path[i - 1].point, Path[i].point));
            }

            Tuple<Point, double> ret = null;

            // Check if inside the boundary.
            int edgeCrossings = 0;
            foreach (var linedef in lineSegments)
            {
                Point newPt = GetLineIntersection(testPoint, linedef, 1);
                var d = CheckIntersection(linedef.Item1, linedef.Item2, newPt);

                if (newPt.X - testPoint.X > 0 && d == Direction.InRange)
                {
                    edgeCrossings++;
                }
            }
            // Topology tells us that a point is in a set if you have to cross the boundary an odd number of time to get outside.
            if (edgeCrossings % 2 == 1)
            {
                return new Tuple<Point, double>(testPoint, -1);
            }

            // Not inside, look for the closest point on the boundary to snap to.
            foreach (var linedef in lineSegments)
            {
                Point newPt = GetLineIntersection(testPoint, linedef);
                var ptOnLine = CheckOnLineSegment(testPoint, linedef.Item1, linedef.Item2, newPt);
                double distsq = Square(Sub(ptOnLine, testPoint));
                if (ret == null || ret.Item2 > distsq)
                {
                    ret = new Tuple<Point, double>(ptOnLine, distsq);
                }
            }

            return ret;
        }

        private static Point CheckOnLineSegment(Point testPoint, Point point1, Point point2, Point newPt)
        {
            switch (CheckIntersection(point1, point2, newPt))
            {
                case Direction.Past1:
                    return point1;
                case Direction.Past2:
                    return point2;
                case Direction.InRange:
                default:
                    return newPt;
            }
        }

        private enum Direction
        {
            InRange,
            Past1,
            Past2,
        }

        private static Direction CheckIntersection(Point point1, Point point2, Point intersection)
        {
            Point lineDelta = Sub(point2, point1);
            double dot1 = Dot(lineDelta, Sub(intersection, point1)) / Dot(lineDelta, lineDelta);
            Direction dir = 0.0 > dot1 ? Direction.Past1 : dot1 > 1.0 ? Direction.Past2 : Direction.InRange;
            return dir;
        }

        private static Point GetLineIntersection(Point testPoint, Tuple<Point, Point> linedef, double? slope = null)
        {
            Point lineDelta = Sub(linedef.Item2, linedef.Item1);
            Point newPt;
            if (lineDelta.X == 0)
            {
                newPt = new Point(linedef.Item1.X, testPoint.Y);
            }
            else if (lineDelta.Y == 0)
            {
                newPt = new Point(testPoint.X, linedef.Item1.Y);
            }
            else
            {
                // Should be able to use the tangents
                double lineTheta = Math.Atan(lineDelta.Y / lineDelta.X);
                double perpTheta = lineTheta + Math.PI / 2;

                double lineSlope = Math.Tan(lineTheta);
                double perpSlope = Math.Tan(perpTheta);

                // y = m (x-x_0) + y_0
                // y_Intersect = m_tangent * (x_Intersect - testPoint.X  ) + testPoint.Y
                // y_Intersect = m_line    * (x_Intersect - line.Point1.X) + line.Point1.Y
                //
                //    m_tangent * x_Intersect - m_tangent * testPoint.X   + testPoint.Y
                //  = m_line    * x_Intersect - m_line    * line.Point1.X + line.Point1.Y
                //
                // x_Intersect =  (line.Point1.Y - testPoint.Y) - (m_line * line.Point1.X - m_tangent * testPoint.X)
                //                 / ( m_tangent-m_line)

                double x_Intersect = (linedef.Item1.Y - testPoint.Y) - (lineSlope * linedef.Item1.X - perpSlope * testPoint.X)
                                                     / (perpSlope - lineSlope);
                double y_Intersect = perpSlope * (x_Intersect - testPoint.X) + testPoint.Y;
                newPt = new Point(x_Intersect, y_Intersect);
            }

            return newPt;
        }

        private static double Square(Point a)
        {
            return Dot(a, a);
        }

        private static double Dot(Point a, Point b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        private static Point Sub(Point a, Point b)
        {
            return new Point(a.X - b.X, a.Y - b.Y);
        }
    }

}
