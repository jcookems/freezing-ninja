using System;
using System.ComponentModel;
using System.Linq;
#if NETFX_CORE
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using FakeKeyEventArgs = Windows.UI.Xaml.Input.KeyRoutedEventArgs;
#else
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows;
using FakeKeyEventArgs = System.Windows.Input.KeyEventArgs;
#endif

namespace MeetWhere
{
#if NETFX_CORE
    public sealed partial class CustomMessageBox : UserControl
#else
    public sealed partial class CustomMessageBox : Window
#endif
    {
#if NETFX_CORE
        private Popup _dialogPopup;
        private Panel root;
#endif

        public CustomMessageBox()
        {
            this.InitializeComponent();
#if NETFX_CORE
            this.root = (Panel)((Page)((Frame)Window.Current.Content).Content).Content;
            this.LayoutRoot.Tapped += OnBackgroundTap;
#else
//            this.root = (Panel)((Page)((Frame)Application.Current.Windows[0].Content).Content).Content;
#endif
        }

#if !NETFX_CORE && !WINDOWS_PHONE
new
#endif
        public string Title { get { return this.title.Text; } set { this.title.Text = value; } }

        public string Message { get { return this.message.Text; } set { this.message.Text = value; } }

        public new UIElement Content { get { return this.userContent.Children[0]; } set { this.userContent.Children.Add(value); } }

        public string LeftButtonContent { get { return this.acceptButton.Content.ToString(); } set { this.acceptButton.Content = value; } }

        public string RightButtonContent { get { return this.cancelButton.Content.ToString(); } set { this.cancelButton.Content = value; } }

        public bool IsFullScreen { get; set; }

        private void OnGlobalKeyUp(object sender, FakeKeyEventArgs e)
        {
            if (e.Key ==
#if !NETFX_CORE
 System.Windows.Input.Key
#else
 VirtualKey
#endif
.Escape)
            {
                cancelClick(sender, e);
                e.Handled = true;
            }
        }

#if NETFX_CORE
        private void OnBackgroundTap(object sender, TappedRoutedEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                cancelClick(sender, e);
                e.Handled = true;
            }
        }
#endif

#if NETFX_CORE
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

        public void Dismiss()
        {
            if (_dialogPopup != null && _dialogPopup.IsOpen)
            {
                _dialogPopup.IsOpen = false;
                root.SizeChanged -= OnParentSizeChanged;
                root.Children.Remove(_dialogPopup);
                _dialogPopup.Child = null;
                _dialogPopup = null;
                Window.Current.Content.KeyUp -= OnGlobalKeyUp;
                this.Visibility = Visibility.Collapsed;
            }
        }
#endif

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

#if NETFX_CORE
            this.Dismiss();
#else 
            this.Close();
#endif

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
