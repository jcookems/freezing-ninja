/* To compile, this needs the flags

   -L/home/jrcooke/c/CLAPACK
   and
   -lm -llapack -lblas -lf2c

   If you are doing this on an Alpha machine, you only need to link with
   -ldxml
   no f2c is needed, or explict Lapack, since, like Prego, its in there.

   Since LAPACK is better than EISPACK, we use it exclusively.
*/
#if !defined(__STD_EIGSYS_H_CPP)  // This prevents loading twice!
#define __STD_EIGSYS_H_CPP

#include <iostream.h>
#include <math.h>

double innerproduct(double *x, double *y, int n);
void mattimes(double *A, double *x, double *y, int n);
void diagchecker(int numevals, int num, double *ham,
		 double *lowevals, double *lowevecs);
void lapackeigsys(int numevals, int num, double *a,
		  int packed, int upper,
		  double *lowevals, double *lowevecs);
void eispackeigsys(int numevals, int num, double *a,
		   double *lowevals, double *lowevecs);
void eigsys(int numevals, int num, double *ham,
	    double *lowevals, double *lowevecs);
void eigsysda(int numevals, int num, double *ham,
	      double *lowevals, double *lowevecs);
void eigsysupper(int numevals, int num, double *ham,
		 double *lowevals, double *lowevecs);
void eigsyslower(int numevals, int num, double *ham,
		 double *lowevals, double *lowevecs);
void testeigroutines();

#endif
