// 2001/6/24 cos.int.v2.C
//   7/18 Fixed up some of the routines to take more general n's. Added some
//        tests to make sure n>0, and if not (which is a more trivial case)
//        fall down to an integration routine with less n's.
//
// 2001/3/21 Power -> pow, removed dependence on jutils.H
//   Trying out try, throw, catch.  Have to compile with -fhandle-exceptions
// cos.int.H    2000/2/29, Leap day on a century year!
// cos.int.h    2000/1/30, Super Bowl Sunday!   Jason Cooke
//
// The mathematics behind this are located cos.int* located throught the
// hard drive.
//
// 2012/01/05 Converted to C#
//
// This is based on lan.v2 ,and make.h1.c.
////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;

namespace CosIntV2
{
    public static class CosInt
    {
        private static int maxM = 10;
        private static double[][] cj = null;
        private static double[][] sj = null;
        private static int maxNUM1 = 200;
        private static double[][] listxphi = null;
        private static double[][] listwphi = null;

        static CosInt()
        {
            cj = new double[maxM][];
            sj = new double[maxM][];
            for (int mT = 0; mT < maxM; mT++)
            {
                cj[mT] = new double[2 * mT];
                sj[mT] = new double[2 * mT];

                for (int j = 0; j < 2 * mT; j++)
                {
                    cj[mT][j] = Math.Cos((Math.PI * j) / mT);
                    sj[mT][j] = Math.Sin((Math.PI * j) / mT);
                }
            }

            listxphi = new double[maxNUM1][];
            listwphi = new double[maxNUM1][];
            for (int nphi1 = 0; nphi1 < maxNUM1; nphi1++)
            {
                GaussInt.makearbweights(GaussIntType.GINTGLEG, nphi1, out listxphi[nphi1], out listwphi[nphi1], 0.0, Math.PI);
                for (int i = 0; i < nphi1; i++)
                {
                    listwphi[nphi1][i] /= Math.PI;
                }
            }
        }

        // --These routines are the real workhorses ---------------------------------

        /// <summary>
        /// Calculates
        ///     c    =       Sign(a)*sqrt(a^2-b^2)
        ///     cint = ( (-a+sign(a)*sqrt(a^2-b^2))/b )^m/c
        /// This is non-trivial because a and b might be very close, so we
        /// might need to use various approximations to avoid numerical noise.
        /// </summary>
        private static void cosintminiwork(double a, double b, int absm, out double c, out double cint)
        {
            double aSqMbSq = a * a - b * b;

            if (absm < 0) { throw new ArgumentOutOfRangeException("absm", "should be non-negative"); }
            if (absm < 0) { throw new ArgumentOutOfRangeException("b", "|b| should be smaller than |a|"); }

            c = (a > 0.0 ? 1 : -1) * Math.Sqrt(aSqMbSq);

            double f2m = 1.0;
            if (absm != 0)
            {
                double angStuff;
                if (b * b > a * a * 1.0e-3)
                {
                    // if b/a is not small then use the full expansion
                    angStuff = (-a + c) / b;
                }
                else
                {
                    // expand the square root!
                    //    c = a *(1 -2*(b/(2*a))^2 -2*(b/(2*a))^4 -4*(b/(2*a))^6 + HOT )
                    // so
                    //    angStuff = -b/(2*a)+ HOT
                    // The error on this is quite small
                    double bo2a = b / (2.0 * a);
                    double bo2a2 = bo2a * bo2a;
                    angStuff = -bo2a * (1 + bo2a2 * (1 + bo2a2 * (2 + bo2a2 * (5 + bo2a2 * (14)))));
                }

                for (int i = 0; i < absm; i++)
                {
                    // This is my cheap version of exponential
                    // Avoid Math.Pow because expect absm to be small;
                    // a few multiplications should be cheaper than Pow.
                    f2m *= angStuff;
                }
            }
            cint = f2m / c;
        }

        // -- Now the next level up. ------------------------------------------------
        // -- Not much is needed for cosint -----------------------------------------
        // -- More work is needed for cos12int and cos14int -------------------------

        /// <summary>
        /// \int d\phi/2\pi exp(i m \phi)/(a+b\cos\phi) =
        /// Sign(a)/sqrt(a^2-b^2) ( (-a+sign(a)sqrt(a^2-b^2))/b )^|m|
        ///
        /// This is slightly different from the next two, since we sometimes
        /// want to look at some different integrals.
        /// </summary>
        private static void cosintwork(double a, double b, int absm, out double int1, out double c)
        {
            cosintminiwork(a, b, absm, out c, out int1);
        }

        /// <summary>
        /// \int d\phi/2\pi exp(i m \phi)/(a+b\cos\phi)^n =
        /// f_n \int d\phi/2\pi exp(i m \phi)/(a+b\cos\phi)^n =
        /// </summary>
        private static double cosintwork(double a, double b, int n, int absm)
        {
            if (n == 0)
            {
                // this is trivial
                return (absm != 0 ? 0.0 : 1.0);
            }
            else
            {
                // do some work
                double c, cint;
                cosintminiwork(a, b, absm, out c, out cint);
                return DrrivFacs.facforderivs(n, a, c, absm) * cint;
            }
        }

        /// <summary>
        /// the integral
        ///     \int d\phi/2\pi exp(i m \phi)/(a1+b\cos\phi)^n1(a2+b\cos\phi)^n2
        /// </summary>
        private static double cosintwork(double a1, double a2, double b,
                  int maxLoop, double sizeDelta, double prec,
                  int n1, int n2, int absm)
        {
            if (n1 == 0) return cosintwork(a2, b, n2, absm);
            if (n2 == 0) return cosintwork(a1, b, n1, absm);

            double ret = 0.0;
            double delta = a2 - a1;
            if (Math.Abs(delta) < sizeDelta * Math.Abs(a1 + a2) * 0.5)
            {
                // delta < sizeDelta ave(a1,a2)
                if (n1 > n2)
                {
                    ret = cosintwork(a2, a1, b, maxLoop, sizeDelta, prec, n2, n1, absm);
                }
                else
                {
                    double c2, cint2; cosintminiwork(a2, b, absm, out c2, out cint2);
                    double deltaTimesBinom = 1.0;
                    double pertg2 = DrrivFacs.facforderivs(n1 + n2, a2, c2, absm);
                    for (int j = 1; j <= maxLoop; j++)
                    {
                        deltaTimesBinom *= delta * (n1 - 1.0 + j) / j;
                        double tmp = deltaTimesBinom * DrrivFacs.facforderivs(n1 + n2 + j, a2, c2, absm);
                        pertg2 += tmp;
                        if (Math.Abs(tmp) < 1.0e-20 && Math.Abs(pertg2) < 1.0e-20) break;
                        if (Math.Abs(tmp / pertg2) < prec) break;
                    }
                    ret = pertg2 * cint2;
                }
            }
            else
            {
                // different enough to use the usual expression
                double c1, cint1; cosintminiwork(a1, b, absm, out c1, out cint1);
                double c2, cint2; cosintminiwork(a2, b, absm, out c2, out cint2);
                double fn, f1, binom;

                binom = 1.0;
                f1 = fn = 1.0 / (a1 - a2); for (int j = 1; j < n2; j++) fn *= f1;
                double g1 = 0.0;
                for (int j = 0; j < n1; j++)
                {
                    g1 += binom * DrrivFacs.facforderivs(n1 - j, a1, c1, absm) * fn;
                    fn *= f1;
                    binom *= (j + n2) / (j + 1.0);
                }
                g1 *= ((n2 % 2) == 1 ? -1 : 1);

                binom = 1.0;
                f1 = fn = 1.0 / (a2 - a1); for (int j = 1; j < n1; j++) fn *= f1;
                double g2 = 0.0;
                for (int j = 0; j < n2; j++)
                {
                    g2 += binom * DrrivFacs.facforderivs(n2 - j, a2, c2, absm) * fn;
                    fn *= f1;
                    binom *= (j + n1) / (j + 1.0);
                }
                g2 *= (n1 % 2 == 1 ? -1 : 1);
                ret = g1 * cint1 + g2 * cint2;
            }

            return ret;
        }

        /// <summary>
        /// the integral
        ///   \int d\phi/2\pi exp(i m \phi)
        ///   1/(a1+b\cos\phi)(a2+b\cos\phi)(a3+b\cos\phi)^n3
        /// </summary>
        private static double cos1nnintwork(double a1, double a2, double a3, double b,
                     int maxLoop,
                     double sizeDelta13,
                     double sizeDelta12,
                     double prec, int sw,
                     int n2, int n3, int absm)
        {
            if (n2 > n3) return cos1nnintwork(a1, a3, a2, b, maxLoop, sizeDelta13, sizeDelta12, prec, sw, n3, n2, absm);
            if (n2 == 0) return cosintwork(a1, a3, b, maxLoop, sizeDelta13, prec, 1, n3, absm);
            if (n2 != 1)
            {
                throw new Exception("Error in cos1nnintwork! n2=" + n2 + " but must be 1");
            }

            // Note that n3 cannot be zero now, since n2<=n3, that would imply
            // n2 also is zero, in which case we were already whisked away to cosintwork!

            double ret = 0.0;
            if (((sw == 0) && Math.Abs(a2 - a3) < sizeDelta13 * Math.Abs(a2 + a3) * 0.5) || (sw == 1))
            {
                // delta < sizeDelta ave(a1,a3)
                double deltapow, delta = deltapow = a3 - a2;
                ret += cosintwork(a1, a3, b, maxLoop, sizeDelta13, prec, 1, n3 + 1, absm);
                for (int j = 1; j <= maxLoop; j++)
                {
                    double tmp = deltapow * cosintwork(a1, a3, b, maxLoop, sizeDelta13, prec, 1, n3 + 1 + j, absm);
                    ret += tmp;
                    if (Math.Abs(tmp) < 1.0e-20 && Math.Abs(ret) < 1.0e-20) break;
                    if (Math.Abs(tmp / ret) < prec) break;
                    deltapow *= delta;
                }
            }
            else if (((sw == 0) && Math.Abs(a1 - a3) < sizeDelta13 * Math.Abs(a1 + a3) * 0.5) || (sw == 2))
            {
                // same but 1<->2
                double deltapow, delta = deltapow = a3 - a1;
                ret += cosintwork(a2, a3, b, maxLoop, sizeDelta13, prec, 1, n3 + 1, absm);
                for (int j = 1; j <= maxLoop; j++)
                {
                    double tmp = deltapow * cosintwork(a2, a3, b, maxLoop, sizeDelta13, prec, 1, n3 + 1 + j, absm);
                    ret += tmp;
                    if (Math.Abs(tmp) < 1.0e-20 && Math.Abs(ret) < 1.0e-20) break;
                    if (Math.Abs(tmp / ret) < prec) break;
                    deltapow *= delta;
                }
            }
            else if (((sw == 0) && Math.Abs(a1 - a2) < sizeDelta12 * Math.Abs(a1 + a2) * 0.5) || (sw == 3))
            {
                // if 1&2 close
                double deltapow, delta = deltapow = a2 - a1;
                ret += cosintwork(a1, a3, b, maxLoop, sizeDelta13, prec, 2, n3, absm);
                for (int j = 1; j <= maxLoop; j++)
                {
                    double tmp = deltapow * cosintwork(a1, a3, b, maxLoop, sizeDelta13, prec, 2 + j, n3, absm);
                    ret += tmp;
                    if (Math.Abs(tmp) < 1.0e-20 && Math.Abs(ret) < 1.0e-20) break;
                    if (Math.Abs(tmp / ret) < prec) break;
                    deltapow *= delta;
                }
            }
            else
            {
                // different enough to use the usual expression
                double c1, cint1;
                cosintminiwork(a1, b, absm, out c1, out cint1);
                double c2, cint2;
                cosintminiwork(a2, b, absm, out c2, out cint2);
                double c3, cint3;
                cosintminiwork(a3, b, absm, out c3, out cint3);

                double g1 = 1.0 / (a2 - a1); // simple since n1=n2=1
                double g2 = 1.0 / (a1 - a2);
                double g3 = 0.0;
                double oo1m3X2m3ip1 = 1.0 / ((a1 - a3) * (a2 - a3));
                for (int i = 0; i < n3; i++)
                {
                    g1 /= (a3 - a1); // simple since n1=n2=1
                    g2 /= (a3 - a2);

                    double innersum = 0.0;
                    double oo1m3jp1X2m3imjp1 = oo1m3X2m3ip1;
                    for (int j = 0; j <= i; j++)
                    {
                        innersum += oo1m3jp1X2m3imjp1;
                        oo1m3jp1X2m3imjp1 *= (a2 - a3) / (a1 - a3);
                    }
                    oo1m3X2m3ip1 /= (a2 - a3);
                    g3 += (i % 2 == 1 ? -1 : 1) * DrrivFacs.facforderivs(n3 - i, a3, c3, absm) * innersum;
                }

                ret = g1 * cint1 + g2 * cint2 + g3 * cint3;
            }

            return ret;
        }

        /// <summary>
        /// This is
        ///    int_0^2pi dphi1 e^{i m1 phi1}
        ///    int_0^2pi dphi2 e^{i m2 phi2}
        ///    1/( (a11+b1*cos(phi1))^n11 *(a21+b1*cos(phi1))^n12 )
        ///    1/( (aM+bM1*cos(phi1)+bM2*cos(phi2)) )
        ///    1/( (a12+b2*cos(phi2))^n21 *(a22+b2*cos(phi2))^n22 )
        /// TODO: Check if near a pole.
        /// </summary>
        private static double cosnn1nnintwork(
            double a11, double a12, double b1, int n11, int n12, int absm1,
            double aM, double bM1, double bM2,
            double a21, double a22, double b2, int n21, int n22, int absm2,
            int num1)
        {
            if (absm1 >= maxM) { throw new ArgumentOutOfRangeException("absm1", "too large"); }

            int nphi1 = (int)(Math.Ceiling(num1 / (1.0 + absm1)));
            double[] xphi = listxphi[nphi1];
            double[] wphi = listwphi[nphi1];
            double ret = 0.0;
            for (int iphi1 = 0; iphi1 < nphi1; iphi1++)
            {
                double phi1 = xphi[iphi1];
                double tmp = 0.0;
                if (absm1 == 0)
                {
                    double c = Math.Cos(phi1);
                    double b2obM2 = (Math.Abs(b2 - bM2) > 1.0e-10 * Math.Abs(aM) ? b2 / bM2 : 1.0);
                    double tmpIG = b2obM2 * cos1nnint(b2obM2 * (aM + bM1 * c), a21, a22, b2, n21, n22, absm2);

                    if (n11 != 0)
                    {
                        double pow = 1.0 / (a11 + b1 * c);
                        for (int k = 0; k < n11; k++)
                        {
                            tmpIG *= pow;
                        }
                    }

                    if (n12 != 0)
                    {
                        double pow = 1.0 / (a12 + b1 * c);
                        for (int k = 0; k < n12; k++)
                        {
                            tmpIG *= pow;
                        }
                    }

                    tmp += tmpIG;
                }
                else
                {
                    double oo2m = 1.0 / (2 * absm1);
                    double fac = Math.Cos(phi1 * 0.5) * oo2m;
                    double c2om = Math.Cos(phi1 * oo2m);
                    double s2om = Math.Sin(phi1 * oo2m);
                    double b2obM2 = (Math.Abs(b2 - bM2) > 1.0e-10 * Math.Abs(aM) ? b2 / bM2 : 1.0);
                    for (int j = 0; j < 2 * absm1; j++)
                    {
                        double c = c2om * cj[absm1][j] - s2om * sj[absm1][j];
                        // cos((Phi+2*M_PI*j)/(2*m));
                        double tmpIG = b2obM2 * cos1nnint(b2obM2 * (aM + bM1 * c), a21, a22, b2, n21, n22, absm2);
                        if (n11 != 0)
                        {
                            double pow = 1.0 / (a11 + b1 * c);
                            for (int k = 0; k < n11; k++)
                            {
                                tmpIG *= pow;
                            }
                        }

                        if (n12 != 0)
                        {
                            double pow = 1.0 / (a12 + b1 * c);
                            for (int k = 0; k < n12; k++)
                            {
                                tmpIG *= pow;
                            }
                        }

                        tmp += fac * tmpIG;
                        fac *= -1.0;
                    }
                }
                ret += wphi[iphi1] * tmp;
            }
            return ret;
        }

        ////////////////////////////////////////////////////////////////////
        // -- The 2.5 level of abstraction ------------------------------ //
        ////////////////////////////////////////////////////////////////////

        private static double cosintwork(double a1, double a2, double b, int n1, int n2, int absm)
        {
            double aa1 = Math.Abs(a1);
            double aa2 = Math.Abs(a2);
            double delta = -0.02 * Math.Log(Math.Abs(b) / (aa1 < aa2 ? aa1 : aa2) + 1.0e-20);
            if (delta > 0.01) delta = 0.01;
            // The equation for delta is strange, but it seems to do better than
            // just picking a fixed value for delta.
            int maxLoop = 15;
            double prec = 1.0e-8;
            return cosintwork(a1, a2, b, maxLoop, delta, prec, n1, n2, absm);
        }

        private static double cos1nnintwork(double a1, double a2, double a3, double b, int n2, int n3, int absm)
        {
            double aa1 = Math.Abs(a1);
            double aa2 = Math.Abs(a2);
            double aa3 = Math.Abs(a3);
            double mina = (aa1 < aa2 ? aa1 : aa2); mina = (mina < aa3 ? mina : aa3);

            double delta13 = -0.02 * Math.Log(Math.Abs(b) / mina + 1.0e-20);
            double delta12 = 0.00001 * delta13;

            if (delta13 > 0.01) delta13 = 0.01;
            // The equation for delta is strange, but it seems to do better than
            // just picking a fixed value for delta.
            int maxLoop = 15;
            double prec = 1.0e-8;
            int sw = 0;
            return cos1nnintwork(a1, a2, a3, b, maxLoop, delta13, delta12, prec, sw, n2, n3, absm);
        }

        private static double cosnn1nnintwork(
            double a11, double a12, double b1, int n11, int n12, int absm1,
            double aM, double bM1, double bM2,
            double a21, double a22, double b2, int n21, int n22, int absm2)
        {
            int num1 = 10;
            double min1 = (n11 != 0 ? (Math.Abs(a11) < Math.Abs(a12) ? a11 : a12) : a12);
            double min2 = (n21 != 0 ? (Math.Abs(a21) < Math.Abs(a22) ? a21 : a22) : a22);
            if (Math.Abs(b1 / min1) < Math.Abs(b2 / min2))
            {
                return cosnn1nnintwork(
                    a11, a12, b1, n11, n12, absm1,
                    aM, bM1, bM2,
                    a21, a22, b2, n21, n22, absm2,
                    num1);
            }
            else
            {
                return cosnn1nnintwork(
                    a21, a22, b2, n21, n22, absm2,
                    aM, bM2, bM1,
                    a11, a12, b1, n11, n12, absm1,
                    num1);
            }
        }

        // -- The thrid level of abstraction.  We call these. -----------------------
        // -- The second cosint is different from the others. -----------------------

        public static double cosint(double a, double b, int n, int m)
        {
            return cosintwork(a, b, n, (m < 0 ? -m : m));
        }

        private static double cosint(double a, double b, int m)
        {
            return cosintwork(a, b, 0, (m < 0 ? -m : m));
        }

        /// <summary>
        /// This calculates the integrals needed for the box diagrams in
        /// Wallace and Mandelzweig's approximation of the Green's function.
        /// See my second paper for exactly what is going on.
        /// </summary>
        private static void cosint(double a, double b, int m,
                out double int1, out  double int2, out double int3)
        {
            int absm = (m < 0 ? -m : m);
            double c;
            cosintwork(a, b, absm, out int1, out c);
            // int2 = \int d\phi/2\pi exp(i m \phi)/(a+b\cos\phi)^2 = int1 * f_2
            int2 = int1 * DrrivFacs.facforderivs(2, a, c, absm);
            // int2 = \int d\phi/2\pi exp(i m \phi)/(a+b\cos\phi)^3 = int1 * f_3
            int3 = int1 * DrrivFacs.facforderivs(3, a, c, absm);
        }

        private static double cos12int(double a1, double a2, double b, int m)
        {
            return cosintwork(a1, a2, b, 1, 2, (m < 0 ? -m : m));
        }

        private static double cos14int(double a1, double a2, double b, int m)
        {
            return cosintwork(a1, a2, b, 1, 4, (m < 0 ? -m : m));
        }

        private static double cosint(double a1, double a2, double b, int n1, int n2, int m)
        {
            return cosintwork(a1, a2, b, n1, n2, (m < 0 ? -m : m));
        }

        private static double cos1nnint(double a1, double a2, double a3, double b, int n2, int n3, int m)
        {
            return cos1nnintwork(a1, a2, a3, b, n2, n3, (m < 0 ? -m : m));
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
        public static double cosnn1nnint(double a11, double a12, double b1, int n11, int n12, int m1,
                   double aM, double bM1, double bM2,
                   double a21, double a22, double b2, int n21, int n22, int m2)
        {
            double ret = cosnn1nnintwork(
                a11, a12, b1, n11, n12, (m1 < 0 ? -m1 : m1),
                aM, bM1, bM2,
                a21, a22, b2, n21, n22, (m2 < 0 ? -m2 : m2));
            return ret;
        }
    }

    /// <summary>
    /// This is f_n from the LaTeX
    ///    f_1 = 1
    ///    f_2 = ( a + |m| c )/c^2
    ///    f_n = ( f_2 * f_{n-1} - \frac{d}{da} f_{n-1} )/(n-1)
    /// Let x = a/c + |m|, and F_n=c^{n-1} f_n
    ///    F_1 = 1
    ///    F_n = 1/(n-1) (x-d/dx) F_n-1
    /// Express F as a power series
    ///    F_n = Sum(i=0,n-1) A[n][i] x^i
    ///    A[1][0] = 1
    ///    A[n][i] = 1/(n-1) * ( A[n-1][i-1] - (i+1) d[n-1][i+1] )
    /// So A is strictly numeric, with no depencency on input
    /// parameters (except n) so can be precomputed.
    /// Then,
    ///    f_n = Sum(i=0,n-1) A[n][i] x^i / c^(n-1)
    /// </summary>
    public static class DrrivFacs
    {
        private const int MAXNFNEW = 31;
        private static double[] xpowers = new double[MAXNFNEW];
        private static double[][] A;

        /// <summary>
        /// Use the recurrance relation for A to precompute the elements.
        /// </summary>
        static DrrivFacs()
        {
            A = new double[MAXNFNEW][];
            A[1] = new double[] { 1 };
            for (int n = 2; n < MAXNFNEW; n++)
            {
                A[n] = new double[n];
                for (int i = 0; i < n; i++)
                {
                    A[n][i] = 1.0 / (n - 1.0) * (
                        (i - 1 < 0 ? 0 : A[n - 1][i - 1]) -
                        (i + 1 >= n - 1 ? 0 : (i + 1) * A[n - 1][i + 1]));
                }
            }
        }

        public static double facforderivs(int n, double aIn, double cIn, int absm)
        {
            double ooc = 1.0 / cIn;
            double oocNm1 = 1;
            for (int i = 0; i < n - 1; i++)
            {
                oocNm1 *= ooc;
            }

            double x = aIn / cIn + absm;
            double fprime = 0;
            for (int i = n - 1; i >= 0; i--)
            {
                fprime = A[n][i] + x * fprime;
            }

            double f = fprime * oocNm1;
            return f;
        }
    }
}