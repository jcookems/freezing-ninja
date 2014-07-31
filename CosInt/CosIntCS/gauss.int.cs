// gauss.int.H   2000/8/18    Jason Cooke
//
// This file provides an interface between the user and the lower-level
// Gausian integration routines.  It defines
//
// GINTEQUAL = equal spaced nodes
// GINTGLEG  = Use Gauss-Legendre integration
// ??? More to come...
//
// The routine to use is
//
// makearbweights(int pickmethod, int num,
//	          double **x,  double **w,
//                double xmin, double xmax)
// - This makes the nodes (x) and weights (w) that conver the range from
//   xmin to xmax using the appropriate method.
//
// makeseminfweights(int pickmethod, int num, double **x, double **w,
//                   double delta){
// - This makes the nodes (x) and weights (w) that conver the range from
//   0 to infinity using the appropriate method.  We need to specify some
//   scale, that is given by delta.  There are several methods for converting
//   Gaussian integrations on a finite range to a finite range.  One is
//   the inversion method I started out using, and there is also the
//   tangent method used my Machleidt.
// - Note that the meaning of delta varies quite a lot for different
//   methods.
//
////////////////////////////////////////////

using System;

namespace CosIntV2
{
    public enum GaussIntType
    {
        GINTEQUAL = 0,
        GINTGLEG = 1,
        GINTGLEGTAN = 2,
    }

    public static class GaussInt
    {
        private const double EPS = 3.0e-11;  /* EPS is the relative precision */

        /// <summary>
        /// Directly from Numerical Recipies
        ///
        /// GIven the upper and lower limits of integration x1 and x2,
        /// and a given n, this routine returns arrays x[1..n] and
        /// w[1..n] of length n, containing the abscissas and weights
        /// of the Gauss-Legendre n-point quadrature formula */
        ///
        /// The way this is implemented is by first calling this routine
        /// to generate the x and w vectors.  Then,
        ///
        /// \int_x1^x2 f(x) dx \approx \sum_{i=1}^n f(x[i]) w[i]
        ///
        /// This approximation is exact if f is polynomial, and get's
        /// worse the less polynomial-ish f is.  (For example, things with
        /// steep slopes).  In those cases, one of the other Gauss-XXX
        /// routines should be used.  In particular, one with a weight
        /// function w(x) such that f(x)/w(x) is polynomial.
        ///
        /// This routine can be used to do semi-infinte integrals, by
        /// breaking the integral up into \int_0^1, \int_1^\inf, then
        /// doing a change of variables on the last one x->1/x to get
        ///
        /// \int_0^\inf f(x) dx = \int_0^1 (f(x) + f(1/x)/x^2) dx
        ///
        /// This is now ammeniable to Gauss-Legendre quadrature. */
        /// </summary>
        public static void gauleg(double x1, double x2, double[] x, double[] w, int n)
        {
            int m, j, i;
            double z1, z, xm, xl, pp, p3, p2, p1; /* High precision is a good idea here */

            m = (n + 1) / 2;
            xm = 0.5 * (x2 + x1); /* Roots are symmertic, only calculate half of them */
            xl = 0.5 * (x2 - x1);
            for (i = 1; i <= m; i++)
            { /* Loop over desired roots */
                z = Math.Cos(Math.PI * (i - 0.25) / (n + 0.5));
                do
                {
                    /* Starting with the above approx to the i^th root, we enter
                     * the main loop of refinement by Newton's method. */
                    p1 = 1.0;
                    p2 = 0.0;
                    for (j = 1; j <= n; j++)
                    {
                        /* Loop up the recurrence relations to
                         * get the Legendre polynomial evaluated
                         * a z */
                        p3 = p2;
                        p2 = p1;
                        p1 = ((2.0 * j - 1.0) * z * p2 - (j - 1.0) * p3) / j;
                    }
                    /* p1 is now the desired Legendre polynomial.  We next
                     * compute pp, its derivative, by a standard relation
                     * involving also p2, the polynomial of one lower order. */
                    pp = n * (z * p1 - p2) / (z * z - 1.0);
                    z1 = z;
                    z = z1 - p1 / pp; /* Newton's method */
                } while (Math.Abs(z - z1) > EPS);
                x[i] = xm - xl * z;     /* Scale the root to the desired interval, */
                x[n + 1 - i] = xm + xl * z; /* and put in it's symmertic counterpart   */
                w[i] = 2.0 * xl / ((1.0 - z * z) * pp * pp); /* Compute the weight and    */
                w[n + 1 - i] = w[i];                 /* the symmetric counterpart */
            }
        }

        public static void makearbweights(GaussIntType pickmethod, int num, out double[] x, out double[] w, double xmin, double xmax)
        {
            double[] xtemp, wtemp;
            int i;

            x = new double[num];
            w = new double[num];

            if (pickmethod == GaussIntType.GINTEQUAL)
                for (i = 0; i < num; i++)
                {
                    x[i] = xmin + (xmax - xmin) * (i + 0.5) / num;
                    w[i] = (xmax - xmin) / num;
                }
            else if (pickmethod == GaussIntType.GINTGLEG || pickmethod == GaussIntType.GINTGLEGTAN)
            {
                xtemp = new double[num + 1];
                wtemp = new double[num + 1];
                gauleg(xmin, xmax, xtemp, wtemp, num);
                double test = 0.0;
                for (i = 0; i < num; i++)
                {
                    x[i] = xtemp[i + 1];
                    w[i] = wtemp[i + 1];
                    test += w[i];
                }
                //    cout + "with xmin="+xmin+" and xmax="+xmax+" int="+test+"\n";
            }
        }

        private static void makeseminfweights(GaussIntType pickmethod, int num, out double[] x, out double[] w,
                       double delta)
        {
            double[] xtemp, wtemp;

            x = new double[num];
            w = new double[num];

            if (pickmethod == GaussIntType.GINTEQUAL)
            {
                for (int i = 0; i < num; i++)
                {
                    x[i] = delta * (i + 1.0);
                    w[i] = delta;
                }
            }
            else if (pickmethod == GaussIntType.GINTGLEG)
            {
                xtemp = new double[num + 1];
                wtemp = new double[num + 1];
                gauleg(0.0, 1.0, xtemp, wtemp, num / 2); // Gauss-Legendre weights & nodes
                for (int i = 0; i < num; i++)
                {
                    if (i < num / 2)
                    {
                        x[i] = delta * xtemp[i + 1];
                        w[i] = delta * wtemp[i + 1];
                    }
                    else
                    {
                        x[i] = delta / xtemp[num - i];
                        w[i] = delta * wtemp[num - i] / (xtemp[num - i] * xtemp[num - i]);
                    }
                }
            }
            else if (pickmethod == GaussIntType.GINTGLEGTAN)
            {
                makearbweights(pickmethod, num, out xtemp, out wtemp, -1.0, +1.0);
                for (int i = 0; i < num; i++)
                {
                    x[i] = delta * Math.Tan(0.25 * Math.PI * (xtemp[i] + 1.0));
                    double tmp = 1.0 / Math.Cos(0.25 * Math.PI * (xtemp[i] + 1.0));
                    double s = delta * 0.25 * Math.PI * wtemp[i];
                    w[i] = s * tmp * tmp;
                }
            }
        }
    }
}