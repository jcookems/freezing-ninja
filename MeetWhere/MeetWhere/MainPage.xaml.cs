using Mapper.Consumer;
using Mapper;
using MeetWhere.Cloud;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System;

#if WINDOWS_PHONE || NETFX_CORE
using Windows.Devices.Geolocation;
#endif

#if WINDOWS_PHONE
using FakeDoubleTapEventArgs = System.Windows.Input.GestureEventArgs;
using FakeManipulationDeltaArgs = System.Windows.Input.ManipulationDeltaEventArgs;
using FakePage = Microsoft.Phone.Controls.PhoneApplicationPage;
using FakePoint = System.Windows.Input.StylusPoint;
using FakePointerButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using FakePointerEventArgs = System.Windows.Input.MouseEventArgs;
using Microsoft.Devices.Sensors;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.UserData;
using System.IO.IsolatedStorage;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows;
using Microsoft.Devices;
using System.Windows.Media.Imaging;
using ZXing;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections.Generic;
using MeetWhere.XPlat;
using Windows.Phone.Media.Capture;
using System.IO;
#elif NETFX_CORE
using FakeDoubleTapEventArgs = Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs;
using FakeManipulationDeltaArgs = Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs;
using FakeMouseWheelEventArgs = Windows.UI.Xaml.Input.PointerRoutedEventArgs;
using FakePage = Windows.UI.Xaml.Controls.Page;
using FakePoint = Windows.Foundation.Point;
using FakePointerButtonEventArgs = Windows.UI.Xaml.Input.PointerRoutedEventArgs;
using FakePointerEventArgs = Windows.UI.Xaml.Input.PointerRoutedEventArgs;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml;
using Windows.UI;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using ZXing;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Media.MediaProperties;
#else
using FakeDoubleTapEventArgs = System.Windows.Input.MouseEventArgs;
using FakeManipulationDeltaArgs = System.Windows.Input.ManipulationDeltaEventArgs;
using FakeMouseWheelEventArgs = System.Windows.Input.MouseWheelEventArgs;
using FakePage = System.Windows.Window;
using FakePoint = System.Windows.Input.StylusPoint;
using FakePointerButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using FakePointerEventArgs = System.Windows.Input.MouseEventArgs;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using MeetWhere.XPlat;
#endif


namespace MeetWhere
{
    public partial class MainPage : FakePage
    {
#if WINDOWS_PHONE
        private Motion motion;
        private double? prevRot = null;
#endif
#if WINDOWS_PHONE || NETFX_CORE
        private Geolocator geolocator;
        private Geocoordinate coordinate;
        private PositionStatus status;
#endif
        // This factor is to combat clipping and smooshing of the controls in XAML
        private double mapScaleFactor = 0.25;

        public MainPage()
        {
            InitializeComponent();
            this.Loaded += async (s, e) =>
            {
#if NETFX_CORE || WINDOWS_PHONE
                try
                {
                    var x = await CloudAccesser.Authenticate(true);
                    UpdateLoginLogoutUI();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
#else
                backToMainScreen();
                await UI.DummyTask();
                Waiter(false);
#endif
            };
        }

#if WINDOWS_PHONE
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
            this.roomDisplay.Text = "";
            view.ResetMap();
        }

        private async Task ShowMap(RoomInfo location, double? longitude = null, double? latitude = null)
        {
            if (location == null) return;

            string content = await CloudAccesser.LoadMapSvg(location, Waiter);
            MapMetadata mapMetadata = await CloudAccesser.LoadMapMetadata(location);

#if NETFX_CORE || WINDOWS_PHONE
            UpdateLoginLogoutUI();
#endif
            if (content == null) return;

            this.roomDisplay.Text = "";
            view.ResetMap();

            this.roomDisplay.Text = location.ToString();

            roomDisplay.Visibility = Visibility.Visible;
            ContentPanel.Visibility = Visibility.Collapsed;
            view.Visibility = Visibility.Visible;
            overlay.Visibility = Visibility.Visible;
            var resources = this.Resources;
            var dispatcher = this.Dispatcher;

            var map = new XamlMapInfo(location, content, mapScaleFactor);
            this.view.MapMetadata = mapMetadata;
            await map.Render(view.LiveChildCollection, view.TextRotation, (a, b, c) =>
                {
                    this.view.SetViewForMap(a, b, c);
                    if (latitude.HasValue && longitude.HasValue)
                    {
                        view.ShowScanLoc(longitude.Value, latitude.Value);
                    }
                }, RunOnUIThread, location, overlay);
        }

#if WINDOWS_PHONE || NETFX_CORE
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
#if WINDOWS_PHONE
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
#if WINDOWS_PHONE || NETFX_CORE
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
#endif
#if WINDOWS_PHONE
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
                List<FakeAppointment> res = e2.Results.Select(p => new FakeAppointment(p)).ToList();
                if (res.Count == 0)
                {
                    res.Add(new FakeAppointment() { Subject = "Zumo test Team meeting", StartTime = new DateTime(2013, 7, 25), Location = "Conf Room 2/2063 (8) AV" });
                }
                this.DateList.ItemsSource = res.OrderBy(p => p.StartTime);
                Waiter(false);
            };
            appointments.SearchAsync(DateTime.Now, DateTime.Now.AddDays(7), null);
#else
            Waiter(false);
#endif
        }

#endif

#if WINDOWS_PHONE
        private async void DateList_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }
            var content = (FakeAppointment)e.AddedItems[0];
            DateList.SelectedIndex = -1;
            await ShowMap(RoomInfo.Parse(content.Location));
        }
#endif


#if !WINDOWS_PHONE
        private void Foo_PointerWheelChanged(object sender, FakeMouseWheelEventArgs e)
        {
            bool isFineTune = this.fineTuneLocation.IsChecked.Value;
            FrameworkElement source = (FrameworkElement)sender;
#if !NETFX_CORE
            var point = e.GetPosition(source);
            double MouseWheelDelta = e.Delta;
#else
            var vpoint = e.GetCurrentPoint(source);
            double MouseWheelDelta = vpoint.Properties.MouseWheelDelta;
            var point = vpoint.Position;
#endif
            if (this.wheelToggle.IsChecked.Value)
            {
                double multipler = MouseWheelDelta * 0.0005;
                if (isFineTune)
                {
                    multipler *= 0.1;
                }

                double newScale = Math.Exp(multipler);
                if (isFineTune)
                {
                    view.DeltaOffsetScale(newScale);
                }
                else
                {
                    view.AddScale(newScale, point.X - source.ActualWidth / 2, point.Y - source.ActualHeight / 2);
                }
            }
            else
            {
                double multipler = MouseWheelDelta / 10;
                if (isFineTune)
                {
                    multipler *= 0.1;
                }

                double newRot = multipler * 2 * Math.PI / 360;

                if (isFineTune)
                {
                    view.DeltaOffsetRot(newRot);
                }
                else
                {
                    view.AddBaseRotation(newRot, new Point(point.X - source.ActualWidth / 2, point.Y - source.ActualHeight / 2));
                }
            }

            e.Handled = true;
        }
#elif WINDOWS_PHONE
        private void CurrentValueChanged(MotionReading reading)
        {
            Microsoft.Xna.Framework.Vector3 transformed = Microsoft.Xna.Framework.Vector3.Transform(new Microsoft.Xna.Framework.Vector3(-1, 0, 0), reading.Attitude.RotationMatrix);
            double rot = Math.Atan2(transformed.X, transformed.Y);

            if (!prevRot.HasValue || Math.Abs(rot - prevRot.Value) > 0.01)
            {
                Dispatcher.BeginInvoke(() => view.SetBaseRotation(rot));
            }
        }
#endif

        private void Foo_ManipulationDelta_1(object sender, FakeManipulationDeltaArgs e)
        {
#if WINDOWS_PHONE
            if (e.PinchManipulation == null)
            {
                return;
            }

            // Scale Manipulation
            FrameworkElement source = (FrameworkElement)sender;
            var point = e.PinchManipulation.Current.Center;
            view.AddScale(e.PinchManipulation.DeltaScale, point.X - source.ActualWidth / 2, point.Y - source.ActualHeight / 2);
            e.Handled = true;
#elif NETFX_CORE
            if (this.fineTuneLocation.IsChecked.Value)
            {
                view.OffsetTranslate(e.Delta.Translation.X, e.Delta.Translation.Y);
            }
            else
            {
                view.AddTranslation(e.Delta.Translation.X, e.Delta.Translation.Y);
            }
#else
            // Touch and mosue are disjoint in WPF? Just don't worry.
#endif
        }


        private bool mouseDown;
        private FakePoint prevMouseLoc;
        private bool isTagGenEnabled = false;

        private void EnableTagGen(object sender, object e)
        {
            isTagGenEnabled = !isTagGenEnabled;
        }

        private void Foo_MouseLeftButtonDown_1(object sender, FakePointerButtonEventArgs e)
        {
            FrameworkElement source = (FrameworkElement)sender;
            prevMouseLoc = GetPointerPoint(sender, e);
            if (isTagGenEnabled)
            {
                var geo = view.GetGeocordOfPoint(prevMouseLoc.X, prevMouseLoc.Y);
                view.ShowScanLoc(geo.Y, geo.X);
                string url = "https://jcookedemo.azure-mobile.net/api/getinfo?cp=" + geo.X.ToString("F5") + "," + geo.Y.ToString("F5") +
                    "&bld=" + view.building.ToString() +
                    "&flr=" + view.floor.ToString();
                string tag = "http://chart.apis.google.com/chart?cht=qr&chs=120x120&chld=L&choe=ISO-8859-1&chl=" + Uri.EscapeDataString(url);
                Debug.WriteLine(tag);
            }

            mouseDown = true;
        }

#if NETFX_CORE || WINDOWS_PHONE
        private void ShowLoc()
        {
            RunOnUIThread(() => view.UpdateCurrent(coordinate));
        }
#endif

        private void Foo_MouseMove_1(object sender, FakePointerEventArgs e)
        {
            if (mouseDown)
            {
                var last = GetPointerPoint(sender, e);

#if !WINDOWS_PHONE
                if (this.fineTuneLocation.IsChecked.Value)
                {
                    view.OffsetTranslate(last.X - prevMouseLoc.X, last.Y - prevMouseLoc.Y);
                }
                else
#endif
                {
                    view.AddTranslation(last.X - prevMouseLoc.X, last.Y - prevMouseLoc.Y);
                }
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
            view.AddScale(1.4, point.X - source.ActualWidth / 2, point.Y - source.ActualHeight / 2);
            e.Handled = true;
        }

        private void ZoomIn(object sender, object e)
        {
            double newScale = 1.1;
            view.AddScale(newScale);
        }

        private void ZoomOut(object sender, object e)
        {
            double newScale = 0.9;
            view.AddScale(newScale);
        }

        private double rot = 0;
        private void RotateRight(object sender, object e)
        {
            rot -= 45 * Math.PI / 360;
            view.SetBaseRotation(rot);
        }

        private void RotateLeft(object sender, object e)
        {
            rot += 45 * Math.PI / 360; ;
            view.SetBaseRotation(rot);
        }

        private async void SearchLocationButton(object sender, object e)
        {
            RoomInfo ri = null;

            try
            {
                Waiter(true);
                var tcs = new TaskCompletionSource<object>();

                InputScope ins = new InputScope();
                ins.Names.Add(new InputScopeName() { NameValue = InputScopeNameValue.TelephoneNumber });
                TextBox roomNumber = new TextBox()
                {
                    Text = "2 2102",
                    InputScope = ins,
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

#if WINDOWS_PHONE || NETFX_CORE
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
#endif

#if NETFX_CORE || WINDOWS_PHONE
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
#elif WINDOWS_PHONE
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
#endif
        private void AboutButton(object sender, object e)
        {
            MessageBoxShow("meet where?\n\nAuthor: Jason Cooke\njcooke@microsoft.com");
        }

        #region Platform-specific code

        private Task RunOnUIThread(Action agileCallback)
        {
#if WINDOWS_PHONE
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
#elif NETFX_CORE
            return Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => agileCallback()).AsTask();
#else
            var tcs = new TaskCompletionSource<object>();
            System.Threading.Thread t = new System.Threading.Thread(() =>
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    agileCallback();
                    tcs.SetResult(null);
                }))
            );
            t.Start();
            return tcs.Task;

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

        private void saveLocationInfo_Click(object sender, RoutedEventArgs e)
        {
            CloudAccesser.SaveMapMetadata(view.MapMetadata);
        }

        async private void enterLocationCoords_click(object sender, RoutedEventArgs e)
        {
            Tuple<double, double> geoloc = null;

            try
            {
                Waiter(true);
                var tcs = new TaskCompletionSource<object>();
                TextBox coordString = new TextBox()
                {
                    BorderBrush = new SolidColorBrush(Colors.Gray)
                };

                CustomMessageBox messageBox = new CustomMessageBox()
                {
                    Title = "Map center coords",
                    Message = "Enter coords, in the form '46.0344, -123.1152'",
                    Content = coordString,
                    LeftButtonContent = "OK",
                    RightButtonContent = "Cancel",
                    IsFullScreen = false,
                };

                messageBox.Dismissed += (s2, e2) =>
                {
                    if (e2.Result == CustomMessageBoxResult.LeftButton && !string.IsNullOrEmpty(coordString.Text))
                    {
                        var parts = coordString.Text.Split(',').Select(p => p.Trim()).ToArray();
                        if (parts.Length != 2) return;

                        double latitude;
                        if (!double.TryParse(parts[0], out latitude)) return;

                        double longitude;
                        if (!double.TryParse(parts[1], out longitude)) return;

                        geoloc = new Tuple<double, double>(latitude, longitude);
                    }
                };

                messageBox.Unloaded += (s2, e2) => tcs.SetResult(null);
                messageBox.Show();
                await tcs.Task;

                if (geoloc != null)
                {
                    var mmd = view.MapMetadata;
                    mmd.CenterLat = geoloc.Item1;
                    mmd.CenterLong = geoloc.Item2;
                    view.MapMetadata = mmd;
                }
            }
            finally
            {
                Waiter(false);
            }
        }

#if WINDOWS_PHONE || NETFX_CORE
#if WINDOWS_PHONE
        PhotoCaptureDevice Device = null;
        WriteableBitmap wbm = null;
#endif

        private async void ScanButton(object sender, object e)
        {
            string result = null;

            try
            {
                Waiter(true);
                var tcs = new TaskCompletionSource<object>();

                StackPanel sp = new StackPanel() { Margin = new Thickness(20) };

#if WINDOWS_PHONE

                if (Device == null)
                {
                    Windows.Foundation.Size initialResolution = new Windows.Foundation.Size(640, 480);
                    wbm = new WriteableBitmap(640, 480);
                    Device = await PhotoCaptureDevice.OpenAsync(CameraSensorLocation.Back, initialResolution);
                    Device.SetProperty(KnownCameraGeneralProperties.EncodeWithOrientation,
                                  Device.SensorLocation == CameraSensorLocation.Back ?
                                  Device.SensorRotationInDegrees : -Device.SensorRotationInDegrees);
                    Device.SetProperty(KnownCameraGeneralProperties.AutoFocusRange, AutoFocusRange.Macro);
                    Device.SetProperty(KnownCameraPhotoProperties.SceneMode, CameraSceneMode.Macro);
                    Device.SetProperty(KnownCameraPhotoProperties.FocusIlluminationMode, FocusIlluminationMode.Off);
                }

                Rectangle previewRect = new Rectangle() { Height = 480, Width = 360 };
                VideoBrush previewVideo = new VideoBrush();
                previewVideo.RelativeTransform = new CompositeTransform()
                {
                    CenterX = 0.5,
                    CenterY = 0.5,
                    Rotation = (Device.SensorLocation == CameraSensorLocation.Back ?
                        Device.SensorRotationInDegrees : -Device.SensorRotationInDegrees)
                };
                previewVideo.SetSource(Device);
                previewRect.Fill = previewVideo;

                Grid combiner = new Grid() { Height = previewRect.Height, Width = previewRect.Width };
                combiner.Children.Add(previewRect);
                Grid.SetColumnSpan(previewRect, 3);
                Grid.SetRowSpan(previewRect, 3);

                int windowSize = 100;
                combiner.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(previewRect.Height / 2 - windowSize) });
                combiner.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(windowSize) });
                combiner.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(previewRect.Height / 2 - windowSize) });
                combiner.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(previewRect.Width / 2 - windowSize) });
                combiner.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(windowSize) });
                combiner.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(previewRect.Width / 2 - windowSize) });
                var maskBrush = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));
                Canvas v1 = new Canvas() { Background = maskBrush };
                Canvas v2 = new Canvas() { Background = maskBrush };
                Canvas v3 = new Canvas() { Background = maskBrush };
                Canvas v4 = new Canvas() { Background = maskBrush };
                combiner.Children.Add(v1); Grid.SetRow(v1, 0); Grid.SetColumnSpan(v1, 3);
                combiner.Children.Add(v2); Grid.SetRow(v2, 2); Grid.SetColumnSpan(v2, 3);
                combiner.Children.Add(v3); Grid.SetRow(v3, 1); Grid.SetColumn(v3, 0);
                combiner.Children.Add(v4); Grid.SetRow(v4, 1); Grid.SetColumn(v4, 2);
                sp.Children.Add(combiner);

                BackgroundWorker bw = new BackgroundWorker();
                DateTime last = DateTime.Now;

                bw.WorkerSupportsCancellation = true;
                bw.DoWork += async (s2, e2) =>
                {
                    var reader = new BarcodeReader();
                    while (!bw.CancellationPending)
                    {
                        if (DateTime.Now.Subtract(last).TotalSeconds > 2)
                        {
                            await Device.FocusAsync();
                            last = DateTime.Now;
                        }

                        try
                        {
                            Device.GetPreviewBufferArgb(wbm.Pixels);
                            Debug.WriteLine(DateTime.Now.Millisecond);
                            var codeVal = reader.Decode(wbm);
                            if (codeVal != null)
                            {
                                result = codeVal.Text;
                                e2.Cancel = true;
                                tcs.TrySetResult(null);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }
                    }
                };

                bw.RunWorkerAsync();

#else
                var cameras = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                if (cameras.Count < 1)
                {
                    return;
                }

                var settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameras.Last().Id };
                var imageSource = new MediaCapture();
                await imageSource.InitializeAsync(settings);
                CaptureElement previewRect = new CaptureElement() { Width = 640, Height = 360 };
                previewRect.Source = imageSource;
                sp.Children.Add(previewRect);
                await imageSource.StartPreviewAsync();

                bool keepGoing = true;
                Action a = null;
                a = async () =>
                {
                    if (!keepGoing) return;
                    var stream = new InMemoryRandomAccessStream();
                    await imageSource.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);

                    stream.Seek(0);

                    var tmpBmp = new WriteableBitmap(1, 1);
                    await tmpBmp.SetSourceAsync(stream);
                    var writeableBmp = new WriteableBitmap(tmpBmp.PixelWidth, tmpBmp.PixelHeight);
                    stream.Seek(0);
                    await writeableBmp.SetSourceAsync(stream);

                    Result _result = null;

                    var barcodeReader = new BarcodeReader
                    {
                        // TryHarder = true,
                        AutoRotate = true
                    };

                    try
                    {
                        _result = barcodeReader.Decode(writeableBmp);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }

                    stream.Dispose();

                    if (_result != null)
                    {
                        result = _result.Text;
                        Debug.WriteLine(_result.Text);
                        keepGoing = false;
                        tcs.TrySetResult(null);
                    }
                    else
                    {
                        var x = RunOnUIThread(a);
                    }
                };

                await RunOnUIThread(a);
#endif

                CustomMessageBox messageBox = new CustomMessageBox()
                {
                    Title = "Scan Tag",
                    Message = "",
                    Content = sp,
                    LeftButtonContent = "OK",
                    RightButtonContent = "Cancel",
                    IsFullScreen = false,
                };

                messageBox.Unloaded += (s2, e2) =>
                {
                    tcs.TrySetResult(null);
                };
                messageBox.Show();

                await tcs.Task;

                messageBox.Dismiss();
#if WINDOWS_PHONE
                bw.CancelAsync();
#else
                keepGoing = false;
                await imageSource.StopPreviewAsync();
#endif

                Debug.WriteLine("result: '" + result + "'");
                string loc = null;
                string building = null;
                string floor = null;
                string room = null;

                if (!string.IsNullOrEmpty(result))
                {
                    var s = result.Split(new char[] { '?' }, 2);
                    if (s.Length == 2)
                    {
                        var paramList = s[1].Split('&')
                            .Select(p => p.Split(new char[] { '=' }, 2))
                            .Where(p => p.Length == 2);
                        loc = pick(paramList, "cp");
                        building = pick(paramList, "bld");
                        floor = pick(paramList, "flr");
                        room = pick(paramList, "rm");
                    }
                }

                double? lat = null;
                double? lon = null;
                if (loc != null)
                {
                    var x = loc.Split(',');
                    if (x.Length == 2)
                    {
                        double tmp;
                        if (double.TryParse(x[0], out tmp))
                        {
                            lon = tmp;
                        }

                        if (double.TryParse(x[1], out tmp))
                        {
                            lat = tmp;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(building) && !string.IsNullOrEmpty(floor))
                {
                    await ShowMap(RoomInfo.Parse(building + "/" + (room == null ? floor : room)), lat, lon);
                }
            }
            finally
            {
                Waiter(false);
            }
        }

        private static string pick(IEnumerable<string[]> paramList, string key)
        {
            return paramList.Where(p => p[0] == key).Select(p => p[1]).FirstOrDefault();
        }

#endif

#if NETFX_CORE // !WINDOWS_PHONE
        private void overlay_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            Debug.WriteLine("overlay_ManipulationStarted: " + e.ToString());
        }

        private void overlay_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            Debug.WriteLine("overlay_ManipulationCompleted: " + e.ToString());
        }

        private void overlay_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            Debug.WriteLine("overlay_ManipulationDelta: " + e.ToString());
        }

        private void overlay_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
        {
            Debug.WriteLine("overlay_ManipulationStarting: " + e.ToString());
        }

        private void overlay_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {

        }
#endif
    }
}
