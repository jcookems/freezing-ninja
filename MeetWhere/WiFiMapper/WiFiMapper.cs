using MappingBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace WiFiAPMapper
{
    public static class WiFiMapper
    {

        private static List<WiFiAP> apLocs = new List<WiFiAP>();

        public static async void Update(double latitude, double longitude)
        {
            try
            {
                var visibileAPs = await WlanClient.GetWiFiAP();
                foreach (var y in visibileAPs)
                {
                    y.Location = new GeoCoord(latitude, longitude);
                    apLocs.Add(y);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public static async Task<GeoCoord?> GetGeoFromWiFi(IEnumerable<WiFiAccessPoint> fits)
        {
            IEnumerable<WiFiAP> visibileAPs = null;
            try
            {
                visibileAPs = await WlanClient.GetWiFiAP();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            if (visibileAPs == null)
            {
                return null;
            }
            else
            {
                return Solver.Minimizer(
                    (x) => visibileAPs.Sum(ap =>
                    {
                        var y = fits.Where(p => p.SSID == ap.SSID)
                            .Select(p => Math.Pow(ap.LinkQuality - p.Strength(x), 2))
                            .FirstOrDefault();
                        return y;
                    }),
                    new GeoCoord(47.6403392227826, -122.125985450779));
            }
        }

        public static IEnumerable<WiFiAccessPoint> ProcessCollectedData(int building, int floor)
        {
            var apSpecific = apLocs.GroupBy(p => p.SSID);
            var locations = apLocs.GroupBy(p => p.Location);

            var fits = new List<WiFiAccessPoint>();
            foreach (var apData in apSpecific)
            {
                string ssid = apData.Key;
                double maxStrength = apData.Max(p => p.LinkQuality);
                if (maxStrength > 60 && apData.Count() > 2)
                {
                    // Strong enough to be useful, maybe.
                    GeoCoord maxPos = apData.First(p => p.LinkQuality == maxStrength).Location.Value;

                    Dictionary<GeoCoord, double> k = new Dictionary<GeoCoord, double>();
                    foreach (var y in locations)
                    {
                        var yy = apData.SingleOrDefault(p => p.Location == y.Key);
                        k.Add(y.Key.Value, (yy == null ? 0 : yy.LinkQuality));
                    }

                    var fit = Solver.LeastSquares(WiFiAccessPoint.FitFunction, k, new Tuple<GeoCoord, double>(maxPos, 0.1));

                    var xx = apData.First();
                    fits.Add(new WiFiAccessPoint()
                    {
                        Name = xx.Name,
                        SSID = xx.SSID,
                        CenterLatitude = fit.Item1.Latitude,
                        CenterLongitude = fit.Item1.Longitude,
                        Spread = fit.Item2,
                        Building = building,
                        Floor = floor
                    });
                }
            }

            return fits;
        }

        private class Solver
        {
            public static Tuple<GeoCoord, double> LeastSquares(
                Func<GeoCoord, GeoCoord, double, double> f,
                Dictionary<GeoCoord, double> measurements,
                Tuple<GeoCoord, double> guess)
            {
                var ret = Minimizer(
                    p => measurements.Sum(q => Math.Pow(q.Value - f(q.Key, new GeoCoord(p[0], p[1]), p[2]), 2)),
                    new double[] { guess.Item1.Latitude, guess.Item1.Longitude, guess.Item2 });
                return new Tuple<GeoCoord, double>(new GeoCoord(ret[0], ret[1]), ret[2]);
            }

            public static GeoCoord Minimizer(Func<GeoCoord, double> f, GeoCoord guess)
            {
                var ret = Minimizer((x) => f(new GeoCoord(x[0], x[1])), new double[] { guess.Latitude, guess.Longitude }, new double[] { 0.1, 0.1 });
                return new GeoCoord(ret[0], ret[1]);
            }

            private static double[] Minimizer(Func<double[], double> f, double[] guess, double[] range = null)
            {
                double[] curGuess = guess;
                double curMinY = f(curGuess);
                var curMinGuess = curGuess;

                var delta = (range != null ? range.ToArray() : curGuess.Select(p => p / 6).ToArray());
                for (var iter = 1; iter < 10; iter++)
                {
                    for (var itemNum = guess.Length - 1; itemNum >= 0; itemNum--)
                    {
                        for (var i = -5; i <= 5; i++)
                        {
                            var newGuess = curGuess.Select((p, j) => p + delta[j] * (itemNum == j ? i : 0)).ToArray();
                            double y = f(newGuess);
                            if (y < curMinY)
                            {
                                curMinY = y;
                                curMinGuess = newGuess;
                            }

                            //    if (newGuess.Length == 2)
                            //     {
                            //     Debug.WriteLine(iter + "," + itemNum + ", (" + string.Join(",", newGuess.Select(p => p.ToString())) + ") = " + y);
                            //     }

                        }

                        //if (curMinGuess.Length == 2)
                        //{
                        // Debug.WriteLine(iter + "," + itemNum + ", (" + string.Join(",", curMinGuess.Select(p => p.ToString())) + ")");
                        //}
                        curGuess = curMinGuess;
                    }

                    delta = delta.Select(p => p / 10).ToArray();
                }

                return curGuess;
            }
        }

        public static IEnumerable<WiFiAccessPoint> SetDefaults()
        {
            if (apLocs.Count == 0)
            {
                var foo = new List<Tuple<GeoCoord.Point, string, double>>();
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64043184, -122.1259273), "6C:F3:7F:55:9F:30", 25));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:55:9F:30", 30));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64043184, -122.1259273), "6C:F3:7F:55:9F:32", 23));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:55:9F:32", 30));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050076, -122.126209), "6C:F3:7F:55:A1:C0", 41));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:A1:C0", 63));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:A1:C2", 60));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405452, -122.126272), "6C:F3:7F:55:A1:C2", 60));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405452, -122.126272), "6C:F3:7F:55:A1:D0", 30));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405452, -122.126272), "6C:F3:7F:55:A1:D2", 30));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405452, -122.126272), "6C:F3:7F:55:A5:92", 26));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:55:C0:A0", 31));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:C0:A0", 36));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050625, -122.1260392), "6C:F3:7F:55:C0:A0", 38));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405452, -122.126272), "6C:F3:7F:55:C0:A0", 45));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:C0:A0", 46));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:C0:A2", 36));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050625, -122.1260392), "6C:F3:7F:55:C0:A2", 41));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405452, -122.126272), "6C:F3:7F:55:C0:A2", 43));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:C0:A2", 46));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050076, -122.126209), "6C:F3:7F:55:C0:A2", 51));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:C0:B0", 33));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64043184, -122.1259273), "6C:F3:7F:55:C0:B2", 25));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:C0:B2", 31));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:C0:B2", 50));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64061113, -122.1261904), "6C:F3:7F:55:C9:90", 38));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64061113, -122.1261904), "6C:F3:7F:55:C9:92", 38));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64061113, -122.1261904), "6C:F3:7F:55:CA:00", 46));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64061113, -122.1261904), "6C:F3:7F:55:CA:02", 45));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:CE:60", 41));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:CE:60", 55));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:CE:62", 35));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:CE:62", 41));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:CE:62", 55));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405452, -122.126272), "6C:F3:7F:55:CE:70", 55));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:CE:70", 56));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050625, -122.1260392), "6C:F3:7F:55:CE:70", 60));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:55:CE:70", 61));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050076, -122.126209), "6C:F3:7F:55:CE:70", 66));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:CE:70", 73));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:CE:70", 73));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:CE:72", 58));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050625, -122.1260392), "6C:F3:7F:55:CE:72", 60));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:55:CE:72", 61));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:CE:72", 78));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:CE:72", 85));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:CE:72", 85));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405452, -122.126272), "6C:F3:7F:55:CE:72", 85));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050076, -122.126209), "6C:F3:7F:55:CE:72", 91));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64043184, -122.1259273), "6C:F3:7F:55:CE:72", 99));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:CF:E0", 43));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:CF:E0", 46));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64043184, -122.1259273), "6C:F3:7F:55:CF:E0", 56));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:CF:E2", 23));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:CF:E2", 43));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:CF:E2", 50));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64043184, -122.1259273), "6C:F3:7F:55:CF:E2", 56));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:CF:F0", 20));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:CF:F0", 25));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:CF:F0", 26));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:CF:F0", 28));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:CF:F2", 20));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:CF:F2", 25));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:CF:F2", 26));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:CF:F2", 28));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050076, -122.126209), "6C:F3:7F:55:D0:02", 30));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050625, -122.1260392), "6C:F3:7F:55:D0:20", 36));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:55:D0:22", 38));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:D0:22", 45));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050625, -122.1260392), "6C:F3:7F:55:D0:30", 28));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:D0:30", 55));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050625, -122.1260392), "6C:F3:7F:55:D0:32", 28));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:D0:32", 55));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:D1:70", 23));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:D1:72", 23));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:D2:20", 36));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64043184, -122.1259273), "6C:F3:7F:55:D2:20", 41));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:D2:20", 53));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:D2:20", 70));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:D2:22", 36));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64043184, -122.1259273), "6C:F3:7F:55:D2:22", 41));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:D2:22", 60));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:D2:22", 68));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:D2:30", 23));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64043184, -122.1259273), "6C:F3:7F:55:D2:30", 26));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:D2:30", 38));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:D2:32", 26));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64043184, -122.1259273), "6C:F3:7F:55:D2:32", 28));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:D3:40", 30));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:D3:41", 30));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050076, -122.126209), "6C:F3:7F:55:D3:E2", 48));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:D3:F0", 25));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:55:D3:F0", 31));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:D3:F0", 40));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050076, -122.126209), "6C:F3:7F:55:D3:F0", 48));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405452, -122.126272), "6C:F3:7F:55:D3:F0", 58));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:D3:F0", 66));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64061113, -122.1261904), "6C:F3:7F:55:D3:F0", 73));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:D3:F2", 25));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:55:D3:F2", 30));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:D3:F2", 40));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050076, -122.126209), "6C:F3:7F:55:D3:F2", 48));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405452, -122.126272), "6C:F3:7F:55:D3:F2", 58));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:D3:F2", 66));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64061113, -122.1261904), "6C:F3:7F:55:D3:F2", 73));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64061113, -122.1261904), "6C:F3:7F:55:D6:20", 43));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050625, -122.1260392), "6C:F3:7F:55:D6:20", 55));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:D6:20", 56));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:D6:20", 58));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:D6:20", 61));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64061113, -122.1261904), "6C:F3:7F:55:D6:22", 43));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:D6:22", 56));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:D6:22", 58));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:D6:22", 61));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:D6:30", 48));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:D6:30", 50));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:D6:30", 51));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:D6:30", 51));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050625, -122.1260392), "6C:F3:7F:55:D6:30", 55));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:55:D6:30", 71));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:D6:30", 76));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64043184, -122.1259273), "6C:F3:7F:55:D6:30", 99));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:D6:32", 48));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:D6:32", 50));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:D6:32", 51));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:D6:32", 51));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050625, -122.1260392), "6C:F3:7F:55:D6:32", 56));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:55:D6:32", 70));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:D6:32", 76));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64043184, -122.1259273), "6C:F3:7F:55:D6:32", 99));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:D7:50", 28));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64043184, -122.1259273), "6C:F3:7F:55:D7:50", 31));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405452, -122.126272), "6C:F3:7F:55:D7:50", 40));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050076, -122.126209), "6C:F3:7F:55:D7:50", 60));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050625, -122.1260392), "6C:F3:7F:55:D7:50", 70));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:D7:52", 30));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64043184, -122.1259273), "6C:F3:7F:55:D7:52", 31));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:D7:52", 56));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050076, -122.126209), "6C:F3:7F:55:D7:52", 60));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050625, -122.1260392), "6C:F3:7F:55:D7:52", 70));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64043184, -122.1259273), "6C:F3:7F:55:D8:20", 26));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:55:D8:20", 50));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64061113, -122.1261904), "6C:F3:7F:55:D8:20", 90));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:D8:20", 93));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:55:D8:22", 38));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64061113, -122.1261904), "6C:F3:7F:55:D8:22", 90));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:D8:22", 93));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405452, -122.126272), "6C:F3:7F:55:D8:30", 41));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050076, -122.126209), "6C:F3:7F:55:D8:30", 50));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64061113, -122.1261904), "6C:F3:7F:55:D8:30", 83));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:D8:30", 95));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:D8:32", 25));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405452, -122.126272), "6C:F3:7F:55:D8:32", 41));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050076, -122.126209), "6C:F3:7F:55:D8:32", 51));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:55:D8:32", 60));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:D8:32", 95));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64061113, -122.1261904), "6C:F3:7F:55:D8:32", 99));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050625, -122.1260392), "6C:F3:7F:55:D8:32", 99));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:D8:80", 31));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050076, -122.126209), "6C:F3:7F:55:D8:80", 43));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64061113, -122.1261904), "6C:F3:7F:55:D8:82", 28));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050076, -122.126209), "6C:F3:7F:55:D8:82", 41));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:55:D8:82", 48));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:55:D8:90", 30));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64043184, -122.1259273), "6C:F3:7F:55:D8:90", 30));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405452, -122.126272), "6C:F3:7F:55:D8:90", 38));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050625, -122.1260392), "6C:F3:7F:55:D8:90", 38));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:D8:90", 48));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:D8:90", 55));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:D8:90", 58));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:D8:90", 80));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:55:D8:92", 30));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64043184, -122.1259273), "6C:F3:7F:55:D8:92", 31));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405452, -122.126272), "6C:F3:7F:55:D8:92", 38));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050625, -122.1260392), "6C:F3:7F:55:D8:92", 38));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:D8:92", 46));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:D8:92", 55));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:D8:92", 58));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:D8:92", 80));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:D8:E0", 41));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:55:D8:E2", 23));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:D8:E2", 26));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050076, -122.126209), "6C:F3:7F:55:D8:E2", 33));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:D8:E2", 43));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:D8:F0", 30));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:D8:F0", 58));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64042235, -122.1260956), "6C:F3:7F:55:D8:F2", 30));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050076, -122.126209), "6C:F3:7F:55:D8:F2", 51));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64045731, -122.12616), "6C:F3:7F:55:D8:F2", 56));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:DA:30", 25));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64061113, -122.1261904), "6C:F3:7F:55:DA:30", 31));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050625, -122.1260392), "6C:F3:7F:55:DA:30", 36));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:55:DA:30", 41));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:DA:30", 55));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:DA:32", 23));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64061113, -122.1261904), "6C:F3:7F:55:DA:32", 33));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64050625, -122.1260392), "6C:F3:7F:55:DA:32", 36));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:55:DA:32", 41));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:DA:32", 55));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:DA:80", 35));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:DA:81", 35));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64061113, -122.1261904), "6C:F3:7F:55:DB:C0", 50));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64061113, -122.1261904), "6C:F3:7F:55:DB:C2", 48));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:DB:D0", 48));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.6405502, -122.1261), "6C:F3:7F:55:DB:D2", 46));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:E1:D0", 35));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:E1:D1", 35));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:E3:00", 35));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:E3:00", 45));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64036741, -122.126017), "6C:F3:7F:55:E3:01", 31));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:E3:01", 33));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64037191, -122.1259266), "6C:F3:7F:55:E3:60", 33));
                foo.Add(new Tuple<GeoCoord.Point, string, double>(new GeoCoord.Point(47.64047578, -122.1259844), "6C:F3:7F:6A:59:E1", 25));

                foreach (var w in foo)
                {
                    apLocs.Add(new WiFiAP()
                    {
                        SSID = w.Item2,
                        LinkQuality = w.Item3,
                        Location = new GeoCoord(w.Item1.X, w.Item1.Y),
                    });
                }
            }

            var ret = apLocs.Select(p => new WiFiAccessPoint()
            {
                CenterLatitude = p.Location.Value.Latitude,
                CenterLongitude = p.Location.Value.Longitude,
                Spread = 0.02
            });

            return ret;
        }
    }
}
