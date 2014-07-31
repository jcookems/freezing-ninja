#pragma warning disable 0649

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WiFiAPMapper
{
    internal class WlanInterop
    {
        public struct WLAN_INTERFACE_INFO_LIST
        {
            public int dwNumberofItems;
            public int dwIndex;
            public WlanInterop.WLAN_INTERFACE_INFO[] InterfaceInfo;
            public WLAN_INTERFACE_INFO_LIST(IntPtr pList)
            {
                this.dwNumberofItems = Marshal.ReadInt32(pList, 0);
                this.dwIndex = Marshal.ReadInt32(pList, 4);
                this.InterfaceInfo = new WlanInterop.WLAN_INTERFACE_INFO[this.dwNumberofItems];
                for (int i = 0; i < this.dwNumberofItems; i++)
                {
                    IntPtr ptr = new IntPtr(pList.ToInt32() + i * 532 + 8);
                    WlanInterop.WLAN_INTERFACE_INFO wLAN_INTERFACE_INFO = default(WlanInterop.WLAN_INTERFACE_INFO);
                    wLAN_INTERFACE_INFO = (WlanInterop.WLAN_INTERFACE_INFO)Marshal.PtrToStructure(ptr, typeof(WlanInterop.WLAN_INTERFACE_INFO));
                    this.InterfaceInfo[i] = wLAN_INTERFACE_INFO;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WLAN_INTERFACE_INFO
        {
            public Guid InterfaceGuid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strInterfaceDescription;
            public WlanInterop.WLAN_INTERFACE_STATE isState;
        }

        public enum WLAN_INTERFACE_STATE
        {
            wlan_interface_state_not_ready,
            wlan_interface_state_connected,
            wlan_interface_state_ad_hoc_network_formed,
            wlan_interface_state_disconnecting,
            wlan_interface_state_disconnected,
            wlan_interface_state_associating,
            wlan_interface_state_discovering,
            wlan_interface_state_authenticating
        }

        public enum Dot11BssType
        {
            Any = 3,
            Independent = 2,
            Infrastructure = 1
        }

        public enum Dot11PhyType
        {
            Any,
            DSSS = 2,
            ERP = 6,
            FHSS = 1,
            HRDSSS = 5,
            IHV_End = -1,
            IHV_Start = -2147483648,
            IrBaseband = 3,
            OFDM,
            Unknown = 0
        }

        public struct Dot11Ssid
        {
            public uint SSIDLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] SSID;
        }

        public struct WlanBssEntry
        {
            public WlanInterop.Dot11Ssid dot11Ssid;
            public uint phyId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] dot11Bssid;
            public WlanInterop.Dot11BssType dot11BssType;
            public WlanInterop.Dot11PhyType dot11BssPhyType;
            public int rssi;
            public uint linkQuality;
            public bool inRegDomain;
            public ushort beaconPeriod;
            public ulong timestamp;
            public ulong hostTimestamp;
            public ushort capabilityInformation;
            public uint chCenterFrequency;
            public WlanInterop.WlanRateSet wlanRateSet;
            public uint ieOffset;
            public uint ieSize;
        }

        internal struct WlanBssListHeader
        {
            internal uint totalSize;
            internal uint numberOfItems;
        }

        public struct WlanRateSet
        {
            private uint rateSetLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 126)]
            private ushort[] rateSet;
            public ushort[] Rates
            {
                get
                {
                    ushort[] array = new ushort[(ulong)this.rateSetLength / (ulong)((long)Marshal.SizeOf(Type.GetType("ushort")))];
                    Array.Copy(this.rateSet, array, array.Length);
                    return array;
                }
            }

            public double GetRateInMbps(int rateIndex)
            {
                if (rateIndex < 0 || rateIndex > this.rateSet.Length)
                {
                    throw new System.ArgumentOutOfRangeException("rateIndex");
                }

                return (double)(this.rateSet[rateIndex] & 32767) * 0.5;
            }
        }

        [DllImport("wlanapi.dll")]
        public static extern int WlanCloseHandle(
            [In] [Out] IntPtr clientHandle,
            [In] [Out] IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        public static extern int WlanEnumInterfaces(
            [In] [Out] IntPtr clientHandle,
            [In] [Out] IntPtr pReserved,
            [In] [Out] ref IntPtr ppInterfaceList);

        [DllImport("wlanapi.dll")]
        public static extern void WlanFreeMemory(
            IntPtr pMemory);

        [DllImport("wlanapi.dll")]
        public static extern int WlanGetNetworkBssList(
            [In] IntPtr clientHandle,
            [MarshalAs(UnmanagedType.LPStruct)] [In] Guid interfaceGuid,
            [In] IntPtr dot11SsidInt,
            [In] WlanInterop.Dot11BssType dot11BssType,
            [In] bool securityEnabled,
            IntPtr reservedPtr,
            out IntPtr wlanBssList);

        [DllImport("wlanapi.dll")]
        public static extern int WlanOpenHandle(
            [In] [Out] uint clientVersion,
            [In] [Out] IntPtr pReserved,
            [In] [Out] ref uint negotiatedVersion,
            [In] [Out] ref IntPtr clientHandle);

        [DllImport("wlanapi.dll")]
        public static extern int WlanScan(
            [In] IntPtr clientHandle,
            [MarshalAs(UnmanagedType.LPStruct)] [In] Guid interfaceGuid,
            IntPtr pDot11Ssid,
            IntPtr pIeData,
            [In] IntPtr pReserved);

    }
}
