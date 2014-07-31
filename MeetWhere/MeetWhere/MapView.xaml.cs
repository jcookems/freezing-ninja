using Mapper;
using System;
using System.Diagnostics;
#if NETFX_CORE || WINDOWS_PHONE
using Windows.Devices.Geolocation;
#endif
using MeetWhere.Cloud;
using MeetWhere.XPlat;

#if !NETFX_CORE
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FakePoint = System.Windows.Input.StylusPoint;
using FakePoint2 = System.Windows.Point;
using FakeVisibility = System.Windows.Visibility;
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

namespace MeetWhere
{
    public partial class MapView : UserControl
    {
        private FakePoint2 geoC;
        private double mapSize;
        private double geoSize;
#if WINDOWS_PHONE || NETFX_CORE
        private Geocoordinate coordinate;
#endif
        public int building { get; private set; }
        public int floor { get; private set; }

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
                this.building = value.Building;
                this.floor = value.Floor;

                this.geoC = new FakePoint2(value.CenterLong, value.CenterLat);
                this.mapSize = value.MapSize;
                this.geoSize = value.GeoSize;
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
                    Building = this.building,
                    Floor = this.floor,

                    CenterLat = this.geoC.Y,
                    CenterLong = this.geoC.X,
                    MapSize = this.mapSize,
                    GeoSize = this.geoSize,

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

            UpdateImage();
        }

        private void UpdateImage()
        {
            var foo = "ma=" +
                (geoC.Y - geoSize) + "," +
                (geoC.X - geoSize) + "," +
                (geoC.Y + geoSize) + "," +
                (geoC.X + geoSize) + "&mapSize=" + mapSize + "," + mapSize;

            Uri imageAddress = new Uri("https://jcookedemo.azure-mobile.net/api/getmapimage?" + foo);
            Debug.WriteLine(imageAddress.ToString());

            var im = new BitmapImage(imageAddress);
            im.DownloadProgress += (s, e) => Debug.WriteLine("Image download progress: " + e.Progress);
#if WINDOWS_PHONE || NETFX_CORE
            im.ImageFailed += (s, e) => Debug.WriteLine("Image failed: " +
#if !NETFX_CORE
 e.ErrorException);
#else
 e.ErrorMessage);
#endif
            im.ImageOpened += (s, e) => Debug.WriteLine("Image opened");
#endif
            this.viewBackground.Source = im;
#if WINDOWS_PHONE || NETFX_CORE
            this.UpdateCurrentUI();
#endif
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
            Debug.WriteLine("(" + ViewTransform.TranslateX + ", " + ViewTransform.TranslateY + ")");
        }

        public FakePoint2 GetGeocordOfPoint(double x, double y)
        {
            FakePoint2 v = new FakePoint2(x, y);
#if NETFX_CORE
            var pointOnMap = ViewTransform.Inverse.TransformPoint(v);
#else
            var pointOnMap = ViewTransform.Inverse.Transform(v);
#endif

            double pX = pointOnMap.X;
            double pY = pointOnMap.Y;

            double fX2 = pX * 2 / mapSize;
            double fY2 = pY * 2 / mapSize;

            double Longitude = fX2 * 2 * geoSize + geoC.X;
            double Latitude = geoC.Y - fY2 * 2 * geoSize * Math.Cos(geoC.Y * Math.PI / 180);

            scanLoc.Visibility = FakeVisibility.Visible;
            return new FakePoint2(Latitude, Longitude);
        }

        public void ShowScanLoc(double Longitude, double Latitude)
        {
            scanLoc.Visibility = FakeVisibility.Visible;
            UpdateCurrentUI(scanLoc, Longitude, Latitude, 1);
        }

#if WINDOWS_PHONE || NETFX_CORE
        public void UpdateCurrent(Geocoordinate coordinate)
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
                UpdateCurrentUI(this.currentLoc, coordinate.Longitude, coordinate.Latitude, coordinate.Accuracy);
            }
        }
#endif

        private void UpdateCurrentUI(FrameworkElement marker, double Longitude, double Latitude, double Accuracy)
        {
            // Determine the actual size of the geography.
            // From http://en.wikipedia.org/wiki/Latitude,
            // and assuming the earth is spherical (e=1)
            double a = 6378137.0; // Length of equator, in meters
            var latDegreeSizeInMeters = Math.PI / 180 * a;
            var lonDegreeSizeInMeters = Math.PI / 180 * a * Math.Cos(Latitude * Math.PI / 180);
            // In terms of pixels, it is 
            double MetersPerScreenPoint = latDegreeSizeInMeters * geoSize / mapSize;

            marker.Width = Accuracy / MetersPerScreenPoint;
            marker.Height = Accuracy / MetersPerScreenPoint;

            if (geoSize != 0)
            {
                // We ask Bing for a square map, with a "square" range of lat-long.
                // Because the longitude degree length shrinks, need scale factor
                double fX2 = (Longitude - geoC.X) / (2 * geoSize);
                double fY2 = -(Latitude - geoC.Y) / (2 * geoSize * Math.Cos(Latitude * Math.PI / 180));
                double pX = mapSize * fX2 / 2;
                double pY = mapSize * fY2 / 2;
                Canvas.SetLeft(marker, pX - marker.Width / 2);
                Canvas.SetTop(marker, pY - marker.Height / 2);
            }
        }
    }
}
