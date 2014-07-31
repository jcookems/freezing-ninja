/* This is Numerical Recipies zbrent.c, modified somewhat by JRC. */
#if !defined(__STD_ZBRENT_H_CPP)  // This prevents loading twice!
#define __STD_ZBRENT_H_CPP

#include <math.h>
#include <iostream.h>

double zbrent(double (*func)(double), double x1,double x2,double tol);
double zbrentmod(double (*func)(double),
		 double x1, double f1inp,
		 double x2, double f2inp, double tol);

#endif
