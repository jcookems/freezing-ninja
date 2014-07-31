using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;


namespace MeetWhere.WiFi
{
    internal class WlanClient : IDisposable
    {
        public static async Task<IEnumerable<WiFiAP>> GetWiFiAP()
        {
            IEnumerable<WiFiAP> aps = null;
            using (WlanClient wlanClient = new WlanClient())
            {
                Guid networkAdapterGuid = wlanClient.GetNetworkAdapterGuid();
                var l = await wlanClient.GetNetworkBssList(networkAdapterGuid);
                aps = l.Select(p => new WiFiAP()
                {
                        Name = System.Text.Encoding.UTF8.GetString(p.dot11Ssid.SSID, 0, (int)p.dot11Ssid.SSIDLength),
                        SSID = string.Join(":", p.dot11Bssid.Select(q => q.ToString("X2"))),
                        LinkQuality = p.linkQuality,
                } )
                    .OrderByDescending(p => p.LinkQuality);
            }

            return aps;
        }

        private IntPtr clientHandle = IntPtr.Zero;

        private WlanClient()
        {
            uint negotiatedVersion = 0;
            int error = WlanInterop.WlanOpenHandle(1, IntPtr.Zero, ref negotiatedVersion, ref this.clientHandle);
            if (error != 0)
            {
                throw new InvalidOperationException("Unable to initialise wifi client");
            }
        }

        private Guid GetNetworkAdapterGuid()
        {
            IntPtr ppInterfaceList = IntPtr.Zero;
            Guid interfaceGuid = Guid.Empty;
            try
            {
                int error = WlanInterop.WlanEnumInterfaces(this.clientHandle, IntPtr.Zero, ref ppInterfaceList);
                if (error != 0)
                {
                    throw new InvalidOperationException("Network adapter not found");
                }
                WlanInterop.WLAN_INTERFACE_INFO_LIST wLAN_INTERFACE_INFO_LIST = new WlanInterop.WLAN_INTERFACE_INFO_LIST(ppInterfaceList);
                interfaceGuid = wLAN_INTERFACE_INFO_LIST.InterfaceInfo[0].InterfaceGuid;
            }
            finally
            {
                if (ppInterfaceList != IntPtr.Zero)
                {
                    WlanInterop.WlanFreeMemory(ppInterfaceList);
                }
            }
            return interfaceGuid;
        }

        private async Task<List<WlanInterop.WlanBssEntry>> GetNetworkBssList(Guid interfaceGuid)
        {
            WlanInterop.WlanScan(this.clientHandle, interfaceGuid, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            // Ideally, wait for a callback, but there were some COM timing issues with that.
            // So just wait.
            await Task.Delay(5000);

            IntPtr wlanBssList = IntPtr.Zero;
            List<WlanInterop.WlanBssEntry> result;
            try
            {
                WlanInterop.WlanGetNetworkBssList(this.clientHandle, interfaceGuid, IntPtr.Zero, WlanInterop.Dot11BssType.Any, false, IntPtr.Zero, out wlanBssList);
                result = this.GetBssListFromPointer(wlanBssList);
            }
            finally
            {
                WlanInterop.WlanFreeMemory(wlanBssList);
            }
            return result;
        }

        private List<WlanInterop.WlanBssEntry> GetBssListFromPointer(IntPtr pBssListPtr)
        {
            var list = new List<WlanInterop.WlanBssEntry>();
            if (pBssListPtr != IntPtr.Zero)
            {
                var wlanBssListHeader = (WlanInterop.WlanBssListHeader)Marshal.PtrToStructure(pBssListPtr, typeof(WlanInterop.WlanBssListHeader));
                long num = pBssListPtr.ToInt64() + (long)System.Runtime.InteropServices.Marshal.SizeOf(typeof(WlanInterop.WlanBssListHeader));
                for (int i = 0; i < wlanBssListHeader.numberOfItems; i++)
                {
                    list.Add((WlanInterop.WlanBssEntry)Marshal.PtrToStructure(new IntPtr(num), typeof(WlanInterop.WlanBssEntry)));
                    num += Marshal.SizeOf(typeof(WlanInterop.WlanBssEntry));
                }
            }

            return list;
        }

        public void Dispose()
        {
            WlanInterop.WlanCloseHandle(this.clientHandle, IntPtr.Zero);
        }
    }
}
