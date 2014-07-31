/* To compile, this needs the flags

   -L/usr/local/lib
   and
   -leispack -lm

   If DOLAPACK is defined, then you also need
   -L/home/jrcooke/c/CLAPACK
   and
   -llapack -lblas -lf2c

   If you are doing this on an Alpha machine, you only need to link with
   -ldxml
   no f2c is needed, or explict Lapack, since, like Prego, its in there.

   NOTE:
   It seems like LAPACK is better than EISPACK.  In fact, for large matrices,
   EISPACK doesn't always have the lowest eigenvalues in order, or even
   in the list of (higher) eigenvalues returned.  LAPACK is explicitly
   better in this case.  So always use it (it is also newer than EISPACK)

*/

using System;

public class EigSys
{
    static double[] mathevals = new double[]{
    505.0947925871215e0,
    248.93150341176647e0,
    151.06564585699488e0,
    97.96507738898202e0,
    65.83725418738476e0,
    42.140615465293536e0,
    23.97123644595429e0,
    10.998943170676084e0,
    3.242061442600786e0,
    0.7528700432254833e0
    };

    private double innerproduct(double[] x, int offsetX, double[] y, int offsetY, int n)
    {
        double ret = 0.0;
        for (int i = 0; i < n; i++) ret += x[offsetX * n + i] * y[offsetY * n + i];
        return ret;
    } // innerproduct

    private void mattimes(double[] A, double[] x, int offset, double[] y, int n)
    {
        for (int i = 0; i < n; i++) y[i] = 0.0;
        for (int i = 0; i < n * n; i++) y[i / n] += A[i] * x[offset * n + (i % n)];
    } // mattimes

    private void diagchecker(int numevals, int num, double[] ham,
             double[] lowevals, double[] lowevecs)
    {
        // Error checking for the eigenvalues and vectors

        double[] tempvec = new double[num];
        for (int j = 0; j < numevals; j++)
        {
            mattimes(ham, lowevecs, j, tempvec, num);
            double norm = Math.Sqrt(innerproduct(lowevecs, j, lowevecs, j, num));
            double temp1 = innerproduct(lowevecs, j, tempvec, 0, num);
            if (norm == 0.0)
            {
                Console.Write("Error!  The " + j + "'th vector has zero norm!\n");
                throw new Exception();
            }
            temp1 /= norm;
            if (Math.Abs(temp1 - lowevals[j]) > 1.0e-10)
            {
                Console.Write("Error!  The " + j + "'th eigenvalue appears to be " + temp1 + "\n");
                Console.Write("But, the calculated eigenvalue is " + lowevals[j] + "!\n.");
                Console.Write("You may wish to look carefully at the answers you\n");
                Console.Write("get out of this calculation, and check that the error\n");
                Console.Write("is within acceptiable bounds for you.\n");
            }

            double temp2 = 0.0;
            for (int i = 0; i < num; i++)
                temp2 += (tempvec[i] - temp1 * lowevecs[j * num + i]) *
              (tempvec[i] - temp1 * lowevecs[j * num + i]);
            if (temp2 > 1.0e-10)
            {
                Console.Write("Error! The square of (H-e)\\psi is: " + temp2 + ">1.0d-10!\n");
                Console.Write("You may wish to look carefully at the answers you\n");
                Console.Write("get out of this calculation, and check that the error\n");
                Console.Write("is within acceptiable bounds for you.\n");
            }
        }
        // delete[] tempvec;
    } // diagchecker

    private void lapackeigsys(int numevals, int num, double[] a,
              bool packed, bool upper,
              double[] lowevals, double[] lowevecs)
    {
        // The inputs
        char jobz = 'V'; // Compute eigenvalues and eigenvectors
        char range = 'I'; // the IL-th through IU-th eigenvalues will be found.
        char uplo = (upper ? 'U' : 'L');
        // If not packed, then it makes no difference if up or down.
        int n = num; // The order of the matrix A.  N >= 0.
        int lda = num; // The leading dimension of the array A.
        // This is not needed for packed, since packed is really a vector, so no LDA
        double vl = 0.0; // Not referenced if RANGE = 'A' or 'I'.
        double vu = 0.0; // Not referenced if RANGE = 'A' or 'I'.
        int il = 1;        // lowest eigenvalue thru the
        int iu = numevals; // numevals eigenvalues are found.
        /* indices (in ascending order) of the
           smallest and largest eigenvalues to be returned.
           1 <= IL <= IU <= N, if N > 0; */
        char slamchchar = 'S';
        double abstol = 2 * Double.Epsilon; // 2 * slamch_(&slamchchar);
        /* The absolute error tolerance for the eigenvalues.
           ABSTOL + EPS *   max( |a|,|b| ) ,
           Eigenvalues will be computed most accurately when ABSTOL is
           set to twice the underflow threshold 2*SLAMCH('S'), not zero.  */
        int ldz = num; // The leading dimension of the array
        int lwork = 8 * num; // The length of the arr WORK.  LWORK >= max(1,8*N).
        // This is not needed for packed, since it assumes LWORK = 8*N

        // now the outputs
        int m;
        /* Output: The total number of eigenvalues found.  0 <= M <= N.
           If RANGE = 'A', M = N, and if RANGE = 'I', M = IU-IL+1. */
        double[] w = new double[num]; // malloc(sizeof(double)*num);
        /* (output) On normal exit, the first M elements contain the selected
           eigenvalues in ascending order.  However, we must allocate extra space?*/
        //  double *z  = new double[num*numevals];
        // This is what we use lowevecs for.  Not so for W, since that has
        // num elements, not numevals.  We have to copy over those values.
        /* (output) If JOBZ = 'V', then if INFO = 0, the first M columns of Z
           contain the orthonormal eigenvectors of the matrix A
           corresponding to the selected eigenvalues, with the i-th
           column of Z holding the eigenvector associated with W(i). */
        double[] work = new double[lwork];
        // On exit, if INFO = 0, WORK(1) returns the optimal LWORK.
        int[] iwork = new int[5 * num]; // dimension (5*N)
        int[] ifail = new int[num];
        /* If JOBZ = 'V', then if INFO = 0, the first M elements of
           IFAIL are zero.  If INFO > 0, then IFAIL contains the
           indices of the eigenvectors that failed to converge. */
        int info = 0;
        /* = 0:  successful exit
           < 0:  if INFO = -i, i-th argument had illegal value
           > 0:  if INFO = i,  i eigenvectors failed to converge
           Their indices are stored in array IFAIL.   */

        // Compute! -- LAPACK driver routine (version 2.0) -- September 30, 1994
        if (packed)
        {
            DotNumerics.LinearAlgebra.CSLapack.DSBEV dsbev_ = new DotNumerics.LinearAlgebra.CSLapack.DSBEV();

            dspevx_(jobz, range, uplo, n,
                a, /*-*/ vl, vu, il, iu, abstol, m, w,
                lowevecs, ldz, work, /*---*/ iwork, ifail, info);

            //           dspevx_.Run(jobz, "",
        }
        else
            dsyevx_(jobz, range, uplo, n,
                a, lda, vl, vu, il, iu, abstol, m, w,
                lowevecs, ldz, work, lwork, iwork, ifail, info);
        // Done!

        if (info != 0)
        {
            string err = "Errors in lapackeigsys with " + (packed ? "sspevx_" : "ssyevx_") + "!!! info: " + info + "\n";
            if (info < 0)
            {
                err += "The " + info + "'th argument had an illegal value!\n";
            }
            else
            {
                err += info + " eigenvectors failed to converge!  They were:\n";
                for (int i = 0; i < num; i++)
                {
                    if (ifail[i] != 0)
                    {
                        err += "Index number:" + ifail[i] + "\n";
                    }
                }
            }
            throw new InvalidOperationException(err);
        }

        // We didn't use lowevals as the input for the routine
        // since it expects num elements in lowevals, instead of numevals.
        for (int i = 0; i < numevals; i++) lowevals[i] = w[i];
    }

    private void eigsys(int numevals, int num, double[] ham,
            double[] lowevals, double[] lowevecs)
    {
        double[] a = new double[num * num];
        for (int i = 0; i < num * num; i++)   // This must be done since a is modified by eig
            a[i] = ham[i];
        lapackeigsys(numevals, num, a, false, true, lowevals, lowevecs);
        // delete[] a;
        // Error checking for the eigenvalues and vectors
        diagchecker(numevals, num, ham, lowevals, lowevecs);
    }

    private void eigsysda(int numevals, int num, double[] ham, double[] lowevals, double[] lowevecs)
    {
        // This does not save the ham
        lapackeigsys(numevals, num, ham, false, true, lowevals, lowevecs);
    }

    private void eigsysupper(int numevals, int num, double[] ham, double[] lowevals, double[] lowevecs)
    {
        lapackeigsys(numevals, num, ham, true, true, lowevals, lowevecs);
    }

    private void eigsyslower(int numevals, int num, double[] ham, double[] lowevals, double[] lowevecs)
    {
        lapackeigsys(numevals, num, ham, true, false, lowevals, lowevecs);
    }

    private void testeigroutines()
    {
        int num = 10;
        int numevals = num;
        double[] mat = new double[num * num];
        for (int i = 0; i < num; i++)
        {
            for (int j = 0; j < num; j++)
            {
                mat[i * num + j] =
                    (1.0 + (i + j) * (i + j)) /
                    (1.0 + (i - j) * (i - j));
            }
        }

        double[] lowevals = new double[numevals];
        double[] lowevecs = new double[numevals * num];

        Console.Write("Running LAPACK:\n");
        lapackeigsys(numevals, num, mat, false, true, lowevals, lowevecs);
        diagchecker(numevals, num, mat, lowevals, lowevecs);

        for (int i = 0; i < numevals; i++)
        {
            Console.Write("ev:" + i + "  Mathem: " + mathevals[num - i - 1] + "  ");
            Console.Write("M-LAPACK: " + (mathevals[num - i - 1] - lowevals[i]) + "\n");
        }
    }
}