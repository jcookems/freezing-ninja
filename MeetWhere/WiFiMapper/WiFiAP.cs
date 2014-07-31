using MappingBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WiFiAPMapper
{
    internal class WiFiAP
    {
        public string Name { get; set; }

        public double LinkQuality { get; set; }

        public string SSID { get; set; }

        public GeoCoord? Location { get; set; }

        public override string ToString()
        {
            return this.Name.PadRight(15) + "\t-\t" + this.SSID + "\t|\t" + this.LinkQuality + "%" +
                (Location.HasValue ? " " + Location.ToString() : "");
        }
    }
}
