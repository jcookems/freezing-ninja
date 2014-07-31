using Mapper;
using System;
using System.Collections.Generic;
using System.Windows;
#if NETFX_CORE
using Windows.Foundation;
#endif

namespace Mapper
{
    class PathInfo
    {
        public PathInfo(SegmentType segmentType)
        {
            this.segmentType = segmentType;
        }

        public SegmentType segmentType;
        public Point point;
        public Point size;
        public double angle;
        public Boolean isLargeArc;
        public Boolean isSweepDirectionClockwise;

        public override string ToString()
        {
            return "segment type: " + segmentType + 
                ", point: " + point + 
                ", size: " + size + 
                ", angle: " + angle +
                ", is large arc: " + isLargeArc +
                ", is sweepdir clockwise: " + isSweepDirectionClockwise;
        }

         public static List<PathInfo> ProcessPathData(string dv)
        {
            var pp = new PathParser(dv);
            return pp.pathData;
        }

        private class PathParser
        {
            int curLoc;
            string dataValue;
            public List<PathInfo> pathData;

            public PathParser(string dv)
            {
                // Just parse the string directly.
                curLoc = 0;
                dataValue = dv;
                pathData = new List<PathInfo>();
                while (!atEnd())
                {
                    SegmentType? ft = getSegmentType();
                    if (ft.HasValue)
                    {
                        PathInfo pi = new PathInfo(ft.Value);
                        pathData.Add(pi);
                        switch (ft.Value)
                        {
                            case SegmentType.Move:
                                pi.point = getPoint();
                                break;
                            case SegmentType.Line:
                                pi.point = getPoint();
                                break;
                            case SegmentType.Close:
                                break;
                            case SegmentType.Arc:
                                pi.size = getPoint();
                                eatWhitespace();
                                pi.angle = getDouble();
                                eatWhitespace();
                                pi.isLargeArc = getBoolean();
                                eatWhitespace();
                                eatOneOptionalComma();
                                eatWhitespace();
                                pi.isSweepDirectionClockwise = getBoolean();
                                eatWhitespace();
                                pi.point = getPoint();

                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            private Point getPoint()
            {
                eatWhitespace();
                double x = getDouble();
                eatWhitespace();
                eatOneOptionalComma();
                eatWhitespace();
                double y = getDouble();
                return new Point(x, y);
            }

            private bool atEnd()
            {
                return curLoc >= dataValue.Length;
            }

            private void moveNext()
            {
                curLoc++;
            }

            private Char curChar()
            {
                return dataValue[curLoc];
            }

            private bool isDigit()
            {
                return Char.IsDigit(curChar());
            }

            private bool isLetter()
            {
                return Char.IsLetter(curChar());
            }

            private void eatWhitespace()
            {
                if (!atEnd() && Char.IsWhiteSpace(curChar()))
                {
                    moveNext();
                }
            }

            private void eatOneOptionalComma()
            {
                if (!atEnd() && curChar() == ',')
                {
                    moveNext();
                }
            }


            private SegmentType? getSegmentType()
            {
                eatWhitespace();
                if (!atEnd())
                {
                    if (isLetter())
                    {
                        SegmentType? ret = null;
                        switch (curChar())
                        {
                            case 'M':
                            case 'm':
                                ret = SegmentType.Move;
                                break;
                            case 'L':
                            case 'l':
                                ret = SegmentType.Line;
                                break;
                            case 'A':
                            case 'a':
                                ret = SegmentType.Arc;
                                break;
                            case 'Z':
                            case 'z':
                                ret = SegmentType.Close;
                                break;
                            default:
                                break;
                        }
                        if (ret.HasValue)
                        {
                            moveNext();
                            return ret;
                        }
                    }
                    else if (isDigit())
                    {
                        return SegmentType.Line;
                    }
                    throw new InvalidOperationException("Did not expect character '" + curChar() + "' in path: '" + dataValue + "'");
                }

                return null;
            }

            private double getDouble()
            {
                int start = curLoc;
                int len = 0;
                while (!atEnd() && (Char.IsDigit(curChar()) || curChar() == '.'))
                {
                    len++;
                    moveNext();
                }
                if (len == 0) throw new InvalidOperationException();

                return double.Parse(dataValue.Substring(start, len));
            }

            private bool getBoolean()
            {
                if (!atEnd() && (curChar() == '0' || curChar() == '1'))
                {
                    char c = curChar();
                    moveNext();
                    return c == '1';
                }
                throw new InvalidOperationException();
            }
        }
    }
}