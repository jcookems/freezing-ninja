using MappingBase;
using System;
using System.Runtime.Serialization;

namespace WiFiAPMapper
{
    [DataContract(Name = "WiFiAccessPoint")]
    public class WiFiAccessPoint
    {
        [DataMember(Name = "id")]
        public int? Id { get; set; }

        [DataMember(Name = "building")]
        public int Building { get; set; }

        [DataMember(Name = "floor")]
        public int Floor { get; set; }

        [DataMember(Name = "lat")]
        public double CenterLatitude { get; set; }

        [DataMember(Name = "long")]
        public double CenterLongitude { get; set; }

        [DataMember(Name = "spread")]
        public double Spread { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "ssid")]
        public string SSID { get; set; }

        internal static double FitFunction(GeoCoord x, GeoCoord x0, double invWidth)
        {
            var ret = 100 * Math.Exp(-x.DistSqr(x0) * invWidth * invWidth);
            return ret;
        }

        internal double Strength(GeoCoord x)
        {
            var ret = FitFunction(x, new GeoCoord(this.CenterLatitude, this.CenterLongitude), 1 / Spread);
            return ret;
        }
    }
}
