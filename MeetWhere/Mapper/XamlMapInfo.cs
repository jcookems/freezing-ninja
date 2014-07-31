using System;
using System.Collections.Generic;
using System.Linq;
using Mapper.Consumer;
using System.Threading.Tasks;

#if NETFX_CORE
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Diagnostics;
#endif

namespace Mapper
{
    public class XamlMapInfo
    {
        private static Dictionary<string, Brush> brushCache;
        private Dictionary<int, Point> RoomLocations = new Dictionary<int, Point>();
        private List<Action> actions = new List<Action>();
        private IEnumerable<ElementDescription> elements = new List<ElementDescription>();
        private BoundingRectangle buildingBounds = new BoundingRectangle();
        private double pathScale;

        public XamlMapInfo(RoomInfo location, string svgDocContent, double scale)
        {
            elements = (List<ElementDescription>)Newtonsoft.Json.JsonConvert.DeserializeObject(svgDocContent, typeof(List<ElementDescription>));
            UpdateBounds(scale);
        }

        public XamlMapInfo(RoomInfo location, IEnumerable<ElementDescription> elements, double scale)
        {
            this.elements = elements;
            UpdateBounds(scale);
        }

        private void UpdateBounds(double scale)
        {
            foreach (var description in this.elements)
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
                        if (description.CenterX.HasValue && description.CenterY.HasValue)
                        {
                            buildingBounds.UpdateBounds(description.CenterX.Value, description.CenterY.Value);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        ///// <summary>
        ///// If the point is in a hall, just return it. Else, move the point so that it is
        ///// inside the nearest hall.
        ///// </summary>
        ///// <param name="p"></param>
        ///// <returns></returns>
        //public Point RebasePointToHall(Point testPoint)
        //{
        //    // We have the hall outlines, 

        //    Tuple<Point, double> prevNearestWall = null;
        //    foreach (var x in elements.Where(p => p.IsHall.Value))
        //    {
        //        var nearestWall = x.GetClosestWallIntersection(testPoint);
        //        if (nearestWall.Item2 <= 0)
        //        {
        //            return testPoint;
        //        }
        //        else if (prevNearestWall == null || nearestWall.Item2 < prevNearestWall.Item2)
        //        {
        //            prevNearestWall = nearestWall;
        //        }
        //    }

        //    return prevNearestWall.Item1;
        //}

        //public Point? GetRoomLocation(int room)
        //{
        //    Point loc;
        //    return (RoomLocations.TryGetValue(room, out loc) ? (Point?)loc : null);
        //}

        public async Task Render(UIElementCollection childCollection, Transform textRotation,
            Action<BoundingRectangle, double, double> mapBounds,
            Func<Action, Task> dispatcher, RoomInfo location,
            FrameworkElement overlay)
        {
            // Use the whole object to allow the overlay to render in the meantime.
            if (overlay.ActualWidth == 0 || overlay.ActualHeight == 0)
            {
#if NETFX_CORE
                EventHandler<object> foo = null;
                foo = (object sender, object e) =>
#else
                EventHandler foo = null;
                foo = (object sender, EventArgs e) =>
#endif
                {
                    mapBounds(buildingBounds, overlay.ActualWidth, overlay.ActualHeight);
                    overlay.LayoutUpdated -= foo;
                };
                overlay.LayoutUpdated += foo;
            }
            else
            {
                await dispatcher(() => mapBounds(buildingBounds, overlay.ActualWidth, overlay.ActualHeight));
            }

            // Do the rooms first
            foreach (var description in elements
                .Where(p => p.Parent == ParentType.Labels || p.Parent == ParentType.Rooms)
                .Where(p => p.Type != ElementType.Text))
            {
                await this.checkRunActions(50, () => RenderElement(childCollection, textRotation, description), dispatcher);
            }

            // Then the text on top
            foreach (var description in elements
                .Where(p => p.Parent == ParentType.Labels || p.Parent == ParentType.Rooms)
                .Where(p => p.Type == ElementType.Text))
            {
                await this.checkRunActions(50, () => RenderElement(childCollection, textRotation, description), dispatcher);
            }

            // Highlight the correct room
            await this.checkRunActions(1, () =>
               {
                   var roomElement = childCollection.OfType<Path>().FirstOrDefault(p => ToInt(p.Tag as string) == location.Room);
                   if (roomElement != null)
                   {
                       roomElement.Fill = new SolidColorBrush(Colors.Magenta);
                   }
               }, dispatcher);

            foreach (var description in elements.Where(p => p.Parent == ParentType.Background))
            {
                // Fewer because these paths are more complex
                //   await this.checkRunActions(50, () => RenderElement(childCollection, textRotation, description), dispatcher);
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
                if (dispatcher == null)
                {
                    foreach (var aa in oldActions) aa();
                }
                else
                {
                    await dispatcher(() => { foreach (var aa in oldActions)  aa(); });
                }
            }
        }

        private static void RenderElement(UIElementCollection childCollection, Transform textRotation, ElementDescription element)
        {
            //    Debug.WriteLine(element.ToString());

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
                    childCollection.Add(e);
                    Canvas.SetLeft(e, element.CenterX.Value);
                    Canvas.SetTop(e, element.CenterY.Value);
                    break;
                case ElementType.Path:
                    Path path = new Path()
                    {
                        Data = ProcessPathData(element.GetPath()),
                        Tag = element.Text,
                        Fill = BrushForColor("#80000000"), // element.Fill),
                        Stroke = BrushForColor(element.Stroke),
                        StrokeThickness = element.Scale.Value * (double)Path.StrokeThicknessProperty.GetMetadata(typeof(Path)).DefaultValue,
                    };
                    childCollection.Add(path);
                    //Canvas.SetLeft(path, element.CenterX.Value);
                    //Canvas.SetTop(path, element.CenterY.Value);
                    break;
                case ElementType.Text:
                    Canvas canvas = new Canvas()
                    {
                        RenderTransform = textRotation,
                    };
                    FrameworkElement tb =
                        new Border()
                        {
                            //       BorderThickness = new Thickness(1),
                            //       BorderBrush = new SolidColorBrush(Colors.Magenta),
                            Child =
                                new TextBlock()
                                {
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    //                                    Foreground = BrushForColor(string.IsNullOrEmpty(element.Stroke) ? "white" : element.Stroke),
                                    Foreground = BrushForColor("white"),
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

                    childCollection.Add(canvas);
                    Canvas.SetLeft(canvas, element.CenterX.Value);
                    Canvas.SetTop(canvas, element.CenterY.Value);
                    break;
                default:
                    throw new InvalidOperationException("Unexpected node: " + element.Type);
            }
        }

        private static Geometry ProcessPathData(List<PathInfo> pis)
        {
            if (pis == null) return null;
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
                if (colorName.StartsWith("#"))
                {
                    string opacityPart = (colorName.Length == 7 ? "FF" : colorName.Substring(1, 2));
                    string colorPart = colorName.Substring((colorName.Length == 7 ? 1 : 3), 6);
                    byte a = (byte)Convert.ToInt32(opacityPart, 16);
                    byte r = (byte)Convert.ToInt32(colorPart.Substring(0, 2), 16);
                    byte g = (byte)Convert.ToInt32(colorPart.Substring(2, 2), 16);
                    byte b = (byte)Convert.ToInt32(colorPart.Substring(4, 2), 16);
                    Color c = Color.FromArgb(a, r, g, b);
                    var scb = new SolidColorBrush(c);
                    brushCache.Add(colorName, scb);
                    return scb;
                }
                throw new ArgumentOutOfRangeException("Color not supported: " + colorName);
            }
        }
    }
}
