using System;
using System.Collections.Generic;
using System.Linq;

namespace ConvertUnitToExtTest
{
    public class Diff
    {
        private const int fuzzDiff = 4;

        private string[] left;
        private string[] right;
        private List<MatchBlock> matches;

        public static string ShowDiff(IEnumerable<string> leftLines, IEnumerable<string> rightLines)
        {
            Diff d = new Diff(leftLines, rightLines);
            return d.ToString();
        }

        private Diff(IEnumerable<string> leftLines, IEnumerable<string> rightLines)
        {
            this.left = leftLines.ToArray();
            this.right = rightLines.ToArray();
            this.matches = InOrderFinder();
        }

        private List<MatchBlock> InOrderFinder()
        {
            int[][] lens = GetDistanceMatrix();

            List<Tuple<int, int, int>> matches = ProcessToRawMatches(lens);

            int leftPos = 0;
            int rightPos = 0;
            List<MatchBlock> ret = new List<MatchBlock>();

            foreach (var match in matches)
            {
                if (leftPos < match.Item1)
                {
                    ret.Add(new MatchBlock(
                        this.left.Skip(leftPos).Take(match.Item1 - leftPos).ToArray(),
                        new string[0]));
                    leftPos = match.Item1;
                }

                if (rightPos < match.Item2)
                {
                    ret.Add(new MatchBlock(
                        new string[0],
                        this.right.Skip(rightPos).Take(match.Item2 - rightPos).ToArray()));
                    rightPos = match.Item2;
                }

                if (leftPos == this.left.Length && rightPos == this.right.Length)
                {
                    continue;
                }

                bool prevExact = left[leftPos] == this.right[rightPos];
                int counter = 0;
                for (int k = 0; k < match.Item3; k++)
                {
                    bool exact = this.left[leftPos + counter] == this.right[rightPos + counter];
                    if (exact != prevExact)
                    {
                        ret.Add(new MatchBlock(
                            this.left.Skip(leftPos).Take(counter).ToArray(),
                            this.right.Skip(rightPos).Take(counter).ToArray()));
                        leftPos += counter;
                        rightPos += counter;
                        counter = 0;
                    }
                    counter++;
                    prevExact = exact;
                }

                ret.Add(new MatchBlock(
                    this.left.Skip(leftPos).Take(counter).ToArray(),
                    this.right.Skip(rightPos).Take(counter).ToArray()));
                leftPos += counter;
                rightPos += counter;
            }

            return ret;
        }

        private int[][] GetDistanceMatrix()
        {
            int[][] dists = new int[this.left.Length][];
            for (int i = 0; i < dists.Length; i++)
            {
                dists[i] = new int[this.right.Length];
                for (int j = 0; j < dists[i].Length; j++)
                {
                    dists[i][j] = (j < this.right.Length ? (this.left[i] == this.right[j] ? 0 : 10)
                        : int.MaxValue);
                }
            }

            // Find longest, best match.
            int[][] lens = new int[dists.Length][];
            for (int i = 0; i < lens.Length; i++)
            {
                lens[i] = new int[dists[i].Length];
                for (int j = 0; j < lens[i].Length; j++)
                {
                    int k = 0;
                    while ((i + k) < dists.Length && (j + k) < dists[i + k].Length)
                    {
                        if (dists[i + k][j + k] > fuzzDiff) break;
                        k++;
                    }

                    lens[i][j] = k;
                }
            }

            return lens;
        }

        private static List<Tuple<int, int, int>> ProcessToRawMatches(int[][] lens)
        {
            List<Tuple<int, int, int>> matches = new List<Tuple<int, int, int>>();
            List<Tuple<int, int, int, int>> ranges = new List<Tuple<int, int, int, int>>();

            ranges.Add(new Tuple<int, int, int, int>(0, lens.Length, 0, lens[0].Length));
            while (true)
            {
                Tuple<int, int, int> best = new Tuple<int, int, int>(0, 0, 0);
                foreach (var range in ranges)
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        for (int j = range.Item3; j < range.Item4; j++)
                        {
                            // Need to clamp the lengths here to the available range.
                            int startLen = lens[i][j];
                            int len = Math.Min(startLen, Math.Min(range.Item2 - i, range.Item4 - j));
                            if (len > best.Item3)
                            {
                                best = new Tuple<int, int, int>(i, j, len);
                            }
                        }
                    }
                }

                if (best.Item3 < 3 || best.Equals(matches.LastOrDefault())) break;
                matches.Add(best);

                // Now remove the good match ranges.
                ranges = RemoveMatchFromRanges(ranges, best);
            }

            matches.Add(new Tuple<int, int, int>(lens.Length, lens[0].Length, 0));

            matches = matches.OrderBy(p => p.Item1).ToList();
            return matches;
        }

        private static List<Tuple<int, int, int, int>> RemoveMatchFromRanges(List<Tuple<int, int, int, int>> ranges, Tuple<int, int, int> match)
        {
            List<Tuple<int, int, int, int>> newRanges = new List<Tuple<int, int, int, int>>();
            foreach (var range in ranges)
            {
                if (range.Item1 > match.Item1 || range.Item2 < (match.Item1 + match.Item3))
                {
                    newRanges.Add(range);
                }
                else
                {
                    if (range.Item1 < match.Item1)
                    {
                        newRanges.Add(new Tuple<int, int, int, int>(range.Item1, match.Item1, range.Item3, match.Item2));
                    }
                    if (range.Item2 > (match.Item1 + match.Item3))
                    {
                        newRanges.Add(new Tuple<int, int, int, int>(match.Item1 + match.Item3, range.Item2, match.Item2 + match.Item3, range.Item4));
                    }
                }
            }

            return newRanges;
        }


        public override string ToString()
        {
            List<string> ret = new List<string>();
            foreach (var match in matches)
            {
                ret.Add(match.ToString());
            }
            if (ret.Count == 1) return "";
            return String.Join("\r\n", ret);
        }

        private class MatchBlock
        {
            private enum MatchTypeEnum
            {
                Exact,
                LeftOnly,
                RightOnly,
            }

            public MatchBlock(string[] leftLines, string[] rightLines)
            {
                this.LeftLines = leftLines;
                this.RightLines = rightLines;
                if (leftLines.Length == 0)
                {
                    this.MatchType = MatchTypeEnum.RightOnly;
                }
                else if (rightLines.Length == 0)
                {
                    this.MatchType = MatchTypeEnum.LeftOnly;
                }
                else if (leftLines.Length != rightLines.Length)
                {
                    throw new InvalidOperationException();
                }
                else if (leftLines.Select((p, i) => p == rightLines[i]).All(p => p))
                {
                    this.MatchType = MatchTypeEnum.Exact;
                }
                else
                {
                    throw new Exception();
                }
            }

            public string[] LeftLines { get; private set; }

            public string[] RightLines { get; private set; }

            private MatchTypeEnum MatchType;

            public override string ToString()
            {
                List<string> ret = new List<string>();
                // ret.Add(LeftOffset + ".." + LeftLines.Length + ":" + RightOffset + ".." + RightLines.Length);
                string prefix = "";
                switch (this.MatchType)
                {
                    case MatchTypeEnum.Exact:
                        prefix = "=";
                        // ret = ret.Union(this.LeftLines.Select((p, i) => /*i +*/ prefix + " " + p)).ToList();
                        ret.Add("...");
                        break;
                    case MatchTypeEnum.LeftOnly:
                        prefix = ">";
                        ret = ret.Union(this.LeftLines.Select((p, i) => /*i +*/ prefix + " '" + p + "'")).ToList();
                        break;
                    case MatchTypeEnum.RightOnly:
                    default:
                        prefix = "<";
                        ret = ret.Union(this.RightLines.Select((p, i) => /*i +*/ prefix + " '" + p + "'")).ToList();
                        break;

                }

                return string.Join("\r\n", ret);
            }
        }
    }
}