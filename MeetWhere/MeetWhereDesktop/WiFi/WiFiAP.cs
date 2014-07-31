using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeetWhere.WiFi
{
    internal class WiFiAP
    {
        public string Name { get;  set; }

        public double LinkQuality { get;  set; }

        public string SSID { get;  set; }

        public override string ToString()
        {
            return this.Name.PadRight(15) + "\t-\t" + this.SSID + "\t|\t" + this.LinkQuality + "%";
        }
    }
}
