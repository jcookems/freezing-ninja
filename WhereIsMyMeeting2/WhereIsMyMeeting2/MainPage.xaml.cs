using Microsoft.Devices.Sensors;
using Microsoft.Phone.Controls;
using Microsoft.Phone.UserData;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Windows.Devices.Geolocation;

namespace WhereIsMyMeeting2
{
    public partial class MainPage : PhoneApplicationPage
    {
        private Motion motion;
        private Pedometer pedometer;
        private Geolocator geolocator;
        private Geocoordinate coordinate;
        private PositionStatus status;
        private double? prevRot = null;
        private System.Windows.Point? curLocUser = null;
        private XamlMapInfo map;

        // Constructor
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
            return ContentPanel.Visibility == System.Windows.Visibility.Visible;
        }

        private void backToMainScreen()
        {
            ContentPanel.Visibility = System.Windows.Visibility.Visible;
            funky.Visibility = System.Windows.Visibility.Collapsed;
            Foo.Visibility = System.Windows.Visibility.Collapsed;
            funkyKids.Children.Clear();
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

            pedometer = new Pedometer();

            Appointments appointments = new Appointments();
            appointments.SearchCompleted += (sender, e2) => { DateList.ItemsSource = e2.Results.OrderBy(p => p.StartTime); };
            appointments.SearchAsync(DateTime.Now, DateTime.Now.AddDays(7), null);
        }

        private void ShowLoc()
        {
            Dispatcher.BeginInvoke(() =>
            {
                curLoc.Text = status.ToString() + ": " + (coordinate == null ? "N/A" : coordinate.PositionSource.ToString() + "," + coordinate.Accuracy + ":" + coordinate.Latitude + ", " + coordinate.Longitude);
                if (coordinate == null)
                {
                    CurrentLoc.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    CurrentLoc.Visibility = System.Windows.Visibility.Visible;
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

            System.Windows.Point? stride = pedometer.getStrideIfStep(reading);
            if (curLocUser.HasValue && stride.HasValue)
            {
                curLocUser = new System.Windows.Point(
                    curLocUser.Value.X + stride.Value.X,
                    curLocUser.Value.Y + stride.Value.Y);
       
                // Correct so don't walk through walls
                curLocUser = map.RebasePointToHall(curLocUser.Value);
         
                Dispatcher.BeginInvoke(() =>
                {
                    // Combine dist with direction vector. 
                    CurrentUserLoc.Visibility = System.Windows.Visibility.Visible;
                    Canvas.SetLeft(CurrentUserLoc, curLocUser.Value.X - CurrentUserLoc.Width / 2);
                    Canvas.SetTop(CurrentUserLoc, curLocUser.Value.Y - CurrentUserLoc.Height / 2);
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

            if (this.setLocation.IsChecked.HasValue && this.setLocation.IsChecked.Value)
            {
                this.setLocation.IsChecked = false;

                // Need to transforom prevMouseLoc from this corrds to funky coords.
                double x = prevMouseLoc.X;
                double y = prevMouseLoc.Y;
                CompositeTransform transform = getTransform(funky);
                curLocUser = transform.Inverse.Transform(new System.Windows.Point(x, y));

          //      System.Diagnostics.Debug.WriteLine("Orig pt: " + curLocUser);
                curLocUser = map.RebasePointToHall(curLocUser.Value);
            //    System.Diagnostics.Debug.WriteLine("Revised pt: " + curLocUser);

                CurrentUserLoc.Visibility = System.Windows.Visibility.Visible;
                Canvas.SetLeft(CurrentUserLoc, curLocUser.Value.X - CurrentUserLoc.Width / 2);
                Canvas.SetTop(CurrentUserLoc, curLocUser.Value.Y - CurrentUserLoc.Height / 2);
            }
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

        private void Foo_DoubleTap_1(object sender, GestureEventArgs e)
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

        private void roomNumber_KeyUp_1(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ShowMap(processRoomText());
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ShowMap(processRoomText());
        }

        private RoomInfo processRoomText()
        {
            return RoomInfo.Parse(roomNumber.Text, (Foo.Visibility == System.Windows.Visibility.Visible ? buildingNumber.Text : null));
        }

        private void ShowMap(RoomInfo location)
        {
            if (location == null) return;

            buildingNumber.Text = location.Building;
            floorNumber.Text = location.Floor;
            shortRoomNumber.Text = location.Room;

            ContentPanel.Visibility = System.Windows.Visibility.Collapsed;
            funky.Visibility = System.Windows.Visibility.Visible;
            Foo.Visibility = System.Windows.Visibility.Visible;
            funkyKids.Children.Clear();

            var f = this.funky;
            var fk = this.funkyKids;
            var resources = this.Resources;
            var textRotation = (CompositeTransform)this.Resources["antiRotation"];
            var dispatcher = this.Dispatcher;
            System.Threading.Thread t = new System.Threading.Thread(() =>
            {
                map = XamlMapInfo.ParseFromSvg(location);
                map.Render(f, fk, Foo, textRotation, dispatcher, location);
            });
            t.Start();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            backToMainScreen();
        }


        private void DateList_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }
            var content = (Appointment)e.AddedItems[0];
            DateList.SelectedIndex = -1;
            ShowMap(RoomInfo.Parse(content.Location, null));
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(roomNumber.Text))
            {
                this.setLocation.IsChecked = false;
                var location = processRoomText();
                if (location != null)
                {
                    System.Windows.Point loc;
                    if (map.RoomLocations.TryGetValue(location.Room, out loc))
                    {
                        curLocUser = loc;
                        CurrentUserLoc.Visibility = System.Windows.Visibility.Visible;
                        Canvas.SetLeft(CurrentUserLoc, curLocUser.Value.X - CurrentUserLoc.Width / 2);
                        Canvas.SetTop(CurrentUserLoc, curLocUser.Value.Y - CurrentUserLoc.Height / 2);
                        roomNumber.Text = "";
                    }
                    else
                    {
                        curLocUser = null;
                        CurrentUserLoc.Visibility = System.Windows.Visibility.Collapsed;
                        MessageBox.Show("unable to find room with name " + location.Room);
                    }
                }
            }
        }
    }
}
