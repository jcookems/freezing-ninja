using Mapper;
using Mapper.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
#if !NETFX_CORE
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
#endif
#if NETFX_CORE
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
using Windows.UI;
using Windows.UI.Xaml;
#endif

using System.Threading;
using Mapper.Consumer;

namespace Mapper
{
    public class XamlMapInfo
    {
        private static Dictionary<string, Brush> brushCache;
        private Dictionary<int, Point> RoomLocations = new Dictionary<int, Point>();
        private List<Action> actions = new List<Action>();
        private List<ElementDescription> elements = new List<ElementDescription>();
        private BoundingRectangle buildingBounds = new BoundingRectangle();

        public XamlMapInfo(RoomInfo location, string svgDocContent)
        {
            elements = SvgParser.ParseSvgDoc(location, svgDocContent);
            foreach (var description in elements)
            {
                switch (description.Type)
                {
                    case ElementType.Circle:
                        buildingBounds.UpdateBounds(description.CenterX, description.CenterY);
                        break;
                    case ElementType.Text:
                        buildingBounds.UpdateBounds(description.CenterX, description.CenterY);
                        int roomNumber;
                        if (int.TryParse(description.Text, out roomNumber))
                        {
                            RoomLocations.Add(roomNumber, new Point(description.CenterX, description.CenterY));
                        }
                        break;
                    case ElementType.Path:
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

        public Point? GetRoomLocation(int room)
        {
            Point loc;
            return (RoomLocations.TryGetValue(room, out loc) ? (Point?)loc : null);
        }

        public void Render(Canvas funky, Canvas parent, Canvas window, CompositeTransform textRotation, Action<Action> dispatcher, RoomInfo location)
        {
            dispatcher(() =>
            {
                CompositeTransform c = (CompositeTransform)funky.RenderTransform;
                c.CenterX = buildingBounds.CenterX;
                c.CenterY = buildingBounds.CenterY;
                funky.Width = buildingBounds.Width;
                funky.Height = buildingBounds.Height;
                c.TranslateX = -buildingBounds.CenterX + window.ActualWidth / 2;
                c.TranslateY = -buildingBounds.CenterY + window.ActualHeight / 2;
            });

            foreach (var description in elements.Where(p => p.Parent == ParentType.Labels || p.Parent == ParentType.Rooms))
            {
                this.checkRunActions(50, () => RenderElement(parent, textRotation, description), dispatcher);
            }

            // Highlight the correct room
            this.checkRunActions(1, () =>
            {
                var roomElement = parent.Children.OfType<Path>().FirstOrDefault(p => ToInt(p.Tag as string) == location.Room);
                if (roomElement != null)
                {
                    roomElement.Fill = new SolidColorBrush(Colors.Magenta);
                }
            }, dispatcher);

            foreach (var description in elements.Where(p => p.Parent == ParentType.Background))
            {
                // Fewer because these paths are more complex
                this.checkRunActions(5, () => RenderElement(parent, textRotation, description), dispatcher);
            }

            this.checkRunActions(0, () => { }, dispatcher);
        }

        private static int ToInt(string s)
        {
            int tmp = 0;
            int.TryParse(s, out tmp);
            return tmp;
        }

        private void checkRunActions(int actionCount, Action a, Action<Action> dispatcher)
        {
            actions.Add(a);
            if (actions.Count >= actionCount)
            {
                var oldActions = actions;
                dispatcher(() => { foreach (var aa in oldActions)  aa(); });
                actions = new List<Action>();
#if !NETFX_CORE
                Thread.Sleep(30);
#endif
            }
        }

        private static void RenderElement(Canvas parent, CompositeTransform textRotation, ElementDescription element)
        {
            switch (element.Type)
            {
                case ElementType.Circle:
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
                case ElementType.Path:
                    Path path = new Path()
                    {
                        Data = ProcessPathData(element.Path),
                        Tag = element.Text,
                        Fill = BrushForColor(element.Fill),
                        Stroke = BrushForColor(element.Stroke),
                    };
                    parent.Children.Add(path);
                    break;
                case ElementType.Text:
                    Grid g = new Grid() { Width = 200, Height = 200 };
                    parent.Children.Add(g);
                    Canvas.SetLeft(g, element.CenterX - g.Width / 2);
                    Canvas.SetTop(g, element.CenterY - g.Height / 2);
                    TextBlock tb = new TextBlock()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
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
                    case SegmentType.Move:
                        figure = new PathFigure()
                        {
                            StartPoint = pi.point
                        };
                        geometry.Figures.Add(figure);
                        break;
                    case SegmentType.Line:
                        figure.Segments.Add(new LineSegment()
                        {
                            Point = pi.point
                        });
                        break;
                    case SegmentType.Arc:
                        figure.Segments.Add(new ArcSegment()
                        {
                            Size = new Size(pi.size.X, pi.size.Y),
                            IsLargeArc = pi.isLargeArc,
                            Point = pi.point,
                            RotationAngle = pi.angle,
                            SweepDirection = pi.isSweepDirectionClockwise ? SweepDirection.Clockwise : SweepDirection.Counterclockwise
                        });
                        break;
                    case SegmentType.Close:
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

            Brush ret = null;
            if (brushCache.TryGetValue(colorName, out ret))
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
