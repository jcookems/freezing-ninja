using Mapper;
using Mapper.Consumer;
using MeetWhere.Cloud;
using System;
#if !NETFX_CORE
using Microsoft.Devices.Sensors;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.UserData;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Windows.Devices.Geolocation;
using FakeDoubleTapEventArgs = System.Windows.Input.GestureEventArgs;
using FakePointerButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using FakePointerEventArgs = System.Windows.Input.MouseEventArgs;
using FakePage = Microsoft.Phone.Controls.PhoneApplicationPage;
using FakePoint = System.Windows.Input.StylusPoint;
using FakePoint2 = System.Windows.Point;
#else
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Input;
using Windows.Foundation;
using FakeDoubleTapEventArgs = Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs;
using FakePointerButtonEventArgs = Windows.UI.Xaml.Input.PointerRoutedEventArgs;
using FakePointerEventArgs = Windows.UI.Xaml.Input.PointerRoutedEventArgs;
using FakePage = Windows.UI.Xaml.Controls.Page;
using FakePoint = Windows.Foundation.Point;
using FakePoint2 = Windows.Foundation.Point;
#endif
using System.Diagnostics;
using Windows.UI;


namespace MeetWhere
{
    public partial class MainPage : FakePage
    {
#if !NETFX_CORE
        private Motion motion;
        private double? prevRot = null;
#endif
        private Geolocator geolocator;
        private Geocoordinate coordinate;
        private PositionStatus status;
        // This factor is to combat clipping and smooshing of the controls in XAML
        private double mapScaleFactor = 0.25;


        private CompositeTransform ViewTransform
        {
            get
            {
                CompositeTransform transform = view.RenderTransform as CompositeTransform;
                if (transform == null)
                {
                    transform = new CompositeTransform();
                    view.RenderTransform = transform;
                }
                return transform;
            }
        }

        public MainPage()
        {
            InitializeComponent();
            this.Loaded += async (s, e) =>
            {
                try
                {
                    var x = await CloudAccesser.Authenticate(true);
                    UpdateLoginLogoutUI();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            };
        }

#if !NETFX_CORE
        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (!isOnMainScreen())
            {
                e.Cancel = true;
                backToMainScreen();
            }
            else
            {
                base.OnBackKeyPress(e);
            }
        }
#endif

        private bool isOnMainScreen()
        {
            return ContentPanel.Visibility == Visibility.Visible;
        }

        private void backToMainScreen()
        {
            roomDisplay.Visibility = Visibility.Collapsed;
            ContentPanel.Visibility = Visibility.Visible;
            view.Visibility = Visibility.Collapsed;
            overlay.Visibility = Visibility.Collapsed;
            ResetMap();
        }

        private async Task<object> ShowMap(RoomInfo location)
        {
            if (location == null) return null;

            string svgDocContent = await CloudAccesser.LoadMapSvg(location, Waiter);

            UpdateLoginLogoutUI();

            if (svgDocContent == null) return null;

            ResetMap();

            this.roomDisplay.Text = location.ToString();

            roomDisplay.Visibility = Visibility.Visible;
            ContentPanel.Visibility = Visibility.Collapsed;
            view.Visibility = Visibility.Visible;
            overlay.Visibility = Visibility.Visible;
            var resources = this.Resources;
            var textRotation = (CompositeTransform)this.Resources["antiRotation"];
            var dispatcher = this.Dispatcher;

            var map = new XamlMapInfo(location, svgDocContent, mapScaleFactor);
            return map.Render(viewChildren, textRotation, SetViewForMap, RunOnUIThread, location);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
#if !NETFX_CORE
            if (!IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent"))
            {
                MessageBoxResult result =
                    MessageBox.Show("This app accesses your phone's location. Is that ok?",
                    "Location",
                    MessageBoxButton.OKCancel);
                IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = (result == MessageBoxResult.OK);
                IsolatedStorageSettings.ApplicationSettings.Save();
            }

            if ((bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"])
#endif
            {
                geolocator = new Geolocator();
                geolocator.DesiredAccuracy = PositionAccuracy.High;
                geolocator.MovementThreshold = 1; // The units are meters.

                geolocator.StatusChanged += (sender, args) =>
                {
                    status = args.Status;
                    ShowLoc();
                };
                geolocator.PositionChanged += (sender, args) =>
                {
                    coordinate = args.Position.Coordinate;
                    ShowLoc();
                };
            }

#if !NETFX_CORE
            if (Motion.IsSupported)
            {
                motion = new Motion();
                motion.TimeBetweenUpdates = TimeSpan.FromMilliseconds(20);
                motion.CurrentValueChanged += (sender, e2) => CurrentValueChanged(e2.SensorReading);

                // Try to start the Motion API.
                try
                {
                    motion.Start();
                }
                catch (Exception)
                {
                    MessageBox.Show("unable to start the Motion API.");
                }
            }

            Appointments appointments = new Appointments();
            appointments.SearchCompleted += (sender, e2) =>
            {
                DateList.ItemsSource = e2.Results.OrderBy(p => p.StartTime);
                Waiter(false);
            };
            appointments.SearchAsync(DateTime.Now, DateTime.Now.AddDays(7), null);
#else
            Waiter(false);
#endif

        }

#if NETFX_CORE
        private void Foo_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            FrameworkElement source = (FrameworkElement)sender;
            var point = e.GetCurrentPoint(source);
            if (this.wheelToggle.IsChecked.Value)
            {
                double newScale = Math.Exp(point.Properties.MouseWheelDelta * 0.0005);
                ScaleIt(newScale, point.Position.X - source.ActualWidth / 2, point.Position.Y - source.ActualHeight / 2);
            }
            else
            {
                double newRot = (ViewTransform.Rotation + point.Properties.MouseWheelDelta / 10) * 2 * Math.PI / 360;
                SetBaseRotation(newRot, new Point(point.Position.X - source.ActualWidth / 2, point.Position.Y - source.ActualHeight / 2));
            }

            e.Handled = true;
        }
#else
        private void Foo_ManipulationDelta_1(object sender, ManipulationDeltaEventArgs e)
        {
            if (e.PinchManipulation == null)
            {
                return;
            }

            // Scale Manipulation
            FrameworkElement source = (FrameworkElement)sender;
            var point = e.PinchManipulation.Current.Center;
            ScaleIt(e.PinchManipulation.DeltaScale, point.X - source.ActualWidth / 2, point.Y - source.ActualHeight / 2);
            e.Handled = true;
        }

        private void CurrentValueChanged(MotionReading reading)
        {
            Microsoft.Xna.Framework.Vector3 transformed = Microsoft.Xna.Framework.Vector3.Transform(new Microsoft.Xna.Framework.Vector3(-1, 0, 0), reading.Attitude.RotationMatrix);
            double rot = Math.Atan2(transformed.X, transformed.Y);

            if (!prevRot.HasValue || Math.Abs(rot - prevRot.Value) > 0.01)
            {
                Dispatcher.BeginInvoke(() => SetBaseRotation(rot));
            }
        }

        private async void DateList_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }
            var content = (Appointment)e.AddedItems[0];
            DateList.SelectedIndex = -1;
            await ShowMap(RoomInfo.Parse(content.Location));
        }
#endif

        private void ResetMap()
        {
            this.roomDisplay.Text = "";
            viewChildren.Children.Clear();

            SetBaseRotation(-90 * 2 * Math.PI / 360);
            SetScale(1);
            SetTranslation();
        }

        private void SetViewForMap(BoundingRectangle buildingBounds)
        {
            ViewTransform.CenterX = buildingBounds.Width / 2;
            ViewTransform.CenterY = buildingBounds.Height / 2;
            view.Width = buildingBounds.Width;
            view.Height = buildingBounds.Height;


            Debug.WriteLine(-buildingBounds.CenterX + buildingBounds.Width / 2);

            Canvas.SetLeft(viewChildren, -buildingBounds.CenterX + buildingBounds.Width / 2);
            Canvas.SetTop(viewChildren, -buildingBounds.CenterY + buildingBounds.Height / 2);

            //            Canvas.SetLeft(viewCenter, buildingBounds.Width / 2 - viewCenter.ActualWidth / 2);
            //          Canvas.SetTop(viewCenter, buildingBounds.Height / 2 - viewCenter.ActualHeight / 2);
            offsetx = buildingBounds.Width / 2;
            offsety = buildingBounds.Height / 2;

            double scaleDiff = Math.Min(this.overlay.ActualWidth / view.Width, this.overlay.ActualHeight / view.Width);
            SetScale(scaleDiff * 0.7);
        }

        double offsetx;
        double offsety;

        /// <summary>
        /// Change the rotation angle in radians
        /// </summary>
        /// <param name="rot">Delta angle in radians</param>
        /// <param name="fixedPointX">Point to remain fixed under rotation, relative to the center of the control.</param>
        private void SetBaseRotation(double rot, Point fixedPoint = new Point())
        {
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
            var ar = (CompositeTransform)this.Resources["antiRotation"];
            ar.Rotation = -ViewTransform.Rotation;
        }


        private void SetScale(double newScale = 1)
        {
            ViewTransform.ScaleX = newScale;
            ViewTransform.ScaleY = newScale;
        }

        /// <summary>
        /// Scale around the center of the screen
        /// </summary>
        private void ScaleIt(double newScale, double fixedPointX = 0, double fixedPointY = 0)
        {
            // Get the location of the fixed point relative to the center of the view
            double centerOffsetX = ViewTransform.TranslateX - fixedPointX;
            double centerOffsetY = ViewTransform.TranslateY - fixedPointY;
            Debug.WriteLine(centerOffsetX + ", " + centerOffsetY);

            // Compute the change of the offset due to the scaling
            double newTranslateX = ViewTransform.TranslateX * newScale + fixedPointX * (1 - newScale);
            double newTranslateY = ViewTransform.TranslateY * newScale + fixedPointY * (1 - newScale);

            // Shift the center so that the fixed point didn't move
            SetTranslation(newTranslateX, newTranslateY);

            ViewTransform.ScaleX *= newScale;
            ViewTransform.ScaleY *= newScale;
        }

        private void SetTranslation(double x = 0, double y = 0)
        {
            ViewTransform.TranslateX = x;
            ViewTransform.TranslateY = y;
        }

        private void TranslateIt(double x, double y)
        {
            ViewTransform.TranslateX += x;
            ViewTransform.TranslateY += y;
        }

        private bool mouseDown;
        private FakePoint prevMouseLoc;

        private void Foo_MouseLeftButtonDown_1(object sender, FakePointerButtonEventArgs e)
        {
            FrameworkElement source = (FrameworkElement)sender;
            prevMouseLoc = GetPointerPoint(sender, e);
            var thisPoint = new FakePoint2(prevMouseLoc.X - source.ActualWidth / 2 + offsetx, prevMouseLoc.Y - source.ActualHeight / 2 + offsety);
            var inv = ViewTransform.Inverse;
#if NETFX_CORE
            var orig = inv.TransformPoint(thisPoint);
#else
            var orig = inv.Transform(thisPoint);
#endif
            Debug.WriteLine("Point p@ = new Point(" + orig.ToString() + ");");
//            Canvas.SetLeft(CurrentLoc, orig.X - CurrentLoc.Width / 2);
//            Canvas.SetTop(CurrentLoc, orig.Y - CurrentLoc.Height / 2);
            mouseDown = true;
        }

        private void ShowLoc()
        {
            RunOnUIThread(() =>
            {
                CurrentLoc.Visibility = (coordinate == null ? Visibility.Collapsed : Visibility.Visible);
                if (coordinate != null)
                {
                    CurrentLoc.Width = coordinate.Accuracy * MetersToScreen();
                    CurrentLoc.Height = coordinate.Accuracy * MetersToScreen();
                    Point loc = GeolocToPoint(coordinate);
                    Canvas.SetLeft(CurrentLoc, loc.X - CurrentLoc.Width / 2);
                    Canvas.SetTop(CurrentLoc, loc.Y - CurrentLoc.Height / 2);
                }
            });
        }

        // TODO: These should be part of the map metadata.
        private Point GeolocToPoint(Geocoordinate coord)
        {
            Debug.WriteLine("Point geo@ = new Point(" + coord.Latitude + ", " + coord.Longitude + ");");

            Point geo1 = new Point(47.6403274536133, -122.126655578613);
            Point geo2 = new Point(47.6408805847168, -122.125839233398);
            Point p1 = new Point(-2.71354556083679, -4.37648582458496);
            Point p2 = new Point(133.738830566406, 132.851181030273);

            double fX2 = (coord.Latitude - geo1.X) / (geo2.X - geo1.X);
            double fY2 = (coord.Longitude - geo1.Y) / (geo2.Y - geo1.Y);
            double pX = p1.X * (1 - fX2) + p2.X * fX2;
            double pY = p1.Y * (1 - fY2) + p2.Y * fY2;
            Point pRet = new Point(pX, pY);

            return pRet;
        }
        private double MetersToScreen()
        {
            return 2.5;
        }

        private void Foo_MouseMove_1(object sender, FakePointerEventArgs e)
        {
            if (mouseDown)
            {
                var last = GetPointerPoint(sender, e);
                TranslateIt(last.X - prevMouseLoc.X, last.Y - prevMouseLoc.Y);
                prevMouseLoc = last;
            }
        }

        private void Foo_MouseLeftButtonUp_1(object sender, FakePointerButtonEventArgs e)
        {
            mouseDown = false;
        }

        private void Foo_DoubleTap_1(object sender, FakeDoubleTapEventArgs e)
        {
            FrameworkElement source = (FrameworkElement)sender;
            var point = e.GetPosition(source);
            ScaleIt(1.4, point.X - source.ActualWidth / 2, point.Y - source.ActualHeight / 2);
            e.Handled = true;
        }

        private async void SearchLocationButton(object sender, object e)
        {
            RoomInfo ri = null;

            try
            {
                Waiter(true);
                var tcs = new TaskCompletionSource<object>();
                TextBox roomNumber = new TextBox()
                {
                    Text = "2 2102",
                    BorderBrush = new SolidColorBrush(Colors.Gray)
                };

                CustomMessageBox messageBox = new CustomMessageBox()
                {
                    Title = "Find Room",
                    Message = "Enter room number, in the form '24 1152'",
                    Content = roomNumber,
                    LeftButtonContent = "OK",
                    RightButtonContent = "Cancel",
                    IsFullScreen = false,
                };

                messageBox.Dismissed += (s2, e2) =>
                {
                    if (e2.Result == CustomMessageBoxResult.LeftButton && !string.IsNullOrEmpty(roomNumber.Text))
                    {
                        ri = RoomInfo.Parse(roomNumber.Text);
                    }
                };

                messageBox.Unloaded += (s2, e2) => tcs.SetResult(null);
                messageBox.Show();
                await tcs.Task;
                await ShowMap(ri);
            }
            finally
            {
                Waiter(false);
            }
        }

        private void Waiter(bool startWait)
        {
            waitUI.Visibility = (startWait ? Visibility.Visible : Visibility.Collapsed);
        }

        private void BackButton(object sender, object e)
        {
            backToMainScreen();
        }

        private void SettingsButton(object sender, object e)
        {
            CloudAccesser.ClearCachedMaps();
        }

        private void GpsButton(object sender, object e)
        {
            MessageBoxShow("Status: " + status.ToString() + "\n" +
                 (coordinate == null ?
                 "No coordinate data" :
#if !NETFX_CORE
 "Coordinate data source: " + coordinate.PositionSource.ToString() + "\n" +
#endif
 "Accuracy: " + coordinate.Accuracy + " meters\n" +
                 "Latitude: " + coordinate.Latitude + "\n" +
                 "Longitude: " + coordinate.Longitude));
        }

        private async void LoginButton(object sender, object e)
        {
            if (CloudAccesser.IsLoggedin())
            {
                CloudAccesser.Logout();
            }
            else
            {
                Waiter(true);
                try
                {
                    await CloudAccesser.TryLogin();
                }
                finally
                {
                    Waiter(false);
                }
            }

            UpdateLoginLogoutUI();
        }

        private void UpdateLoginLogoutUI()
        {
#if NETFX_CORE
            if (CloudAccesser.IsLoggedin())
            {
                loginButton.Content = "logout";
            }
            else
            {
                loginButton.Content = "login";
            }
#else
            var loginButton = ApplicationBar.Buttons.OfType<ApplicationBarIconButton>().First(p => p.Text == "login" || p.Text == "logout");
            if (CloudAccesser.IsLoggedin())
            {
                loginButton.Text = "logout";
                loginButton.IconUri = new Uri("/Images/logout.png", UriKind.Relative);
            }
            else
            {
                loginButton.Text = "login";
                loginButton.IconUri = new Uri("/Images/login.png", UriKind.Relative);
            }
#endif
        }


        private void ZoomIn(object sender, object e)
        {
            double newScale = 1.1;
            ScaleIt(newScale);
        }

        private void ZoomOut(object sender, object e)
        {
            double newScale = 0.9;
            ScaleIt(newScale);
        }

        private double rot = 0;
        private void RotateRight(object sender, object e)
        {
            rot -= 45 * Math.PI / 360;
            SetBaseRotation(rot);
        }

        private void RotateLeft(object sender, object e)
        {
            rot += 45 * Math.PI / 360; ;
            SetBaseRotation(rot);
        }

        private void AboutButton(object sender, object e)
        {
            MessageBoxShow("meet where?\n\nAuthor: Jason Cooke\njcooke@microsoft.com");
        }

        #region Platform-specific code

        private Task RunOnUIThread(Action agileCallback)
        {
#if !NETFX_CORE
            var tcs = new TaskCompletionSource<object>();
            System.Threading.Thread t = new System.Threading.Thread(() =>
                Dispatcher.BeginInvoke(() =>
                {
                    agileCallback();
                    tcs.SetResult(null);
                })
            );
            t.Start();
            return tcs.Task;
#else
            return Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => agileCallback()).AsTask();
#endif
        }

        private static FakePoint GetPointerPoint(object sender, FakePointerEventArgs e)
        {
#if NETFX_CORE
            return e.GetCurrentPoint((UIElement)sender).Position;
#else
            var ret = e.GetPosition((UIElement)sender);
            return new FakePoint(ret.X, ret.Y);
#endif
        }

        private static Task MessageBoxShow(string message)
        {
#if NETFX_CORE
            MessageDialog dlg = new Windows.UI.Popups.MessageDialog(message);
            return dlg.ShowAsync().AsTask();
#else
            MessageBox.Show(message);
            var t = new Task<object>(() => null);
            t.Start();
            return t;
#endif
        }
        #endregion

    }
}
