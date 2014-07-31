using Microsoft.Devices.Sensors;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.UserData;
using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MeetWhere.Cloud;
using Windows.Devices.Geolocation;
using Mapper;
using Mapper.Consumer;
using System.Threading.Tasks;

namespace MeetWhere
{
    public partial class MainPage : PhoneApplicationPage
    {
        private Motion motion;
        private Geolocator geolocator;
        private Geocoordinate coordinate;
        private PositionStatus status;
        private double? prevRot = null;
        private XamlMapInfo map;

        public MainPage()
        {
            InitializeComponent();
        }

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

        private bool isOnMainScreen()
        {
            return ContentPanel.Visibility == Visibility.Visible;
        }

        private void backToMainScreen()
        {
            roomDisplay.Visibility = Visibility.Collapsed;
            ContentPanel.Visibility = Visibility.Visible;
            funky.Visibility = Visibility.Collapsed;
            Foo.Visibility = Visibility.Collapsed;
            ResetMap();
        }

        private void ResetMap()
        {
            this.roomDisplay.Text = "";
            funkyKids.Children.Clear();

            var t = getTransform(funky);
            t.CenterX = 0;
            t.CenterY = 0;
            t.ScaleX = 0.2;
            t.ScaleY = 0.2;
            t.TranslateX = 0;
            t.TranslateY = 0;

            map = null;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
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
        }

        private void ShowLoc()
        {
            Dispatcher.BeginInvoke(() =>
            {
                CurrentLoc.Visibility = (coordinate == null ? Visibility.Collapsed : Visibility.Visible);
                if (coordinate != null)
                {
                    CurrentLoc.Width = coordinate.Accuracy * 10;
                    CurrentLoc.Height = coordinate.Accuracy * 10;
                    Canvas.SetLeft(CurrentLoc, funky.ActualWidth / 2 + (coordinate.Latitude - 47.64147649) * 2000000 - CurrentLoc.Width / 2);
                    Canvas.SetTop(CurrentLoc, funky.ActualHeight / 2 + (coordinate.Longitude - (-122.1315723)) * 2000000 - CurrentLoc.Height / 2);
                }
            });
        }

        private void CurrentValueChanged(MotionReading reading)
        {
            Vector3 transformed = Vector3.Transform(new Vector3(-1, 0, 0), reading.Attitude.RotationMatrix);
            double rot = Math.Atan2(transformed.X, transformed.Y);

            if (!prevRot.HasValue || Math.Abs(rot - prevRot.Value) > 0.01)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    // Base rotation around what is currently in the center of the screen.
                    var t = getTransform(funky);
                    prevRot = t.Rotation * 2 * Math.PI / 360;
                    double deltaRot = prevRot.Value - rot;

                    // Get the location of the center in screen coords
                    double centerOffsetX = (t.TranslateX + t.CenterX) - Foo.ActualWidth / 2;
                    double centerOffsetY = (t.TranslateY + t.CenterY) - Foo.ActualHeight / 2;

                    // Compute the movement in the offset due to the rotation
                    double c = Math.Cos(deltaRot);
                    double s = Math.Sin(deltaRot);
                    double newOffsetX = centerOffsetX * c - centerOffsetY * s;
                    double newOffsetY = centerOffsetX * s + centerOffsetY * c;

                    // Shift the center so that the point in the center of  the screen didn't move
                    t.TranslateX += centerOffsetX - newOffsetX;
                    t.TranslateY += centerOffsetY - newOffsetY;

                    t.Rotation = rot * 360 / (2 * Math.PI);
                    prevRot = rot;

                    var ar = (CompositeTransform)this.Resources["antiRotation"];
                    ar.Rotation = -t.Rotation;
                });
            }
        }

        private static CompositeTransform getTransform(UIElement element)
        {
            CompositeTransform transform = element.RenderTransform as CompositeTransform;
            if (transform == null)
            {
                transform = new CompositeTransform();
                element.RenderTransform = transform;
            }
            return transform;
        }

        private void Foo_ManipulationDelta_1(object sender, ManipulationDeltaEventArgs e)
        {
            if (e.PinchManipulation == null)
            {
                return;
            }

            CompositeTransform transform = getTransform(funky);
            double newScale = e.PinchManipulation.DeltaScale;
            var newCenter = e.PinchManipulation.Current.Center;

            // Scale Manipulation
            transform.ScaleX *= newScale;
            transform.ScaleY *= newScale;

            // Also need to move the center relative to the pinch center
            transform.TranslateX = ((transform.TranslateX + transform.CenterX) * newScale + newCenter.X * (1 - newScale)) - transform.CenterX;
            transform.TranslateY = ((transform.TranslateY + transform.CenterY) * newScale + newCenter.Y * (1 - newScale)) - transform.CenterY;

            e.Handled = true;
        }

        bool mouseDown;
        StylusPoint prevMouseLoc;

        private void Foo_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            prevMouseLoc = e.StylusDevice.GetStylusPoints((UIElement)sender).Last();
            mouseDown = true;
        }

        private void Foo_MouseMove_1(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                var points = e.StylusDevice.GetStylusPoints((UIElement)sender);
                var last = points.Last();
                CompositeTransform transform = getTransform(funky);
                transform.TranslateX += last.X - prevMouseLoc.X;
                transform.TranslateY += last.Y - prevMouseLoc.Y;
                prevMouseLoc = last;
            }
        }

        private void Foo_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            mouseDown = false;
        }

        private void Foo_DoubleTap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            CompositeTransform transform = getTransform(funky);
            double newScale = 1.4;
            var newCenter = prevMouseLoc;

            // Scale Manipulation
            transform.ScaleX *= newScale;
            transform.ScaleY *= newScale;

            // Also need to move the center relative to the pinch center
            transform.TranslateX = ((transform.TranslateX + transform.CenterX) * newScale + newCenter.X * (1 - newScale)) - transform.CenterX;
            transform.TranslateY = ((transform.TranslateY + transform.CenterY) * newScale + newCenter.Y * (1 - newScale)) - transform.CenterY;

            e.Handled = true;
        }

        private async void Button_Click_1(object sender, EventArgs e)
        {
            RoomInfo ri = null;

            try
            {
                Waiter(true);
                var tcs = new TaskCompletionSource<object>();
                TextBox roomNumber = new TextBox() { Margin = new Thickness(0, 14, 12, -2) };

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
            funky.Visibility = Visibility.Visible;
            Foo.Visibility = Visibility.Visible;
            funkyKids.Children.Clear();

            var f = this.funky;
            var fk = this.funkyKids;
            var resources = this.Resources;
            var textRotation = (CompositeTransform)this.Resources["antiRotation"];
            var dispatcher = this.Dispatcher;

            var tcs = new TaskCompletionSource<object>();
            System.Threading.Thread t = new System.Threading.Thread(() =>
            {
                map = new XamlMapInfo(location, svgDocContent);
                map.Render(f, fk, Foo, textRotation, (x) => dispatcher.BeginInvoke(x), location);
                tcs.SetResult(null);
            });
            t.Start();
            return tcs.Task;
        }

        private void Button_Click_3(object sender, EventArgs e)
        {
            backToMainScreen();
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

        private void SettingsButton(object sender, EventArgs e)
        {
            CloudAccesser.ClearCachedMaps();
        }

        private void GpsButton(object sender, EventArgs e)
        {
            MessageBox.Show("Status: " + status.ToString() + "\n" +
                 (coordinate == null ?
                 "No coordinate data" :
                 "Coordinate data source: " + coordinate.PositionSource.ToString() + "\n" +
                 "Accuracy: " + coordinate.Accuracy + " meters\n" +
                 "Latitude: " + coordinate.Latitude + "\n" +
                 "Longitude: " + coordinate.Longitude));
        }

        private async void LoginButton(object sender, EventArgs e)
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
        }

        private void AboutButton(object sender, EventArgs e)
        {
            MessageBox.Show("meet where?\n\nAuthor: Jason Cooke\njcooke@microsoft.com");
        }
    }
}
