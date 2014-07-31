using System;
using System.Collections.Generic;
using System.Linq;
using Mapper.Consumer;
using System.Threading.Tasks;

#if !NETFX_CORE
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
#endif
#if NETFX_CORE
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
#endif

namespace Mapper
{
    public class XamlMapInfo
    {
        private static Dictionary<string, Brush> brushCache;
        private Dictionary<int, Point> RoomLocations = new Dictionary<int, Point>();
        private List<Action> actions = new List<Action>();
        private List<ElementDescription> elements = new List<ElementDescription>();
        private BoundingRectangle buildingBounds = new BoundingRectangle();
        private double pathScale;

        public XamlMapInfo(RoomInfo location, string svgDocContent, double scale)
        {
            elements = (List<ElementDescription>)Newtonsoft.Json.JsonConvert.DeserializeObject(svgDocContent, typeof(List<ElementDescription>));
            foreach (var description in elements)
            {
                description.SetScale(scale);
                pathScale = scale;
              
                switch (description.Type)
                {
                    case ElementType.Circle:
                        buildingBounds.UpdateBounds(description.CenterX.Value, description.CenterY.Value);
                        break;
                    case ElementType.Text:
                        buildingBounds.UpdateBounds(description.CenterX.Value, description.CenterY.Value);
                        int roomNumber;
                        if (int.TryParse(description.Text, out roomNumber))
                        {
                            RoomLocations.Add(roomNumber, new Point(description.CenterX.Value, description.CenterY.Value));
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
            foreach (var x in elements.Where(p => p.IsHall.Value))
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

        public async Task Render(Canvas parent, CompositeTransform textRotation,
            Action<BoundingRectangle> mapBounds, Func<Action, Task> dispatcher, RoomInfo location)
        {
            await dispatcher(() => mapBounds(buildingBounds));

            foreach (var description in elements.Where(p => p.Parent == ParentType.Labels || p.Parent == ParentType.Rooms))
            {
                await this.checkRunActions(50, () => RenderElement(parent, textRotation, description), dispatcher);
            }

            // Highlight the correct room
            await this.checkRunActions(1, () =>
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
                await this.checkRunActions(5, () => RenderElement(parent, textRotation, description), dispatcher);
            }

            await this.checkRunActions(0, () => { }, dispatcher);
        }

        private static int ToInt(string s)
        {
            int tmp = 0;
            int.TryParse(s, out tmp);
            return tmp;
        }

        private async Task checkRunActions(int actionCount, Action a, Func<Action, Task> dispatcher)
        {
            actions.Add(a);
            if (actions.Count >= actionCount)
            {
                var oldActions = actions;
                actions = new List<Action>();
                await dispatcher(() => { foreach (var aa in oldActions)  aa(); });
#if !NETFX_CORE
                //                System.Threading.Thread.Sleep(30);
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
                        Width = element.Width.Value,
                        Height = element.Height.Value,
                        Fill = BrushForColor(element.Fill),
                        Stroke = BrushForColor(element.Stroke),
                    };
                    parent.Children.Add(e);
                    Canvas.SetLeft(e, element.CenterX.Value);
                    Canvas.SetTop(e, element.CenterY.Value);
                    break;
                case ElementType.Path:
                    Path path = new Path()
                    {
                        Data = ProcessPathData(element.GetPath()),
                        Tag = element.Text,
                        Fill = BrushForColor(element.Fill),
                        Stroke = BrushForColor(element.Stroke),
                        StrokeThickness = element.Scale.Value * (double)Path.StrokeThicknessProperty.GetMetadata(typeof(Path)).DefaultValue
                    };
                    parent.Children.Add(path);
                    break;
                case ElementType.Text:
                    Canvas canvas = new Canvas()
                    {
                        RenderTransform = textRotation,
                    };
                    FrameworkElement tb =
                        new Border()
                        {
                            //BorderThickness = new Thickness(1),
                            //BorderBrush = new SolidColorBrush(Colors.Magenta),
                            Child =
                                new TextBlock()
                                {
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    Foreground = BrushForColor("white"), //  element.Stroke),
                                    FontSize = element.FontSize * 0.85,
                                    Text = element.Text,
                                }
                        };
                    canvas.Children.Add(tb);

                    tb.LayoutUpdated += (a, b) =>
                    {
                        Canvas.SetLeft(tb, -tb.ActualWidth / 2);
                        Canvas.SetTop(tb, -tb.ActualHeight / 2);
                    };

                    parent.Children.Add(canvas);
                    Canvas.SetLeft(canvas, element.CenterX.Value);
                    Canvas.SetTop(canvas, element.CenterY.Value);
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
                            Size = new Size(pi.size.Value.X, pi.size.Value.Y),
                            IsLargeArc = pi.isLargeArc.Value,
                            Point = pi.point,
                            RotationAngle = pi.angle.Value,
                            SweepDirection = pi.isSweepDirectionClockwise.Value ? SweepDirection.Clockwise : SweepDirection.Counterclockwise
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
