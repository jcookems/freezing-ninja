using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if !NETFX_CORE
using FakePoint = System.Windows.Point;
using System.Diagnostics;
#else
using FakePoint = Windows.Foundation.Point;
#endif

namespace MeetWhere.XPlat
{
    public struct GeoCoord
    {
        public double Latitude;
        public double Longitude;
        public double Accuracy;
        private static GeoCoord empty = new GeoCoord();
        private static double PiDiv180 = Math.PI / 180;
        public static double a = 6378137.0; // Length of equator, in meters

        public static GeoCoord Empty { get { return empty; } }

        public static double LatDegreeSizeInMeters { get { return a * PiDiv180; } }

        public double LonDegreeSizeInMeters { get { return LatDegreeSizeInMeters * Math.Cos(this.Latitude * PiDiv180); } }

        public GeoCoord(double latitude, double longitude, double accuracy = 1)
        {
            this.Latitude = latitude;
            this.Longitude = longitude;
            this.Accuracy = accuracy;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GeoCoord)) return false;
            GeoCoord other = (GeoCoord)obj;
            return this.Latitude == other.Latitude && this.Longitude == other.Longitude;
        }

        public override int GetHashCode()
        {
            return this.Latitude.GetHashCode() ^ this.Longitude.GetHashCode();
        }

        public static bool operator ==(GeoCoord a, GeoCoord b)
        {
            return a.Latitude == b.Latitude && a.Longitude == b.Longitude;
        }

        public static bool operator !=(GeoCoord a, GeoCoord b)
        {
            return !(a == b);
        }

        /// <returns>Meters squared</returns>
        internal double DistSqr(GeoCoord x0)
        {
            // Determine the actual size of the geography.
            // From http://en.wikipedia.org/wiki/Latitude,
            // and assuming the earth is spherical (e=1)

            var offsetMeters = OffsetInMeters(x0);
            var ret = Math.Pow(offsetMeters.X, 2) + Math.Pow(offsetMeters.Y, 2);
            return ret;
        }

        public GeoCoord FromOffset(double x, double y)
        {
            double Longitude = this.Longitude + x / LonDegreeSizeInMeters;
            double Latitude = this.Latitude - y / LatDegreeSizeInMeters;
            var ret = new GeoCoord(Latitude, Longitude);
            return ret;
        }

        public override string ToString()
        {
            return this.Latitude + "," + this.Longitude;
        }

        public static GeoCoord? Parse(string s)
        {
            if (s != null)
            {
                var parts = s.Split(',').Select(p => p.Trim()).ToArray();
                if (parts.Length == 2)
                {
                    double latitude;
                    if (double.TryParse(parts[0], out latitude))
                    {
                        double longitude;
                        if (double.TryParse(parts[1], out longitude))
                        {
                            return new GeoCoord(latitude, longitude);
                        }
                    }
                }
            }

            return null;
        }

        public double MetersPerScreenPoint(int ZoomLevel)
        {
            double MapSize = (uint)256 << ZoomLevel; // 256 * 2^levelOfDetail
            return LonDegreeSizeInMeters * 360 / MapSize;
        }

#if WINDOWS_PHONE || NETFX_CORE
        public GeoCoord(Windows.Devices.Geolocation.Geocoordinate geocoordinate) :
            this(geocoordinate.Latitude, geocoordinate.Longitude, geocoordinate.Accuracy)
        {
        }
#endif
        public FakePoint OffsetInMeters(GeoCoord geoC)
        {
            // Determine the actual size of the geography.
            // From http://en.wikipedia.org/wiki/Latitude,
            // and assuming the earth is spherical (e=1)

            double x = -(this.Longitude - geoC.Longitude) * LonDegreeSizeInMeters;
            double y = (this.Latitude - geoC.Latitude) * LatDegreeSizeInMeters;
            var ret = new FakePoint(x, y);
            return ret;
        }

        internal double GetLonEqualInLengthToLatDelta(double latDelta)
        {
            return latDelta / (LonDegreeSizeInMeters / LatDegreeSizeInMeters);
        }

        public GeoCoord FromPixelOffset2(double x, double y, int levelOfDetail)
        {
            ulong twotoPow = 2;
            for (int i = 1; i < levelOfDetail; i++)
            {
                twotoPow *= 2;
            }

            double MapSize = (uint)256 << levelOfDetail;
            //  MapSize /= 8;
            double Longitude = x / (PiDiv180 * MapSize);
            double Latitude = y * Math.Cos(this.Latitude * PiDiv180) / (PiDiv180 * MapSize);
            var ret = new GeoCoord(Latitude, Longitude);
            return ret;
        }

        /// <returns>The ground resolution, in meters per pixel.</returns>
        public double GroundResolution(int levelOfDetail)
        {
            return LonDegreeSizeInMeters / (PiDiv180 * ((uint)256 << levelOfDetail));
        }

        // If image is 300 pixels wide, then it is 300 * groundRes meters wide. That means it is 
        // Lat Degrees = 300 * groundRes/LatDegreeSizeInMeters = 300 * LonDegreeSizeInMeters / (PiDiv180  * ((uint)256 << levelOfDetail)) / LatDegreeSizeInMeters
        // = 300  Math.Cos(this.Latitude * PiDiv180) / (PiDiv180  * ((uint)256 << levelOfDetail))
        // Lon Degrees = 300 * groundRes/LonDegreeSizeInMeters = 300 / (PiDiv180  * ((uint)256 << levelOfDetail))
        //
        // So, need funs 
        // LonDegreeDelta = deltaPixel  / (PiDiv180  * ((uint)256 << levelOfDetail))
        // LatDegreeDelta = LonDegreeDelta * Math.Cos(this.Latitude * PiDiv180)


    }
}
