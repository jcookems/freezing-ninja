// test.cos.C   2000/6/30  Jason R. Cooke
//
// To compile, use
// //// gcc -I. -L. -I/home/jrcooke/c/numerical.recipies test.cos.C -lm -lutils -lutils -lstdc++
//
// gcc -L. test.cos.cpp -lutils -lstdc++
//
/////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;

namespace CosIntV2
{
    internal class Program
    {
        private static void testit(
            double a11, double a12, double b1,
            double aM, double bM1, double bM2,
            double a21, double a22, double b2)
        {
            for (int n12 = 0; n12 < 3; n12++)
                for (int n22 = 0; n22 < 3; n22++)
                    for (int m1 = 0; m1 < 3; m1++)
                        for (int n11 = 0; n11 < 3; n11++)
                            for (int n21 = 0; n21 < 3; n21++)
                                for (int m2 = 0; m2 < 3; m2++)
                                {
                                    if (n21 == 2 && n22 == 2) continue;
                                    if (n11 == 2 && n12 == 2) continue;
                                    NewMethod(a11, a12, b1, aM, bM1, bM2, a21, a22, b2, n12, n22, m1, n11, n21, m2);
                                }
            Console.ReadLine();
        }

        private static void NewMethod(double a11, double a12, double b1, double aM, double bM1, double bM2, double a21, double a22, double b2, int n12, int n22, int m1, int n11, int n21, int m2)
        {
            double c3 = Integral(a11, a12, b1, n11, n12, m1, aM, bM1, bM2, a21, a22, b2, n21, n22, m2);
            string s = (n11 + "\t" + n12 + "\t" + m1 + "\t" + n21 + "\t" + n22 + "\t" + m2);
            double[] cs = new double[8];
            try
            {
                //These should all be the same
                cs[0] = CosInt.cosnn1nnint(a11, a12, b1, n11, n12, m1, aM, bM1, bM2, a22, a21, b2, n22, n21, m2);
                cs[1] = CosInt.cosnn1nnint(a11, a12, b1, n11, n12, m1, aM, bM1, bM2, a21, a22, b2, n21, n22, m2);
                cs[2] = CosInt.cosnn1nnint(a12, a11, b1, n12, n11, m1, aM, bM1, bM2, a21, a22, b2, n21, n22, m2);
                cs[3] = CosInt.cosnn1nnint(a12, a11, b1, n12, n11, m1, aM, bM1, bM2, a22, a21, b2, n22, n21, m2);
                cs[4] = CosInt.cosnn1nnint(a22, a21, b2, n22, n21, m2, aM, bM2, bM1, a11, a12, b1, n11, n12, m1);
                cs[5] = CosInt.cosnn1nnint(a21, a22, b2, n21, n22, m2, aM, bM2, bM1, a11, a12, b1, n11, n12, m1);
                cs[6] = CosInt.cosnn1nnint(a21, a22, b2, n21, n22, m2, aM, bM2, bM1, a12, a11, b1, n12, n11, m1);
                cs[7] = CosInt.cosnn1nnint(a22, a21, b2, n22, n21, m2, aM, bM2, bM1, a12, a11, b1, n12, n11, m1);
            }
            catch (Exception ex)
            {
                s += "\t" + c3 + ex.Message;
            }

            s += // "\t" + c3 + "\t" + cs[0] + "\t" + cs[4] + "\t" + Math.Abs((cs[0] - c3) / c3) +
                "\t" + Math.Abs((cs[4] - c3) / c3);
            cw(s);
        }

        private static void cw(string s)
        {
            Console.WriteLine(s);
            Debug.WriteLine(s);
        }

        public static double Integrand(
            double a11, double a12, double b1, int n11, int n12, int m1,
            double aM, double bM1, double bM2,
            double a21, double a22, double b2, int n21, int n22, int m2,
            double phi1, double phi2)
        {
            return 1.0 / Math.Pow(2 * Math.PI, 2) * Math.Cos(m1 * phi1) * Math.Cos(m2 * phi2) / (
                Math.Pow(a11 + b1 * Math.Cos(phi1), n11) *
                Math.Pow(a12 + b1 * Math.Cos(phi1), n12) *
                (aM + bM1 * Math.Cos(phi1) + bM2 * Math.Cos(phi2)) *
                Math.Pow(a21 + b2 * Math.Cos(phi2), n21) *
                Math.Pow(a22 + b2 * Math.Cos(phi2), n22));
        }

        /// <summary>
        /// 1/(2*Pi)^2*NIntegrate[
        ///     Cos[m1*phi1]*Cos[m2*phi2]/(
        ///         (a11 + b1 *Cos[phi1]               )^n11
        ///         (a12 + b1 *Cos[phi1]               )^n12
        ///         (aM  + bM1*Cos[phi1]+ bM2*Cos[phi2])
        ///         (a21                + b2 *Cos[phi2])^n21
        ///         (a22                + b2 *Cos[phi2])^n22)
        ///      ,{phi1,0,2*Pi},{phi2,0,2*Pi}]
        /// </summary>
        public static double Integral(
            double a11, double a12, double b1, int n11, int n12, int m1,
            double aM, double bM1, double bM2,
            double a21, double a22, double b2, int n21, int n22, int m2)
        {
            double deltaPhi = 0.01;
            double ret = 0;
            for (double phi1 = 0; phi1 < 2 * Math.PI; phi1 += deltaPhi)
                for (double phi2 = 0; phi2 < 2 * Math.PI; phi2 += deltaPhi)
                {
                    ret += deltaPhi * deltaPhi * Integrand(a11, a12, b1, n11, n12, m1, aM, bM1, bM2, a21, a22, b2, n21, n22, m2, phi1, phi2);
                }
            return ret;
        }

        public static double Integrand(double a11, double b1, int n11, int m1, double phi1)
        {
            return 1.0 / (2 * Math.PI) * Math.Cos(m1 * phi1) / (Math.Pow(a11 + b1 * Math.Cos(phi1), n11));
        }

        public static double Integral(double a11, double b1, int n11, int m1)
        {
            double deltaPhi = 0.0001;
            double ret = 0;
            for (double phi1 = 0; phi1 < 2 * Math.PI; phi1 += deltaPhi)
            {
                ret += deltaPhi * Integrand(a11, b1, n11, m1, phi1);
            }
            return ret;
        }

        private static void testit()
        {
            double theta11 = 1.23037188371195;
            double theta12 = 6.17830935222018;
            double theta21 = 1.04018623057762;
            double theta22 = 1.16982711999203;
            double theta1 = 4.9651041370654;
            double theta2 = 8.04014591874562;
            double m1 = 4.23922379232907;
            double m2 = 0.994535740928043;
            double m3 = 8.10535582625557;

            double b1 = 10.0 * Math.Cos(m1);
            double a11 = b1 / Math.Tanh(theta11);
            double a12 = b1 / Math.Tanh(theta12);

            double b2 = 10.0 * Math.Cos(m2);
            double a21 = b2 / Math.Tanh(theta21);
            double a22 = b2 / Math.Tanh(theta22);

            double aM = m3;
            double bM1 = aM * Math.Cos(theta1);
            double bM2 = (aM - Math.Abs(bM1)) * Math.Cos(theta2);

            testit(a11, a12, b1, aM, bM1, bM2, a21, a22, b2);
        }

        private static void Main(string[] args)
        {
            for (int j = 0; j < 5; j++)
            {
                DateTime t1 = DateTime.Now;
                for (int i = 0; i < 100; i++)
                {
                    testit();
                }
                DateTime t2 = DateTime.Now;
                Console.WriteLine(t2 - t1);
            }

            double a11 = 2.0; double aM = 1.9; double a21 = 5.0;
            double a12 = 2.0; double bM1 = 0.1; double a22 = 3.2;
            double b1 = 0.2; double bM2 = 0.79; double b2 = -2.6;

            for (int n11 = 0; n11 < 2; n11++)
                for (int n21 = 0; n21 < 2; n21++)
                    for (int n12 = 0; n12 < 3; n12 += 2)
                        for (int n22 = 0; n22 < 3; n22 += 2)
                            for (int m1 = 0; m1 < 3; m1++)
                                for (int m2 = 0; m2 < 3; m2++)
                                {
                                    double c1 = CosInt.cosnn1nnint(
                                        a11, a12, b1, n11, n12, m1,
                                        aM, bM1, bM2,
                                        a21, a22, b2, n21, n22, m2);
                                    double c2 = CosInt.cosnn1nnint(
                                        a21, a22, b2, n21, n22, m2,
                                        aM, bM2, bM1,
                                        a11, a12, b1, n11, n12, m1);
                                    Console.Write("i[" + n11 + "," + n12 + "," + m1 + "," + n21 + "," + n22 + "," + m2 + "] = \t" + c1 + "\t" + c2 + "\t");
                                    Console.WriteLine((c1 - c2) / (c1 + c2));
                                    Console.WriteLine("Timing[(i[" + n11 + "," + n12 + "," + m1 + "," + n21 + "," + n22 + "," + m2 + "] - (" + c1 + "))/" + "(" + c1 + ")]");
                                }
        }
    }
}