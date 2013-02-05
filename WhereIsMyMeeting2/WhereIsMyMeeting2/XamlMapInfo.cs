using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;

namespace WhereIsMyMeeting2
{
    public class XamlMapInfo
    {
        private static Dictionary<string, Brush> brushCache;
        private static Dictionary<string, Brush> BrushCache
        {
            get
            {
                if (brushCache == null)
                {
                    brushCache = new Dictionary<string, Brush> {
                        { "black",     new SolidColorBrush(Colors.Black)     },
                        { "white",     new SolidColorBrush(Colors.White)     },
                        { "lightgray", new SolidColorBrush(Colors.LightGray) },
                        { "yellow",    new SolidColorBrush(Colors.Yellow)    },
                        { "none",      null                                  },
                    };
                }
                return brushCache;
            }
        }

        public Dictionary<string, Point> RoomLocations { get; private set; }


        private List<Action> actions = new List<Action>();
        private List<ElementDescription> elements = new List<ElementDescription>();
        private Dispatcher dispatcher;
        private BoundingRectangle buildingBounds;

        private XamlMapInfo(RoomInfo location)
        {
            this.RoomLocations = new Dictionary<string, Point>();
            this.buildingBounds = new BoundingRectangle();

            StreamResourceInfo xml = Application.GetResourceStream(new Uri(
                "Maps/" + location.Building + "_" + location.Floor + ".svg", UriKind.Relative));
            XElement doc = null;
            try
            {
                doc = XDocument.Load(xml.Stream).Root;
            }
            catch (Exception)
            {
                MessageBox.Show("Could not load the map for " +
                    "building '" + location.Building + "', " +
                    "floor'" + location.Floor + "'");
            }

            foreach (XElement node in doc.Nodes().OfType<XElement>())
            {
                foreach (XElement node2 in node.Nodes().OfType<XElement>())
                {
                    this.elements.Add(ElementDescription.ParseSvgElement(node2, node.Attribute("id").Value));
                }
            }
            this.elements = this.elements.Where(p => p != null).ToList();

            foreach (var description in this.elements)
            {
                switch (description.Type)
                {
                    case ElementDescription.ElementType.Circle:
                        buildingBounds.UpdateBounds(description.CenterX, description.CenterY);
                        break;
                    case ElementDescription.ElementType.Text:
                        buildingBounds.UpdateBounds(description.CenterX, description.CenterY);
                        this.RoomLocations.Add(description.Text, new Point(description.CenterX, description.CenterY));
                        break;
                    case ElementDescription.ElementType.Path:
                        if (description.IsHall)
                        {
                            var pd = description.Path;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// If the point is in a hall, just return it. Else, move the point so that it is
        /// inside the nearest hall.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public Point RebasePointToHall(Point testPoint)
        {
            // We have the hall outlines, 

            Tuple<Point, double> prevNearestWall = null;
            foreach (var x in elements.Where(p => p.IsHall))
            {
                var nearestWall = x.GetClosestWallIntersection(testPoint);
                if (nearestWall.Item2 <= 0)
                {
                    return testPoint;
                }
                else if (prevNearestWall == null || nearestWall.Item2 < prevNearestWall.Item2)
                {
                    prevNearestWall = nearestWall;
                }
            }

            return prevNearestWall.Item1;
        }

        public static XamlMapInfo ParseFromSvg(RoomInfo location)
        {
            return new XamlMapInfo(location);
        }

        public void Render(Canvas funky, Canvas parent, Canvas window, CompositeTransform textRotation, Dispatcher dispatcher, RoomInfo location)
        {
            this.dispatcher = dispatcher;
            dispatcher.BeginInvoke(() =>
            {
                CompositeTransform c = (CompositeTransform)funky.RenderTransform;
                c.CenterX = buildingBounds.CenterX;
                c.CenterY = buildingBounds.CenterY;
                funky.Width = buildingBounds.Width;
                funky.Height = buildingBounds.Height;
                c.TranslateX = -buildingBounds.CenterX + window.ActualWidth / 2;
                c.TranslateY = -buildingBounds.CenterY + window.ActualHeight / 2;
            });

            foreach (var description in elements.Where(p => p.Parent == ElementDescription.ParentType.Labels || p.Parent == ElementDescription.ParentType.Rooms))
            {
                this.checkRunActions(50, () => ParseSvgElement(parent, textRotation, description));
            }

            // Highlight the correct room
            this.checkRunActions(1, () =>
            {
                var roomElement = parent.Children.OfType<Path>().FirstOrDefault(p => (p.Tag as string) == location.Room);
                if (roomElement != null)
                {
                    roomElement.Fill = new SolidColorBrush(Colors.Magenta);
                }
            });

            foreach (var description in elements.Where(p => p.Parent == ElementDescription.ParentType.Background))
            {
                // Fewer because these paths are more complex
                this.checkRunActions(5, () => ParseSvgElement(parent, textRotation, description));
            }

            this.checkRunActions(0, () => { });
        }

        private void checkRunActions(int actionCount, Action a)
        {
            actions.Add(a);
            if (actions.Count >= actionCount)
            {
                var oldActions = actions;
                dispatcher.BeginInvoke(() => { foreach (var aa in oldActions)  aa(); });
                actions = new List<Action>();
                System.Threading.Thread.Sleep(30);
            }
        }

        private static void ParseSvgElement(Canvas parent, CompositeTransform textRotation, ElementDescription element)
        {
            switch (element.Type)
            {
                case ElementDescription.ElementType.Circle:
                    Ellipse e = new Ellipse()
                    {
                        Width = element.Width,
                        Height = element.Height,
                        Fill = BrushForColor(element.Fill),
                        Stroke = BrushForColor(element.Stroke),
                    };
                    parent.Children.Add(e);
                    Canvas.SetLeft(e, element.CenterX);
                    Canvas.SetTop(e, element.CenterY);
                    break;
                case ElementDescription.ElementType.Path:
                    Path path = new Path()
                    {
                        Data = ProcessPathData(element.Path),
                        Tag = element.Text,
                        Fill = BrushForColor(element.Fill),
                        Stroke = BrushForColor(element.Stroke),
                    };
                    parent.Children.Add(path);
                    break;
                case ElementDescription.ElementType.Text:
                    Grid g = new Grid() { Width = 200, Height = 200 };
                    parent.Children.Add(g);
                    Canvas.SetLeft(g, element.CenterX - g.Width / 2);
                    Canvas.SetTop(g, element.CenterY - g.Height / 2);
                    TextBlock tb = new TextBlock()
                    {
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        RenderTransform = textRotation,
                        Foreground = BrushForColor(element.Stroke),
                        FontSize = element.FontSize,
                        Text = element.Text,
                    };
                    g.Children.Add(tb);
                    break;
                default:
                    throw new InvalidOperationException("Unexpected node: " + element.Type);
            }
        }

        private static Geometry ProcessPathData(List<PathInfo> pis)
        {
            PathGeometry geometry = new PathGeometry();
            PathFigure figure = null;
            foreach (var pi in pis)
            {
                switch (pi.segmentType)
                {
                    case PathInfo.SegmentType.Move:
                        figure = new PathFigure()
                        {
                            StartPoint = pi.point
                        };
                        geometry.Figures.Add(figure);
                        break;
                    case PathInfo.SegmentType.Line:
                        figure.Segments.Add(new LineSegment()
                        {
                            Point = pi.point
                        });
                        break;
                    case PathInfo.SegmentType.Arc:
                        figure.Segments.Add(new ArcSegment()
                        {
                            Size = new Size(pi.size.X, pi.size.Y),
                            IsLargeArc = pi.isLargeArc,
                            Point = pi.point,
                            RotationAngle = pi.angle,
                            SweepDirection = pi.isSweepDirectionClockwise ? SweepDirection.Clockwise : SweepDirection.Counterclockwise
                        });
                        break;
                    case PathInfo.SegmentType.Close:
                        figure.IsClosed = true;
                        break;
                    default:
                        break;
                }
            }

            return geometry;
        }

        private static Brush BrushForColor(String colorName)
        {
            if (colorName == null) return null;
            Brush ret = null;
            if (BrushCache.TryGetValue(colorName, out ret))
            {
                return ret;
            }
            else
            {
                throw new ArgumentOutOfRangeException("Color not supported: " + colorName);
            }
        }
    }
}
