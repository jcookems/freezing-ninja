using Mapper;
using System;
using System.Diagnostics;
#if NETFX_CORE || WINDOWS_PHONE
using Windows.Devices.Geolocation;
#else
using WiFiAPMapper;
#endif
using MeetWhere.Cloud;
using MeetWhere.XPlat;
using System.Linq;
using System.Collections.Generic;

#if !NETFX_CORE
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FakePoint = System.Windows.Input.StylusPoint;
using FakePoint2 = System.Windows.Point;
using FakeVisibility = System.Windows.Visibility;
using System.Text;

#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using FakePoint = Windows.Foundation.Point;
using FakePoint2 = Windows.Foundation.Point;
using FakeVisibility = Windows.UI.Xaml.Visibility;

#endif
using MappingBase;


namespace MeetWhere
{
    public partial class MapView : UserControl
    {
        private GeoCoord geoC;
        private int mapSize;
        private int zoomLevel;
        private GeoCoord coordinate;
        private double MetersPerScreenPoint
        {
            get
            {
                // This is Bing-Maps specific
                double MapSize = (uint)256 << zoomLevel; // 256 * 2^levelOfDetail
                return geoC.LonDegreeSizeInMeters * 360 / MapSize;
            }
        }

        public int Building { get; private set; }
        public int Floor { get; private set; }

        public MapView()
        {
            InitializeComponent();
        }

        private CompositeTransformHelper _viewTransform;
        private CompositeTransformHelper ViewTransform
        {
            get
            {
                if (_viewTransform == null)
                {
                    _viewTransform = new CompositeTransformHelper();
                    this.RenderTransform = _viewTransform.Transform;
                }
                return _viewTransform;
            }
        }

        private CompositeTransformHelper _viewChildrenTransform;
        private CompositeTransformHelper ViewChildrenTransform
        {
            get
            {
                if (_viewChildrenTransform == null)
                {
                    _viewChildrenTransform = new CompositeTransformHelper();
                    this.viewChildren.RenderTransform = _viewChildrenTransform.Transform;
                }
                return _viewChildrenTransform;
            }
        }

        private CompositeTransformHelper _textRotation2;
        private CompositeTransformHelper TextRotation2
        {
            get
            {
                if (_textRotation2 == null)
                {
                    _textRotation2 = new CompositeTransformHelper();
                }
                return _textRotation2;
            }
        }

        public Transform TextRotation
        {
            get
            {
                return TextRotation2.Transform;
            }
        }

        public UIElementCollection LiveChildCollection
        {
            get
            {
                return this.viewChildren.Children;
            }
        }

        public MapMetadata MapMetadata
        {
            set
            {
                this.Building = value.Building;
                this.Floor = value.Floor;

                this.geoC = new GeoCoord(value.CenterLat, value.CenterLong);
                this.mapSize = value.MapSize;
                this.zoomLevel = value.ZoomLevel;
                this.ViewChildrenTransform.Rotation = -value.Angle;
                this.ViewChildrenTransform.Scale = value.Scale;
                this.ViewChildrenTransform.TranslateX = value.OffsetX;
                this.ViewChildrenTransform.TranslateY = value.OffsetY;

                UpdateImage();
            }
            get
            {
                return new MapMetadata()
                {
                    Building = this.Building,
                    Floor = this.Floor,

                    CenterLat = this.geoC.Latitude,
                    CenterLong = this.geoC.Longitude,
                    MapSize = this.mapSize,
                    ZoomLevel = this.zoomLevel,

                    Angle = -this.ViewChildrenTransform.Rotation,
                    Scale = this.ViewChildrenTransform.Scale,
                    OffsetX = this.ViewChildrenTransform.TranslateX,
                    OffsetY = this.ViewChildrenTransform.TranslateY,
                };
            }
        }

#if !WINDOWS_PHONE
        public void DeltaOffsetRot(double delta)
        {
            ViewChildrenTransform.Rotation -= delta;
            TextRotation2.Rotation = -ViewChildrenTransform.Rotation;
        }

        public void DeltaOffsetScale(double delta)
        {
            ViewChildrenTransform.Scale *= delta;
        }

        public void OffsetTranslate(double x, double y)
        {
            ViewChildrenTransform.TranslateX += x / ViewChildrenTransform.Scale / ViewTransform.Scale;
            ViewChildrenTransform.TranslateY += y / ViewChildrenTransform.Scale / ViewTransform.Scale;
        }
#endif

        public void SetViewForMap(BoundingRectangle buildingBounds, double overlayActualWidth, double overlayActualHeight)
        {
            // NOTE: Do *not* reorder the transformation calls.
            // Transforms are non-abelian.
            ViewTransform.CenterX = overlayActualWidth / 2;
            ViewTransform.CenterY = overlayActualHeight / 2;
            SetBaseRotation();
            this.SetTranslation(overlayActualWidth / 2, overlayActualHeight / 2);

            // After centering, then can do the scaling.
            double sizeOfMapOnScreen = Math.Sqrt(
                buildingBounds.Width * buildingBounds.Width +
                buildingBounds.Height * buildingBounds.Height)
                * ViewChildrenTransform.Scale;
            double minViewSize = Math.Min(overlayActualHeight, overlayActualWidth);
            if (minViewSize != 0)
            {
                AddScale(minViewSize / sizeOfMapOnScreen);
            }

            Canvas.SetLeft(this.viewBackground, -mapSize / 2);
            Canvas.SetTop(this.viewBackground, -mapSize / 2);
    }

        private void UpdateImage()
        {
            var loadTask = CloudAccesser.LoadMapImage(geoC, this.zoomLevel, mapSize);
            loadTask.ContinueWith(t =>
            {
                var x = UI.InvokeOnUI(this.Dispatcher, async () =>
                {
                    BitmapImage bm = await Cache.GetImage(t.Result);
                    this.viewBackground.Source = bm;
#if WINDOWS_PHONE || NETFX_CORE
                    this.UpdateCurrentUI();
#endif
                });
            });
        }

        public void ResetMap()
        {
            viewChildren.Children.Clear();
            SetBaseRotation();
            SetScale(1);
            SetTranslation();
        }

        public void AddBaseRotation(double deltaRot = 0, FakePoint2 fixedPoint = new FakePoint2())
        {
            SetBaseRotation(this.ViewTransform.Rotation + deltaRot, fixedPoint);
        }

        /// <summary>
        /// Change the rotation angle in radians
        /// </summary>
        /// <param name="rawRot">Delta angle in radians</param>
        /// <param name="fixedPointX">Point to remain fixed under rotation, 
        /// relative to the center of the control.</param>
        public void SetBaseRotation(double rawRot = -Math.PI / 2, FakePoint2 fixedPoint = new FakePoint2())
        {
            double rot = rawRot + Math.PI / 2;
            double prevRot = ViewTransform.Rotation * 2 * Math.PI / 360;
            double deltaRot = prevRot - rot;

            // Get the location of the fixed point relative to the center of the view
            double centerOffsetX = ViewTransform.TranslateX - fixedPoint.X;
            double centerOffsetY = ViewTransform.TranslateY - fixedPoint.Y;

            // Compute the change of the offset due to the rotation
            double c = Math.Cos(deltaRot);
            double s = Math.Sin(deltaRot);
            double newTranslateX = ViewTransform.TranslateX + centerOffsetX * (c - 1) + centerOffsetY * s;
            double newTranslateY = ViewTransform.TranslateY + centerOffsetY * (c - 1) - centerOffsetX * s;

            // Shift the center so that the fixed point didn't move
            SetTranslation(newTranslateX, newTranslateY);

            ViewTransform.Rotation = rot * 360 / (2 * Math.PI);
            TextRotation2.Rotation = -ViewTransform.Rotation - ViewChildrenTransform.Rotation;
        }

        public void SetScale(double newScale = 1)
        {
            ViewTransform.Scale = newScale;
        }

        /// <summary>
        /// Scale around the center of the screen
        /// </summary>
        public void AddScale(double newScale, double fixedPointX = 0, double fixedPointY = 0)
        {
            if (double.IsNaN(newScale) || double.IsInfinity(newScale))
            {
                Debug.WriteLine("Error in AddScale: newscale = " + newScale);
                return;
            }

            // Get the location of the fixed point relative to the center of the view
            double centerOffsetX = ViewTransform.TranslateX - fixedPointX;
            double centerOffsetY = ViewTransform.TranslateY - fixedPointY;

            // Compute the change of the offset due to the scaling
            double newTranslateX = ViewTransform.TranslateX * newScale + fixedPointX * (1 - newScale);
            double newTranslateY = ViewTransform.TranslateY * newScale + fixedPointY * (1 - newScale);

            // Shift the center so that the fixed point didn't move
            SetTranslation(newTranslateX, newTranslateY);

            ViewTransform.Scale *= newScale;
        }

        public void SetTranslation(double x = 0, double y = 0)
        {
            ViewTransform.TranslateX = x;
            ViewTransform.TranslateY = y;
        }

        public void AddTranslation(double x = 0, double y = 0)
        {
            ViewTransform.TranslateX += x;
            ViewTransform.TranslateY += y;
        }

        public GeoCoord GetGeocordOfPoint(double x, double y)
        {
            FakePoint2 v = new FakePoint2(x, y);
#if NETFX_CORE
            var pointOnMap = ViewTransform.Inverse.TransformPoint(v);
#else
            var pointOnMap = ViewTransform.Inverse.Transform(v);
#endif
            scanLoc.Visibility = FakeVisibility.Visible;
            return geoC.FromOffset(pointOnMap.X * MetersPerScreenPoint, pointOnMap.Y * MetersPerScreenPoint);
        }

#if !NETFX_CORE && !WINDOWS_PHONE
        internal void ShowAPsPoints(IEnumerable<WiFiAccessPoint> aps)
        {

            foreach (var ap in aps)
            {
                NewMethod(ap.CenterLatitude, ap.CenterLongitude, ap.Spread * 20);
            }

        }
#endif
        private static Random r = new Random();
        private void NewMethod(double lat, double lon, double rad)
        {

            Ellipse e = new Ellipse();
            this.LayoutRoot.Children.Add(e);
            var x = new byte[3];
            r.NextBytes(x);
#if !NETFX_CORE
            e.Fill = new SolidColorBrush(Color.FromArgb(255, x[0], x[1], x[2]));
#endif
            var g = new GeoCoord(lat, lon, rad);

            UpdateCurrentUI(e, g);
        }

        public void ShowScanLoc(GeoCoord? loc)
        {
            scanLoc.Visibility = FakeVisibility.Visible;
            if (loc.HasValue)
            {
                UpdateCurrentUI(scanLoc, loc.Value);
            }
        }

        public void UpdateCurrent(GeoCoord coordinate)
        {
            this.coordinate = coordinate;
            UpdateCurrentUI();
        }

        private void UpdateCurrentUI()
        {
            if (coordinate == null)
            {
                this.currentLoc.Visibility = FakeVisibility.Collapsed;
            }
            else
            {
                this.currentLoc.Visibility = FakeVisibility.Visible;
                UpdateCurrentUI(this.currentLoc, coordinate);
            }
        }

        private void UpdateCurrentUI(FrameworkElement marker, GeoCoord loc)
        {
            // In terms of pixels, it is 
            marker.Width = loc.Accuracy / MetersPerScreenPoint;
            marker.Height = loc.Accuracy / MetersPerScreenPoint;

            var metersOff = geoC.OffsetInMeters(loc);
            if (zoomLevel != 0)
            {
                Canvas.SetLeft(marker, metersOff.X / MetersPerScreenPoint - marker.Width / 2);
                Canvas.SetTop(marker, metersOff.Y / MetersPerScreenPoint - marker.Height / 2);
            }
        }

    }
}
