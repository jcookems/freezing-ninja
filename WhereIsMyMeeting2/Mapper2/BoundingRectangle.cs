using Mapper;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Mapper2;

namespace WhereIsMyMeeting2
{
    class BoundingRectangle
    {
        private double? minX = null;
        private double? maxX = null;
        private double? minY = null;
        private double? maxY = null;

        public BoundingRectangle() { }

        public static BoundingRectangle GetBoundsFromPath(List<PathInfo> list)
        {
            // Only support simple paths
            if (list.First().segmentType != SegmentType.Move)
            {
                return null;
            }
            if (list.Last().segmentType != SegmentType.Close)
            {
                return null;
            }
            if (list.Skip(1).Take(list.Count - 2).Any(p => p.segmentType != SegmentType.Line))
            {
                return null;
            }

            BoundingRectangle ret = new BoundingRectangle();
            foreach (var i in list.Take(list.Count - 1))
            {
                ret.UpdateBounds(i.point);
            }

            return ret;
        }

        public double Width { get { return (minX.HasValue && maxX.HasValue ? maxX.Value - minX.Value : 0); } }
        public double Height { get { return (minY.HasValue && maxY.HasValue ? maxY.Value - minY.Value : 0); } }
        public double CenterX { get { return (minX.HasValue && maxX.HasValue ? (maxX.Value + minX.Value) / 2 : 0); } }
        public double CenterY { get { return (minY.HasValue && maxY.HasValue ? (maxY.Value + minY.Value) / 2 : 0); } }

        internal bool ContainsPoint(Point testPoint)
        {
            return minX.HasValue &&
                (minX.Value <= testPoint.X) && (testPoint.X <= maxX.Value) &&
                (minY.Value <= testPoint.Y) && (testPoint.Y <= maxY.Value);
        }

        public void UpdateBounds(Point p)
        {
            UpdateBounds(p.X, p.Y);
        }

        public void UpdateBounds(double x, double y)
        {
            if (minX == null || minX.Value > x) minX = x;
            if (maxX == null || maxX.Value < x) maxX = x;
            if (minY == null || minY.Value > y) minY = y;
            if (maxY == null || maxY.Value < y) maxY = y;
        }
    }
}
