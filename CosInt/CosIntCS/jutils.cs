// jutils.H
//
// This is a collection of mathematical routines that are nice to have
// around.  They are
//
// uniformdouble    - Gives a random double precision number in the range [0,1)
// Power(x,n)       - gives x^n, for both n integer and double.
//                    This is for easy compatablity with output from
//                    Mathematica.
// factrl(n)        - gives n!,  for both n integer and double.
// atan(x,y)        - gives angle in range (-pi,pi] to point (x,y).  Again,
//                    Mathematica gives this function.
// checkeq(x,y,err) - checks if x and y are equal to within frac. error err
// checkfracdiff(x,y,err)
//                  - gives fractional diff. of x and y if greater than err
//
// 2000/11/3  Added atan(x,y)
// 2000/11/8  Replaced my versions of the functions with math.h's version
//
////////////////////////////////////////////////////////////////

using System;

public static class JUtils
{
    private static int ntop;
    private static double[] a;

    static JUtils()
    {
        ntop = 1;
        a = new double[100];
        a[0] = 1;
    }

    static double[] cof = new double[]{
        76.18009172947146,
        -86.50532032941677,
        24.01409824083091,
        -1.231739572450155,
        0.1208650973866179e-2,
        -0.5395239384953e-5
    };

    /// <summary>
    /// Returns the value of ln[Gamma[xx]] for xx>0
    /// From Numerical Recipies
    /// </summary>
    private static double gammln(int xx)
    {
        double x = xx;
        double y = xx;
        double tmp = x + 5.5;
        tmp -= (x + 0.5) * Math.Log(tmp);
        double ser = 1.000000000190015;
        for (int j = 0; j <= 5; j++)
        {
            ser += cof[j] / ++y;
        }

        return -tmp + Math.Log(2.5066282746310005 * ser / x);
    }

    public static double factrl(int n)
    {
        if (n < 0 || n > a.Length) throw new System.ArgumentOutOfRangeException("n");
        if (a[n] != 0)
        {
            return a[n];
        }

        if (n > 32)
        {
            a[n] = Math.Exp(gammln(n));
        }
        else
        {
            while (ntop < n)
            {
                int j = ntop++;
                a[ntop] = a[j] * ntop;
            }
        }

        return a[n];
    }

    public static bool checkeq(double x, double y, double err)
    {
        if (x == y) return true;
        else if (y == 0.0) return false;
        else return (Math.Abs(x / y - 1.0) < err);
    }

    public static double checkfracdiff(double x, double y, double err)
    {
        if (x == y) return 0.0;
        else if (y == 0.0) return 1.0e30;
        else if (x == 0.0) return 0.0;
        else return (Math.Abs(x / y - 1.0) < err ? 0.0 : x / y - 1.0);
    }
}