using System;
using System.ComponentModel;
using System.Linq;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace MeetWhere
{
    public sealed partial class CustomMessageBox : UserControl
    {
        private Popup _dialogPopup;
        private Panel root;

        public CustomMessageBox()
        {
            this.InitializeComponent();
            this.root = (Panel)((Page)((Frame)Window.Current.Content).Content).Content;
            this.LayoutRoot.Tapped += OnBackgroundTap;
        }

        public string Title { get { return this.title.Text; } set { this.title.Text = value; } }

        public string Message { get { return this.message.Text; } set { this.message.Text = value; } }

        public new UIElement Content { get { return this.userContent.Children.FirstOrDefault(); } set { this.userContent.Children.Add(value); } }

        public string LeftButtonContent { get { return this.acceptButton.Content.ToString(); } set { this.acceptButton.Content = value; } }

        public string RightButtonContent { get { return this.cancelButton.Content.ToString(); } set { this.cancelButton.Content = value; } }

        public bool IsFullScreen { get; set; }

        private void OnGlobalKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                cancelClick(sender, e);
                e.Handled = true;
            }
        }

        private void OnBackgroundTap(object sender, TappedRoutedEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                cancelClick(sender, e);
                e.Handled = true;
            }
        }

        private void ResizeLayoutRoot()
        {
            this.Width = root.ActualWidth;
            this.Height = root.ActualHeight;
        }

        private void OnParentSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            ResizeLayoutRoot();
        }

        public void Show()
        {
            this.Visibility = Visibility.Visible;
            Window.Current.Content.KeyUp += OnGlobalKeyUp;
            _dialogPopup = new Popup { Child = this };
            root.Children.Add(_dialogPopup);
            root.SizeChanged += OnParentSizeChanged;
            _dialogPopup.IsOpen = true;
            ResizeLayoutRoot();
        }

        private void Close()
        {
            _dialogPopup.IsOpen = false;
            root.SizeChanged -= OnParentSizeChanged;
            root.Children.Remove(_dialogPopup);
            _dialogPopup.Child = null;
            _dialogPopup = null;
            Window.Current.Content.KeyUp -= OnGlobalKeyUp;
            this.Visibility = Visibility.Collapsed;
        }

        private void acceptClick(object sender, RoutedEventArgs e)
        {
            ProcessButtonClick(CustomMessageBoxResult.LeftButton);
        }

        private void cancelClick(object sender, RoutedEventArgs e)
        {
            ProcessButtonClick(CustomMessageBoxResult.RightButton);
        }

        private void ProcessButtonClick(CustomMessageBoxResult c)
        {
            if (this.Dismissing != null)
            {
                var dea = new DismissingEventArgs(c);
                this.Dismissing(this, dea);
                if (dea.Cancel == true)
                {
                    return;
                }
            }

            Close();

            if (this.Dismissed != null)
            {
                this.Dismissed(this, new DismissedEventArgs(c));
            }
        }

        public event EventHandler<DismissingEventArgs> Dismissing;

        public event EventHandler<DismissedEventArgs> Dismissed;
    }

    public class DismissedEventArgs : EventArgs
    {
        public DismissedEventArgs(CustomMessageBoxResult result)
        {
            this.Result = result;
        }

        public CustomMessageBoxResult Result { get; private set; }
    }

    public class DismissingEventArgs : CancelEventArgs
    {
        public DismissingEventArgs(CustomMessageBoxResult result)
        {
            this.Result = result;
        }

        public CustomMessageBoxResult Result { get; private set; }
    }

    public enum CustomMessageBoxResult
    {
        LeftButton = 0,
        RightButton = 1,
        None = 2,
    }
}
