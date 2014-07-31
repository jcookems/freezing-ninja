using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
#if WINDOWS_PHONE || !NETFX_CORE
using System.Windows.Threading;
#else
using Windows.UI.Core;
#endif
#if NETFX_CORE
using Windows.UI.Popups;
#endif

namespace MeetWhere.XPlat
{
    public static class UI
    {

        public static Task MessageBoxShow(string message)
        {
#if NETFX_CORE
            MessageDialog dlg = new Windows.UI.Popups.MessageDialog(message);
            return dlg.ShowAsync().AsTask();
#else
            MessageBox.Show(message);
            return Task.Delay(0);
#endif
        }

        public static Task MessageBoxShow(string message, string title)
        {
#if NETFX_CORE
            MessageDialog dlg = new Windows.UI.Popups.MessageDialog(message, title);
            return dlg.ShowAsync().AsTask();
#else
            MessageBox.Show(message, title, MessageBoxButton.OK);
            return Task.Delay(0);
#endif
        }

#if WINDOWS_PHONE || !NETFX_CORE
        public async static Task InvokeOnUI(Dispatcher d, Action a)
#else
        public async static Task InvokeOnUI(CoreDispatcher d, Action a)
#endif
        {
#if WINDOWS_PHONE 
            d.BeginInvoke(a);
            await Task.Delay(0);
#elif !NETFX_CORE
            await d.BeginInvoke(a);
#else
            await d.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(a));
#endif
        }
    }
}
