using Mapper.Consumer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
#if NETFX_CORE
using Windows.Foundation;
#endif

namespace Mapper.Parser
{
    public class VisioParser
    {
        private static double ptsMultiplier = 1.0 / .013836;
        private static XNamespace ns = XNamespace.Get("urn:schemas-microsoft-com:office:visio");

        string[] layerDescriptions = null;
        private double pageWidth = 0;
        private double pageHeight = 0;

        private static string[] goodLayerPrefixes = { "A-CASE-", "A-DOOR-", "A-FLOR-", "A-GLAZ-", "A-WALL-" };

        public static IEnumerable<ElementDescription> ParseVisioDoc(RoomInfo location, string content)
        {
            var _this = new VisioParser();
            return _this.parseit(location, content);
        }

        private VisioParser() { }

        private IEnumerable<ElementDescription> parseit(RoomInfo location, string content)
        {
            var ret = new List<ElementDescription>();

            try
            {
                var xd = XDocument.Parse(content);

                layerDescriptions = xd.Root.Descendants(ns + "Layer").Select(p => p.Value).ToArray();
                double pageWidthInch = double.Parse(xd.Root.Descendants(ns + "PageWidth").First().Value);
                double pageHeightInch = double.Parse(xd.Root.Descendants(ns + "PageHeight").First().Value);

                pageWidth = pageWidthInch * ptsMultiplier;
                pageHeight = pageHeightInch * ptsMultiplier;

                var xx = xd.Root.Descendants(ns + "Shape");
                foreach (var x in xx)
                {
                    if (x.Attribute("LineStyle") == null || x.Attribute("LineStyle").Value != "2") Debug.WriteLine("A: " + x.ToString());
                    if (x.Attribute("TextStyle") == null || x.Attribute("TextStyle").Value != "3") Debug.WriteLine("B: " + x.ToString());
                    if (x.Attribute("Type") == null || x.Attribute("Type").Value != "Shape")
                    {
                        Debug.WriteLine("C: " + x.ToString());
                        continue;
                    }

                    Line l = new Line();
                    foreach (var xxx in x.Elements())
                    {
                        Parse(xxx, l);
                    }

                    foreach (var el in ConvertToElements(l))
                    {
                        ret.Add(el);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Could not load the map for " +
                    "building '" + location.Building + "', " +
                    "floor'" + location.Floor + "'\n" + e.Message);
            }

            return ret.Where(p => p != null);
        }

        private Point FixPoint(Point raw)
        {
            var tmp = new Point(raw.X, pageHeight - raw.Y);
            return new Point(
                double.Parse(tmp.X.ToString("F3")),
                double.Parse(tmp.Y.ToString("F3")));
        }

        private IEnumerable<ElementDescription> ConvertToElements(Line l)
        {
            List<ElementDescription> ret = new List<ElementDescription>();

            ElementDescription ed = new ElementDescription();
            if (l.LayerDescription == "RM$")
            {
                ed.Parent = ParentType.Rooms;
            }
            else if (goodLayerPrefixes.Any(p => l.LayerDescription.StartsWith(p)))
            {
                ed.Parent = ParentType.Background;
            }
            else
            {
                return ret;
            }

            ed.Type = ElementType.Path;

            var fixedPt = FixPoint(new Point(l.CenterX, l.CenterY));
            ed.CenterX = fixedPt.X;
            ed.CenterY = fixedPt.Y;
            ed.Text = l.Text;

            ed.Width = (l.Width.HasValue ? double.Parse(l.Width.Value.ToString("F3")) : 0);
            ed.Height = (l.Height.HasValue ? double.Parse(l.Height.Value.ToString("F3")) : 0);
            ed.Path = Pathify(l.Geom);
            ed.Stroke = l.LineColor;
            ed.Fill = l.FillForegnd;
            ret.Add(ed);

            if (ed.Parent == ParentType.Rooms)
            {
                ed = new ElementDescription();
                ed.Parent = ParentType.Labels;
                ed.Type = ElementType.Text;
                ed.FontSize = 10;
                ed.CenterX = fixedPt.X + l.Width / 2;
                ed.CenterY = fixedPt.Y - l.Height / 2;
                ed.Text = l.Text;
                ret.Add(ed);
            }

            return ret;
        }

        private string Pathify(List<PathInfo2> list)
        {
            if (list == null) return null;

            string s = "";
            foreach (var x in list)
            {
                switch (x.segmentType)
                {
                    case SegmentType.Move:
                        s += "M";
                        s += FixPoint(x.point);
                        break;
                    case SegmentType.Line:
                        s += "L";
                        s += FixPoint(x.point);
                        break;
                    //case SegmentType.Arc: 
                    //    s +=  "A";
                    //                    pi.size = getPoint();
                    //eatWhitespace();
                    //pi.angle = getDouble();
                    //eatWhitespace();
                    //pi.isLargeArc = getBoolean();
                    //eatWhitespace();
                    //eatOneOptionalComma();
                    //eatWhitespace();
                    //pi.isSweepDirectionClockwise = getBoolean();
                    //eatWhitespace();
                    //pi.point = getPoint();
                    //                    break;
                    default:
                        Debug.WriteLine("Don't know how to serialize segment type " + x.segmentType.ToString());
                        return null;
                }
                s += " ";
            }
            if (list.First().point.Equals(list.Last().point))
            {
                s += "Z";
            }

            return s.TrimEnd();
        }

        private enum SegmentType
        {
            Move,
            Line,
            Arc,
            Ellipse
        }

        private class Line
        {
            public double CenterX;
            public double CenterY;
            public double? Width;
            public double? Height;

            private string t;
            public List<PathInfo2> Geom;
            public string LineColor;
            public string LayerDescription;
            public string Text
            {
                get
                {
                    return t;
                }
                set
                {
                    if (value == null)
                    {
                        t = null;
                    }
                    else
                    {
                        t = value.Split('\r', '\n').Last().Trim();
                    }
                }
            }

            public override string ToString()
            {
                string s = "l=" + this.LayerDescription +
                    (this.Text != null ? ", t=" + this.Text : "") +
                    ", cx=" + CenterX + ", cy=" + CenterY +
                    ", w=" + Width + ", h=" + Height + "\n";
                if (Geom != null)
                {
                    s += "[" + string.Join(",", Geom.Select(p => "(" + p.ToString() + ")")) + "]";
                }
                return s;
            }

            public string FillForegnd { get; set; }
        }

        private void Parse(XElement e, Line l)
        {
            string name = e.Name.LocalName;

            if (name == "Line")
            {
                // l.LineWeight = double.Parse(e.Element(ns + "LineWeight").Value);
                l.LineColor = e.Element(ns + "LineColor").Value;
                //<LinePattern>1</LinePattern>
            }
            else if (name == "LayerMem")
            {
                l.LayerDescription = this.layerDescriptions[int.Parse(e.Element(ns + "LayerMember").Value)];
            }
            else if (name == "Fill")
            {
                l.FillForegnd = e.Element(ns + "FillForegnd").Value;
                // l.FillBackgnd = e.Element(ns + "FillBackgnd").Value;
            }
            else if (name == "TextBlock")
            {
                // Don't worry about these
                // <TextBlock>
                //   <LeftMargin Units="PT">0</LeftMargin>
                //   <RightMargin Units="PT">0</RightMargin>
                //   <TopMargin Units="PT">0</TopMargin>
                //   <BottomMargin Units="PT">0</BottomMargin>
                // </TextBlock>
            }
            else if (name == "Char")
            {
                // Don't worry about these
                // <Char IX="0">
                //   <Size Unit="PT">0.0238</Size>
                //   <Font>0</Font>
                // </Char>
            }
            else if (name == "Text")
            {
                l.Text = e.Value;
            }
            else if (name == "XForm")
            {
                l.CenterX = double.Parse(e.Element(ns + "PinX").Value) * ptsMultiplier;
                l.CenterY = double.Parse(e.Element(ns + "PinY").Value) * ptsMultiplier;
                l.Width = double.Parse(e.Element(ns + "Width").Value) * ptsMultiplier;
                l.Height = double.Parse(e.Element(ns + "Height").Value) * ptsMultiplier;
                //        <ResizeMode>2</ResizeMode>
            }
            else if (name == "Geom")
            {
                List<PathInfo2> geom = new List<PathInfo2>();
                foreach (var geomPart in e.Elements())
                {
                    geom.Add(ParseGeom(l, geomPart));
                }
                l.Geom = geom;
            }
            else if (name == "Cell" || name == "Section")
            {
            }
            else
            {
                throw new Exception();
            }
        }

        private static PathInfo2 ParseGeom(Line l, XElement geomPart)
        {
            PathInfo2 pi = null;
            string n2 = geomPart.Name.LocalName;
            if (n2 == "MoveTo")
            {
                var x = ParsePointInfo(l, geomPart, "X");
                var y = ParsePointInfo(l, geomPart, "Y");
                pi = new PathInfo2(SegmentType.Move);
                pi.point = new Point(x + l.CenterX, y + l.CenterY);
            }
            else if (n2 == "LineTo")
            {
                var x = ParsePointInfo(l, geomPart, "X");
                var y = ParsePointInfo(l, geomPart, "Y");
                pi = new PathInfo2(SegmentType.Line);
                pi.point = new Point(x + l.CenterX, y + l.CenterY);
            }
            else if (n2 == "ArcTo")
            {
                var x = ParsePointInfo(l, geomPart, "X");
                var y = ParsePointInfo(l, geomPart, "Y");
                var a = ParsePointInfo(l, geomPart, "A");
                pi = new PathInfo2(SegmentType.Arc);
                pi.point = new Point(x + l.CenterX, y + l.CenterY);
                pi.angle = a;
            }
            else if (n2 == "Ellipse")
            {
                var x = ParsePointInfo(l, geomPart, "X");
                var y = ParsePointInfo(l, geomPart, "Y");
                pi = new PathInfo2(SegmentType.Ellipse);
                pi.point = new Point(x + l.CenterX, y + l.CenterY);
                pi.EllipseA = ParsePointInfo(l, geomPart, "A");
                pi.EllipseB = ParsePointInfo(l, geomPart, "B");
                pi.EllipseC = ParsePointInfo(l, geomPart, "C");
                pi.EllipseD = ParsePointInfo(l, geomPart, "D");
            }
            else if (n2 == "EllipticalArcTo")
            {
                Debug.WriteLine(n2);
                var x = ParsePointInfo(l, geomPart, "X");
                var y = ParsePointInfo(l, geomPart, "Y");
                pi = new PathInfo2(SegmentType.Ellipse);
                pi.point = new Point(x + l.CenterX, y + l.CenterY);
                pi.EllipseA = ParsePointInfo(l, geomPart, "A");
                pi.EllipseB = ParsePointInfo(l, geomPart, "B");
                pi.EllipseC = ParsePointInfo(l, geomPart, "C");
                pi.EllipseD = ParsePointInfo(l, geomPart, "D");
            }
            else
            {
                Debug.WriteLine(geomPart.ToString());
                throw new Exception();
            }
            return pi;
        }

        private static double ParsePointInfo(Line l, XElement x, string name)
        {
            var a = x.Element(ns + name).Attribute("F");
            var v = (a != null ? a.Value : x.Value);
            double computed = 1;
            foreach (var c in v.Split('*')
                .Select(p => p.Trim())
                .Select(p => p == "Width" ? l.Width.ToString() : p)
                .Select(p => p == "Height" ? l.Height.ToString() : p)
                .Select(p => double.Parse(p)))
            {
                computed *= c;
            }
            return computed;
        }

        private class PathInfo2
        {
            public PathInfo2(SegmentType segmentType)
            {
                this.segmentType = segmentType;
            }

            public SegmentType segmentType;

            public Point point;
            public double? angle;
            public double? EllipseA;
            public double? EllipseB;
            public double? EllipseC;
            public double? EllipseD;

            public override string ToString()
            {
                return "segment type: " + segmentType.ToString() +
                    ", point: " + point +
                    (angle.HasValue ? ", angle:" + angle.Value : "") +
                    (EllipseA.HasValue ? ", EllipseA:" + EllipseA.Value : "") +
                    (EllipseB.HasValue ? ", EllipseB:" + EllipseB.Value : "") +
                    (EllipseC.HasValue ? ", EllipseC:" + EllipseC.Value : "") +
                    (EllipseD.HasValue ? ", angle:" + EllipseD.Value : "");

                //                    ", size: " + size +
                //                  ", angle: " + angle +
                //                ", is large arc: " + isLargeArc +
                //              ", is sweepdir clockwise: " + isSweepDirectionClockwise
            }

        }


    }
}
