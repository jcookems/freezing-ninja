using System;
using System.Linq;

namespace MappingBase
{
    public struct GeoCoord
    {
        public double Latitude;
        public double Longitude;
        public double Accuracy;
        private static GeoCoord empty = new GeoCoord();
        private static double PiDiv180 = Math.PI / 180;
        private static double a = 6378137.0; // Length of equator, in meters

        public static GeoCoord Empty { get { return empty; } }

        public static double LatDegreeSizeInMeters { get { return a * PiDiv180; } }

        public double LonDegreeSizeInMeters { get { return LatDegreeSizeInMeters * Math.Cos(this.Latitude * PiDiv180); } }

        public GeoCoord(double latitude, double longitude, double accuracy = 1)
        {
            this.Latitude = latitude;
            this.Longitude = longitude;
            this.Accuracy = accuracy;
        }

        /// <returns>Meters squared</returns>
        public double DistSqr(GeoCoord x0)
        {
            var offsetMeters = OffsetInMeters(x0);
            return offsetMeters.Square();
        }

        public Point OffsetInMeters(GeoCoord geoC)
        {
            // Determine the actual size of the geography.
            // From http://en.wikipedia.org/wiki/Latitude,
            // and assuming the earth is spherical (e=1)

            double x = -(this.Longitude - geoC.Longitude) * LonDegreeSizeInMeters;
            double y = (this.Latitude - geoC.Latitude) * LatDegreeSizeInMeters;
            var ret = new Point(x, y);
            return ret;
        }

        public GeoCoord FromOffset(double x, double y)
        {
            double Longitude = this.Longitude + x / LonDegreeSizeInMeters;
            double Latitude = this.Latitude - y / LatDegreeSizeInMeters;
            var ret = new GeoCoord(Latitude, Longitude);
            return ret;
        }

        //public double MetersPerScreenPoint(int ZoomLevel)
        //{
        //    double MapSize = (uint)256 << ZoomLevel; // 256 * 2^levelOfDetail
        //    return LonDegreeSizeInMeters * 360 / MapSize;
        //}

        //private double GetLonEqualInLengthToLatDelta(double latDelta)
        //{
        //    return latDelta / (LonDegreeSizeInMeters / LatDegreeSizeInMeters);
        //}

        //private GeoCoord FromPixelOffset2(double x, double y, int levelOfDetail)
        //{
        //    ulong twotoPow = 2;
        //    for (int i = 1; i < levelOfDetail; i++)
        //    {
        //        twotoPow *= 2;
        //    }

        //    double MapSize = (uint)256 << levelOfDetail;
        //    double Longitude = x / (PiDiv180 * MapSize);
        //    double Latitude = y * Math.Cos(this.Latitude * PiDiv180) / (PiDiv180 * MapSize);
        //    var ret = new GeoCoord(Latitude, Longitude);
        //    return ret;
        //}

        ///// <returns>The ground resolution, in meters per pixel.</returns>
        //private double GroundResolution(int levelOfDetail)
        //{
        //    return LonDegreeSizeInMeters / (PiDiv180 * ((uint)256 << levelOfDetail));
        //}

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


        public override string ToString()
        {
            return this.Latitude + "," + this.Longitude;
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

        public struct Point
        {
            public double X;
            public double Y;
            public Point(double x, double y)
            {
                this.X = x;
                this.Y = y;
            }
            public double Square()
            {
                return X * X + Y * Y;
            }
        }
    }
}
